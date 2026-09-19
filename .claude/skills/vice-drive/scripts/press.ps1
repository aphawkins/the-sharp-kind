<#
.SYNOPSIS
  Hold keys into the VICE window - joystick directions, or the digits the front end wants.

.DESCRIPTION
  rebb64 reads a joystick, not the keyboard buffer, so -keybuf cannot drive it.
  The keyset in vice.ini maps port 2 to W/S/A/D/space, and the game samples key
  STATE, so each key has to be held for a few frames and then released.

  Two things this script is careful about, both of which have cost a session:

  1. It sends scancodes through SendInput, not virtual keys through keybd_event.
     VICE's GTK input wants the scancode. This path is proved end to end - type
     a letter at the BASIC prompt with it and the letter appears on screen.

  2. One invocation holds one set of keys and releases them before returning.
     Do NOT start a second invocation while the first is still holding: the
     first one's key-UP lands after the second one's key-DOWN and silently
     releases the key. If two keys must be held at once, pass them both to a
     single call. If two must overlap unevenly, background one and leave a
     clear margin - never let the holds meet.

  The window must be foreground; this raises it.

.EXAMPLE
  ./press.ps1 -Keys fire
  ./press.ps1 -Keys one
  ./press.ps1 -Keys right,up -HoldMs 6000
#>
param(
  [string[]]$Keys = @('fire'),
  [int]$Holds = 1,
  [int]$HoldMs = 250,
  [int]$GapMs = 700,
  [int]$WaitFirst = 0
)

if ($WaitFirst -gt 0) { Start-Sleep -Seconds $WaitFirst }

Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class ViceIn {
  [StructLayout(LayoutKind.Sequential)]
  public struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }
  [StructLayout(LayoutKind.Explicit, Size = 40)]
  public struct INPUT { [FieldOffset(0)] public uint type; [FieldOffset(8)] public KEYBDINPUT ki; }

  [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint n, INPUT[] p, int size);
  [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint mapType);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a, uint b, bool attach);

  // Windows refuses SetForegroundWindow to a process that does not already own
  // the foreground, and it fails SILENTLY - the keys then go to whatever does.
  // That is why driving press.ps1 from a subprocess was flaky while the same
  // call from an interactive shell worked. Borrowing the foreground thread's
  // input queue lifts the restriction.
  public static void Focus(IntPtr h) {
    ShowWindow(h, 9);
    uint us = GetCurrentThreadId();
    uint them = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
    if (us != them) AttachThreadInput(us, them, true);
    SetForegroundWindow(h);
    if (us != them) AttachThreadInput(us, them, false);
  }

  const uint SCANCODE = 0x0008, KEYUP = 0x0002;

  public static void Key(ushort vk, bool up) {
    INPUT[] i = new INPUT[1];
    i[0].type = 1;
    i[0].ki.wVk = 0;
    i[0].ki.wScan = (ushort)MapVirtualKey(vk, 0);
    i[0].ki.dwFlags = SCANCODE | (up ? KEYUP : 0);
    SendInput(1, i, Marshal.SizeOf(typeof(INPUT)));
  }
}
"@

# vice.ini KeySet1: North=119 w, South=115 s, West=97 a, East=100 d, Fire=32 space.
# one/two are the real keyboard keys the player-select screen reads - they are
# NOT joystick, and no amount of fire will substitute for them.
$VK = @{ up = 0x57; down = 0x53; left = 0x41; right = 0x44; fire = 0x20; one = 0x31; two = 0x32 }

foreach ($k in $Keys) {
  if (-not $VK.ContainsKey($k)) {
    Write-Output "unknown key '$k' - use up, down, left, right, fire, one, two"
    exit 1
  }
}

$p = Get-Process x64sc -ErrorAction SilentlyContinue |
     Where-Object { $_.MainWindowHandle -ne 0 } |
     Select-Object -First 1

if (-not $p) { Write-Output 'no VICE window found - is x64sc running?'; exit 1 }

[ViceIn]::Focus($p.MainWindowHandle)
Start-Sleep -Milliseconds 500

for ($i = 0; $i -lt $Holds; $i++) {
  foreach ($k in $Keys) { [ViceIn]::Key($VK[$k], $false) }
  Start-Sleep -Milliseconds $HoldMs
  foreach ($k in $Keys) { [ViceIn]::Key($VK[$k], $true) }
  if ($i -lt $Holds - 1) { Start-Sleep -Milliseconds $GapMs }
}

Write-Output "held $($Keys -join '+') for ${HoldMs}ms x$Holds"
