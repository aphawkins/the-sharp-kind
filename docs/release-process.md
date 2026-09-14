# Release Process

How to cut a release. Nothing here needs remembering beyond this page: there
is no version number to type anywhere in the repo — [MinVer](https://github.com/adamralph/minver)
derives it from the git tag alone, and pushing the right tag does everything
else.

## 1. Before you tag

- `main` is green — check the [Build and Package](https://github.com/aphawkins/the-sharp-kind/actions/workflows/build-and-package.yml)
  workflow, or run `dotnet test` locally.
- Both games have been smoke-tested live if anything in `src/useful/` or
  either game loop changed (see each backlog file's "Definition of done").
- [backlog-issues.md](backlog-issues.md) has no open **Must** items — both
  games are expected to work before a tag goes out.
- Decide the version number (see [Choosing the version](#choosing-the-version)
  below) and finish [release-notes.md](release-notes.md): rename its
  top `## Unreleased` heading to the real version and today's date. The
  heading carries no number until this moment, because the number is chosen
  here — from what that section ended up containing. Commit that on `main`
  before tagging — the tag should point at a commit whose release notes
  already say what the tag means.

## 2. Tag and push

```bash
git tag -a v1.1.0 -m "v1.1.0"
git push origin v1.1.0
```

Use an annotated tag (`-a`), not a lightweight one — `git tag v1.1.0` on its
own works with MinVer too, but an annotated tag carries a message and a date,
which is what you want on something permanent. The tag name is the whole
input: no file in the repo declares the version, so there is nothing else to
bump first.

## 3. What the push triggers

Pushing a `v*` tag fires [release.yml](https://github.com/aphawkins/the-sharp-kind/blob/main/.github/workflows/release.yml).
It runs unattended and, in order:

1. **Computes the version.** MinVer reads the tag (`MinVerTagPrefix` is `v`
   — see [Directory.Build.props](https://github.com/aphawkins/the-sharp-kind/blob/main/Directory.Build.props)) and stamps it
   onto every project in the solution.
2. **Packs and publishes the `SharpKind.*` libraries to NuGet.org**, every
   packable project under `src/useful/libs/`, authenticating via Trusted
   Publishing (OIDC) — no API key is stored in the repo. `EliteSharp` and
   `StuntCarRacerSharp` are apps, not packages, and are never packed.
3. **Publishes both games self-contained** for `win-x64`, `linux-x64` and
   `linux-arm64` (six builds total), strips `.xml`/`.ico` files, and zips
   each `app-rid` combination as `<app>-<rid>-<tag>.zip`.
4. **Creates the GitHub Release**, attaching the six zips, with
   `generate_release_notes: true` (GitHub's own auto-summary of commits/PRs
   since the last tag) plus a fixed line noting Stunt Car Racer is a preview
   build.

## 4. After the tag lands

GitHub's auto-generated notes are commit/PR titles, not the curated summary
in [release-notes.md](release-notes.md) — they're a floor, not the final
copy. Once the workflow finishes:

- Open the new [release](https://github.com/aphawkins/the-sharp-kind/releases)
  and replace (or prepend to) its description with the matching version
  section copied from [release-notes.md](release-notes.md).
- Confirm all six zips are attached and the
  [Release workflow run](https://github.com/aphawkins/the-sharp-kind/actions/workflows/release.yml)
  is green.
- Confirm the new package versions show up on
  [NuGet.org](https://www.nuget.org/profiles/Hawky) (can take a few minutes
  to index).
- Open a fresh `## Unreleased` section at the top of
  [release-notes.md](release-notes.md), above the version just dated. Do this
  even though there is nothing to put in it yet: without it the next fix has
  nowhere to be written down, and the "add a line to release-notes.md" step
  in each backlog file's definition of done gets skipped silently rather than
  failing visibly. (This is exactly what happened after v1.1.0.)
- Download at least one zip and smoke-test it — the workflow publishing
  successfully doesn't prove the exe actually runs on a machine with no .NET
  SDK installed.

## Choosing the version

[Semantic versioning](https://semver.org/): `MAJOR.MINOR.PATCH`.

- **PATCH** — bug fixes only, nothing new to play with or build against.
- **MINOR** — new features, gameplay additions, new settings — anything
  additive that doesn't break an existing save, config file or a consumer of
  the `SharpKind.*` NuGet packages.
- **MAJOR** — a save or config file from the previous version stops loading
  (rather than migrating), or a public member of a published `SharpKind.*`
  package is removed, renamed, or changes what it does. Since the
  `SharpKind.*` libraries are published to NuGet.org (step 3.2 above), this
  applies to them like any other public library, not just to the games.

[release-notes.md](release-notes.md)'s `## Unreleased` section is the source of
truth for what to weigh: read down its Features/Fixes for each of Elite,
Stunt Car Racer and the engine before picking a number, rather than
guessing from memory.

## If something goes wrong

- **Tag pushed too early / wrong version**: delete it before anything
  downstream depends on it existing —
  `git tag -d v1.1.0 && git push origin :refs/tags/v1.1.0` — fix
  `release-notes.md`, and re-tag.
- **The workflow ran and published to NuGet.org before you noticed a
  problem**: don't delete or re-push the tag. NuGet.org refuses to accept
  the same package version twice, so a fix has to be a new, higher version —
  treat the bad release as shipped and follow this process again for the
  correction.
