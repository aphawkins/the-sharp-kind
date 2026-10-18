# Contributing

Thank you for considering contributing!

## How to Contribute
- Fork the repository
- Create a branch for your changes
- Make your edits and add tests if applicable
- Submit a pull request

## Tests

The suite has two levels. A test that exercises one component in isolation is
a unit test and needs no marking. A test that drives the real DI composition,
the headless game harness, the file system or a plugin loaded off disk is an
integration test, and its class carries:

```csharp
[Trait("Level", "Integration")]
```

Run the fast level while you work, and the whole suite before you push:

```
dotnet test -- --filter-not-trait "Level=Integration"
dotnet test
```

Keep a unit test under a few milliseconds. If a new test needs a second, it
belongs at the integration level, and its cost belongs in a shared fixture
rather than in every test method.

## Guidelines
- Keep commits focused and clear
- Follow existing code style
- Be respectful in discussions

This is a personal project, so contributions may be reviewed at the maintainer's discretion.
