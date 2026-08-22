using UnityEngine;
using UnityEngine.InputSystem;

namespace ThinkFast.Player
{
    /// <summary>
    /// Turns the raw Input System action map into plain values the movement code
    /// can read. The fighter half of the game is keyboard-only on purpose: the
    /// mouse belongs to the puzzle half and must never be needed here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [Header("Bindings")]
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string actionMapName = "Fighter";

        private InputActionMap map;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;

        // A press is latched in Update and drained in FixedUpdate. The two run at
        // different rates, so reading "was pressed this frame" straight from
        // FixedUpdate would drop presses on some frames and see them twice on
        // others.
        private bool jumpPressLatch;
        private bool attackPressLatch;
        private bool downPressLatch;
        private bool downWasHeld;

        /// <summary>Raw WASD/arrow value. X drives running, Y is reserved for fast-fall and crouch.</summary>
        public Vector2 Move { get; private set; }

        public float MoveX => Move.x;

        /// <summary>True for as long as the jump key is down. Drives variable jump height.</summary>
        public bool JumpHeld { get; private set; }

        /// <summary>True while down is held. Drives fast fall, which is a hold rather than a tap.</summary>
        public bool DownHeld { get; private set; }

        /// <summary>
        /// Returns true once per fresh press of down, then forgets it. Dropping
        /// through a platform is edge-triggered so that holding down (to crouch,
        /// later) cannot make you fall through repeatedly.
        /// </summary>
        public bool ConsumeDownPress()
        {
            bool pressed = downPressLatch;
            downPressLatch = false;
            return pressed;
        }

        /// <summary>
        /// Returns true once per physical jump press, then forgets it. Call this
        /// from FixedUpdate exactly once per step.
        /// </summary>
        public bool ConsumeJumpPress()
        {
            bool pressed = jumpPressLatch;
            jumpPressLatch = false;
            return pressed;
        }

        /// <summary>
        /// Returns true once per physical attack press, then forgets it. Call
        /// this from FixedUpdate exactly once per step.
        /// </summary>
        public bool ConsumeAttackPress()
        {
            bool pressed = attackPressLatch;
            attackPressLatch = false;
            return pressed;
        }

        private void Awake()
        {
            if (actions == null)
            {
                Debug.LogError($"{nameof(PlayerInputReader)} on '{name}' has no InputActionAsset assigned.", this);
                enabled = false;
                return;
            }

            map = actions.FindActionMap(actionMapName, throwIfNotFound: false);
            if (map == null)
            {
                Debug.LogError($"Action map '{actionMapName}' not found in '{actions.name}'.", this);
                enabled = false;
                return;
            }

            moveAction = map.FindAction("Move", throwIfNotFound: false);
            jumpAction = map.FindAction("Jump", throwIfNotFound: false);
            attackAction = map.FindAction("Attack", throwIfNotFound: false);

            if (moveAction == null || jumpAction == null || attackAction == null)
            {
                Debug.LogError($"Map '{actionMapName}' is missing a 'Move', 'Jump' or 'Attack' action.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            map?.Enable();
        }

        private void OnDisable()
        {
            map?.Disable();
            Move = Vector2.zero;
            JumpHeld = false;
            jumpPressLatch = false;
            attackPressLatch = false;
            downPressLatch = false;
            downWasHeld = false;
            DownHeld = false;
        }

        private void Update()
        {
            Move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

            bool downNow = Move.y < -0.5f;
            if (downNow && !downWasHeld)
            {
                downPressLatch = true;
            }

            downWasHeld = downNow;
            DownHeld = downNow;

            if (jumpAction == null)
            {
                return;
            }

            JumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame())
            {
                jumpPressLatch = true;
            }

            if (attackAction != null && attackAction.WasPressedThisFrame())
            {
                attackPressLatch = true;
            }
        }
    }
}
