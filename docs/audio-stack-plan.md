# Own audio stack in SharpKind.Audio: WAV, MIDI/SF2 and C64 SID

## Context

`SharpKind.Audio` depends on three third-party packages:

- **NVorbis** decodes Elite's 14 `.ogg` effects (`SoftwareSound`).
- **MeltySynth** plays Elite's two `.mid` tunes through `TimGM6mb.sf2`
  (`SoftwareSound`, and the predecode in `SharpKind.SDL/SDLSound.cs`).
- **OggVorbisEncoder** only builds `.ogg` test fixtures.

The maintainer wants one audio format per visual tier (`docs/work-items/audio-own-stack.md`, and `docs/decisions.md` 2026-09-18):

| Tier | Audio |
|---|---|
| 8-bit | C64 SID (`.sid`) |
| 16-bit | MIDI + SoundFont2 |
| Modern | anything, WAV included |

The 8-bit renditions of Elite and Bubble Bobble already hold `.sid` files that
nothing plays. This plan replaces all three packages with our own code and
adds a SID player. Decisions taken with the maintainer:

1. **Vorbis:** no decoder. Elite's 16-bit tier switches to the `.wav` sources
   that already sit beside each `.ogg` (PCM16, mono, 44.1 kHz). The `.ogg`
   effects go.
2. **MIDI:** our own Standard MIDI File reader and SoundFont2 sample player.
3. **SID:** 6502 CPU + SID chip + PSID player, and the 8-bit renditions of
   Elite and Bubble Bobble play their `.sid` music. Bubble Bobble's sound
   *effects* stay in `docs/bb-port-plan.md` phase 8.
4. **Elite 8-bit effects are SID, not `.wav`.** They are ported from the
   Commodore 64 source (`C:\code\github\markmoxon\elite-source-code-commodore-64`,
   `1-source-files/main-sources/elite-source.asm`): the effect tables
   `SFXPR` `SFXCNT` `SFXFQ` `SFXCR` `SFXATK` `SFXSUS` `SFXFRCH` `SFXVCH`
   (16 effects), the start routines `NOISE`/`NOISE2`/`HYPNOISE`, and the
   per-frame engine `SOINT`. Effects and music share one SID chip, as on
   the C64.
5. **SDL backend:** `SDLSound` streams `.mid` and SID audio from our code into
   SDL3_mixer tracks, so both backends sound the same.

`ISound` does not change. The games do not learn which tier is sounding.

## Progress

- Phase 0: done (2026-10-10).
- Phase 1: done (2026-10-10). `IMusicStream` moved to its own file in 1.2
  (internal; 2.11 makes it public), because `SampleMusicStream` must
  implement it.
- Phase 2: done (2026-10-10). Where it differs from the steps below:
  - The public pair is `IMusicSource` and `MusicSources`: CA1711 forbids a
    public `IMusicStream`. `MusicSources.Open` also opens `.sid` songs.
  - `SDLSound` streams `.mid` and `.sid` music through `MusicOutput` (the old
    `SidOutput`, made general), an audio-device stream of its own, not
    `MIX_SetTrackAudioStream`.
  - Initial attenuation is applied at 40%, as on the EMU hardware fonts are
    voiced for; without it Danube played at a quarter of MeltySynth's level.
    Master gain is 0.57, which matches MeltySynth's level on both tunes.
  - A preset's sample address offsets are ignored (SoundFont 2.01, 8.1.3).
  - A missing drum kit falls back to bank 128's first kit, never to a
    melodic program.
  - 200 cB of sustain is gain 0.1 (2.5 said 0.316; that is 100 cB).
- Phase 3: re-planned (2026-10-10) against the SID code that landed before
  this plan was worked; see the phase. Done (2026-10-10), except the
  maintainer's listening check. Where it differs from the steps:
  - `SidEffectEngine` takes a register-write delegate (`SidMachine.Chip.Write`
    in `SidSound`), not a `SidChip`, so the tests can see every write.
  - `SidSound.StopMusic` does nothing when no song plays, so a game's
    `StopMusic` does not cut effects short.
  - 3.7 is `ShippedSidTuneTests` (explicit). It wrote each song's first 30 s
    to `%TEMP%\elite-<n>.wav` and `%TEMP%\bubble-bobble-<n>.wav`. Bubble
    Bobble's song 8 plays 87.5 s and song 7 7.5 s, as bb-port-plan step 8
    found; both Elite songs loop for ever. No `vsid` comparison was done.
- Phase 4: 4.4 is done. Bubble Bobble plays all 12 songs through
  `GameLoop.TakeSong` (bb-port-plan step 8).
- Phase 4: done (2026-10-10). Where it differs from the steps:
  - 4.1 went further, with the maintainer's agreement: `RenditionAssets` and
    the game's own manifest are gone, and Elite registers the chosen
    rendition's `AssetLocator` directly. The two `.ogg` music renders, which
    nothing played, were deleted at the maintainer's request.
  - 4.3: song 1 is the theme and song 2 the Blue Danube, from the notes each
    plays (song 2 has a waltz pulse over an A pedal, in D). Not yet confirmed
    by ear. `RenditionAudioTests` also plays every 8-bit effect on an idle
    chip and expects sound (silence for `Dock`).
  - 4.5: all four runs (8-bit and 16-bit, Software and Hardware) reached
    flight with music on and no log errors. Not heard.
  - SID CPU (2026-10-10): an idle chip cost 9.5% of a core in Release (16.5%
    in Debug), because the filter and DC blocker decayed through denormal
    floats for ever. They now settle to zero below 1e-20, and a chip whose
    three envelopes are silent advances its voices a sample at a time
    (`SidOscillator.Advance`, `SidEnvelope.Advance`, exact). Release: music
    1.9% → 1.4%, idle 9.5% → 0.1%. Output is bit-identical except the
    settling, which moves it by at most 1e-20.
