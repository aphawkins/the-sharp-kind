// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Runtime.CompilerServices;

// Views stay internal - the game only ever sees them as IView - but the draw tests need them.
[assembly: InternalsVisibleTo("EliteSharpLib.Tests")]
