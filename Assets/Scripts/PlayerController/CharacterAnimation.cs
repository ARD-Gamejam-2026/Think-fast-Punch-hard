using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// Drives the fighter Animator from gameplay events and movement state.
    ///
    /// Lives on the same GameObject as the <see cref="Animator"/> (typically the
    /// visual child). One-shots -- attack, hit, knockout -- arrive through
    /// <see cref="ICharacterAnimation"/>; idle, walk, jump and fall are derived
    /// each frame from <see cref="PlayerController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class CharacterAnimation : MonoBehaviour, ICharacterAnimation
    {
        private const string IdleState = "Idle";
        private const string WalkState = "Walk";
        private const string JumpState = "Jump";
        private const string FallState = "Fall";
        private const string HitState = "Hit";
        private const string HitAirState = "Hit_Air";
        private const string AttackState = "Attack";
        private const string AttackAirState = "Attack_Air";
        private const string KnockoutState = "KO_Fall";

        private const float CrossFadeDuration = 0.08f;
        private const float WalkSpeedThreshold = 0.25f;
        private const float RisingVelocityThreshold = 0.1f;

        [Tooltip("How long to hold a hit clip before locomotion may resume, even if hitstun ends first.")]
        [SerializeField] private float hitClipHold = 0.25f;

        private Animator animator;
        private PlayerController controller;

        private int currentStateHash;
        private bool isKnockedOut;
        private bool hitClipActive;
        private float hitClipTimer;
        private bool attackClipActive;
        private bool attackClipAirborne;
        private int attackStateHash;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            controller = GetComponentInParent<PlayerController>();

            if (controller == null)
            {
                Debug.LogError($"{nameof(CharacterAnimation)} on '{name}' needs a {nameof(PlayerController)} on a parent.", this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (animator == null || controller == null)
            {
                return;
            }

            ApplyFacing();
            animator.SetBool("isGrounded", controller.IsGrounded);

            if (isKnockedOut)
            {
                PlayState(KnockoutState);
                return;
            }

            if (hitClipActive)
            {
                hitClipTimer -= Time.deltaTime;
                if (hitClipTimer <= 0f && !controller.IsStunned)
                {
                    hitClipActive = false;
                }
                else
                {
                    return;
                }
            }

            if (attackClipActive)
            {
                bool landedDuringAirAttack = attackClipAirborne && controller.IsGrounded;
                if (!landedDuringAirAttack && !IsAttackClipFinished())
                {
                    return;
                }

                attackClipActive = false;
                attackClipAirborne = false;
            }

            UpdateLocomotion();
        }

        /// <inheritdoc />
        public void NotifyJump()
        {
            if (isKnockedOut || hitClipActive)
            {
                return;
            }

            attackClipActive = false;
            attackClipAirborne = false;
            PlayState(JumpState);
        }

        /// <inheritdoc />
        public void NotifyAttackStarted(bool airborne)
        {
            if (isKnockedOut || hitClipActive)
            {
                return;
            }

            attackClipActive = true;
            attackClipAirborne = airborne;
            string stateName = airborne ? AttackAirState : AttackState;
            attackStateHash = Animator.StringToHash(stateName);
            PlayState(stateName);
        }

        /// <inheritdoc />
        public void NotifyHit(bool airborne)
        {
            if (isKnockedOut)
            {
                return;
            }

            attackClipActive = false;
            attackClipAirborne = false;
            hitClipActive = true;
            hitClipTimer = hitClipHold;
            PlayState(airborne ? HitAirState : HitState);
        }

        /// <inheritdoc />
        public void NotifyKnockout()
        {
            isKnockedOut = true;
            attackClipActive = false;
            attackClipAirborne = false;
            hitClipActive = false;
            PlayState(KnockoutState);
        }

        /// <inheritdoc />
        public void NotifyRespawn()
        {
            isKnockedOut = false;
            attackClipActive = false;
            attackClipAirborne = false;
            hitClipActive = false;
            currentStateHash = 0;
            UpdateLocomotion();
        }

        private void UpdateLocomotion()
        {
            if (!controller.IsGrounded)
            {
                if (controller.Velocity.y > RisingVelocityThreshold)
                {
                    PlayState(JumpState);
                }
                else
                {
                    PlayState(FallState);
                }

                return;
            }

            if (Mathf.Abs(controller.Velocity.x) >= WalkSpeedThreshold)
            {
                PlayState(WalkState);
            }
            else
            {
                PlayState(IdleState);
            }
        }

        private void PlayState(string stateName)
        {
            int hash = Animator.StringToHash(stateName);
            if (currentStateHash == hash)
            {
                return;
            }

            animator.CrossFade(stateName, CrossFadeDuration);
            currentStateHash = hash;
        }

        /// <summary>
        /// True once the active attack clip has played through, even if
        /// <see cref="PlayerAttack.IsAttacking"/> already returned to false.
        /// Air attacks are released early by landing instead; see
        /// <see cref="LateUpdate"/>.
        /// </summary>
        private bool IsAttackClipFinished()
        {
            if (animator.IsInTransition(0))
            {
                return false;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash != attackStateHash)
            {
                return true;
            }

            return state.normalizedTime >= 1f;
        }

        /// <summary>
        /// Yaws the mesh so it faces the same way as <see cref="PlayerController.Facing"/>.
        /// </summary>
        private void ApplyFacing()
        {
            float yaw = controller.Facing > 0f ? 90f : -90f;
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