- Phase 5: done (2026-10-10). The decisions entry, the readmes, reference
  sources and asset structure are updated. The roadmap item is replaced by
  the [listening check](work-items/audio-listening-check.md) and the
  follow-ups the phase lists. bb-port-plan step 8 already said the music
  plays through the own SID stack, so it needed no change.

## Rules for the implementor

- Read `AGENTS.md` first. UK spelling. Match the house style (file header
  `// 'SharpKind Libraries' - Andy Hawkins 2023-2026.`, file-scoped namespaces,
  `SharpKindException` for bad content). Warnings are errors, all analysers on.
- **Do not commit.** Leave each step uncommitted and ready to commit on its
  own. Stop at the end of each phase for the maintainer to review.
- One step = one small, buildable, tested change. Do not start the next step
  with the build or tests red.
- New types are `internal` unless a step says `public`. Step 0.2 adds
  `InternalsVisibleTo("SharpKind.Audio.Tests")`.
- **Clean room.** The repo is MIT. Do **not** copy or port reSID,
  libsidplayfp, MeltySynth, TinySoundFont or any other GPL/LGPL source. The
  formulas and tables in this plan are from public hardware and format
  documentation and are what to use. This rule is about emulator and synth
  code only: porting the games' own routines and data (C64 Elite's sound
  engine, 3.2 and 4.2) is how this repo always works.
- **Test runner:** `dotnet test` reports "Zero tests ran" in this sandbox. It is
  not a failure of the change. Build, then run the test exe directly:

  ```bash
  dotnet build TheSharpKind.slnx
  ```

  ```bash
  cd src/useful/test/SharpKind.Audio.Tests/bin/Debug/net10.0 && ./SharpKind.Audio.Tests.exe
  ```

  Repo-wide: `for f in $(find src -path "*bin/Debug/net10.0/*.Tests.exe"); do (cd $(dirname $f) && ./$(basename $f)); done`

## Target layout of `src/useful/libs/SharpKind.Audio`

```
AudioController.cs  AudioOptions.cs  ISound.cs  SfxSample.cs   (unchanged)
SoftwareSound.cs                (loses Vorbis/MeltySynth; uses the below)
Program.cs                      (InternalsVisibleTo)
IMusicSource.cs  MusicSources.cs  MusicPath.cs     (public: SDLSound uses them)
Wav/WavDecoder.cs  Wav/SampleMusicStream.cs
Midi/MidiFile.cs  Midi/MidiEvent.cs  Midi/MidiSequencer.cs  Midi/MidiMusicStream.cs
Midi/Synthesizer.cs  Midi/MidiChannel.cs  Midi/Voice.cs  Midi/VolumeEnvelope.cs
SoundFonts/SoundFont.cs  SoundFonts/SoundFontSample.cs  SoundFonts/Zone.cs
SoundFonts/Preset.cs  SoundFonts/Instrument.cs  SoundFonts/GeneratorType.cs
SoundFonts/VoiceParameters.cs
SidSongPlayer.cs                (internal from 3.3: one song on a shared SidMachine)
Sid/SidEffectFile.cs  Sid/SidEffectEngine.cs
SidSound.cs                     (public: SoftwareSound and SDLSound share it)
```

The 6502, the bus, the PSID reader and the SID chip are not in this library.
They are in `src/useful/libs/SharpKind.Emulation` (`Cpu/`, `Sid/`), which
Bubble Bobble's oracle tests also use.

Outside the library: `tools/elite/export-c64-sfx.py` and the generated
`src/elite/libs/EliteSharp.Renditions.EightBit/Assets/SFX/elite.sidfx`.

---

## Phase 0 - Baseline

**0.0** Copy this plan to `docs/audio-stack-plan.md` and add it to
`docs/toc.yml`, so the implementor model and the maintainer share one copy.
Tick steps there as they finish.

**0.1** Build the solution and run all test exes. Note the pass counts. → verify: all green.

**0.2** Add `src/useful/libs/SharpKind.Audio/Program.cs` with
`[assembly: InternalsVisibleTo("SharpKind.Audio.Tests")]`, copying
`src/useful/libs/SharpKind.SDL/Program.cs`. → verify: build green.

**0.3** Move the fixture builders out of `SoftwareSoundTests.AudioAssetFixture`
into a new `src/useful/test/SharpKind.Audio.Tests/AudioFixtures.cs`
(`internal static class`): `BuildMinimalMidiFile`, `BuildMinimalSoundFont`,
`BuildTestWavFloat32`, and the chunk helpers. `AudioAssetFixture` calls them.
No behaviour change. → verify: the same tests pass.

**Check in with the maintainer.**

---

## Phase 1 - WAV only; remove NVorbis and OggVorbisEncoder

