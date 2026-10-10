# SoundFont: filter, LFOs, modulation envelope, reverb and chorus

[SharpKind.Audio] The SoundFont 2 player reads only the volume envelope,
tuning, pan, attenuation, key and velocity ranges, sample addresses and
loops, and exclusive class. It ignores the filter, both LFOs, the modulation
envelope, reverb and chorus sends, and every modulator (`pmod`/`imod`). Elite's
16-bit tunes sound right without them (heard 2026-10-10).

Only worth doing if a tune needs them. Write them from the SoundFont 2.01
spec, never from MeltySynth, TinySoundFont or other GPL/LGPL code (see the
clean-room rule in [decisions.md](../decisions.md)).
