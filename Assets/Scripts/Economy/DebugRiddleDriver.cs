using UnityEngine;
using UnityEngine.InputSystem;

namespace ThinkFast.Economy
{
    /// <summary>
    /// THROWAWAY. Stands in for the puzzle half until it exists.
    ///
    /// It deliberately talks to the fighter through exactly the same seam the
    /// real puzzle system will use -- <see cref="RiddleRewards.GrantSolve"/> --
    /// and knows nothing else about the fighter. If the economy works with this
    /// driver, it will work when the real riddles are plugged in.
    ///
    /// Keys are read straight off the device rather than through the action
    /// asset, since these are test affordances, not game bindings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugRiddleDriver : MonoBehaviour
    {
        [Header("Simulated solves")]
        [Tooltip("Flow granted by a slow solve. Key 1.")]
        [SerializeField] private float slowSolveFlow = 8f;

        [Tooltip("Flow granted by a fast solve. Key 2.")]
        [SerializeField] private float fastSolveFlow = 25f;

        [Tooltip("Seconds between solves while the auto-solver is running. Key 3 toggles it.")]
        [SerializeField] private float autoSolveInterval = 0.6f;

        [Tooltip("Off by default: auto-solve fires several times a second, and Unity attaches a stack trace to every Debug.Log, so this floods the console four lines at a time. The on-screen AP and flow readouts already show what happened. Switch on only when chasing a specific solve.")]
        [SerializeField] private bool logSolves;

        private FighterResources resources;
        private bool autoSolving;
        private float autoSolveTimer;

        private void Awake()
        {
            resources = GetComponent<FighterResources>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                Solve(slowSolveFlow, "slow");
            }

            if (keyboard.digit2Key.wasPressedThisFrame)
            {
                Solve(fastSolveFlow, "fast");
            }

            if (keyboard.digit3Key.wasPressedThisFrame)
            {
                autoSolving = !autoSolving;
                autoSolveTimer = 0f;
            }

            if (keyboard.digit0Key.wasPressedThisFrame && resources != null)
            {
                autoSolving = false;
                resources.ResetResources();
            }

            if (!autoSolving)
            {
                return;
            }

            autoSolveTimer -= Time.deltaTime;
            if (autoSolveTimer <= 0f)
            {
                autoSolveTimer = autoSolveInterval;
                Solve(fastSolveFlow, "auto");
            }
        }

        private void Solve(float flow, string label)
        {
            // Going through the static seam rather than calling `resources`
            // directly is the whole point: this exercises the real integration
            // path, including the case where no fighter is listening.
            RiddleRewards.GrantSolve(flow);

            if (logSolves)
            {
                Debug.Log($"[DebugRiddleDriver] {label} solve: +1 AP, +{flow} flow requested.");
            }
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            GUILayout.BeginArea(new Rect(12f, Screen.height - 108f, 420f, 100f));
            GUILayout.Label("DEBUG RIDDLES  (stand-in for the puzzle half)", style);
            GUILayout.Label($"1  slow solve  (+1 AP, +{slowSolveFlow} flow)", style);
            GUILayout.Label($"2  fast solve  (+1 AP, +{fastSolveFlow} flow)", style);
            GUILayout.Label($"3  auto-solve {(autoSolving ? "ON" : "off")}     0  reset", style);
            GUILayout.EndArea();
        }
    }
}
