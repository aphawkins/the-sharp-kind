// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SDL;

namespace SharpKind.SDL.Tests;

public class SDLHelperTests
{
    // A case left out of the switch falls through to ConsoleKey.None, which is what these catch, one key at a time.
    public static TheoryData<SDL_Keycode, ConsoleKey> Letters => Pairs('A', 'Z', c => c.ToString());

    public static TheoryData<SDL_Keycode, ConsoleKey> Digits => Pairs('0', '9', c => "D" + c);

    public static TheoryData<SDL_Keycode, ConsoleKey> FunctionKeys
    {
        get
        {
            TheoryData<SDL_Keycode, ConsoleKey> data = [];
            for (int i = 1; i <= 12; i++)
            {
                data.Add(Enum.Parse<SDL_Keycode>("SDLK_F" + i), Enum.Parse<ConsoleKey>("F" + i));
            }

            return data;
        }
    }

    // Regression: SDLK_RIGHT was previously mapped to OemPeriod, so the physical Right Arrow never produced ConsoleKey.RightArrow.
    // Elite's views masked this by checking both, but anything checking RightArrow alone (e.g. SCR's steering) silently never saw it.
    [Theory]
    [InlineData(SDL_Keycode.SDLK_LEFT, ConsoleKey.LeftArrow)]
    [InlineData(SDL_Keycode.SDLK_RIGHT, ConsoleKey.RightArrow)]
    [InlineData(SDL_Keycode.SDLK_UP, ConsoleKey.UpArrow)]
    [InlineData(SDL_Keycode.SDLK_DOWN, ConsoleKey.DownArrow)]
    [InlineData(SDL_Keycode.SDLK_COMMA, ConsoleKey.OemComma)]
    [InlineData(SDL_Keycode.SDLK_PERIOD, ConsoleKey.OemPeriod)]
    [InlineData(SDL_Keycode.SDLK_SPACE, ConsoleKey.Spacebar)]
    [InlineData(SDL_Keycode.SDLK_BACKSPACE, ConsoleKey.Backspace)]
    [InlineData(SDL_Keycode.SDLK_TAB, ConsoleKey.Tab)]
    [InlineData(SDL_Keycode.SDLK_RETURN, ConsoleKey.Enter)]
    [InlineData(SDL_Keycode.SDLK_ESCAPE, ConsoleKey.Escape)]
    [InlineData(SDL_Keycode.SDLK_SLASH, ConsoleKey.Oem2)]
    [MemberData(nameof(Letters))]
    [MemberData(nameof(Digits))]
    [MemberData(nameof(FunctionKeys))]
    public void KeyConverterMapsPhysicalKeyToExpectedConsoleKey(SDL_Keycode sdlKey, ConsoleKey expected)
    {
        (ConsoleKey key, ConsoleModifiers modifiers) = SDLHelper.KeyConverter(sdlKey);

        Assert.Equal(expected, key);
        Assert.Equal(ConsoleModifiers.None, modifiers);
    }

    // Both control keys are the same modifier and no key of their own: a
    // modifier that also arrived as a key would be typed into a name.
    [Theory]
    [InlineData(SDL_Keycode.SDLK_LCTRL)]
    [InlineData(SDL_Keycode.SDLK_RCTRL)]
    public void EitherControlKeyIsAModifierAndNotAKey(SDL_Keycode sdlKey)
    {
        (ConsoleKey key, ConsoleModifiers modifiers) = SDLHelper.KeyConverter(sdlKey);

        Assert.Equal(ConsoleKey.None, key);
        Assert.Equal(ConsoleModifiers.Control, modifiers);
    }

    [Fact]
    public void AKeyTheGameHasNoUseForIsNotAKeyAtAll()
    {
        (ConsoleKey key, ConsoleModifiers modifiers) = SDLHelper.KeyConverter(SDL_Keycode.SDLK_PRINTSCREEN);

        Assert.Equal(ConsoleKey.None, key);
        Assert.Equal(ConsoleModifiers.None, modifiers);
    }

    [Fact]
    public void AFailedSdlCallIsReportedWithTheMethodThatFailed()
    {
        SDLException exception = Assert.Throws<SDLException>(() => SDLHelper.Throw("SDL_CreateWindow"));

        Assert.Contains("SDL_CreateWindow", exception.Message, StringComparison.Ordinal);
    }

    private static TheoryData<SDL_Keycode, ConsoleKey> Pairs(char first, char last, Func<char, string> consoleKeyName)
    {
        TheoryData<SDL_Keycode, ConsoleKey> data = [];
        for (char c = first; c <= last; c++)
        {
            data.Add(Enum.Parse<SDL_Keycode>("SDLK_" + c), Enum.Parse<ConsoleKey>(consoleKeyName(c)));
        }

        return data;
    }
}
