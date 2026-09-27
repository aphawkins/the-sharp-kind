# Spike: WASM build for Playwright-driven visual testing

[Repo] **Low-priority spike**, not a commitment to shipping a browser build.

`run-elite`/`run-scr`
([sdl-drive/drive.ps1](https://github.com/aphawkins/the-sharp-kind/blob/main/.claude/skills/sdl-drive/drive.ps1))
drive the native SDL window via Win32 `PostMessage`/`CopyFromScreen`, since
Playwright can't see a raw SDL window. The Software backends of
`SharpKind.Graphics`/`SharpKind.Audio` and both game libs are pure managed C#,
so a browser-hosted build (`SharpKind.Wasm` + per-game `*.Wasm` app targeting
`browser-wasm`) could let Playwright drive it headlessly.

Needs: a DOM keyboard adapter into `IKeyboardSink` (`SoftwareKeyboard` already
consumes it), a per-tick canvas blit of the software framebuffer
(`putImageData`), and asset loading via HTTP fetch or embedded resources. Only
exercises the Software backend — complements the native driver.
