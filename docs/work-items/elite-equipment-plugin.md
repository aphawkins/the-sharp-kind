# Elite: move the equipment table to a plugin

[EliteSharpLib] The 34-row `_equipmentStock` literal at
[EquipmentController.cs:23-58](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Views/EquipmentController.cs)
is the last hardcoded content table (ships and `ShipType` landed 2026-08-31).

Follow the goods precedent — **the assembly is the door, the file is the table**
(2026-08-27, [decisions.md](../decisions.md)): `IEquipmentSet` in
`EliteSharp.Abstractions`, an `EliteSharp.Equipment.Classic` plugin with
`equipment.json`, a loader beside `GoodsLoader`, and the app's csproj dropping
the DLL into an `Equipment` folder.

Unlike a good, equipment *acts* — `EquipmentType`
([EquipmentType.cs](https://github.com/aphawkins/the-sharp-kind/blob/main/src/elite/libs/EliteSharpLib/Equipment/EquipmentType.cs))
is switched on to fit an E.C.M., mount a laser, or expand the hold — so the enum
stays as the behaviour key and the file states only the inert half: name,
price, tech level, `Show`/`CanBuy` flags and the behaviour key. A row naming an
unknown key is what the loader refuses. Laser rows are a two-level list (a
`+`/`-` category expanding into four `>` mounts), so the file needs that shape.