**1.1 WAV decoder.** Move `DecodeWavFully` and `DecodePcm16` out of
`SoftwareSound` into `Wav/WavDecoder.cs` as
`internal static float[] Decode(string path)` returning interleaved stereo
float32. Add:
- mono input (channels = 1): duplicate each sample to L and R;
- PCM16 and float32 for both mono and stereo;
- a `SharpKindException` (not `Debug.Assert`) when the rate is not 44100 or
  the channel count is not 1 or 2. Keep the existing chunk walk.

Tests (`WavDecoderTests.cs`): mono PCM16 fixture decodes to 2x the samples with
L == R; stereo float32 round-trips; 22050 Hz throws; the existing truncated
`fmt ` test still throws. Add `AudioFixtures.BuildTestWavPcm16Mono`.

**1.2 WAV music.** Add `Wav/SampleMusicStream.cs`: plays a decoded `float[]`
through `Render(Span<float>)`, wraps to 0 at the end when `repeat`, else writes
silence. `SoftwareSound.CreateMusicStream` uses it for `.wav`. Test: a 0.3 s
WAV music track with repeat is non-silent in the second half of a 1 s render;
without repeat the tail is silent.

**1.3 Remove Vorbis from `SoftwareSound`.** Delete `DecodeOggFully`,
`OggMusicStream`, `using NVorbis`. An unknown extension throws
`SharpKindException` naming the file. In the tests, replace every `.ogg`
fixture with a WAV one (`OggBeep`→`WavBeep`, `OggQuiet`→`WavQuiet`,
`OggTheme`→`WavTheme`) and delete `BuildTestOgg`. Keep every test's intent.

**1.4 Elite assets.** In `src/elite/libs/EliteSharpLib/Assets/AssetManifest.json`
point every `Sfx` entry at its `.wav`. In `EliteSharpLib.csproj` change the
`Never` rule to `Assets\Music\*.ogg` only, so the `.wav` effects are copied.
`git rm` the 14 `Assets/SFX/*.ogg`. Leave `Assets/Music/*.ogg` (unused
reference renders; mention them to the maintainer). SDL3_mixer loads mono WAV
natively, so `SDLSound` needs no change. → verify: Elite tests green; run
Elite with the `run-elite` skill on both backends and hear a laser and a beep.

**1.5 Remove the packages.** Delete `NVorbis` from `SharpKind.Audio.csproj`,
`OggVorbisEncoder` from `SharpKind.Audio.Tests.csproj`, and both from
`Directory.Packages.props`. → verify: clean build, all tests green,
`grep -rn "NVorbis\|OggVorbis" src Directory.Packages.props` finds nothing
outside `bin/obj`.

**Check in with the maintainer.**

---

## Phase 2 - MIDI and SoundFont2; remove MeltySynth

Reference numbers used below (SoundFont 2.01 spec, SMF 1.0 spec):
- timecents → seconds: `2^(tc/1200)`; -12000 means "instant" (about 1 ms).
- centibels → gain: `10^(-cB/200)`.
- generator IDs needed: 0 startAddrsOffset, 1 endAddrsOffset,
  2 startloopAddrsOffset, 3 endloopAddrsOffset, 4 startAddrsCoarseOffset,
  12 endAddrsCoarseOffset, 17 pan, 33 delayVolEnv, 34 attackVolEnv,
  35 holdVolEnv, 36 decayVolEnv, 37 sustainVolEnv, 38 releaseVolEnv,
  41 instrument, 43 keyRange, 44 velRange, 45 startloopAddrsCoarseOffset,
  46 keynum, 47 velocity, 48 initialAttenuation, 50 endloopAddrsCoarseOffset,
  51 coarseTune, 52 fineTune, 53 sampleID, 54 sampleModes, 56 scaleTuning,
  57 exclusiveClass, 58 overridingRootKey. Ignore all others (filter, LFOs,
  modulation envelope, reverb, chorus, modulators).
- defaults: env times -12000, sustainVolEnv 0, scaleTuning 100,
  overridingRootKey -1, keynum/velocity -1, key/vel ranges 0-127, rest 0.

**2.1 `SoundFonts/GeneratorType.cs`** - an enum of the IDs above. No test.

**2.2 SoundFont reader, raw tables.** `SoundFonts/SoundFont.cs`:
`internal static SoundFont Read(string path)`. Walk `RIFF sfbk`; take
`LIST sdta/smpl` as `short[]`; take `LIST pdta` sub-chunks `phdr` (38 bytes),
`pbag` (4), `pgen` (4), `inst` (22), `ibag` (4), `igen` (4), `shdr` (46).
Ignore `pmod`/`imod` and `INFO`. Throw `SharpKindException` on a missing
chunk. Tests with `AudioFixtures.BuildMinimalSoundFont`: one preset (bank 0,
patch 0), one instrument, one sample with the fixture's start/end/rate/root.

**2.3 Zones.** `Zone.cs` holds a `short[64]` of generator values plus a
`bool[64]` of which were set. `Instrument`/`Preset` build zones from the bag
index ranges (last record of each table is a terminator). The first zone of
an instrument without `sampleID` (or of a preset without `instrument`) is the
**global zone**; other zones start from its values.
Tests: build a fixture SF2 variant with a global zone and check inheritance.

