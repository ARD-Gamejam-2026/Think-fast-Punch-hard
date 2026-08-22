# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

"Think fast, Punch hard" — a game for the ARD Game Jam 2026, built with **Unity 6000.5.9f1** using the **Universal Render Pipeline (URP)** and the **new Input System** (`com.unity.inputsystem`). The project is currently in template state (Unity URP starter template); gameplay code has not been added yet.

Links:
- Design board: https://miro.com/app/board/uXjVHv0lDhg=/
- Jam page: https://itch.io/jam/-ard-game-jam-2026
- Game page: https://rhinocerosgamesproduction.itch.io/think-fast-punch-hard

## Commands

Unity is installed via Unity Hub at `/Applications/Unity/Hub/Editor/6000.5.9f1/`. The editor binary for CLI use:

```
UNITY="/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity"
```

Run EditMode tests (batch mode; project must not be open in the editor at the same time):

```
"$UNITY" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults results.xml -logFile -
```

Run PlayMode tests: same command with `-testPlatform PlayMode`. Filter to a single test with `-testFilter "Namespace.Class.TestName"`.

Headless build check (compile without opening the editor):

```
"$UNITY" -batchmode -quit -projectPath . -logFile -
```

There is no lint step; C# compilation errors surface in the Unity log / `Logs/`.

## Structure & Conventions

- `Assets/Scenes/SampleScene.unity` — the only scene, and the one registered in Build Settings.
- `Assets/Settings/` — URP render pipeline assets: separate `PC_RPAsset`/`PC_Renderer` and `Mobile_RPAsset`/`Mobile_Renderer` quality tiers, plus the default volume profile.
- `Assets/InputSystem_Actions.inputactions` — input action map (Player + UI action maps from the template). Use the Input System package for all input, not the legacy `Input` class.
- `Assets/TutorialInfo/` — Unity template readme scaffolding; safe to delete when the project gets real content.
- Quiz code lives in its own asmdefs (`Quiz`, `Quiz.Editor`, `Quiz.EditModeTests` — required by the Unity Test Framework); everything else compiles into `Assembly-CSharp`.

## Code Style (enforced by Teamscale on PRs)

The project is analyzed by Teamscale (`.teamscale.toml`; fetch/fix findings
via the teamscale plugin skills). Write new code so it passes the profile:

- **Always use braces**, even for single-statement `if`/`else`/loop bodies.
- **Document the public runtime API** with XML `<summary>` comments; word
  them to share vocabulary with the member name (Teamscale flags comments
  as "unrelated" when they have no word overlap with the identifier).
  Test methods need no comments — descriptive names suffice.
- **No ternary operators** (the profile flags them) — use `if`/`else`.
- `switch` statements need a `default` case; tuple element names are
  PascalCase.
- Keep methods under ~30 source lines where reasonable; extract helpers
  (e.g. a try/catch or a repeated request pattern) instead of nesting
  deeper than 3 levels.
- Static analyzers cannot see through weight/flag gating: when an invariant
  guarantees non-null (e.g. "this branch is only reachable when X != null"),
  make it explicit with a pattern guard or null check instead of relying on
  the invariant.

Known Unity false-positive patterns (flag in Teamscale, do NOT "fix"):
- Unity lifecycle methods (`Awake`, `Start`, `Update`, `OnEnable`, …) are
  engine-invoked — never remove them as "unused".
- **Never rename serialized/JSON-mapped public fields** to PascalCase
  (`questionText`, `thumbnail.source`, …): JsonUtility parsing and existing
  `.asset`/prefab/scene data depend on the exact names.
- `Action<>`-based events are the Unity idiom; do not convert to
  `EventHandler<T>` (and the quiz event signatures are a cross-team
  contract).
- `while (true)` + `yield` coroutines are the standard prefetch/loop
  pattern, not "infinite loops".

Unity-specific rules that matter here:
- Every asset and folder under `Assets/` has a paired `.meta` file — always move/rename/delete them together, and commit `.meta` files with their assets.
- `Library/`, `Logs/`, `UserSettings/`, and the generated `.csproj`/`.slnx` files are build artifacts (gitignored); never edit them by hand.
- Scene files and most `ProjectSettings/` assets are Unity-serialized YAML — prefer making scene/settings changes via small editor scripts or by minimal targeted YAML edits, and avoid reformatting these files.
