# Resizeable window and native-resolution fullscreen for Modern

[SharpKind.SDL] **[LARGE]** — survey and split before starting. After
[elite-modern-rendition.md](elite-modern-rendition.md).

This is what the Modern rendition is ultimately for. `ScreenWidth`/`ScreenHeight`
are set once in the constructor, and the render target, depth buffer and
surface-id buffer are all sized from them
([SDLGraphics.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.SDL/SDLGraphics.cs)).
So it needs a resize signal, a rebuild of those buffers, and a way to replace the
immutable `ViewLayout` every view was built against — `BaseView8Bit` and
`BaseView16Bit` capture it in their constructors; other views read it per draw.

Harder than it looks: most views and controllers are written against *pixel*
positions rather than scaled ones, so they would all have to move when the
screen does. Survey that before splitting.
