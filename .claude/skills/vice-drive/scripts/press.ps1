<#
.SYNOPSIS
  Hold joystick keys into the VICE window.

.DESCRIPTION
  rebb64 reads a joystick, not the keyboard buffer, so -keybuf cannot drive it.
  The keyset in vice.ini maps port 2 to W/S/A/D/space, and the game samples the
  key STATE - a single keystroke is usually missed, so each direction has to be
  held down for a few frames and then released.

  The window must be foreground for keybd_event to land.

.EXAMPLE
  ./press.ps1 -Keys fire -Holds 5
  ./press.ps1 -Keys right,fire -HoldMs 400
  ./press.ps1 -WaitFirst 30 -Keys fire
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
public class ViceWin {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int n);
  [DllImport("user32.dll")] public static extern void keybd_event(byte k, byte s, uint f, UIntPtr e);
}
"@

# vice.ini KeySet1: North=119 w, South=115 s, West=97 a, East=100 d, Fire=32 space
$VK = @{ up = 0x57; down = 0x53; left = 0x41; right = 0x44; fire = 0x20 }

foreach ($k in $Keys) {
  if (-not $VK.ContainsKey($k)) {
    Write-Output "unknown key '$k' - use up, down, left, right, fire"
    exit 1
  }
}

$p = Get-Process x64sc -ErrorAction SilentlyContinue |
     Where-Object { $_.MainWindowHandle -ne 0 } |
     Select-Object -First 1

if (-not $p) { Write-Output 'no VICE window found - is x64sc running?'; exit 1 }
Write-Output "window: $($p.MainWindowTitle)"

[void][ViceWin]::ShowWindow($p.MainWindowHandle, 9)   # SW_RESTORE
[void][ViceWin]::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 600

$KEYUP = 2
for ($i = 0; $i -lt $Holds; $i++) {
  foreach ($k in $Keys) { [ViceWin]::keybd_event($VK[$k], 0, 0, [UIntPtr]::Zero) }
  Start-Sleep -Milliseconds $HoldMs
  foreach ($k in $Keys) { [ViceWin]::keybd_event($VK[$k], 0, $KEYUP, [UIntPtr]::Zero) }
  Start-Sleep -Milliseconds $GapMs
  Write-Output "held $($Keys -join '+') $($i + 1)/$Holds"
}
