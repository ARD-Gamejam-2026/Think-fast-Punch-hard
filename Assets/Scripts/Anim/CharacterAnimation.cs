using ThinkFast.Combat;
using UnityEngine;

namespace ThinkFast.Anim
{
    /// <summary>
    /// Drives the fighter Animator from gameplay events and movement state.
    ///
    /// Lives on the same GameObject as the <see cref="Animator"/> (typically the
    /// visual child). One-shots -- attack, hit, knockout -- arrive through
    /// <see cref="ICharacterAnimation"/>; idle, walk, jump and fall are derived
    /// each frame from the parent body's <see cref="IFighterMotor"/>.
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

        private Animator animator;
        private IFighterMotor motor;

        private int currentStateHash;
        private bool isKnockedOut;
        private bool hitClipActive;
        private int hitStateHash;
        private bool attackClipActive;
        private bool attackClipAirborne;
        private int attackStateHash;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            motor = GetComponentInParent<IFighterMotor>();

            if (motor == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterAnimation)} on '{name}' needs a component implementing {nameof(IFighterMotor)} on a parent.",
                    this);
                enabled = false;
            }
        }

        private void LateUpdate()
        {
            if (animator == null || motor == null)
            {
                return;
            }

            ApplyFacing();
            animator.SetBool("isGrounded", motor.IsGrounded);

            if (isKnockedOut)
            {
                PlayState(KnockoutState);
                return;
            }

            if (hitClipActive)
            {
                if (motor.IsStunned)
                {
                    MaintainHitState();
                    return;
                }

                hitClipActive = false;
            }

            if (attackClipActive)
            {
                bool landedDuringAirAttack = attackClipAirborne && motor.IsGrounded;
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
            PlayState(JumpState, true);
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
            PlayState(stateName, true);
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
            string stateName = airborne ? HitAirState : HitState;
            hitStateHash = Animator.StringToHash(stateName);
            PlayState(stateName, true);
        }

        /// <inheritdoc />
        public void NotifyKnockout()
        {
            isKnockedOut = true;
            attackClipActive = false;
            attackClipAirborne = false;
            hitClipActive = false;
            PlayState(KnockoutState, true);
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
            if (!motor.IsGrounded)
            {
                if (motor.Velocity.y > RisingVelocityThreshold)
                {
                    PlayState(JumpState);
                }
                else
                {
                    PlayState(FallState);
                }

                return;
            }

            if (Mathf.Abs(motor.Velocity.x) >= WalkSpeedThreshold)
            {
                PlayState(WalkState);
            }
            else
            {
                PlayState(IdleState);
            }
        }

        private void PlayState(string stateName, bool interrupt = false)
        {
            int hash = Animator.StringToHash(stateName);

            if (interrupt)
            {
                animator.Play(hash, 0, 0);
            }
            else if (currentStateHash != hash)
            {
                animator.CrossFade(stateName, CrossFadeDuration);
            }
            else
            {
                return;
            }

            currentStateHash = hash;
        }

        /// <summary>
        /// Keeps the hit clip active while stun lasts, even if the Animator
        /// controller would otherwise transition out early.
        /// </summary>
        private void MaintainHitState()
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == hitStateHash)
            {
                return;
            }

            animator.Play(hitStateHash, 0, 0f);
            currentStateHash = hitStateHash;
        }

        /// <summary>
        /// True once the active attack clip has played through, even if combat
        /// recovery already returned to false. Air attacks are released early by
        /// landing instead; see <see cref="LateUpdate"/>.
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
        /// Yaws the mesh so it faces the same way as <see cref="IFighterMotor.Facing"/>.
        /// </summary>
        private void ApplyFacing()
        {
            float yaw = motor.Facing > 0f ? 90f : -90f;
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
