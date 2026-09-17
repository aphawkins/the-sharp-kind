## Architecture

* Old games from 8 and 16 bit computers ported over to dotnet and C#.
* Games should retain their original look and feel, while also taking advantage of modern hardware and software capabilities.
* Prefer dotnet framework intrinsics and libraries, over third party libraries.
* As much code as possible should be in the 'SharpKind' libraries and only game specific code should be in the game projects themselves.
* The highest possible coding standards should be used.
* Always choose best practice and the superior architecture when making a change, rather than the shape that happens to be there already. See [Choosing between designs](#choosing-between-designs) for what decides which is superior.
* The code focus should be on correctness, security, maintainability, readability, and performance.
* Code should adhere to DRY (Don't Repeat Yourself), YAGNI (You Aren't Gonna Need It), KISS (Keep It Simple, Stupid), and SOLID principles.
* Technical debt should be prioritised over new features.
* Development prioritisation technique should use the MoSCoW method
* Embrace continuous refactoring, continuous automated unit testing, and continuous integration.
* OOP principles should be used where appropriate, but not at the expense of performance or simplicity.
* The game engine should be designed to be as modular and extensible as possible, allowing for easy addition of new features and functionality.
* The game engine may differ from established engines in that it may be architected more like a business application, allowing developers from a business application background to more easily understand and contribute to the codebase.
* Unit testing, integration testing, and end-to-end testing should be used to ensure the quality and reliability of the codebase.
* Performance profiling and optimization should be an ongoing process throughout development, with a focus on identifying and addressing performance bottlenecks early in the development cycle.
* Code should be cross-platform, with support for Windows, Linux, x64, and ARM64 architectures. 32 bit architectures are not supported.

## Business application practices

The engine is architected like a business application; these are the house rules that follow from that.

### Composition and dependency injection

* Each executable has exactly one composition root: `Program.Main`, using `Microsoft.Extensions.DependencyInjection` (a plain `ServiceCollection`; the full Generic Host is not needed). What is identical between the executables — resolving the user-data path, standing up logging, running the game, reporting a failure — lives in `SharpKind.App`'s `GameApp.Run`; each `Main` supplies only its own title, log file and service registrations.
* Everything below the composition root receives its collaborators by constructor injection. Domain classes never `new` up their own dependencies (no `AssetLocator.Create()` or `new ConfigFile()` inside game classes) and never reach through a god object for them — a class's constructor signature is the honest list of what it uses.
* The container owns lifetimes and disposal: `IAbstraction`, graphics/sound/keyboard, configuration and asset services are singletons disposed by the provider in reverse order. Never call `Environment.Exit` to end the game; return from the loop and let `Main` unwind.
* No two-phase construction: an instance must never be observable half-built. Fold `Initialize(...)` methods and post-construction property mutation into constructors or static factories.

### Logging

* Libraries depend only on `Microsoft.Extensions.Logging.Abstractions` and accept `ILogger<T>` by constructor. The logging provider (Serilog) and sink configuration are app-level policy: they live in the app layer — `Program.Main` or the shared `SharpKind.App` host it delegates to — and no game or engine library references Serilog.
* Log messages use source-generated `[LoggerMessage]` partials (allocation-free, compile-checked) — the existing app `LogMessages.cs` pattern, replicated per library.
* Log at the edges, not per frame: startup and asset loading (Information), config/save read-write outcomes (Information/Warning), recoverable oddities (Warning), failures and top-level handlers (Error/Critical). The frame path stays logging-free.
* App sinks: console plus a rolling file (with a retained-file limit), minimum level Information, raisable via environment variable. A Debug-only sink is not acceptable as the sole sink — a crash on a player's machine must leave a trace.
* `System.Diagnostics` is not a logging channel: `Debug.Assert` is fine for internal invariants, but anything reporting an operational failure (`Debug.Fail`, `Debug.WriteLine`) must become a thrown exception or a logged Warning/Error — Debug calls compile away in the builds players run.

### Configuration and user data

* Settings are read via `Microsoft.Extensions.Configuration` (JSON provider) bound onto plain options classes, with defaults supplied by the POCO and validation at startup; an invalid or missing file logs a Warning and falls back to defaults, never crashes.
* Writing settings stays behind a small interface seam (M.E.Configuration is read-only), so views depend on the seam, not a file class.
* User data (settings, save files) lives under `Environment.SpecialFolder.ApplicationData`, not the current working directory, with the base path injected so it can be redirected in tests. Filenames derived from user input are sanitised before use.

### Error handling

* Fail fast at startup with a logged Critical and an actionable message (missing assets, SDL init failure).
* Never catch-all on the frame path; catch the narrowest type that can actually occur and always log what was caught.
* Degrade gracefully for user data: config and save load failures return defaults/false with a Warning, they do not throw past the caller.
* Exceptions always carry context (`throw new EliteException($"Unknown ship roll {rnd}")`, never a bare `throw new EliteException()`).

### Testability seams

* The three ambient dependencies are always injectable: time (`TimeProvider`, as `GameLoop` already does), the file system (an injected base path at minimum), and randomness (an injected, seedable random source — never a static mutable RNG).
* Every library has a corresponding Tests project and, where consumers need doubles, a Fakes project; new logic lands with tests against those seams.

### Interfaces and API surface

* Keep producer and consumer roles on separate interfaces (interface segregation): the code that feeds input events and the code that polls key state should not share one interface.
* An interface member that a primary implementation cannot honour (silent no-op) is a design error: implement it or remove it.
* Prefer framework intrinsics over home-grown equivalents (`ArgumentNullException.ThrowIfNull` over a custom `Guard`), per the third-party-libraries principle above.

### Choosing between designs

* When a change has a better shape available, take the better shape. "It matches what is already there" is not on its own a reason to repeat a design whose cost is visible, and neither is "it is how the other game does it".
* **Duplication is the evidence.** The same few lines in three places usually means a responsibility is in the wrong home, not that three callers were lazy. Before moving code, find who had to copy it and what stopped them reusing it — that names the right owner. The character grid moved off `BaseView8Bit` on 2026-09-17 because three static list styles could never inherit a view and had each reimplemented `Column`/`Row` over it.
* **Prefer the design that needs less to test.** Arithmetic assertable on its own is better placed than the same arithmetic reachable only by standing up a surface, a palette and a backend.
* **Converge, do not diverge.** Where a port and the code it was modelled on disagree about a shape, change both rather than letting them drift or copying the weaker one forward. A rule like "copy Elite, not Stunt Car Racer" exists to stop arbitrary drift; it is not a reason to propagate a shape that has already cost something.
* **A tier or a game that lacks a concept is given none of it.** The better design is often the one that models what is actually there — the 16-bit tier has a proportional font and so holds no character grid at all, rather than an invented cell size.
* **Supersede, do not contradict.** If the better design overturns something in [decisions.md](decisions.md), add the new entry and mark what it replaces.
* Cost the change before taking it: how many call sites move, what proves nothing broke, and whether the golden frames or the other games could shift. A better design that cannot be verified is not better yet.

### Screens and renderers: controllers, models and per-rendition drawing

The rules the 2026-07-29/30 controller/view split settled on. They apply to
every Elite screen and to any new one.

* Each screen is a `<Screen>Controller` (behaviour: `Reset`/`Update`/`HandleInput`, and all formatting), a `<Screen>Model` record (everything the screen draws, including its fixed wording), and one drawing-only view per tier. `ScreenManager` holds controllers, never views. **The two settings screens are the exception** (2026-08-05): they have no view and no model, because their content is controls bound to the game's own settings and a setting's value cannot live in a rendition. The rendition supplies a `SettingsListStyle` — every colour and position, and nothing else — and the controller owns the controls. **The market and inventory screens joined them** (2026-08-27), once the goods a market lists became a plugin's own count rather than a fixed seventeen a rendition could be hand-laid-out against: both are now a `ListView<TableRow>` built over a `MarketListStyle`/`InventoryListStyle` the rendition supplies, and `MarketView8Bit`/`MarketView16Bit` (and their inventory equivalents) are gone. See [decisions.md](decisions.md).
* One view interface: `IView<TModel> { void Draw(TModel model); }`. **Do not reintroduce a parameterless `IView`** — it was tried and removed on 2026-07-29: two screens needed it at once and a single registration can only serve one, with the last silently winning. Give a screen a model even when the model is only its wording.
* The three properties to preserve: behaviour has one home per screen, both tiers provably render the same data, and no rendition holds anything authoritative. The third was "views are stateless" until 2026-08-05, when the control library gave views retained `Container`/`Label` instances to draw with. Holding *how* it draws is a view's business; holding *what is true* is not — a control's position is a cache rebuilt from the model each frame, whereas a selected value would be an answer the other rendition could contradict. This is not MVC, MVP or MVVM and shouldn't be described as any of them. Every control is bound to an `ISetting` as of 2026-08-06, which is half of what MVVM means by binding; the other half — change notification — is still rejected, and for the same reason as before: Elite is immediate-mode with a full redraw per tick and polled input, so a control reading its binding as it draws is already current and has nothing to be notified about. The `Controller` suffix matches `PlanetController`/`AudioController` house style.
* Views live in a plugin assembly per rendition — `EliteSharp.Renditions.EightBit/<Screen>View8Bit.cs` and `EliteSharp.Renditions.SixteenBit/<Screen>View16Bit.cs` — each referencing `EliteSharp.Abstractions` and nothing else, and neither referenced by the game (2026-08-03, see [decisions.md](decisions.md)). The game finds them in a `Renditions` folder beside the executable and takes the one whose `Name` matches; nothing in the game branches on which rendition is running. The tier contract is a signature, not a convention: both views take the same model, so they can differ only in coordinates and font, and the formatting stays testable without a renderer.
* A pack hands over its screens as a `ViewSet`, keyed by the model each draws, and `ViewRegistry` checks that against the game's screens **once, at startup**. A missing screen or a missing pack is a startup failure that names what is missing; neither is something a commander should discover by opening a screen. What a rendition contributes that is *not* a view — `ShipColours`, the planet and sun renderers, `SettingsListStyle` — is a method on `IRendition` instead, so the compiler makes the check the startup pass would have. Prefer that shape when a screen's tier-specific content is data rather than drawing. What a view may touch of the game is `IViewSurface` — `Graphics`, `Layout`, `Palette` — and nothing else: `IEliteDraw` implements it, and everything past those three (projection, clipping, frame start/end) stays on the game's side of the seam.
* Label text is chrome the view supplies, not model content, so each tier picks its own wording ("System:" at 8-bit against "Present System:" at 16-bit). Column-alignment format specifiers (`{qty,2}`) also stay in the view; a number the original drew as one indivisible string is content and belongs in the model.
* The 8-bit tier is a 320x256 canvas with a fixed 8x8 font — 40 characters per row, full stop — so its layouts are authored fresh rather than scaled down from the 16-bit one. Check a layout against the *shape* of its content (the fully-equipped ship, the longest credit line), not against whatever one screenshot happens to show.
* Screenshot diffing cannot prove "pixel-identical" on this app: two runs of the same build differ by ~2,000 pixels (window occlusion and the animated dashboard). Verify layout equivalence by algebra plus renderer-free controller tests, and use screenshots only to confirm nothing gross broke.
* What screenshots *can* prove is one row at a time: take the min and max lit pixel across a text row's band and compare that span before and after. Both sources of noise are excluded by construction — the dashboard is not in the band, and occlusion moves the window rather than the ink within it. This is how the widget conversion was held to the pixel across 14 rows in both tiers on 2026-08-05, and it caught two real shifts (an 8px one from snapping a text offset instead of its final position, a 2px one from the old screen-centring's integer division) that eye-checking the same screenshots had missed twice.
* Screens with nothing to extract stay plain `IScreenController` implementations rather than being split for consistency — `DockingView`/`HyperspaceView`/`LaunchView` all draw a shared, resolution-independent `BreakPattern` and have no formatting and no tier variance.
* The screen registrations live in `EliteSplitScreensServiceCollectionExtensions` and `EliteSplitAnimatedScreensServiceCollectionExtensions`, subdivided into `AddSplit*Screens` methods. They were split to stay under CA1506's class-coupling limit when each screen named two view types; the views have since gone to plugins and each registration names only its model, so the pressure is off — but the split stays, and another method or static class is still the intended way to absorb the limit rather than a suppression.
* **Nothing that puts pixels on screen belongs to the game** (maintainer, 2026-07-30, widened 2026-08-03). Every screen, the HUD, the four planet styles, the three sun styles and the starfield draw in a rendition; the game computes and hands over. The shape to copy is **a base in the contracts holding the drawing that does not vary, and a subclass per rendition holding every colour and every position — the base holds no literal of either.** `ScannerViewBase` is the reference: ~20 offsets once hardcoded to the 512x129 scanner are named members, which is what let the 640-wide art, and then the full-screen `hud.bmp` that replaced it, be absorbed by editing one file. A subclass may override as far as the whole draw, which is the point of a base rather than injected colours.
* What the game keeps is the universe: where a thing is, how it moves, when it is cloned, and the one seeded source of entropy. A renderer is handed screen terms - a centre, a radius, an orientation, a list of marks - and never a position in space. A renderer needing randomness is handed the game's stream rather than making one, or the same system would stop looking the same on every visit.
* `ViewLayout` is the single source of screen metrics: screen size, the console band's height and design scale in, everything else derived. Take metrics from `IEliteDraw.Layout`; do not add convenience mirrors of them elsewhere, which is what `IEliteDraw` accumulated before 2026-07-30 and what left every value with two spellings.

