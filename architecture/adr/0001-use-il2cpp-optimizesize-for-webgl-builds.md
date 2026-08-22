---
status: "accepted"
date: 2026-08-22
decision-makers: Marcel Bruckner
consulted: Claude Code (build-pipeline analysis), Unity Web optimization guide
informed: ARD Game Jam 2026 team
---

# Use IL2CPP "Faster (smaller) builds" (OptimizeSize) for WebGL builds

## Context and Problem Statement

The game is published to itch.io as a WebGL build via GitHub Actions. A cold
build takes ~50–60 minutes on a standard runner, most of it Unity import and
IL2CPP/WebAssembly compilation, which makes release iterations during the jam
painfully slow. Players also download the wasm binary on every page load, so
binary size directly affects time-to-play on itch.io. Which IL2CPP Code
Generation mode should the WebGL target use?

## Decision Drivers

* CI build time per release tag (jam schedule allows only short iteration loops)
* Download size / page-load time for players on itch.io
* Runtime performance of the shipped game
* Low risk and easy reversibility during a jam

## Considered Options

* IL2CPP Code Generation: "Faster runtime" (OptimizeSpeed, Unity default)
* IL2CPP Code Generation: "Faster (smaller) builds" (OptimizeSize)
* Larger (paid) GitHub-hosted runner

## Decision Outcome

Chosen option: "Faster (smaller) builds (OptimizeSize)", because it cuts the
IL2CPP compile phase and shrinks the wasm binary at a runtime cost that only
affects generic code paths — negligible for a UI-driven quiz/reaction game —
and it is Unity's own recommendation for Web builds. It is a one-line,
trivially reversible setting, unlike paying for larger runners.

### Consequences

* Good, because the IL2CPP C++ generation/compile/link phase gets meaningfully
  shorter (estimated 5–15 minutes per cold build).
* Good, because the wasm binary is smaller, so the itch.io page loads faster
  for players.
* Bad, because generic code (e.g. tight loops over `List<T>` of structs, LINQ
  chains) runs through fully-shared implementations with lookup overhead —
  worst-case around 1.5–2× slower on those specific paths. This game has no
  per-frame generic number-crunching, so the practical impact is negligible.
* Bad, because changing `ProjectSettings.asset` rotated the CI Library cache
  key once (mitigated by `restore-keys` partial matching).

### Confirmation

The setting is serialized in `ProjectSettings/ProjectSettings.asset` as
`il2cppCodeGeneration: { WebGL: 1 }` (1 = OptimizeSize) and was applied via
`PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL,
Il2CppCodeGeneration.OptimizeSize)` in a headless editor run, not by hand-
editing the YAML. Compliance check: the key stays present in the settings
file; effectiveness check: compare the "Build WebGL" step duration and the
uploaded build size in the `Publish to itch.io` workflow before/after
(baseline: run 32571523522, ~50 min cold, built with OptimizeSpeed).

## Pros and Cons of the Options

### Faster runtime (OptimizeSpeed, Unity default)

Generates a specialized C++ copy of every generic instantiation.

* Good, because generic code runs at full speed with no lookup indirection.
* Bad, because IL2CPP generates and compiles far more C++, lengthening every
  CI build.
* Bad, because the wasm binary is larger, slowing page load on itch.io.

### Faster (smaller) builds (OptimizeSize)

Compiles only the fully-shared version of generic code; recommended by
Unity's Web optimization guide for Web targets.

* Good, because IL2CPP compile time and binary size drop.
* Good, because it is a single reversible player setting.
* Neutral, because non-generic code, engine internals, physics, and rendering
  are unaffected.
* Bad, because generics-heavy hot loops pay a runtime penalty (irrelevant for
  this game's workload).

### Larger (paid) GitHub-hosted runner

* Good, because more cores roughly halve the whole build, not just the IL2CPP
  phase.
* Bad, because larger runners are billed even for public repositories — not
  justified for a jam project.
* Bad, because it does nothing for player-facing download size.

## More Information

Related build-pipeline decisions made the same day (commits on `main`):
removal of `docker image prune` from the publish workflow (it deleted the
pre-built itch-publish action image) and splitting the Library cache into
explicit restore/save steps so a publish failure cannot discard the import
work. Revisit this decision if the game ever gains generics-heavy per-frame
logic (e.g. heavy LINQ or struct-generic math in the core loop).
