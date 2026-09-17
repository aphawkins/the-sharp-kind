// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Runtime.CompilerServices;

// The goods table stays internal - the game reads it off IGoodsSet - but the economy tests need it.
[assembly: InternalsVisibleTo("EliteSharpLib.Tests")]
