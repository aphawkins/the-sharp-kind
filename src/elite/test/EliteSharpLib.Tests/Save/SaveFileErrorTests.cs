// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Text.Json.Nodes;
using EliteSharpLib.Save;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Save;

/// <summary>
/// A save is turned away for one of a dozen reasons, and which one is the
/// difference between a file a commander can repair and a file they cannot.
/// The screen used to say "Error Loading Commander!" whichever it was.
/// </summary>
// Serialised against other classes reading/writing ELITE_DEBUG_COMMANDER: it belongs to the process, and xUnit runs test classes in parallel.
[Collection("EnvironmentVariables")]
public class SaveFileErrorTests
{
    [Fact]
    public void AMissingFileSaysThereIsNoSuchCommander()
    {
        SaveFile saveFile = CreateSaveFile();

        Assert.False(saveFile.LoadCommander("NoSuchCommander"));
        Assert.Equal("No Such Commander", saveFile.LastLoadError);
    }

    [Fact]
    public void AFileThatIsNotJsonSaysItCouldNotBeRead()
    {
        SaveFile saveFile = CreateSaveFile();
        File.WriteAllText(saveFile.PathFor("Corrupt"), "{ not valid json");

        Assert.False(saveFile.LoadCommander("Corrupt"));
        Assert.Equal("Unreadable Commander File", saveFile.LastLoadError);
    }

    [Theory]
    [InlineData("fuel", 99, "Bad Fuel")]
    [InlineData("missiles", 9, "Bad Missiles")]
    [InlineData("cargoCapacity", 21, "Bad Cargo Bay")]
    [InlineData("score", -1, "Bad Score")]
    [InlineData("galaxyNumber", 8, "Bad Galaxy")]
    [InlineData("marketRandomiser", 256, "Bad Market")]
    [InlineData("version", 99, "Bad Version")]
    public void AFieldOutOfRangeIsNamed(string field, int value, string expected)
    {
        SaveFile saveFile = CreateSaveFile();
        saveFile.SaveCommander("Edited");
        Edit(saveFile, "Edited", save => save[field] = value);

        Assert.False(saveFile.LoadCommander("Edited"));
        Assert.Equal(expected, saveFile.LastLoadError);
    }

    [Fact]
    public void AnUnknownLaserIsNamedAsALaser()
    {
        SaveFile saveFile = CreateSaveFile();
        saveFile.SaveCommander("Laser");
        Edit(saveFile, "Laser", save => save["lasers"]!["rear"] = "Disintegrator");

        Assert.False(saveFile.LoadCommander("Laser"));
        Assert.Equal("Bad Lasers", saveFile.LastLoadError);
    }

    [Fact]
    public void AGoodTheSetDoesNotHaveIsNamedAsCargo()
    {
        SaveFile saveFile = CreateSaveFile();
        saveFile.SaveCommander("Typo");
        Edit(
            saveFile,
            "Typo",
            save =>
            {
                JsonObject cargo = save["cargo"]!.AsObject();
                cargo.Remove("Furs");
                cargo["Pelts"] = 0;
            });

        Assert.False(saveFile.LoadCommander("Typo"));
        Assert.Equal("Bad Cargo", saveFile.LastLoadError);
    }

    [Fact]
    public void StationStockIsNamedSeparatelyFromCargo()
    {
        SaveFile saveFile = CreateSaveFile();
        saveFile.SaveCommander("Shelf");
        Edit(saveFile, "Shelf", save => save["stationStock"]!["Food"] = 64);

        Assert.False(saveFile.LoadCommander("Shelf"));
        Assert.Equal("Bad Station Stock", saveFile.LastLoadError);
    }

    [Fact]
    public void AMissionNothingProvidesIsNamedAsAMission()
    {
        SaveFile saveFile = CreateSaveFile();
        saveFile.SaveCommander("Unknown");
        Edit(saveFile, "Unknown", save => save["missions"]!["Smuggling"] = new JsonObject { ["stage"] = "Briefed" });

        Assert.False(saveFile.LoadCommander("Unknown"));
        Assert.Equal("Bad Missions", saveFile.LastLoadError);
    }

