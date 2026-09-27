# Stunt Car Racer: read controls from a file

[StuntCarRacerSharpLib] The controls machinery is a library
([SharpKind.Abstraction.Controls](https://github.com/aphawkins/the-sharp-kind/tree/main/src/useful/libs/SharpKind.Abstraction/Controls)),
generic over a game's actions and axes, so SCR needs an action enum, a set of
defaults and the registration — the same three pieces Elite keeps. Steering is
fixed at +/-15, so there are no axes to bind; worth doing for consistency.

The library takes an enum's zero value to mean "nothing", so SCR's enums must
reserve it — see the Elite entry in
[CHANGELOG.md](https://github.com/aphawkins/the-sharp-kind/blob/main/CHANGELOG.md).
