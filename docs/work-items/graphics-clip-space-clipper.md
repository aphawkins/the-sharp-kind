# Clip polygons against all six clip-space planes

[SharpKind.Graphics] Part 3 of 6 of the clip-space run. After
[graphics-projection-matrix.md](graphics-projection-matrix.md).

Generalise
[NearPlaneClip.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/useful/libs/SharpKind.Graphics/NearPlaneClip.cs)
into a Sutherland-Hodgman clipper against all six planes (`-w <= x, y, z <= w`),
keeping the two interpolating overloads — an invented corner still needs its
colour and texture coordinate. Existing camera-space entry points stay until the
games move, so this adds a path rather than replacing one. Library and tests
only.
