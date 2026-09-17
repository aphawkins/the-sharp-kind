// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using Microsoft.Extensions.Logging;
using SharpKind.Input;

namespace SharpKind.SDL;

// A pad that "does nothing" is almost always a mapping question that can't be answered from the game's side; these messages log the device's own account.
internal static partial class SDLInputLogMessages
{
    [LoggerMessage(
        EventId = 100,
        Level = LogLevel.Information,
        Message = "Gamepad connected: '{Name}' (id {Which}), {Axes} axes, {Buttons} buttons, {Hats} hats, SDL mapping applied")]
    internal static partial void GamepadConnected(
        ILogger logger,
        string name,
        uint which,
        int axes,
        int buttons,
        int hats);

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "Joystick connected (no SDL mapping): '{Name}' (id {Which}), {Axes} axes, {Buttons} buttons, {Hats} hats")]
    internal static partial void JoystickConnected(
        ILogger logger,
        string name,
        uint which,
        int axes,
        int buttons,
        int hats);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Device disconnected: id {Which}")]
    internal static partial void DeviceDisconnected(ILogger logger, uint which);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Debug,
        Message = "Button {Index} {State} -> {Button} (id {Which})")]
    internal static partial void ButtonEvent(
        ILogger logger,
        int index,
        string state,
        GamepadButton button,
        uint which);

    [LoggerMessage(
        EventId = 104,
        Level = LogLevel.Debug,
        Message = "Axis {Index} = {Raw} ({Value:0.00}) -> {Axis} (id {Which})")]
    internal static partial void AxisEvent(
        ILogger logger,
        int index,
        short raw,
        float value,
        GamepadAxis? axis,
        uint which);

    [LoggerMessage(EventId = 105, Level = LogLevel.Debug, Message = "Hat {Value} -> x {X:0.00}, y {Y:0.00} (id {Which})")]
    internal static partial void HatEvent(ILogger logger, byte value, float x, float y, uint which);
}