**2.4 Voice parameters.** `SoundFont.FindVoices(int bank, int program, int key, int velocity)`
returns `IReadOnlyList<VoiceParameters>`: for every preset zone whose ranges
contain key/vel, for every instrument zone of its instrument whose ranges
contain key/vel, combine: instrument value (absolute) **plus** preset value
(relative) for every generator except ranges, `instrument`, `sampleID`,
`sampleModes`, `exclusiveClass`, `overridingRootKey`, `keynum`, `velocity`
(instrument-only). `VoiceParameters` is a readonly record struct: sample
start/end/loopStart/loopEnd (after address offsets, coarse = x32768), sample
rate, root key (overridingRootKey if >= 0 else sample's originalPitch), pitch
correction, coarse/fine tune, scaleTuning, pan, attenuation, the six envelope
values, sampleModes, exclusiveClass. Missing bank/program falls back to
program in bank 0, then preset 0 of that bank. Tests: preset tuning adds to
instrument tuning; key outside every range returns empty; fallback works.

**2.5 Volume envelope.** `Midi/VolumeEnvelope.cs`, per-sample stepping at
44100 Hz. Stages: delay (silent) → attack (gain rises linearly 0→1) → hold
(1) → decay (attenuation rises linearly in dB from 0 towards sustain cB, at a
rate of 1000 cB per decayVolEnv seconds) → sustain → release on note-off
(attenuation rises 1000 cB per releaseVolEnv seconds from wherever it is).
`IsFinished` when attenuation >= 960 cB in release. Tests: attack reaches 1 at
the right sample count (±1); sustain 200 cB settles at gain 0.316; release
finishes; note-off during attack goes straight to release.

**2.6 Voice.** `Midi/Voice.cs`: plays one `VoiceParameters` from the
`short[]` sample data with linear interpolation. Pitch ratio =
`2^((key - rootKey) * scaleTuning/100 + coarse + (fine + pitchCorrection)/100 + bendSemitones) / 12) * sampleRate / 44100`.
sampleModes 0: stop at end; 1: loop between loopStart/loopEnd for ever;
3: loop until note-off, then play on to end. Gain = envelope ×
`10^(-attenuation/200)` × velocity gain `(vel/127)^2` × channel gain.
Pan: `p = clamp(genPan/500 + channelPan, -1, 1)`,
`L = cos((p+1)·π/4)`, `R = sin((p+1)·π/4)`. Mixes into an interleaved
stereo span. Finished when the envelope is finished or mode 0 runs out.
Tests: a looped fixture sample keeps sounding after its length; mode 0 stops;
key root+12 doubles zero crossings; full left pan leaves R silent.

**2.7 Channel state.** `Midi/MidiChannel.cs`: program, bank (CC0), volume
CC7 (default 100), expression CC11 (127), pan CC10 (64), sustain CC64,
pitch bend (14-bit, centre 8192), bend range (default 2 semitones; set by
RPN 0,0 via CC101=0, CC100=0, CC6 = semitones, CC38 = cents). Channel gain =
`(vol/127)^2 · (expr/127)^2`; channel pan = `(cc10 - 64)/64`. CC121 resets
controllers. Channel 9 (the tenth) is percussion: bank 128. Tests for each.

**2.8 Synthesizer.** `Midi/Synthesizer.cs`: 64-voice pool.
`NoteOn(ch, key, vel)` (vel 0 = NoteOff) starts one voice per
`FindVoices` result; kills voices on the same channel with the same non-zero
exclusiveClass first. When the pool is full, steal the oldest voice in
release, else the oldest. `NoteOff` releases, or marks "held" while sustain
is down; CC64 < 64 releases held voices. CC120 silences at once, CC123
releases all. `Render(Span<float>)` clears, mixes all voices, then multiplies
by a master gain of 0.5 (tune by ear against the old output). Tests: 65
note-ons never throw and sound; exclusive class cuts; sustain holds; channel
9 uses bank 128.

**2.9 MIDI file.** `Midi/MidiFile.cs`: parse `MThd` (format 0 or 1;
division must be ticks-per-quarter, SMPTE throws) and each `MTrk`: varlen
deltas, running status, channel messages 0x8-0xE, meta 0x51 (tempo,
µs/quarter, default 500000) and 0x2F (end); skip other metas and sysex
(F0/F7 with varlen length). Merge all tracks by absolute tick (stable: track
order, then file order). Convert ticks to seconds with the tempo map, then to
sample offsets at 44100. Result: `IReadOnlyList<MidiEvent>` (sample time,
channel, command, data1, data2) and total length in samples. Tests: the
minimal fixture gives note-on at 0 and note-off at 11025 samples (0.25 s); a
two-track fixture with a tempo change halfway times correctly; running
status parses.

**2.10 Sequencer.** `Midi/MidiSequencer.cs`: holds a `MidiFile` and a
`Synthesizer`; `Render(Span<float>)` renders in sub-blocks, sending each
event at its sample time. At the end with `repeat`, restart from event 0
(voices keep ringing). `EndOfSequence` and `Synthesizer.ActiveVoiceCount`
support full decode. Test: a looped render of the fixture has a note-on in
every 0.25 s period.

**2.11 Music stream contract (public).** Add public
`IMusicStream : IDisposable { void Render(Span<float> buffer); }` and public
`static class MusicStreams { IMusicStream Open(string path, string? soundFontPath, bool repeat); }`
dispatching on extension: `.wav` → `SampleMusicStream`, `.mid` →
`MidiMusicStream` (wraps the sequencer; throws `SharpKindException` when no
SoundFont is configured). Cache the parsed `SoundFont` per path inside
`MusicStreams` (it is 6 MB). Move `SoftwareSound`'s private `IMusicStream` to
this one. `SoftwareSound.DecodeMidiFully` (MIDI as an effect) uses the new
sequencer the same way it used MeltySynth. → verify: every existing
`SoftwareSoundTests` MIDI test passes unchanged.

**2.12 SDLSound streams music.** In `src/useful/libs/SharpKind.SDL/SDLSound.cs`:
- `.wav`/other music keeps `MIX_LoadAudio`.
- `.mid` goes through `MusicStreams.Open` (SID arrives in 3.6). Create an
  `SDL_AudioStream` (F32, 2 ch, 44100 both sides) with
  `SDL_SetAudioStreamGetCallback`; the callback renders from the current
  `IMusicStream` under a lock and calls `SDL_PutAudioStreamData`. Copy the
  pattern of `SoftwareSoundOutput.FillCallback`/`Fill` (GCHandle userdata,
  `[UnmanagedCallersOnly]`, grow-only scratch buffer). Attach it with
  `MIX_SetTrackAudioStream(_musicTrack, stream)` and `MIX_PlayTrack`. Repeat is
  handled by the stream, not `MIX_SetTrackLoops`.
- `StopMusic` stops the track and disposes the current stream.
- Delete `DecodeMidiFully`, `MaxDecodeSeconds`, `using MeltySynth`.
→ verify: build; run Elite on the SDL backend; the intro plays the theme and
docking plays the Blue Danube.

**2.13 Remove MeltySynth** from `SharpKind.Audio.csproj` and
`Directory.Packages.props`. Update the csproj `Description` to
"Software audio playback (WAV, MIDI/SoundFont2, C64 SID) for SharpKind-based
games." → verify: clean build, all tests green, no `MeltySynth` in `src`.

**Check in with the maintainer:** listen to both Elite tunes on both backends
against the pre-change build. Tune the master gain if needed.

---

## Phase 3 - C64 SID

### Already done (before this plan was worked)

These steps of the first plan landed with Bubble Bobble's port, in
`SharpKind.Emulation` rather than in this library. They stay as they are. Read
them before starting, because the steps below build on them.

| First plan's step | Where it is | Tests |
|---|---|---|
| old 3.1 PSID reader | `Emulation/Sid/PsidFile.cs` | `SidMachineTests` (header, load address, refusals) |
| old 3.2 Bus | `Emulation/Sid/C64Bus.cs` | through `SidMachineTests` |
| old 3.3-3.6 CPU, `Call` | `Emulation/Cpu/Cpu6502.cs`, `.Opcodes.cs` | `Cpu6502Tests`; Bubble Bobble's oracle tests run the real game on it |
| old 3.7-3.10 Oscillator, envelope, filter, chip | `Emulation/Sid/Sid*.cs` | `SidOscillatorTests`, `SidEnvelopeTests`, `SidChipTests` |
| old 3.11-3.12 Player and machine | `Emulation/Sid/SidMachine.cs` (play routine each PAL frame; `IdleFrame` runs when no tune plays) | `SidMachineTests` |
| old 3.13 Music paths | `Audio/MusicPath.cs`; `SidMachine.StartMusic` refuses song 0 and songs past the end | `MusicPathTests`, `SidMachineTests` |
| part of old 3.16 | `Audio/SidSongPlayer.cs` (ends or repeats a song after 2 s of quiet); `MusicSources` opens `.sid` | `SidSongPlayerTests`, `SoftwareSoundSidTests` |

What is still missing: the C64 Elite effects, and music and effects on one
chip, as on the C64. Today every SID song gets a `SidMachine` of its own
(`SidMusicStream`), and nothing plays SID effects.

**3.1 Effect file.** `Sid/SidEffectFile.cs` reads a `.sidfx` file (JSON):

```json
{
  "Effects": {
    "sfxbeep": { "Priority": 104, "Frames": 5, "Frequency": 240, "Control": 17,
                 "AttackDecay": 0, "SustainRelease": 240, "FrequencyChange": 0,
                 "VolumeMask": 255 }
  },
  "Sounds": {
    "Beep": [ { "Effect": "sfxbeep" } ],
    "Hyperspace": [
      { "Effect": "sfxhyp1", "Frequency": 240, "SustainRelease": 245 },
      { "Effect": "sfxwhosh" },
      { "Effect": "sfxhyp1", "Layer": true, "DelayFrames": 1 }
    ],
    "Dock": []
  }
}
```

- An **effect** is one row of the C64 tables: `SFXPR` → Priority,
  `SFXCNT` → Frames, `SFXFQ` → Frequency, `SFXCR` → Control, `SFXATK` →
  AttackDecay, `SFXSUS` → SustainRelease, `SFXFRCH` → FrequencyChange
  (signed, −128..127), `SFXVCH` → VolumeMask. All others are bytes.
- A **sound** is what one `ISound.Play` key triggers: a list of steps. A step
  with `Frequency` and `SustainRelease` is a `NOISE2` call (both or neither
  must be given). `Layer` is the C64's "effect + 128" (skip the
  already-playing check). `DelayFrames` waits that many engine ticks. An
  empty list is a deliberate silence.
