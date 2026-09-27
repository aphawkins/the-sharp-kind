# Decide whether render resolution is configurable independently of rendition

[Apps] **[DECISION]** — maintainer to decide.

Elite no longer hardcodes resolution: the rendition declares it, so the asset
set and the resolution can never disagree. The open question is whether a launch
resolution/aspect should also be settable *independently*, in
`EngineConfigSettings`. If yes, it exposes rather than fixes fixed-size
assumptions, so it must follow
[elite-arbitrary-size-test-rendition.md](elite-arbitrary-size-test-rendition.md).
SCR still hardcodes its own consts either way.

Widescreen is a Modern-rendition concern only (2026-08-01,
[decisions.md](../decisions.md)); 8-bit and 16-bit are fixed-width.
[scr-widescreen.md](scr-widescreen.md) depends on this answer.
