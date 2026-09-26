# Own audio stack in SharpKind.Audio: WAV, MIDI/SF2 and C64 SID

## Context

`SharpKind.Audio` depends on three third-party packages:

- **NVorbis** decodes Elite's 14 `.ogg` effects (`SoftwareSound`).
- **MeltySynth** plays Elite's two `.mid` tunes through `TimGM6mb.sf2`
  (`SoftwareSound`, and the predecode in `SharpKind.SDL/SDLSound.cs`).
- **OggVorbisEncoder** only builds `.ogg` test fixtures.

The maintainer wants one audio format per visual tier (roadmap "Audio per
rendition", `docs/backlog-roadmap.md`, and `docs/decisions.md` 2026-09-18):

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
  engine, 3.15 and 4.2) is how this repo always works.
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
IMusicStream.cs  MusicStreams.cs  MusicPath.cs     (public: SDLSound uses them)
Wav/WavDecoder.cs  Wav/SampleMusicStream.cs
Midi/MidiFile.cs  Midi/MidiEvent.cs  Midi/MidiSequencer.cs  Midi/MidiMusicStream.cs
Midi/Synthesizer.cs  Midi/MidiChannel.cs  Midi/Voice.cs  Midi/VolumeEnvelope.cs
SoundFonts/SoundFont.cs  SoundFonts/SoundFontSample.cs  SoundFonts/Zone.cs
SoundFonts/Preset.cs  SoundFonts/Instrument.cs  SoundFonts/GeneratorType.cs
SoundFonts/VoiceParameters.cs
Sid/PsidFile.cs  Sid/C64Bus.cs  Sid/Cpu6502.cs  Sid/Cpu6502.Opcodes.cs
Sid/SidOscillator.cs  Sid/SidEnvelope.cs  Sid/SidFilter.cs  Sid/SidChip.cs
Sid/SidPlayer.cs  Sid/SidMachine.cs
Sid/SidEffectFile.cs  Sid/SidEffectEngine.cs
SidSound.cs                     (public: SoftwareSound and SDLSound share it)
```

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
- `.mid` goes through `MusicStreams.Open` (SID arrives in 3.16). Create an
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

Facts both `.sid` files share (checked): PSID v2, speed 0 (50 Hz vertical
blank), PAL, 6581, no data inside `$D000-$DFFF`.
Bubble Bobble: load `$EA40-$FE87`, init `$EE50`, play `$F53C`, 12 songs,
start 8. Elite: load `$B46F-$CCB7`, init `$B46F`, play `$B4B4`, 2 songs,
start 1.

**3.1 PSID reader.** `Sid/PsidFile.cs`: `Read(byte[])`. Big-endian header:
magic `PSID` or `RSID` (RSID throws "not supported"), version, dataOffset
(0x06), loadAddress (0x08), initAddress (0x0A), playAddress (0x0C), songs
(0x0E), startSong (0x10), speed (0x12, 32 bits), name/author/released (0x16,
0x36, 0x56, 32 bytes each, Latin-1, NUL-trimmed), flags (0x76, v2+). If
loadAddress is 0, the first two data bytes are the little-endian load
address. Throw `SharpKindException` when playAddress is 0 or the speed bit
for any song is set (CIA timing): neither shipped file needs them.
Tests with an `AudioFixtures.BuildPsid(...)` builder: fields round-trip;
load address from data; RSID and play = 0 throw.

**3.2 Bus.** `Sid/C64Bus.cs`: 64 KB RAM. `Read(ushort)`/`Write(ushort, byte)`.
Addresses `$D400-$D7FF` go to the SID (register = address & 0x1F). Every
other address is plain RAM (no ROMs, no other I/O; neither tune needs them).
Tests: RAM round-trip; `$D418` write reaches the SID stub; `$D438` mirrors
`$D418`.

**3.3 CPU skeleton.** `Sid/Cpu6502.cs`: A, X, Y, S, PC, flags N V - B D I Z C.
`Step()` executes one instruction and returns its cycles. Implement the
addressing modes (imm, zp, zp+X, zp+Y, abs, abs+X, abs+Y, (ind,X), (ind),Y,
indirect JMP with the page-wrap bug, relative) and the load/store/transfer
group: LDA LDX LDY STA STX STY TAX TAY TXA TYA TSX TXS, plus NOP. Put the
opcode table in `Cpu6502.Opcodes.cs` (256 entries: operation, mode, base
cycles, page-cross penalty). An undocumented opcode throws
`SharpKindException` with the opcode and PC. Tests: each addressing mode, N/Z
flags, zero-page wrap for zp+X, cycle count with page cross.

**3.4 CPU arithmetic.** ADC SBC (binary and decimal mode; in decimal mode set
C correctly, and N/Z/V from the binary result as the NMOS part does), AND ORA
EOR, CMP CPX CPY, BIT, INC DEC INX INY DEX DEY. Tests: the classic ADC
overflow cases (0x50+0x50, 0xD0+0x90), SBC borrow, BCD 0x09+0x01 = 0x10,
BCD 0x99+0x01 = 0x00 with C.

**3.5 CPU control.** ASL LSR ROL ROR (A and memory), all branches (+1 cycle
taken, +1 more on page cross), JMP JSR RTS RTI BRK, PHA PLA PHP PLP (B and
bit 5 set when pushed), CLC SEC CLI SEI CLD SED CLV. Stack is page 1 and
wraps. Tests: JSR/RTS round trip returns to PC+3; PHP pushes 0x30 bits;
branch cycle counts.

**3.6 Subroutine runner.** `Cpu6502.Call(ushort address, byte a, int maxCycles)`:
push the return address `0xFFFF` (so RTS lands on `0x0000`) — i.e. push hi
`0xFF`, lo `0xFF`; set PC; `Step()` until PC == 0x0000; return cycles used.
Throw `SharpKindException` past `maxCycles` or on BRK. Tests: a tiny routine
`LDA #$05 : STA $D418 : RTS` writes 5 to the SID register; an endless
`JMP *` throws.