* `ViewLayout` describes **one region, the viewport** — everything above the HUD — and it starts at the screen origin (2026-07-31). It exposes no border and no scanner members: the frame is part of the HUD art (2026-09-15), and the console's own geometry belongs to `ScannerViewBase`. This is what makes a position mean what it says: no border inset and no HUD edge enter the arithmetic, so the 8-bit viewport is exactly 40x25 of its 8x8 character cells and the frame can change width without moving a single view. Screens that used to lay out from `ScannerLeft`/`Offset` lay out from `ViewportLeft`.

* **8-bit text sits on the character grid.** `BaseView8Bit` exposes `Row(n)`/`Column(n)`, and the 8-bit screens position against those rather than pixel constants; `DrawTextCentreOnGrid` centres on a column because `DrawTextCentre` lands odd-length strings on half a cell, and `SnapToGrid` rounds up for text the chart's packing places arbitrarily. Row 0 and the outermost columns belong to the border, which overlays their first pixel — most of the font's glyphs ink theirs — so content starts at row 1 (chrome), row 2 (the header rule) and row 3 (screen content). The 16-bit font is proportional and has no grid.
* `IBaseView` (`BaseView8Bit`/`BaseView16Bit`) holds the chrome every screen shares — the header, the border, the countdown, the word-wrap. Views inherit their tier's; `EliteMain` resolves `IBaseView` for the border and countdown.
* **Palette names are per tier and the two sets need not overlap.** The 8-bit palette is sixteen web colour names, the 16-bit one is 29 ramp names; nothing may assume a name exists in both. This is why the models are tier-scoped, and why a `Palette["..."]` lookup in shared code is a latent startup crash — it must sit in tier-specific code or use a name both palettes define.
* **A tier's channel depth is enforced, not assumed.** `AssetColourBudget.ChannelBits` says how many bits per channel the tier's hardware could drive — four for 16-bit, its 12-bit DAC — and `AssetSet.Load` fails startup on any palette entry or bitmap pixel that sits between those levels. Widen an n-bit channel by **replication** (`0xE` → `0xEE`), never a left shift (`0xE0`), which cannot reach white. Anything that authors a colour for a tier — art, palette, a literal in a view — has to land on the grid.

### Code comments

* Comments are terse and to the point. One line where one line does.
* A comment explains **why**, not what — the code already says what it does.
* No justification, no history, no dated rationale, no argument for the decision taken. That belongs in [decisions.md](decisions.md) and the commit message.
* Delete a comment rather than update it to say what the code now plainly says.

### Solution hygiene

* NuGet package versions are managed centrally (`Directory.Packages.props`); shared `PackageReference` blocks (analyzers) live in `Directory.Build.props`/`.targets`, not copy-pasted per project.
* Generated artifacts (benchmark reports, build output) are not committed.
* Avoid `#pragma warning disable` and `[SuppressMessage]`: fix the underlying code first. If a diagnostic is a genuine, verified false positive for this codebase (not just this call site), configure its severity in `.editorconfig` instead — repo-wide, or project-scoped via a nested `.editorconfig` when the false positive is specific to one assembly (e.g. `unsafe` interop code) — rather than scattering local suppressions. Reach for a local suppression only when neither a code fix nor an `.editorconfig` change is possible, and justify it with a comment.

