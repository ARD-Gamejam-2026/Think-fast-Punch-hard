# TODO

## Before making this repository public

- [ ] Deal with the self-hosted runner (`mbruckner-m4max`, Marcel's Mac).
  On a public repository, anyone can fork and open a PR whose workflow code
  runs on the self-hosted machine — `test-fast.yml` triggers on
  `pull_request`, so this is an arbitrary-code-execution risk.
  Options, in order of safety:
  - Remove the runner and point `test-fast.yml` back at `ubuntu-latest`
    with `game-ci/unity-test-runner` (see `test-full.yml` for the pattern).
  - Keep the runner but set Settings → Actions → General →
    "Require approval for all external contributors" and never approve
    workflow runs from forks without reading their diff.

## Cleanup

- [ ] Remove the smoke test (`Assets/Scripts/Smoke`, `Assets/Tests/Smoke`,
  including `.meta` files) once real gameplay tests exist — it only proves
  the CI test + coverage pipeline works.
