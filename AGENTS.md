# Repository Guidelines

## Project Structure & Module Organization
This repository is a Unity 6 project. Core gameplay code lives in `Assets/Scripts/GameCore`, including passenger flow, economy, UI controllers, and store systems. Main scene content is in `Assets/Scenes/GameScene.unity`. Art and sprites are under `Assets/Art`, while render pipeline and scene template settings live in `Assets/Settings`. Unity-generated folders such as `Library`, `Temp`, and `Logs` should not be edited manually.

## Build, Test, and Development Commands
- `dotnet build Assembly-CSharp.csproj`
  Builds the gameplay assembly and catches C# compile errors without opening Unity.
- Open the project in Unity 6 and load `Assets/Scenes/GameScene.unity`
  Use this for manual gameplay validation, UI checks, and scene wiring verification.
- `git status --short`
  Quick check of modified files before committing.

There is no standalone automated gameplay test suite in the repository yet, so treat `dotnet build` plus targeted Unity validation as the minimum check.

## Coding Style & Naming Conventions
Use 4-space indentation and standard C# braces on new lines. Keep classes `PascalCase`, private serialized fields `_camelCase`, and public properties/methods `PascalCase`. Follow the existing controller naming pattern such as `PassengerProcessor`, `WaitingRoomUIController`, and `StoresManager`. Prefer small, focused changes inside existing systems over broad architectural refactors.

## Testing Guidelines
Before submitting changes:
- Run `dotnet build Assembly-CSharp.csproj`.
- Manually validate the affected flow in Unity, especially scene references, button states, and coroutine-driven UI behavior.
- If changing passenger flow, verify queue, scanner, waiting room, and economy interactions in `GameScene`.

When adding tests later, place EditMode or PlayMode tests under a dedicated `Assets/Tests` folder with names matching the class or feature under test.

## Commit & Pull Request Guidelines
Recent commits use short imperative English summaries, for example: `Add waiting-room reservations and scanner fixes`. Keep commit messages concise and action-oriented.

Pull requests should include:
- A brief summary of the gameplay or UI change.
- The affected scene(s) and script(s).
- Manual verification notes.
- Screenshots or short clips for visible UI/layout changes.

## Agent Notes
Do not modify shops, persistence, or mobile layout unless the task explicitly requires it. Preserve existing prefabs, scenes, and serialized field contracts where possible.
