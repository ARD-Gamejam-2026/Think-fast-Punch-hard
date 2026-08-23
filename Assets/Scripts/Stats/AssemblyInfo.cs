using System.Runtime.CompilerServices;

// The brief's HighscoreBackendTests exercises RestFirebaseBackend's internal
// static helpers (BuildCollectionUrl, SerializeRecord, ParseCollection)
// directly, so the EditMode test assembly needs internals access.
[assembly: InternalsVisibleTo("Quiz.EditModeTests")]