**3.7 Oscillator.** `Sid/SidOscillator.cs`, clocked once per CPU cycle
(PAL clock 985248 Hz). Registers: freq (16-bit), pulse width (12-bit),
control (gate 0x01, sync 0x02, ring 0x04, test 0x08, tri 0x10, saw 0x20,
pulse 0x40, noise 0x80).
- accumulator: 24-bit, `acc = (acc + freq) & 0xFFFFFF`; test bit holds it at 0.
- saw = `acc >> 12`.
- tri = `((acc ^ (msb ? 0xFFFFFF : 0)) >> 11) & 0xFFF`, where msb is bit 23,
  XORed with the **ring source's** bit 23 when ring is set.
- pulse = `(acc >> 12) >= pw ? 0xFFF : 0` (test bit forces 0xFFF).
- noise: 23-bit LFSR, initial `0x7FFFF8`, clocked when acc bit 19 rises:
  `bit0 = bit22 ^ bit17`, shift left. Output = bits 22,20,16,13,11,7,4,2 as an
  8-bit value, placed in bits 11-4 of the 12-bit output.
- several waveform bits set: AND the selected outputs (an approximation; the
  real chip differs). No waveform: 0.
- sync: when the **sync source's** bit 23 rises this cycle, reset acc to 0.
- source wiring: voice 1 ← voice 3, voice 2 ← voice 1, voice 3 ← voice 2.
Tests: saw period = 2^24/freq cycles; pulse duty follows pw; test bit holds
0; noise changes only on bit-19 rises; sync resets.

**3.8 Envelope.** `Sid/SidEnvelope.cs`, clocked once per cycle. 8-bit level.
Rate periods in cycles, index 0-15:
`9, 32, 63, 95, 149, 220, 267, 313, 392, 977, 1954, 3126, 3907, 11720, 19532, 31251`.
A rate counter counts cycles to the period of the current stage, then:
attack adds 1 (at 0xFF go to decay); decay/release subtract 1 only every
Nth period, where N = 1 above level 93, 2 at ≤93, 4 at ≤54, 8 at ≤26,
16 at ≤14, 30 at ≤6, and they stop at 0 (release) or at sustain
(`S * 0x11`, decay). Gate 0→1 starts attack; 1→0 starts release. Ignore the
ADSR "delay bug". Tests: attack 0 reaches 0xFF in ≈ 255×9 cycles; sustain
level holds; release reaches 0; re-gate from mid-release goes to attack.

**3.9 Filter.** `Sid/SidFilter.cs`: Chamberlin state-variable filter,
stepped once per cycle (stable at this rate).
- cutoff register 11 bits: `$D415` bits 0-2 low, `$D416` high 8 bits.
- 6581 cutoff in Hz: a table/formula in one place, first cut
  `fc = 30 + cutoff * 5.8` (tune by ear in 3.12).
- `f = 2 · sin(π · fc / 985248)`; `q = 1 / (0.707 + res/15 · 1.5)`.
- per step: `lp += f·bp; hp = in − lp − q·bp; bp += f·hp`.
- `$D417`: bits 0-2 route voices 1-3 through the filter, bits 4-7 resonance.
- `$D418`: bit 4 LP, 5 BP, 6 HP (sum of the chosen outputs), bit 7 disconnects
  voice 3 when it is not filtered, bits 0-3 master volume.
Tests: LP with low cutoff attenuates a high saw; HP removes DC; routing bits
choose which voices pass through.

**3.10 Chip.** `Sid/SidChip.cs`: three oscillator+envelope voices, filter,
`Write(int reg, byte value)`, `Read(int reg)` (`$1B` = voice 3 wave >> 4,
`$1C` = voice 3 envelope, others 0). Voice output
`(wave − 0x800) · level`; sum voices (filtered and unfiltered), apply
volume/15, scale to about ±1 (divide by `3 · 0x800 · 0xFF`), then a one-pole
DC-blocking high-pass at about 16 Hz.
`Render(Span<float> stereo)`: for each 44.1 kHz frame, clock
`985248 / 44100` cycles (carry the fraction), **average** the per-cycle output
over those cycles (box decimation), write it to L and R. Register writes take
effect at once and in order, so writing control `0x00` then `0x11` in one
frame restarts the attack. Tests: silence with volume 0; a gated saw at freq
0x1CD6 (≈440 Hz) gives ≈440 Hz zero crossings at 44.1 kHz; rendering 1 s
takes the right number of cycles; gate off-then-on restarts the attack.

