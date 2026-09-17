// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Assets;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Input;

namespace SharpKind.Fakes.Harness;

// Drives a game's Update()/Draw() against a real SoftwareGraphics with no SDL window; shared machinery behind both StuntCarRacerSharpLib and EliteSharpLib's headless harnesses.
public abstract class HeadlessGameHarnessBase<TState> : IDisposable
{
    private FastBitmap? _lastFrame;

    protected HeadlessGameHarnessBase(int width, int height, IAssetLocator assetLocator)
        => Graphics = SoftwareGraphics.Create(width, height, b => _lastFrame = b, assetLocator);

    public FakeKeyboard Keyboard { get; protected set; } = null!;

    public int Tick { get; private set; }

    public abstract TState State { get; }

    protected SoftwareGraphics Graphics { get; }

    // Single-tick taps are released after Update(), not before.
    public TState Step(IReadOnlyList<KeyScriptEvent> script)
    {
        ArgumentNullException.ThrowIfNull(script);

        List<(ConsoleKey Key, ConsoleModifiers Modifiers)>? taps = null;
        foreach (KeyScriptEvent scriptEvent in script)
        {
            if (scriptEvent.Tick != Tick)
            {
                continue;
            }

            switch (scriptEvent.Action)
            {
                case KeyScriptAction.Tap:
                    Keyboard.KeyDown(scriptEvent.Key, scriptEvent.Modifiers);
                    (taps ??= []).Add((scriptEvent.Key, scriptEvent.Modifiers));
                    break;

                case KeyScriptAction.Hold:
                    Keyboard.KeyDown(scriptEvent.Key, scriptEvent.Modifiers);
                    break;

                case KeyScriptAction.Release:
                    Keyboard.KeyUp(scriptEvent.Key, scriptEvent.Modifiers);
                    break;

                case KeyScriptAction.SaveFrame:
                    // No-op here: only KeyScriptPlayer (the real-app counterpart) acts on this; headless callers use SaveFrame directly.
                    break;
            }
        }

        UpdateGame();

        if (taps is not null)
        {
            foreach ((ConsoleKey key, ConsoleModifiers modifiers) in taps)
            {
                Keyboard.KeyUp(key, modifiers);
            }
        }

        Tick++;
        return State;
    }

    public TState Run(int ticks, IReadOnlyList<KeyScriptEvent> script)
    {
        TState state = State;
        for (int i = 0; i < ticks; i++)
        {
            state = Step(script);
        }

        return state;
    }

    // Includes screens and HUD.
    public void SaveFrame(string path) => BitmapWriter.Write(CaptureFrame(), path);

    // For a caller that measures the frame rather than looking at it, e.g. comparing against a committed reference.
    public FastBitmap CaptureFrame()
    {
        DrawGame();
        return _lastFrame ?? throw new InvalidOperationException("No frame has been rendered yet.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // One game tick: composes the whole frame (StuntCarRacerMain.Update) or just advances state where drawing is separate (EliteMain).
    protected abstract void UpdateGame();

    protected abstract void DrawGame();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Graphics.Dispose();
        }
    }
}
