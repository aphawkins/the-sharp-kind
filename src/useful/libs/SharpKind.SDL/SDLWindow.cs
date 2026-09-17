// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SDL;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace SharpKind.SDL;

public sealed unsafe class SDLWindow : IDisposable
{
    private readonly nint _window;
    private bool _isDisposed;

    public SDLWindow(int screenWidth, int screenHeight, string title)
    {
        // SDL3 handles the debugger's thread-naming exception internally, so the SDL 2.x-era disable hint is no longer needed here.
        SDLGuard.Execute(() => SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO));
        SDLGuard.Execute(() => TTF_Init());

        _window = SDLGuard.Execute(() => (nint)SDL_CreateWindow(
            title,
            screenWidth,
            screenHeight,
            default));

        // SDL3's SDL_CreateWindow no longer takes a position, so centre the
        // window explicitly once it has been created.
        SDLGuard.Execute(() => SDL_SetWindowPosition(
            (SDL_Window*)_window,
            (int)SDL_WINDOWPOS_CENTERED,
            (int)SDL_WINDOWPOS_CENTERED));
    }

    public static implicit operator nint(SDLWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        return window._window;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public nint ToIntPtr() => _window;

    private void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            _isDisposed = true;

            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            SDL_DestroyWindow((SDL_Window*)_window);
            SDL_Quit();
        }
    }
}
