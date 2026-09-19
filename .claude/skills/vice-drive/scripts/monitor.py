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
    python monitor.py passes 4      # game loop passes over a 4 second window
    python monitor.py watch 4 A9FA AA0C AA1E   # sample bytes across a window
"""

import socket
import struct
import sys
import time

STX = 0x02
API = 0x02

CMD_MEM_GET = 0x01
CMD_CHECKPOINT_GET = 0x11
CMD_CHECKPOINT_SET = 0x12
CMD_CHECKPOINT_DELETE = 0x13
CMD_EXIT = 0xAA

RESP_MEM_GET = 0x01
RESP_CHECKPOINT = 0x11

UNSOLICITED = 0xFFFFFFFF

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


class MonitorError(RuntimeError):
    pass


class Monitor:
    def __init__(self, host="127.0.0.1", port=6502, timeout=10.0):
        self.sock = socket.create_connection((host, port), timeout=timeout)
        self.sock.settimeout(timeout)
        self._buf = b""
        self._next_id = 1

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

    def resume(self):
        """Let the machine run again.

        EVERY command halts the emulator, not just a memory read - setting a
        checkpoint halts it too. Without this the machine stays stopped and every
        counter reads the same value for ever.
        """
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


def _passes(argv):
    window = float(argv[0]) if argv else 4.0
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


COMMANDS = {"selftest": _selftest, "passes": _passes, "watch": _watch}


def main():
    if len(sys.argv) < 2 or sys.argv[1] not in COMMANDS:
        print(__doc__)
        return 2
    return COMMANDS[sys.argv[1]](sys.argv[2:])


if __name__ == "__main__":
    sys.exit(main())
