# Think fast, Punch hard

- https://miro.com/app/board/uXjVHv0lDhg=/
- https://itch.io/jam/-ard-game-jam-2026
- https://rhinocerosgamesproduction.itch.io/think-fast-punch-hard

## Quiz system

The quiz gameplay (questions, timer, random math, live Wikipedia place
questions) is documented in [`Assets/Quiz/README.md`](Assets/Quiz/README.md).

**Attribution:** place-question images are loaded live from Wikipedia
(Wikimedia Commons) — the itch.io page must carry an
"Images: Wikipedia (Wikimedia Commons)" credit.

## How to release

Push a `v*` tag — CI builds the WebGL player and publishes it to [itch.io](https://rhinocerosgamesproduction.itch.io/think-fast-punch-hard):

```
git tag v0.1
git push origin v0.1
```

Progress shows up under [Actions](https://github.com/ARD-Gamejam-2026/Think-fast-Punch-hard/actions) (a build takes a few minutes, up to ~30 without a warm cache). The tag name becomes the version on itch. A run can also be started manually from the Actions tab ("Publish to itch.io" → Run workflow).
