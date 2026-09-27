# Elite: move ship rendering onto the clip-space path

[EliteSharpLib] **[DECISION]** Part 4 of 6 of the clip-space run. After
[graphics-clip-space-clipper.md](graphics-clip-space-clipper.md).

`ShipBase` transforms to clip space, clips there, divides, and applies the
viewport
([ShipBase.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Ships/ShipBase.cs)).

Two things resist and need deciding with the maintainer, not deleting:
`ProjectPoint`'s `vec.Z <= 0` clamp, which exists only so a laser aim behind the
camera still yields a point, and `ShowsDetail`, which reads `Focus * radius / z`
directly as a screen size.

Verify with the golden-trace harness, and smoke-test — this is a game loop.
