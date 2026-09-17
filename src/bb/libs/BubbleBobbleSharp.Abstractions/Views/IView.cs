// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// The drawing half of one part of the screen. Everything it needs arrives in
/// <typeparamref name="TModel"/>, so it holds no state and derives nothing:
/// one implementation per rendition, differing only in how it draws.
/// </summary>
/// <typeparam name="TModel">The view model, computed on the game's side.</typeparam>
public interface IView<in TModel>
{
    public void Draw(TModel model);
}
