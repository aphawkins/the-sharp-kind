# Bubble Bobble: the music restarts when a player loses a life

[BubbleBobbleSharp] **Bug**, found in play testing. When a player loses a life the level's tune starts again from
the beginning. It should carry on playing.

Where to look: the song requests in `src/bb/libs/BubbleBobbleSharpLib/GameLoop.cs` (`Ask`, `LevelSong`, and the
`Song.Theme`/`Song.Resume` choice), and `PlaySongs` in `BubbleBobbleMain.cs`, which calls `Sound.Play` again for
every song it takes, so asking for the tune already playing restarts it. Check against the reference what `$05AD`
is (or is not) asked for on a death, and whether the C64 keeps the current song running.

Verify: a test that a death asks for no song, or asks for none that is already playing; then hear it in the app.
