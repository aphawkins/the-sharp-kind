"""VICE binary monitor client for driving rebb64 under emulation.

Written because the same protocol traps were rediscovered across sessions. Read
SKILL.md beside this file before changing anything here - every guard in this
module exists because its absence once produced a confident-looking table of
numbers from a machine that was not running.

Usage as a library:

    from monitor import Monitor
    mon = Monitor()
    mon.require_game()          # refuses to go on unless rebb64 is really there
    print(mon.mem(0x0008, 0x0008).hex())
    mon.resume()                # the machine is halted until you say this

Usage from the command line:

    python monitor.py selftest      # is the game loaded and actually running?
    python monitor.py start         # drive the front end into a live level
    python monitor.py passes 4      # game loop passes over a 4 second window
    python monitor.py watch 4 A9FA AA0C AA1E   # sample bytes across a window
"""

import os
import socket
import struct
import subprocess
import sys
import time

STX = 0x02
API = 0x02

CMD_MEM_GET = 0x01
CMD_MEM_SET = 0x02
CMD_CHECKPOINT_GET = 0x11
CMD_CHECKPOINT_SET = 0x12
CMD_CHECKPOINT_DELETE = 0x13
CMD_EXIT = 0xAA

RESP_MEM_GET = 0x01
RESP_MEM_SET = 0x02
RESP_CHECKPOINT = 0x11
RESP_CHECKPOINT_DELETE = 0x13
RESP_STOPPED = 0x62
RESP_RESUMED = 0x63

UNSOLICITED = 0xFFFFFFFF

# CHECKPOINT_SET's operation byte: vice_13.html, "Command 0x12: Checkpoint Set".
# Confirmed against a captured wire example (checkpoint set for an exec trap):
# ... 12 | e2 fc | e3 fc | 01 | 01 | 04 | 01 - the 04 there is exec.
OP_LOAD = 0x01
OP_STORE = 0x02
OP_EXEC = 0x04

# jsr $0CF2 at $0A4E - the game loop's call to enemy_ai_update. If these three
# bytes are not here, rebb64 is not loaded and nothing else read is worth having.
GAME_FINGERPRINT_ADDR = 0x0A4E
GAME_FINGERPRINT = bytes([0x20, 0xF2, 0x0C])

# master.s: ENDCHR = $08, the frame counter the IRQ increments. 50 a second while
# the machine runs, whether or not a game is in progress. This is THE liveness
# guard - see SKILL.md on why a KERNAL address is not.
FRAME_COUNTER = 0x0008

# $13BE decrements $A9FA for all eighteen slots every game loop pass, with no
# gating at all, so any slot is a free pass counter mod 256.
PASS_COUNTER = 0xA9FA

# Both counters are single bytes. $08 runs at ~50/s, so it wraps in 256/50 = 5.1
# seconds and a longer window silently loses a whole turn of it: a 6 second
# measurement once returned 47 frames instead of 303 and a ratio of 0.47 instead
# of 3.00, which is wrong in a way that still looks like a number. $A9FA at ~17/s
# has 15 seconds of room, so $08 is the binding constraint.
MAX_WINDOW = 4.5

# master.s, eight slots each, players in 0 and 1. State $01 is a live player -
# watched in VICE, not inferred. On level 1 player 1 starts at X $2C, Y $DD, and
# $DD is exactly what check_player_state writes when it respawns one.
PLAYER_STATE = 0x00B2
PLAYER_X = 0x00BA
PLAYER_Y = 0x00C2

PRESS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "press.ps1")


class MonitorError(RuntimeError):
    pass