**3.11 Player.** `Sid/SidPlayer.cs`: takes a `PsidFile`, a song number and
the `SidChip` to write to (its `C64Bus` maps `$D400-$D7FF` onto that chip).
Load the data at loadAddress in a zeroed bus;
`Cpu.Call(init, a: song − 1, maxCycles: 2_000_000)`. `PlayFrame()` runs
`Cpu.Call(play, a: 0, maxCycles: 100_000)`. The player does not render; the
machine in 3.12 does. Tests: a hand-built PSID whose init stores A in RAM
proves `song − 1` arrives; its play routine's SID writes reach the chip.

**3.12 Machine.** `Sid/SidMachine.cs`: one `SidChip` shared by music and
effects, as on the C64. PAL frames of 19656 cycles (312 × 63, ≈50.12 Hz).
At the start of each frame: if music is playing, `player.PlayFrame()`;
**otherwise** `effects.Tick()` (3.15). This matches C64 Elite's default: the
interrupt handler runs `SOINT` only when no music plays (`MUSILLY` = 0,
`elite-source.asm` around line 44102). Then clock the chip for the frame.
`Render(Span<float>)` carries the partial frame across calls.
- `StartMusic(PsidFile, int song)`: zero all SID registers, start a
  `SidPlayer`.
- `StopMusic()`: as the C64's `stopat` — flush the effect engine (every
  voice counter set to 1, as `SOFLUSH`), zero SID registers `$00-$18`, write
  `$D418 = 0x0F` (volume 15, no filter).
- On construction, write `$D418 = 0x0F`.
Tests: music frames call play and not the engine; after `StopMusic` the next
tick silences any effect voice; volume register is 15 at start.

**3.13 Music paths.** `MusicPath.cs` (public): `Parse(string path)` →
`(string File, int? Song)`. A trailing `#<n>` selects the song, 1-based;
without it the file's start song plays. Tests: `elite.sid#2` → song 2; no
suffix → null; `#0` and a song above the file's count throw
`SharpKindException` when the tune is started.

**3.14 Effect file.** `Sid/SidEffectFile.cs` reads a `.sidfx` file (JSON):

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
`AssetLocator` does. Tests: the example round-trips; each invalid case throws.

**3.15 Effect engine.** `Sid/SidEffectEngine.cs` is a port of C64 Elite's
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
   equals `index(e) + 1`, `chosen` = that voice.
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

`Flush()` (= `SOFLUSH`): every voice's Counter = 1.
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

**3.16 SID sound for both backends.** `SidSound.cs` (public), used by both
backends. Built from the `.sid` music entries and `.sidfx` effect entries of
an `IAssetLocator`; owns one `SidMachine` and a lock.
`PlayEffect(key)` → `engine.Trigger(key)` (the sound with the same name as
the manifest key, in that entry's `.sidfx` file). `PlayMusic(key)` →
`MusicPath.Parse`, then `machine.StartMusic`. `StopMusic()`.
`Render(Span<float>)`. `Handles…` queries let the backends route.
- `SoftwareSound`: `.sidfx` effects and `.sid` music go to `SidSound`
  instead of the decoded-sample cache and `MusicStreams`; `Render` adds its
  output. Playing non-SID music stops SID music and the reverse.
- `SDLSound`: when the locator has any SID entry, create one extra track fed
  for the whole session by an `SDL_AudioStream` whose get-callback calls
  `SidSound.Render` (same pattern as 2.12). Route `Play`/`StopMusic` to it.
Tests (`SoftwareSound` with a fixture `.sidfx` and a fixture PSID): a SID
effect is non-silent; SID music is non-silent; `StopMusic` then an effect
plays the effect.

**3.17 Real tunes (manual, with the maintainer).** A console check in the test
project, `[Fact(Explicit = true)]`, that renders 30 s of each shipped `.sid`
(paths relative to the repo root) to WAV files in the temp folder, so the
maintainer can listen. Then, optionally, the ground truth: VICE's `vsid.exe`
(same folder as `x64sc.exe`, see `.claude/skills/vice-drive/SKILL.md`) with
`-sounddev dump -soundarg <file>` logs every SID register write. Compare the
first 500 frames of register writes with our player's (add a debug hook on
`C64Bus` writes). Differences mean a CPU bug. Record what was found in
`docs/decisions.md`.

**Check in with the maintainer:** they listen to both tunes against `vsid`,
and say which Elite song is the theme and which is the Blue Danube, and which
Bubble Bobble song plays in game (expected: start song 8).

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
if 3.17 found them the other way round). Tests in `EliteSharpLib.Tests`
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
- `docs/backlog-roadmap.md` "Audio per rendition": tick the done items (the
  manifest move, the synth voice, the chip choice: SID). Correct the claim
  that 8-bit effects would be SN76489 data from the BBC `.SFX` table: they
  are SID data from the C64 tables. Add follow-ups: per-laser sounds
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
