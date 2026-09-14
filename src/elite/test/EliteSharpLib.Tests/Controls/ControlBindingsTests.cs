// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Controls;
using SharpKind.Abstraction.Controls;
using SharpKind.Input;

namespace EliteSharpLib.Tests.Controls;

public class ControlBindingsTests
{
    // The file is only as good as its vocabulary: an action nothing binds
    // cannot be performed at all, so a new one must arrive with a binding.
    [Fact]
    internal void EveryActionIsBoundSomewhere()
    {
        ControlBindings bindings = EliteControlDefaults.Create();

        IEnumerable<string> bound = bindings.Keyboard.Keys
            .Concat(bindings.Controllers.SelectMany(c => c.Buttons.Keys))
            .Concat(bindings.Controllers.SelectMany(c => c.Triggers.Keys));

        HashSet<EliteAction> actions = [.. bound
            .Select(name => Enum.Parse<EliteAction>(name, ignoreCase: true))];

        List<EliteAction> unbound = [.. Enum.GetValues<EliteAction>()
            .Where(action => action != EliteAction.None && !actions.Contains(action))];

        // Galactic hyperspace is Ctrl-H, which is a modifier and not a
        // binding, so nothing binds it.
        Assert.Equal([EliteAction.GalacticHyperspace], unbound);
    }

    // The library takes an enum's zero value to mean "nothing", so both of
    // Elite's must reserve it. Without that, whichever control happened to
    // be declared first would be dropped from every file as though the game
    // had never heard of it - which is exactly what happened to Roll.
    [Fact]
    internal void BothEnumsReserveZero()
    {
        Assert.Equal(0, (int)EliteAction.None);
        Assert.Equal(0, (int)EliteAxis.None);
        Assert.NotEqual(0, (int)EliteAxis.Roll);
    }

    [Fact]
    internal void TheDefaultsSurviveTheirOwnRepair()
        => Assert.False(Repair(EliteControlDefaults.Create()));

    // Empty is what a missing file binds to, and the defaults are what it
    // has to become - otherwise every control is dead on a first run.
    [Fact]
    internal void AnEmptyFileTakesTheKeyboardFromTheDefaults()
    {
        ControlBindings bindings = new();

        Assert.True(Repair(bindings));
        Assert.NotEmpty(bindings.Keyboard);
    }

    // One bad line costs that line, not the whole file.
    [Fact]
    internal void AnUnknownActionIsDroppedAndTheRestKept()
    {
        ControlBindings bindings = new();
        bindings.Keyboard[nameof(EliteAction.FireLaser)] = [nameof(ConsoleKey.A)];
        bindings.Keyboard["PolishTheHull"] = [nameof(ConsoleKey.Z)];

        Assert.True(Repair(bindings));
        Assert.True(bindings.Keyboard.ContainsKey(nameof(EliteAction.FireLaser)));
        Assert.False(bindings.Keyboard.ContainsKey("PolishTheHull"));
    }

    [Fact]
    internal void AnUnknownKeyIsDroppedAndTheRestKept()
    {
        ControlBindings bindings = new();
        bindings.Keyboard[nameof(EliteAction.FireLaser)] = [nameof(ConsoleKey.A), "NotAKey"];

        Assert.True(Repair(bindings));
        Assert.Equal([nameof(ConsoleKey.A)], bindings.Keyboard[nameof(EliteAction.FireLaser)]);
    }

    [Fact]
    internal void AControllerEntryNamingNoDeviceIsDropped()
    {
        ControlBindings bindings = new();
        bindings.Keyboard[nameof(EliteAction.FireLaser)] = [nameof(ConsoleKey.A)];
        bindings.Controllers.Add(new ControllerBindings { Device = " " });

        Assert.True(Repair(bindings));
        Assert.Empty(bindings.Controllers);
    }

    [Fact]
    internal void AnUnknownButtonIsDropped()
    {
        ControllerBindings controller = new() { Device = "a stick" };
        controller.Buttons[nameof(EliteAction.FireLaser)] = "NotAButton";
        controller.Buttons[nameof(EliteAction.Ecm)] = nameof(GamepadButton.A);

        Assert.True(controller.Repair<EliteAction, EliteAxis>());
        Assert.False(controller.Buttons.ContainsKey(nameof(EliteAction.FireLaser)));
        Assert.True(controller.Buttons.ContainsKey(nameof(EliteAction.Ecm)));
    }

    // A stick axis named as a trigger would fire the lasers whenever the
    // stick was pushed over, so only the real triggers are allowed.
    [Theory]
    [InlineData(nameof(GamepadAxis.LeftX), false)]
    [InlineData(nameof(GamepadAxis.Throttle), false)]
    [InlineData(nameof(GamepadAxis.LeftTrigger), true)]
    [InlineData(nameof(GamepadAxis.RightTrigger), true)]
    internal void OnlyARealTriggerMayBeBoundAsOne(string axis, bool kept)
    {
        ControllerBindings controller = new() { Device = "a stick" };
        controller.Triggers[nameof(EliteAction.FireLaser)] = axis;

        _ = controller.Repair<EliteAction, EliteAxis>();

        Assert.Equal(kept, controller.Triggers.ContainsKey(nameof(EliteAction.FireLaser)));
    }

    [Fact]
    internal void AnUnknownAxisIsDropped()
    {
        ControllerBindings controller = new() { Device = "a stick" };
        controller.Axes[nameof(EliteAxis.Roll)] = new() { Axis = "NotAnAxis" };

        Assert.True(controller.Repair<EliteAction, EliteAxis>());
        Assert.Empty(controller.Axes);
    }

    // An axis the game does not have is dropped too, which is what keeps
    // the library honest about whose vocabulary it is validating.
    [Fact]
    internal void AnAxisTheGameDoesNotHaveIsDropped()
    {
        ControllerBindings controller = new() { Device = "a stick" };
        controller.Axes["Elevation"] = new() { Axis = nameof(GamepadAxis.LeftX) };

        Assert.True(controller.Repair<EliteAction, EliteAxis>());
        Assert.Empty(controller.Axes);
    }

    // No shipped controller entry may bind one control to two things, or a
    // single press would run two commands.
    [Fact]
    internal void NoShippedEntryBindsAButtonTwice()
    {
        foreach (ControllerBindings controller in EliteControlDefaults.Create().Controllers)
        {
            List<string> buttons = [.. controller.Buttons.Values];

            Assert.Equal(buttons.Count, buttons.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }
    }

    private static bool Repair(ControlBindings bindings)
        => bindings.Repair<EliteAction, EliteAxis>(EliteControlDefaults.Create());
}