class Monitor:
    def __init__(self, host="127.0.0.1", port=6502, timeout=10.0):
        self.sock = socket.create_connection((host, port), timeout=timeout)
        self.sock.settimeout(timeout)
        self._buf = b""
        self._next_id = 1
        self._stops = []

    # -- wire ------------------------------------------------------------

    def _recv(self, n):
        while len(self._buf) < n:
            chunk = self.sock.recv(65536)
            if not chunk:
                raise MonitorError("monitor closed the connection")
            self._buf += chunk
        out, self._buf = self._buf[:n], self._buf[n:]
        return out

    def _read_response(self):
        while True:
            head = self._recv(2)
            if head[0] == STX:
                break
        length, rtype, err, rid = struct.unpack("<IBBI", self._recv(10))
        body = self._recv(length) if length else b""
        return rtype, err, rid, body

    def _send(self, cmd, body=b""):
        rid = self._next_id
        self._next_id += 1
        self.sock.sendall(struct.pack("<BBIIB", STX, API, len(body), rid, cmd) + body)
        return rid

    def request(self, cmd, body=b"", want=None, tries=64):
        """Send a command and return the reply carrying OUR request id.

        Every reply must be consumed, including EXIT's. Leaving one in the socket
        desynchronises the stream: the next read resynchronises on a stray STX
        inside the unread packet and hands back bytes that still parse.
        """
        rid = self._send(cmd, body)
        for _ in range(tries):
            rtype, err, got, payload = self._read_response()
            if got == UNSOLICITED and rtype == RESP_STOPPED:
                # A checkpoint close to where the machine was resumed can hit before
                # EXIT's own reply arrives. Keep it for wait_for_stop, or it is lost.
                # A command's own halt sends one of these too, so keeping it proves
                # nothing on its own - wait_for_stop(pc=...) tells the two apart.
                self._stops.append(payload)
            if got != rid:
                continue  # unsolicited, or an earlier command's answer
            if want is not None and rtype != want:
                continue
            if err:
                raise MonitorError(f"command {cmd:#04x} failed, error {err:#04x}")
            return payload
        raise MonitorError(f"no reply matched request {rid} for command {cmd:#04x}")

    # -- operations ------------------------------------------------------

    def mem(self, start, end=None):
        """Read an inclusive byte range. This HALTS the machine - see resume()."""
        end = start if end is None else end
        payload = self.request(
            CMD_MEM_GET, struct.pack("<BHHBH", 0, start, end, 0, 0), want=RESP_MEM_GET
        )
        return payload[2:]

    def byte(self, addr):
        return self.mem(addr)[0]

    def set_mem(self, start, data):
        """Write bytes from start. HALTS the machine, same as any command - resume() after.

        This is an intervention on the game. Say so in whatever the capture supports.
        """
        body = struct.pack("<BHHBH", 0, start, start + len(data) - 1, 0, 0) + bytes(data)
        self.request(CMD_MEM_SET, body, want=RESP_MEM_SET)

    def set_checkpoint(self, start, end=None, operation=OP_EXEC, stop_when_hit=True,
                        enabled=True, temporary=False):
        """Arm a checkpoint. HALTS the machine, same as any command - resume() after.

        Returns the checkpoint number, for delete_checkpoint(). temporary=True asks
        VICE to remove it itself on the first hit; delete it explicitly too, since a
        client that vanishes with one pending leaves VICE's own monitor window open
        and the emulator frozen - see SKILL.md, "Cleaning up".
        """
        end = start if end is None else end
        body = struct.pack(
            "<HHBBBB", start, end, int(stop_when_hit), int(enabled), operation,
            int(temporary),
        )
        payload = self.request(CMD_CHECKPOINT_SET, body, want=RESP_CHECKPOINT)
        return struct.unpack("<I", payload[:4])[0]

    def delete_checkpoint(self, number):
        self.request(CMD_CHECKPOINT_DELETE, struct.pack("<I", number),
                     want=RESP_CHECKPOINT_DELETE)

    def wait_for_stop(self, timeout=20.0, pc=None):
        """Block for the unsolicited STOPPED event a hit checkpoint sends.

        Do NOT send any other command between resume() and this call - every command
        halts the machine on its own, which would stop it somewhere that is not the
        checkpoint and this would then report that halt instead. A checkpoint hit
        sends CHECKPOINT_INFO (0x11) first, then STOPPED (0x62), both with request id
        UNSOLICITED - see SKILL.md, "The traps", and vice_13.html on the binary
        monitor. Returns the STOPPED body (PC is its first two bytes, little-endian).

        Pass pc to wait for a stop at that address and nothing else. A command's own
        halt sends STOPPED too, at wherever the machine was - usually $E498 - so
        without pc the first stop of any kind is returned. With pc, a hit that came
        in while resume() was still waiting for EXIT's reply is found as well: a
        checkpoint a few hundred instructions on hits before that reply arrives.
        """
        def at(body):
            return pc is None or (body[0] | (body[1] << 8)) == pc

        for body in self._stops:
            if pc is not None and at(body):
                self._stops.clear()
                return body
        self._stops.clear()
        self.sock.settimeout(timeout)
        try:
            while True:
                rtype, err, rid, body = self._read_response()
                if rid != UNSOLICITED:
                    continue  # an ordinary reply arriving out of turn - not our event
                if rtype == RESP_STOPPED and at(body):
                    return body
        finally:
            self.sock.settimeout(10.0)

    def resume(self):
        """Let the machine run again.

        EVERY command halts the emulator, not just a memory read - setting a
        checkpoint halts it too. Without this the machine stays stopped and every
        counter reads the same value for ever.
        """
        self._stops.clear()  # a stop from before this EXIT is not a checkpoint hit
        self.request(CMD_EXIT)

    def close(self):
        try:
            self.resume()
        except (MonitorError, OSError):
            pass
        finally:
            self.sock.close()

    # -- guards ----------------------------------------------------------

    def game_loaded(self):
        return self.mem(GAME_FINGERPRINT_ADDR, GAME_FINGERPRINT_ADDR + 2) == GAME_FINGERPRINT

    def require_game(self):
        if not self.game_loaded():
            raise MonitorError(
                "rebb64 is not loaded - autostart has not finished, or the machine "
                "was halted mid-boot by an early monitor command"
            )

    def running(self, seconds=1.0):
        """True if the IRQ frame counter advanced across a real window."""
        first = self.byte(FRAME_COUNTER)
        self.resume()
        time.sleep(seconds)
        second = self.byte(FRAME_COUNTER)
        self.resume()
        return (second - first) % 256, first, second

    def player(self, slot=0):
        """State, X and Y for one player. The direct answer to 'are we in a level'.

        $A9FA turning says the game loop runs; this says a player exists in it.
        """
        block = self.mem(PLAYER_STATE, PLAYER_Y + 1)
        return block[slot], block[8 + slot], block[16 + slot]

    def in_level(self, seconds=1.0):
        """True if the game loop is turning, not just the IRQ.

        The title screen ticks the IRQ but never reaches the game loop, so the
        frame counter moves while the pass counter does not.
        """
        first = self.byte(PASS_COUNTER)
        self.resume()
        time.sleep(seconds)
        second = self.byte(PASS_COUNTER)
        self.resume()
        return (first - second) % 256


