// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Tests.Oracle;

// All of RAM, before and after a bracketed routine, and the CIA timer bytes its `$E9EA` rolls mixed in, in order.
internal sealed record Capture(byte[] Before, byte[] After, byte[] Timer);
