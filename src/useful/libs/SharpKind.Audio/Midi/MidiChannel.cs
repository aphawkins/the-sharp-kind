// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.SoundFonts;

namespace SharpKind.Audio.Midi;

// The controller state of one of the 16 MIDI channels. The tenth (number 9) is General MIDI percussion.
internal sealed class MidiChannel(int number)
{
    private const int PercussionChannel = 9;
    private const int BendCentre = 8192;
    private const int NoParameter = 127;

    private int _volume = 100;
    private int _expression = 127;
    private int _pan = 64;
    private int _bend = BendCentre;
    private int _bendSemitones = 2;
    private int _bendCents;
    private int _parameterMsb = NoParameter;
    private int _parameterLsb = NoParameter;

    internal int Program { get; private set; }

    internal int Bank => number == PercussionChannel ? SoundFont.PercussionBank : BankSelect;

    internal bool Sustain { get; private set; }

    internal float Gain => Square(_volume / 127f) * Square(_expression / 127f);

    // -1 is full left, 1 full right.
    internal float Pan => (_pan - 64) / 64f;

    internal double BendSemitones => (_bend - BendCentre) / (double)BendCentre * (_bendSemitones + (_bendCents / 100.0));

    private int BankSelect { get; set; }

    internal void ProgramChange(int program) => Program = program;

    internal void PitchBend(int lsb, int msb) => _bend = lsb | (msb << 7);

    internal void ControlChange(int controller, int value)
    {
        switch (controller)
        {
            case 0:
                BankSelect = value;
                break;
            case 6 when IsBendRangeSelected():
                _bendSemitones = value;
                break;
            case 7:
                _volume = value;
                break;
            case 10:
                _pan = value;
                break;
            case 11:
                _expression = value;
                break;
            case 38 when IsBendRangeSelected():
                _bendCents = value;
                break;
            case 64:
                Sustain = value >= 64;
                break;
            case 100:
                _parameterLsb = value;
                break;
            case 101:
                _parameterMsb = value;
                break;
            case 121:
                ResetControllers();
                break;
        }
    }

    private static float Square(float value) => value * value;

    // Registered parameter 0,0 is the pitch-bend range.
    private bool IsBendRangeSelected() => _parameterMsb == 0 && _parameterLsb == 0;

    // Volume, pan, program and bank survive a reset (General MIDI RP-015).
    private void ResetControllers()
    {
        _expression = 127;
        _bend = BendCentre;
        Sustain = false;
        _parameterMsb = NoParameter;
        _parameterLsb = NoParameter;
    }
}