def press(*keys, hold_ms=250):
    """One press.ps1 invocation, waited on. See its header on why never two at once."""
    subprocess.run(
        ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", PRESS,
         "-Keys", ",".join(keys), "-HoldMs", str(hold_ms)],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=False,
    )


def _start(argv):
    """Drive the front end into a live level, and prove it got there.

    Two presses, not one, and that is the whole trick. Fire only buys a credit
    and moves the game on to PRESS 1 OR 2 TO PLAY, which reads the keyboard
    rather than the stick. A session was lost to holding fire at that screen.
    """
    tries = int(argv[0]) if argv else 4
    mon = Monitor()
    mon.require_game()

    for attempt in range(1, tries + 1):
        state, x, y = mon.player()
        print(f"attempt {attempt}: state=${state:02X} X=${x:02X} Y=${y:02X}")
        if state == 0x01 and x != 0x00:
            print(f"in play: player 1 at X=${x:02X} Y=${y:02X}")
            mon.close()
            return 0

        mon.resume()
        press("fire")           # title screen: inserts a credit
        time.sleep(4)
        press("one")            # select screen: starts a one-player game
        time.sleep(9)

    state, x, y = mon.player()
    print(f"gave up after {tries}: state=${state:02X} X=${x:02X} Y=${y:02X}")
    print("if state is $00 the front end never took - screenshot the window and look")
    mon.close()
    return 1


def _selftest(argv):
    window = float(argv[0]) if argv else 2.0
    mon = Monitor()
    print("connected")

    loaded = mon.game_loaded()
    print(f"game loaded: {loaded}")
    if not loaded:
        seen = mon.mem(GAME_FINGERPRINT_ADDR, GAME_FINGERPRINT_ADDR + 2)
        print(f"  $0A4E = {seen.hex()}, wanted {GAME_FINGERPRINT.hex()}")
        mon.close()
        return 1

    frames, a, b = mon.running(window)
    rate = frames / window
    print(f"frame counter $08: {a} -> {b}, {frames} in {window:.1f}s = {rate:.1f}/s")
    print(f"  machine running: {frames > 0} (expect about 50/s)")

    passes = mon.in_level(window)
    print(f"pass counter $A9FA: {passes} in {window:.1f}s = {passes / window:.1f}/s")
    print(f"  game loop turning: {passes > 0} (zero means the title screen)")

    mon.close()
    return 0


def _check_window(window):
    if window > MAX_WINDOW:
        raise SystemExit(
            f"window {window}s would wrap the $08 frame counter (limit "
            f"{MAX_WINDOW}s at ~50/s) - the ratio would come back wrong rather "
            f"than come back failed. Use a shorter window, or several."
        )


def _passes(argv):
    window = float(argv[0]) if argv else 4.0
    _check_window(window)
    mon = Monitor()
    mon.require_game()

    frames_a = mon.byte(FRAME_COUNTER)
    pass_a = mon.byte(PASS_COUNTER)
    mon.resume()
    time.sleep(window)
    frames_b = mon.byte(FRAME_COUNTER)
    pass_b = mon.byte(PASS_COUNTER)
    mon.resume()

    frames = (frames_b - frames_a) % 256
    passes = (pass_a - pass_b) % 256
    print(f"IRQ frames:       {frames}")
    print(f"game loop passes: {passes}")
    if passes:
        print(f"IRQ frames per game loop pass: {frames / passes:.4f}")
    else:
        print("game loop not turning - not in a level")
    mon.close()
    return 0


def _watch(argv):
    window = float(argv[0])
    _check_window(window)
    addrs = [int(a, 16) for a in argv[1:]]
    mon = Monitor()
    mon.require_game()

    before = {a: mon.byte(a) for a in addrs}
    frames_a = mon.byte(FRAME_COUNTER)
    mon.resume()
    time.sleep(window)
    after = {a: mon.byte(a) for a in addrs}
    frames_b = mon.byte(FRAME_COUNTER)
    mon.resume()

    print(f"IRQ frames across window: {(frames_b - frames_a) % 256}")
    for a in addrs:
        print(f"${a:04X}: {before[a]:02X} -> {after[a]:02X}")
    mon.close()
    return 0


COMMANDS = {"selftest": _selftest, "start": _start, "passes": _passes, "watch": _watch}


def main():
    if len(sys.argv) < 2 or sys.argv[1] not in COMMANDS:
        print(__doc__)
        return 2
    return COMMANDS[sys.argv[1]](sys.argv[2:])


if __name__ == "__main__":
    sys.exit(main())
