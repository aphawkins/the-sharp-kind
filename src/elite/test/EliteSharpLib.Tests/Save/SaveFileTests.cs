// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Text.Json.Nodes;
using EliteSharp.Missions.Classic;
using EliteSharpLib.Save;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Trader;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;

namespace EliteSharpLib.Tests.Save;

// Serialised against other classes reading/writing ELITE_DEBUG_COMMANDER: it belongs to the
// process, and xUnit runs test classes in parallel.
[Collection("EnvironmentVariables")]
public class SaveFileTests
{
    [Fact]
    public void LoadCommanderWithNoSaveFileReturnsFalse()
    {
        // Arrange
        SaveFile saveFile = CreateSaveFile(out _);

        // Act
        bool result = saveFile.LoadCommander("NoSuchCommander");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderWithCorruptJsonReturnsFalseInsteadOfThrowing()
    {
        // Arrange
        SaveFile saveFile = CreateSaveFile(out _);
        File.WriteAllText(saveFile.PathFor("Corrupt"), "{ not valid json");

        // Act
        bool result = saveFile.LoadCommander("Corrupt");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderWithTruncatedArraysReturnsFalseInsteadOfThrowing()
    {
        // Arrange: a hand-edited file missing nearly everything - the mapping reads the
        // seed and the goods without checking, so without validation this used to throw.
        SaveFile saveFile = CreateSaveFile(out _);
        File.WriteAllText(
            saveFile.PathFor("Truncated"),
            /*lang=json,strict*/ "{\"galaxySeed\": {\"a\": 1}}");

        // Act
        bool result = saveFile.LoadCommander("Truncated");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SaveCommanderThenLoadCommanderRoundTrips()
    {
        // Arrange
        SaveFile saveFile = CreateSaveFile(out _);

        // Act
        bool saved = saveFile.SaveCommander("RoundTrip");
        bool loaded = saveFile.LoadCommander("RoundTrip");

        // Assert
        Assert.True(saved);
        Assert.True(loaded);
    }

    [Fact]
    public void SaveCommanderNamesEveryGoodItsLasersAndItsMission()
    {
        // Arrange: the point of the format - nothing positional, so a save can be read
        // without counting array indices against the code that wrote it.
        SaveFile saveFile = CreateSaveFile(out _);

        // Act
        saveFile.SaveCommander("Named");
        JsonObject save = ReadSave(saveFile, "Named");

        // Assert
        Assert.Equal(SaveState.CurrentFileType, (string?)save["fileType"]);
        Assert.Equal(SaveState.CurrentVersion, (int?)save["version"]);

        // A fresh commander has started nothing, and the file holds only the
        // stages that have been reached.
        Assert.Empty(save["missions"]!.AsObject());
        Assert.Equal("Clean", (string?)save["legalStatus"]!["status"]);
        Assert.Equal("Pulse", (string?)save["lasers"]!["front"]);
        Assert.Equal("None", (string?)save["lasers"]!["rear"]);
        Assert.Equal(0, (int?)save["cargo"]!["Narcotics"]);
        Assert.Equal(0x3A, (int?)save["stationStock"]!["Minerals"]);
    }

    [Fact]
    public void LoadCommanderRestoresTheGoodsByName()
    {
        // Arrange: cargo is keyed by name, so it has to land on the named good and not
        // on whichever one happens to sit at that position.
        SaveFile saveFile = CreateSaveFile(out _, out Trade trade);
        saveFile.SaveCommander("Cargo");
        Edit(saveFile, "Cargo", save => save["cargo"]!["Furs"] = 3);

        // Act
        bool loaded = saveFile.LoadCommander("Cargo");

        // Assert
        Assert.True(loaded);
        Assert.Equal(3, trade["Furs"].CurrentCargo);
    }

    [Theory]

    // A file from before the format was versioned, and one from a later format.
    [InlineData("version", 0)]
    [InlineData("version", 2)]
    [InlineData("fileType", "Something else")]

    // Values the game itself never produces.
    [InlineData("fuel", 8)]
    [InlineData("missiles", 5)]
    [InlineData("cargoCapacity", 21)]
    [InlineData("credits", -1)]
    [InlineData("galaxyNumber", 8)]
    [InlineData("marketRandomiser", 256)]

    // Enum members have to be named, not numbered.
    [InlineData("energyUnit", "Nuclear")]
    public void LoadCommanderRejectsAValueTheGameCouldNotHaveWritten(string property, object value)
    {
        // Arrange
        SaveFile saveFile = CreateSaveFile(out _);
        saveFile.SaveCommander("Edited");
        Edit(saveFile, "Edited", save => save[property] = JsonValue.Create(value));

        // Act
        bool result = saveFile.LoadCommander("Edited");

        // Assert
        Assert.False(result);
    }

    [Theory]

    // A stage that mission does not have, a stage belonging to the other
    // mission, and a number where a name belongs.
    [InlineData("Constrictor", "Summoned")]
    [InlineData("Thargoid", "Destroyed")]
    [InlineData("Constrictor", "NoSuchStage")]
    [InlineData("Thargoid", "2")]
    public void LoadCommanderRejectsAStageThatIsNotThatMissions(string mission, string stage)
    {
        // Arrange: each mission declares its own stages, so one mission's stages are not
        // another's - the single mission number this replaced could not tell them apart.
        SaveFile saveFile = CreateSaveFile(out _);
        saveFile.SaveCommander("WrongStage");
        Edit(
            saveFile,
            "WrongStage",
            save => save["missions"]!.AsObject()[mission] = new JsonObject { ["stage"] = stage });

        // Act
        bool result = saveFile.LoadCommander("WrongStage");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderRejectsAMissionItDoesNotKnow()
    {
        // Arrange: a mission from a later version of the game, or a typo'd name. Either
        // way the file holds something this build cannot act on.
        SaveFile saveFile = CreateSaveFile(out _);
        saveFile.SaveCommander("Unknown");
        Edit(
            saveFile,
            "Unknown",
            save => save["missions"]!.AsObject()["Generation"] = new JsonObject { ["stage"] = "Briefed" });

        // Act
        bool result = saveFile.LoadCommander("Unknown");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderRestoresBothMissionsIndependently()
    {
        // Arrange: the point of a mission each - the Constrictor being finished and the
        // Thargoid run being under way is one state, not two readings of one number.
        SaveFile saveFile = CreateSaveFile(out _, out _, out GameState gameState);
        saveFile.SaveCommander("MidRun");
        Edit(
            saveFile,
            "MidRun",
            save =>
            {
                JsonObject missions = save["missions"]!.AsObject();
                missions[ConstrictorMission.Id] = new JsonObject { ["stage"] = ConstrictorMission.Rewarded };
                missions[ThargoidMission.Id] = new JsonObject { ["stage"] = ThargoidMission.CarryingPlans };
            });

        // Act
        bool loaded = saveFile.LoadCommander("MidRun");

        // Assert
        Assert.True(loaded);
        Assert.Equal(ConstrictorMission.Rewarded, gameState.Cmdr.Missions.StageOf(ConstrictorMission.Id));
        Assert.Equal(ThargoidMission.CarryingPlans, gameState.Cmdr.Missions.StageOf(ThargoidMission.Id));
    }

    [Fact]
    public void LoadCommanderReadsAMissionTheSaveDoesNotMentionAsNotStarted()
    {
        // A commander saved before a mission existed, or who never met it: absence is the answer, not a refusal.
        SaveFile saveFile = CreateSaveFile(out _, out _, out GameState gameState);
        saveFile.SaveCommander("PartWay");
        Edit(
            saveFile,
            "PartWay",
            save => save["missions"]!.AsObject()[ConstrictorMission.Id]
                = new JsonObject { ["stage"] = ConstrictorMission.Briefed });

        // Act
        bool loaded = saveFile.LoadCommander("PartWay");

        // Assert
        Assert.True(loaded);
        Assert.Equal(ConstrictorMission.Briefed, gameState.Cmdr.Missions.StageOf(ConstrictorMission.Id));
        Assert.Equal(ThargoidMission.None, gameState.Cmdr.Missions.StageOf(ThargoidMission.Id));
    }

    [Fact]
    public void LoadCommanderRejectsAnUnknownGood()
    {
        // Arrange: a typo'd name would have been read as a missing one, and the array
        // format could not have caught it at all.
        SaveFile saveFile = CreateSaveFile(out _);
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

        // Act
        bool result = saveFile.LoadCommander("Typo");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderRejectsAQuantityTheMarketCouldNotHold()
    {
        // Arrange
        SaveFile saveFile = CreateSaveFile(out _);
        saveFile.SaveCommander("Overstocked");
        Edit(saveFile, "Overstocked", save => save["stationStock"]!["Food"] = 64);

        // Act
        bool result = saveFile.LoadCommander("Overstocked");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderRejectsMoreCargoThanTheHoldTakes()
    {
        // Arrange: gold, platinum and gem stones are not weighed in tonnes, so it is the
        // tonnage and not the number of goods that has to fit.
        SaveFile saveFile = CreateSaveFile(out _);
        saveFile.SaveCommander("Overloaded");
        Edit(saveFile, "Overloaded", save => save["cargo"]!["Food"] = 21);

        // Act
        bool result = saveFile.LoadCommander("Overloaded");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderRejectsALegalStatusThatContradictsItsBounty()
    {
        // Arrange: the bounty is what the game works in, so a file naming a band its
        // bounty does not fall in has been edited wrongly rather than usefully.
        SaveFile saveFile = CreateSaveFile(out _);
        saveFile.SaveCommander("Liar");
        Edit(saveFile, "Liar", save => save["legalStatus"]!["bounty"] = 64);

        // Act
        bool result = saveFile.LoadCommander("Liar");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LoadCommanderRestoresABountyItsBandAgreesWith()
    {
        // Arrange
        SaveFile saveFile = CreateSaveFile(out _, out _, out GameState gameState);
        saveFile.SaveCommander("Fugitive");
        Edit(
            saveFile,
            "Fugitive",
            save =>
            {
                save["legalStatus"]!["bounty"] = 64;
                save["legalStatus"]!["status"] = "Fugitive";
            });

        // Act
        bool loaded = saveFile.LoadCommander("Fugitive");

        // Assert
        Assert.True(loaded);
        Assert.Equal(64, gameState.Cmdr.LegalStatus);
    }

    [Fact]
    public void SaveCommanderWithPathSeparatorsInNameStaysInsideSaveDirectory()
    {
        // Arrange: a commander name containing path separators must not escape
        // the save directory or be treated as a subdirectory.
        SaveFile saveFile = CreateSaveFile(out string directory);

        // Act
        bool saved = saveFile.SaveCommander("../../Escaped");

        // Assert
        Assert.True(saved);
        Assert.Single(Directory.GetFiles(directory));
    }

    // The load/save screens can only make upper-case names (a letter arrives as a ConsoleKey), so a
    // hand-edited or older-build file in any other case must still be reachable on a case-sensitive filesystem.
    [Theory]
    [InlineData("Mixed", "MIXED")]
    [InlineData("lower", "LOWER")]
    [InlineData("UPPER", "upper")]
    public void ACommanderLoadsWhateverCaseTheNameIsGivenIn(string saved, string loaded)
    {
        SaveFile saveFile = CreateSaveFile(out _);

        Assert.True(saveFile.SaveCommander(saved));
        Assert.True(saveFile.LoadCommander(loaded));
        Assert.Equal(string.Empty, saveFile.LastLoadError);
    }

    // One commander, one file: saving the same name in two cases must not
    // leave two saves that a case-sensitive filesystem tells apart.
    [Fact]
    public void TwoCasesOfOneNameAreOneCommander()
    {
        SaveFile saveFile = CreateSaveFile(out string directory);

        Assert.True(saveFile.SaveCommander("Twice"));
        Assert.True(saveFile.SaveCommander("TWICE"));

        Assert.Single(Directory.GetFiles(directory));
    }

    // Both helpers ask the save file where a commander lives, rather than rebuilding the path here and risking a case mismatch.
    private static JsonObject ReadSave(SaveFile saveFile, string name)
        => JsonNode.Parse(File.ReadAllText(saveFile.PathFor(name)))!.AsObject();

    private static void Edit(SaveFile saveFile, string name, Action<JsonObject> edit)
    {
        string path = saveFile.PathFor(name);
        JsonObject save = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        edit(save);
        File.WriteAllText(path, save.ToJsonString());
    }

    private static SaveFile CreateSaveFile(out string directory)
        => CreateSaveFile(out directory, out _, out _);

    private static SaveFile CreateSaveFile(out string directory, out Trade trade)
        => CreateSaveFile(out directory, out trade, out _);

    private static SaveFile CreateSaveFile(out string directory, out Trade trade, out GameState gameState)
    {
        // Written against Commander Jameson, so the debug commander is cleared rather than left to the machine's setting.
        using EnvironmentVariableScope commander =
            EnvironmentVariableScope.Set(SaveFile.DebugCommanderEnvVar, null);

        ScreenManager<Screen, IScreenController> views = new(new FakeKeyboard());
        gameState = new(views, TestMissions.Registry());
        PlayerShip ship = new(gameState);
        trade = TestGoods.Trade(gameState, ship);
        PlanetController planet = new(gameState);
        directory = Path.Combine(Path.GetTempPath(), "SaveFileTests_" + Guid.NewGuid().ToString("N"));
        SaveFile saveFile = new(gameState, ship, trade, planet, TestMissions.Registry(), directory);

        // As the game does on startup, so the state a save is written from is
        // Commander Jameson rather than an empty ship.
        saveFile.GetLastSave();
        return saveFile;
    }
}