    // A hold that overflows is the cargo's fault, not the bay's: the bay size
    // itself is one of the two the game fits.
    [Fact]
    public void MoreCargoThanTheHoldTakesIsNamedAsCargo()
    {
        SaveFile saveFile = CreateSaveFile();
        saveFile.SaveCommander("Overloaded");
        Edit(saveFile, "Overloaded", save => save["cargo"]!["Food"] = 21);

        Assert.False(saveFile.LoadCommander("Overloaded"));
        Assert.Equal("Bad Cargo", saveFile.LastLoadError);
    }

    [Fact]
    public void ALoadThatWorksLeavesNoError()
    {
        SaveFile saveFile = CreateSaveFile();
        saveFile.SaveCommander("Fine");

        Assert.True(saveFile.LoadCommander("Fine"));
        Assert.Equal(string.Empty, saveFile.LastLoadError);
    }

    // The screen shows whatever the save file reported, so a commander reads
    // the reason rather than a fixed sentence.
    [Fact]
    public void TheLoadScreenShowsTheReason()
    {
        FakeKeyboard keyboard = new();
        ScreenManager<Screen, IScreenController> views = new(keyboard);
        GameState gameState = new(views, TestMissions.Registry());
        PlayerShip ship = new(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        string directory = Path.Combine(Path.GetTempPath(), "SaveFileErrorTests_" + Guid.NewGuid().ToString("N"));
        SaveFile saveFile = new(gameState, ship, trade, new PlanetController(gameState), TestMissions.Registry(), directory);
        LoadCommanderController controller = new(gameState, keyboard, saveFile, new FakeLoadView());

        controller.Reset();

        // Saved under the name the keys actually produced, not a second copy spelled out here, since that could pick a case the keyboard can't make.
        string typed = Type(controller, keyboard, "BROKEN");
        saveFile.SaveCommander(typed);
        Edit(saveFile, typed, save => save["fuel"] = 99);

        keyboard.KeyDown(ConsoleKey.Enter, default);
        controller.HandleInput();

        Assert.Equal("Bad Fuel", controller.BuildModel().ErrorMessage);
    }

    // Letters reach the screen as ConsoleKey values, so a typed name is always
    // upper case. Returning what the controller made of the keys keeps that
    // fact in one place.
    private static string Type(LoadCommanderController controller, FakeKeyboard keyboard, string text)
    {
        foreach (char letter in text)
        {
            keyboard.KeyDown((ConsoleKey)letter, default);
            controller.HandleInput();
            keyboard.KeyUp((ConsoleKey)letter, default);
        }

        return controller.BuildModel().Name;
    }

    // The path comes from the save file itself, not rebuilt here, since a mismatch on case or extension would be silent on Windows.
    private static void Edit(SaveFile saveFile, string name, Action<JsonObject> edit)
    {
        string path = saveFile.PathFor(name);
        JsonObject save = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        edit(save);
        File.WriteAllText(path, save.ToJsonString());
    }

    private static SaveFile CreateSaveFile()
    {
        // Only SaveFile's constructor reads the variable, so the scope need
        // not outlive it - and restoring leaves the process as it was found.
        using EnvironmentVariableScope commander =
            EnvironmentVariableScope.Set(SaveFile.DebugCommanderEnvVar, null);

        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        GameState gameState = new(views, TestMissions.Registry());
        PlayerShip ship = new(gameState);
        Trade trade = TestGoods.Trade(gameState, ship);
        string directory = Path.Combine(Path.GetTempPath(), "SaveFileErrorTests_" + Guid.NewGuid().ToString("N"));

        SaveFile saveFile = new(
            gameState,
            ship,
            trade,
            new PlanetController(gameState),
            TestMissions.Registry(),
            directory);

        saveFile.GetLastSave();

        return saveFile;
    }

    private sealed class FakeLoadView : EliteSharp.Abstractions.Views.IView<EliteSharp.Abstractions.Views.LoadCommanderModel>
    {
        public void Draw(EliteSharp.Abstractions.Views.LoadCommanderModel model)
        {
        }
    }
}