Validation throws `SharpKindException`: unknown effect name, one of
Frequency/SustainRelease without the other, a value out of range.
Use `System.Text.Json` with `UnmappedMemberHandling.Disallow`, as
`AssetLocator` does. The reader resolves every step to its effect row and the
effect's index in the file, so the engine never sees names. Tests: the example
round-trips; each invalid case throws.

**3.2 Effect engine.** `Sid/SidEffectEngine.cs` is a port of C64 Elite's
`NOISE`/`NOISE2` (`elite-source.asm` line 43384) and `SOINT` (line 44135).
Read those routines alongside this spec. State per voice `v` = 0..2
(the C64's `SOFLG SOCNT SOPR SOFRCH SOFRQ SOCR SOATK SOSUS SOVCH`): Flag,
Counter, Priority, FrequencyChange, Frequency, Control, AttackDecay,
SustainRelease, VolumeMask. Plus `PulseWidth` (starts 2) and a queue of
delayed steps. All arithmetic is 8-bit and wraps. SID register for voice `v`
field `r` is `7·v + r`.

`Trigger(string sound)`: steps with `DelayFrames` 0 start now; others go on
the queue. `Start(step)` (= `NOISE`):
1. `e` = the step's effect. `chosen` = none.
2. If not `Layer` and `e.Priority` bit 0 is clear: if a voice's Flag & 0x3F
   equals `index(e) + 1`, `chosen` = that voice. (With `Layer`, the C64 reads
   `SFXPR` past the end of its table, so whether it checks depends on the game
   code there. Always skipping the check is a deliberate simplification.)
3. If `chosen` is none, find the lowest-priority voice exactly as `SOUX9`:
   `x = 0, a = pr[0]; if (!(pr[0] < pr[1])) { x = 1; a = pr[1]; } if (!(a < pr[2])) x = 2;`
   (ties go to the higher voice, so the first effect on an idle chip plays on
   voice 3).
4. If `e.Priority < pr[x]`, drop it.
5. Else set voice `x`: Priority = e.Priority; SustainRelease = step override
   or e's; Counter = e.Frames; FrequencyChange; Control; Frequency = step
   override or e's; AttackDecay; VolumeMask; Flag = `(index(e) + 1) | 0x80`.

`Tick()` (= `SOINT`), once per frame, voices `v` = 2 down to 0:
- Flag 0: skip.
- Flag bit 7 (new): write 0 to registers 0-6; reg 4 = Control; reg 5 =
  AttackDecay; reg 6 = SustainRelease; then **frequency write** with add 0;
  then Flag &= 0x7F; next voice.
- Else, if FrequencyChange ≠ 0, **frequency write** with add =
  FrequencyChange. Then Priority −= 1, and if it reaches 0 set it to 1.
  Counter −= 1; at 0: reg 4 = Control & 0xFE (gate off), Flag = 0,
  Priority = 0, next voice. Else if `(Counter & VolumeMask) == 0`:
  SustainRelease −= 16; reg 6 = SustainRelease.
- **Frequency write:** Frequency = (Frequency + add) & 0xFF; reg 1 =
  Frequency >> 2; reg 0 = (Frequency << 6) & 0xFF; reg 3 = PulseWidth.
- After the three voices: PulseWidth ^= 4 (2 ↔ 6).
- Then count down the delayed steps; start each that reaches 0 (it is
  processed as new on the next tick, as `HYPNOISE`'s `DELAY` does).
- The engine writes to the `SidChip` it is given (`SidMachine.Chip`).

`Flush()` (= `SOFLUSH`): every voice's Counter = 1, and the delayed steps
are dropped (a simplification: on the C64 `DELAY` waits whatever plays).
Volume, pan and pitch from `ISound.Play` are ignored for SID effects.

Tests, each traced by hand from the assembly:
- the first beep on an idle engine plays on voice 3 (regs `$0E-$14`); the
  next different effect on voice 2; the next on voice 1;
- beep frame by frame: tick 1 writes control 0x11, AD 0x00, SR 0xF0,
  freq hi 0x3C, freq lo 0x00, PW hi 2; ticks 2-5 write no registers;
  tick 6 writes control 0x10 and frees the voice;
- `sfxplas`: frequency falls by 2 per tick; SR drops by 16 when
  `Counter & 3 == 0`;
- the same even-priority effect twice reuses its voice;
- a low-priority effect is dropped while all three voices hold higher ones;
- `Hyperspace`: the layered `sfxhyp1` starts on the tick after the first two;
- PulseWidth alternates 2, 6;
- `Flush` then `Tick` gates every playing voice off.

**3.3 Songs on a shared machine.** Change `SidSongPlayer` to play on a
`SidMachine` it is given:
`internal SidSongPlayer(SidMachine machine, PsidFile tune, int song, bool repeat)`.
Its quiet detection stays. When a song that does not repeat ends, the player
calls `machine.StopMusic()`, so the chip goes back to effects. Make the class
`internal` (only `SidSound` will use it). Move `SidSongPlayerTests` to the new
constructor and add: a finished song leaves `machine.IsPlaying` false.

**3.4 SID sound.** `SidSound.cs` (public, an `IMusicSource`): one
`SidMachine` for the whole session, a `SidEffectEngine` on its chip, and a
lock.
- It is built from an `IAssetLocator`: its `.sid` music entries, and its `Sfx`
  entries whose file is a `.sidfx` (each key is the sound of that name in that
  file; a missing name throws `SharpKindException` naming the key and file).
- `machine.IdleFrame = engine.Tick`, so effects run only when no music plays
  (C64 Elite's default, `MUSILLY` = 0, `elite-source.asm` around line 44102).
- `PlayEffect(key)` calls `engine.Trigger`. Volume, pan and pitch from
  `ISound.Play` are ignored.
- `PlayMusic(key, repeat)`: `MusicPath.Parse`, read the `PsidFile` (cache it
  per file), drop the engine's delayed steps, start a `SidSongPlayer`.
- `StopMusic()`: `engine.Flush()`, then `machine.StopMusic()` (the C64's
  `stopat`: `SOFLUSH`, then the chip silenced at volume 15).
- `Render(Span<float>)` renders the player while a song plays, else the
  machine (effects only).
- `HandlesEffect(key)` and `HandlesMusic(key)` let the backends route.
- `Dispose` does nothing (there are no unmanaged resources).

Tests with a fixture `.sidfx` and `SidFixtures.BuildTune()`: an effect is
non-silent; a song is non-silent; an effect during a song is not heard (the
song's output does not change); `StopMusic` then an effect plays the effect;
the end of a song that does not repeat lets effects play again; an unknown
sound name throws.

**3.5 SoftwareSound routes SID.** When the locator has any `.sid` music or
`.sidfx` effect, `SoftwareSound` owns one `SidSound`:
- `.sidfx` entries are not decoded into the sample cache; `Play(sfx, ...)` for
  them goes to `SidSound.PlayEffect`.
- `.sid` music goes to `SidSound.PlayMusic`. Playing other music stops SID
  music, and playing SID music stops other music.
- `Render` adds the output of `SidSound` after the music.

Remove `SidMusicStream` and the `.sid` branch of `MusicSources.Open` (a `.sid`
there is then an unsupported format). Keep `SoftwareSoundSidTests` passing,
and add: a SID effect is non-silent; `StopMusic` then an effect plays the
effect.

**3.6 SDLSound routes SID.** When the locator has any SID entry, `SDLSound`
creates one `SidSound` and a second `MusicOutput` that plays it for the whole
session. `.sid` music and `.sidfx` effects go to it, and `.sidfx` files are
not loaded with `MIX_LoadAudio`. `.mid` music keeps its own `MusicOutput`.
Playing either kind of music stops the other. → verify: build; run Bubble
Bobble on the Hardware backend and hear the title tune.

**3.7 Real tunes (manual, with the maintainer).** An explicit test
(`[Fact(Explicit = true)]`) renders 30 s of each song of both shipped `.sid`
files (paths from the repo root) to WAV files in the temp folder. Bubble
Bobble's oracle tests already prove the CPU, so the first plan's `vsid`
register-dump comparison is optional. Do it only if a tune sounds wrong
(`vsid.exe` is beside `x64sc.exe`, see `.claude/skills/vice-drive/SKILL.md`;
`-sounddev dump -soundarg <file>`; add a write hook on `C64Bus` and compare
the first 500 frames). Record what was found in `docs/decisions.md`.

**Check in with the maintainer:** they listen to Elite's two songs and say
which is the theme and which is the Blue Danube. Bubble Bobble's songs are
already mapped (bb-port-plan step 8).

---

## Phase 4 - Wire audio per rendition

**4.1 Elite routing.** In `src/elite/libs/EliteSharpLib/Renditions/RenditionAssets.cs`
take `SfxPaths`, `MusicPaths` and `SoundFontPaths` from `_rendition`.
`git mv` `EliteSharpLib/Assets/SFX/*.wav`, `Assets/Music/*.mid` and
`Assets/SoundFonts/TimGM6mb.sf2` into `EliteSharp.Renditions.SixteenBit/Assets/`
(same subfolders). Move the `Sfx`, `Music` and `SoundFonts` sections from the
game manifest to the 16-bit rendition manifest. Make sure the rendition
csproj copies the new folders (follow how it copies `Fonts`), and drop the
now-empty `.wav` exclusion from `EliteSharpLib.csproj`. → verify: the 16-bit
tier sounds exactly as after Phase 2.

**4.2 Extract the C64 effects.** Write `tools/elite/export-c64-sfx.py`
(follow the style of `tools/bb/`). Argument: the path to the C64
`elite-source.asm`. It:
- reads the 16 `EQUB` values after each of the labels `.SFXPR .SFXCNT .SFXFQ
  .SFXCR .SFXATK .SFXSUS .SFXFRCH .SFXVCH` (decimal, `$hex` and `%binary`),
  and the effect names from the `; Sound N = sfxname` comments;
- converts `SFXFRCH` to signed;
- writes `Effects` for all 16 and this hand-written `Sounds` map, each line
  commented in the script with the C64 routine it copies:

| Key (`SoundEffect`) | Steps | C64 source |
|---|---|---|
| Launch | sfxwhosh | `LAUN` |
| Missile | sfxwhosh | `SFRMIS` / `FRMIS` |
| Crash | sfxexpl | `EXNO3` (collision `MA63`, shield hit `OO3`) |
| Gameover | sfxexpl | `EXNO3` from `DEATH` |
| Explode | sfxexpl, Frequency 81, SustainRelease 0xF3 | `EXNO2`, nearest range |
| HitEnemy | sfxhit, Frequency 208, SustainRelease 0xF3 | `EXNO`, nearest range |
| Pulse | sfxplas | `MA68` (picks plas/mlas/blas/alas by laser type; the port has one key) |
| Ecm | sfxecm | `ECMOF` stops it with `NOISEOFF`; not ported |
| Hyperspace | sfxhyp1 (240, 0xF5); sfxwhosh; sfxhyp1 Layer, DelayFrames 1 | `HYPNOISE` |
| IncomingFire1 | sfxelas; sfxelas2 | `TA3` plays both on every hit |
| IncomingFire2 | sfxelas; sfxelas2 | `TA3` (the 1/2 split by shield state is TNK's, not the C64's) |
| Beep | sfxbeep | `BEEP` |
| Boop | sfxboop | `MA4`, `HME6`, `WA1` |
| Dock | (empty) | no docking sound on the C64 |

Before writing the table in, check each claim against the assembly (grep
`LDY #sfx`, `JSR EXNO`, `JSR BEEP`, `JSR HYPNOISE`). Output:
`src/elite/libs/EliteSharp.Renditions.EightBit/Assets/SFX/elite.sidfx`,
committed. Record in its header comment (JSON has none, so in the script and
in `docs/decisions.md`) where the data came from. → verify: running the
script twice gives identical output; `SidEffectFile` loads it.

**4.3 Elite 8-bit manifest.** Add to
`EliteSharp.Renditions.EightBit/Assets/AssetManifest.json`:
`"Sfx"`: every `SoundEffect` name → `"elite.sidfx"`, and
`"Music": { "EliteTheme": "elite.sid#1", "BlueDanube": "elite.sid#2" }` (swap
if 3.7 found them the other way round). Tests in `EliteSharpLib.Tests`
(use `TestAssets.Locator(rendition)`): for each rendition, every
`SoundEffect` and `MusicType` name resolves to an existing file (after
`MusicPath.Parse`), and in the 8-bit rendition every `SoundEffect` name is a
sound in `elite.sidfx`. → verify by ear against C64 Elite in VICE (disk
images in the C64 repo's `5-compiled-game-disks`): laser, hit, explosion,
beep, boop, ECM, hyperspace.

**4.4 Bubble Bobble.** Add `"Music": { "Theme": "bubble-bobble.sid#8" }` to
`BubbleBobbleSharp.Renditions.EightBit/Assets/AssetManifest.json`. In
`BubbleBobbleMain`'s constructor, after `ShowLevel(FirstLevel)`, play it when
`AudioOptions.MusicOn`: `Sound.Play("Theme", repeat: true)`. Test with
`FakeSound`: music plays with MusicOn, not with MusicOff. This is a stop-gap
until the front end (bb-port-plan phase 9) owns music.

**4.5 Run it.** Elite 8-bit and 16-bit on both backends (`run-elite` skill);
Bubble Bobble on both backends. → verify: the right tune and effects for each
tier (SID effects on 8-bit, `.wav` on 16-bit); on 8-bit, effects are silent
while the title or docking music plays and return when it stops; no audio
glitches; CPU use of the audio thread is modest.

**Check in with the maintainer.**

---

## Phase 5 - Docs

- `docs/decisions.md`: a dated entry (2026-09-26): own audio stack, why each
  package went, the tier-to-format table, the clean-room rule, the SID
  simplifications (combined waveforms by AND, linear 6581 cutoff, no ADSR
  delay bug, no CIA timing), and that Elite's 8-bit effects are extracted
  from the C64 source by `tools/elite/export-c64-sfx.py`, with the
  `SoundEffect` → C64 routine table from 4.2.
- `docs/backlog-roadmap.md` and `docs/work-items/audio-own-stack.md`: remove
  the item and its file when the plan is complete. Add follow-ups as
  new work items: per-laser sounds
  (`sfxblas`/`sfxalas`/`sfxmlas` need game-side keys), distance-scaled
  explosion volume (`EXNO`/`EXNO2` sustain 11-15), `NOISEOFF` when the ECM
  stops, the "sounds during music" toggle (`MUSILLY`), 8580 model and
  CIA-timed tunes if ever needed.
- `docs/reference-sources.md`: add the C64 Elite source as the reference for
  the 8-bit tier's audio.
- `docs/bb-port-plan.md` phase 8: the SID emulator has arrived; port the
  player rather than recording `.wav`. The Elite effect engine
  (`SidEffectEngine`) shows the shape, but Bubble Bobble's `sound.s` is its
  own engine and gets its own port.
- `docs/asset-structure.md`: fix "The `.ogg` files never move" and the audio
  folder layout now that all audio sits in the renditions.
- `docs/elite-readme.md`, `docs/bb-readme.md`: one line each on where the
  8-bit tier's music and effects come from.

## Verification (end to end)

1. `dotnet build TheSharpKind.slnx` with zero warnings.
2. Every `*.Tests.exe` green; counts up by the new tests only.
3. `grep -rn "NVorbis\|MeltySynth\|OggVorbisEncoder" src Directory.Packages.props`
   (excluding `bin/obj`) is empty.
4. By ear, with the maintainer: Elite 16-bit music matches the old MeltySynth
   output closely enough; Elite 8-bit and Bubble Bobble play their SID tunes
   recognisably against `vsid`; Elite 8-bit effects match C64 Elite under
   VICE; Elite 16-bit and Stunt Car Racer effects sound as before, including
   SCR's pitched engine loop.

## Out of scope

- Bubble Bobble sound effects through the SID (bb-port-plan phase 8).
- The Elite 8-bit follow-ups listed in Phase 5 (per-laser sounds,
  distance-scaled volume, `NOISEOFF`, `MUSILLY`).
- Reverb, chorus, filters and LFOs in the SoundFont player.
- RSID files, CIA-timed PSIDs, 8580 model, digi playback.
