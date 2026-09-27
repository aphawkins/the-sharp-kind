# Own audio stack: WAV, MIDI/SoundFont2 and C64 SID per rendition

[SharpKind.Audio] The full, step-by-step plan is
[audio-stack-plan.md](../audio-stack-plan.md); work it phase by phase and stop
at the end of each phase for maintainer review.

Decided with the maintainer (see the plan and [decisions.md](../decisions.md),
2026-09-18): one audio format per visual tier — 8-bit C64 SID, 16-bit MIDI +
SoundFont2, Modern anything. The 8-bit chip is the **SID**; Elite's 8-bit
effects are ported from the C64 Elite source. Bubble Bobble's `.sid` music
plays through the same player; its sound *effects* stay in
[bb-port-plan.md](../bb-port-plan.md) phase 8. `ISound` does not change.

This replaces the earlier roadmap items for moving audio into rendition
manifests, a synth voice in the mixer, and the chip choice.
