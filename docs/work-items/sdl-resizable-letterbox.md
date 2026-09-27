# Resizable window with letterboxed scaling

[SharpKind.SDL] Add `SDL_WINDOW_RESIZABLE` in
[SDLWindow.cs:23-29](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.SDL/SDLWindow.cs)
and handle `SDL_EVENT_WINDOW_RESIZED`, so the window can be any size while both
games keep rendering at their native size — zero game-code changes.

Most of this landed with `WindowScale` (2026-07-29): `SDLRenderer.SetLogicalSize`
already fixes the logical coordinate space. What remains is the resizable flag,
switching that call's mode from `INTEGER_SCALE` to `LETTERBOX` when the window
is free to be any size, and deciding how the two settings interact.
