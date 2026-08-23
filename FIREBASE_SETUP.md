# Firebase setup — highscores

> ✅ **The Firebase project is already set up** (Realtime Database created,
> Anonymous auth enabled, rules published) and its database URL + public Web API
> key are **baked into the game** as `EndScreenUploader` defaults. **The only
> thing left is wiring the components into the scenes — jump to
> [step 7](#7-wire-the-components-into-the-scenes-unity-editor).**
>
> Steps 1–6 below are reference: how the project was set up, and what to change
> if you ever point the game at a *different* Firebase project.

The highscores backend stores one record per finished round in a Firebase
**Realtime Database** and authenticates each write with an anonymous ID token,
so **players set up nothing** — the game ships a public Web API key and signs in
anonymously on their behalf.

You only need two values from Firebase: a **database URL** and a **Web API key**.
Both are pasted into the `EndScreenUploader` component in `Scene_End`.

Everything below is done in the Firebase console
(<https://console.firebase.google.com>) — no CLI, no SDK, no `google-services`
file.

## 1. Create a project

*Add project* → give it a name (e.g. `think-fast-punch-hard`) → Google
Analytics can be disabled (not needed) → **Create project**.

## 2. Create the Realtime Database

*Build → Realtime Database → Create Database*.

- Pick a location (e.g. United States, or Europe-West1).
- Choose **Start in locked mode** (proper rules are set in step 4 — do not use
  test mode, its open rules expire after 30 days).

Copy the URL shown at the top of the **Data** tab, e.g.
`https://think-fast-punch-hard-default-rtdb.firebaseio.com`
(or a regional form like `https://<name>.europe-west1.firebasedatabase.app`).

➡️ This is **`databaseUrl`**.

> ⚠️ Make sure it is **Realtime Database**, not **Firestore Database** — they
> are different products and the code targets Realtime Database.

## 3. Enable anonymous sign-in

*Build → Authentication → Get started* → **Sign-in method** tab → select
**Anonymous** → **Enable** → **Save**.

## 4. Set the database rules

*Realtime Database → Rules* tab → replace the contents with the following, then
**Publish**:

```json
{
  "rules": {
    "highscores": {
      ".read": "auth != null",
      ".write": "auth != null"
    }
  }
}
```

This allows only signed-in callers to read or write the `highscores` node.

## 5. Get the Web API key

*⚙️ (gear) → Project settings → General*.

Under **Your apps**, if there are no apps yet, click the **Web** icon (`</>`),
register an app (any nickname, skip Hosting) — this makes the key appear. You
do not need anything else from the app config.

Copy the **Web API key** (looks like `AIzaSy...`).

➡️ This is **`webApiKey`**. It is designed to be public/embeddable, so shipping
it in the build is expected.

## 6. Put the values in the game

The project's database URL and Web API key are already committed as the
**default values** of the `EndScreenUploader` component
(`Assets/Scripts/StatsBridge/EndScreenUploader.cs`), so a freshly added
component is pre-filled — you normally do not need to type them in.

If you point the game at a **different** Firebase project, override them in the
Inspector on the `EndScreenUploader` in `Scene_End`:

- **Database Url** → the URL from step 2
- **Web Api Key** → the key from step 5

If **either** value is empty, the game falls back to an in-memory store and
uploads nothing (a warning is logged). Both must be set to go live.

## 7. Wire the components into the scenes (Unity editor)

The scripts are in the project but must be attached to scene objects.

**`Assets/Scenes/SampleScene.unity`** (the fight):

1. Select the **player** fighter → *Add Component* → **Fighter Stats Reporter** →
   set **Role = Player**. Leave `health` empty if the `Health` component is on
   the same GameObject; otherwise assign it.
2. Select the **opponent** fighter → add **Fighter Stats Reporter** →
   **Role = Opponent**.
3. Select any scene object (e.g. the quiz root that already holds
   `QuizRewardBridge`) → add **Match Stats Coordinator**. Leave `quiz` empty to
   auto-find the `QuizController`.

**`Assets/Scenes/Scene_End.unity`** (the end screen):

4. Select an object (e.g. the one with `EndScreen`) → add **End Screen Uploader**.
   Confirm `Database Url` and `Web Api Key` are populated (from the defaults).

**`Assets/Scenes/Scene_Menu.unity`** (optional — the player name):

5. If there is a TMP `InputField` for the name, add **Player Name Field** and
   assign the field. Without it, names default to `"anon"`.

## 8. Verify

Press Play, and win a fight. Then:

- Open *Realtime Database → Data* in the console — a `highscores` node should
  appear with a child holding all the record fields (`playerName`,
  `timeToBeatOpponent`, quiz counts, `damageDealt`/`damageTaken`, both end
  healths, `playerWon`, `finishedAt`).
- If nothing appears, check the Unity Console:
  - `"anonymous sign-in returned no token"` → the Anonymous provider (step 3)
    or the Web API key (step 5) is wrong.
  - a warning with an HTTP error → the rules (step 4) or the database URL are
    wrong.

## How it works (for reference)

- `FirebaseAnonymousAuth` calls the Firebase Auth REST endpoint
  `accounts:signUp?key=<webApiKey>` with `{"returnSecureToken":true}` to get a
  short-lived ID token.
- `RestFirebaseBackend` appends that token as `?auth=<idToken>` on each Realtime
  Database REST call (`POST`/`GET <databaseUrl>/highscores.json`). Sign-in
  happens once per upload (uploads are once-per-match), so there is no token
  refresh to manage.
- All of this sits behind the `IHighscoreBackend` seam, so the storage choice
  can be swapped without touching gameplay.

## Security note

With `auth != null` scoped to `/highscores`, only signed-in callers can touch
the highscores. Because the Web API key ships with the game, someone who
extracts the build can also sign in anonymously and write to `/highscores` — but
the blast radius is limited to that node; no other data and no master/full-
database access is exposed. That is the practical ceiling for a game that
requires no player setup. Tightening further (per-entry ownership, writes behind
a Cloud Function) is a possible follow-up.
