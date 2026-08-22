using ThinkFast.Common;
using UnityEngine;

namespace ThinkFast.CameraRig
{
    /// <summary>
    /// Follows the fighter with a dead zone, smoothing and look-ahead.
    ///
    /// The three parts each solve a different problem, and all three are needed:
    ///
    ///   Dead zone  -- a box in the middle the target can move inside without the
    ///                 camera reacting at all. Without it, every small step and
    ///                 every landing nudges the camera and the whole screen
    ///                 twitches constantly.
    ///   Smoothing  -- the camera eases toward where it wants to be instead of
    ///                 snapping. Separate vertical timing, because a jump should
    ///                 not throw the camera around the way running does.
    ///   Look-ahead -- the camera leads in the direction you are actually moving,
    ///                 so you see more of where you are going than where you have
    ///                 been. In a fighter that is the difference between reacting
    ///                 and being surprised.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [SplitScreenTodo("If the riddle UI overlaps or crowds the fighter view, bias X here so the fighter sits in the clear part of the viewport rather than dead centre.")]
        [Tooltip("Framing offset from the target. A little upward bias means the fighter sits slightly low, leaving room to see what is above.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

        [Header("Dead zone")]
        [SplitScreenTodo("Sized against a full-screen 16:9 view. A half-width viewport is much narrower, so this box will cover a far larger share of the screen and will feel sluggish unless X is reduced.")]
        [Tooltip("Size of the central box the target can move inside without the camera following. Wider means calmer; too wide and the camera feels like it is lagging behind you.")]
        [SerializeField] private Vector2 deadZone = new Vector2(3f, 2.4f);

        [Header("Smoothing")]
        [Tooltip("Roughly how long the camera takes to catch up horizontally.")]
        [SerializeField] private float horizontalSmoothTime = 0.22f;

        [Tooltip("Deliberately slower than horizontal, so jumping does not bounce the whole screen.")]
        [SerializeField] private float verticalSmoothTime = 0.45f;

        [Header("Look ahead")]
        [SplitScreenTodo("Look-ahead matters more the narrower the view, since less of the stage is visible ahead of you. Expect to raise this once the fighter is in a half-width viewport.")]
        [Tooltip("How far the camera leads at full running speed.")]
        [SerializeField] private float lookAheadDistance = 2f;

        [SerializeField] private float lookAheadSmoothTime = 0.35f;

        [Tooltip("Speed treated as 'full' for look-ahead. Should match the fighter's top running speed.")]
        [SerializeField] private float lookAheadSpeedReference = 9f;

        [Header("Bounds")]
        [Tooltip("Stops the camera showing past the edges of the stage.")]
        [SerializeField] private bool useBounds = true;

        [SplitScreenTodo("These MUST be recalculated for split screen. They are derived from how wide the camera sees, which depends on viewport aspect: halfWidth = tan(fov/2) * distance * aspect, and the limit is stageHalfWidth - halfWidth. A half-width viewport roughly halves the aspect, so the camera gains a lot of room and these values become far too tight. Camera distance (currently z = -9) and FOV want a pass at the same time.")]
        [SerializeField] private Vector2 boundsMin = new Vector2(-5.8f, 3f);

        [SerializeField] private Vector2 boundsMax = new Vector2(5.8f, 5.5f);

        private Vector2 smoothVelocity;
        private float lookAhead;
        private float lookAheadVelocity;
        private Vector3 previousTargetPosition;
        private bool hasPreviousPosition;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            hasPreviousPosition = false;
        }

        private void OnEnable()
        {
            hasPreviousPosition = false;
        }

        // LateUpdate, so the fighter has already moved this frame. Following in
        // Update would always be a frame stale and would visibly judder.
        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            Vector3 targetPosition = target.position;

            // Velocity is measured from the target's own movement rather than
            // read off its Rigidbody, so this component stays usable for
            // anything, not just the player.
            float targetVelocityX = hasPreviousPosition
                ? (targetPosition.x - previousTargetPosition.x) / dt
                : 0f;

            previousTargetPosition = targetPosition;
            hasPreviousPosition = true;

            float speedRatio = lookAheadSpeedReference > 0f
                ? Mathf.Clamp(targetVelocityX / lookAheadSpeedReference, -1f, 1f)
                : 0f;

            lookAhead = Mathf.SmoothDamp(
                lookAhead, speedRatio * lookAheadDistance, ref lookAheadVelocity, lookAheadSmoothTime);

            var focus = new Vector2(
                targetPosition.x + offset.x + lookAhead,
                targetPosition.y + offset.y);

            Vector3 position = transform.position;

            float desiredX = ResolveAxis(position.x, focus.x, deadZone.x);
            float desiredY = ResolveAxis(position.y, focus.y, deadZone.y);

            if (useBounds)
            {
                desiredX = Mathf.Clamp(desiredX, boundsMin.x, boundsMax.x);
                desiredY = Mathf.Clamp(desiredY, boundsMin.y, boundsMax.y);
            }

            position.x = Mathf.SmoothDamp(position.x, desiredX, ref smoothVelocity.x, horizontalSmoothTime);
            position.y = Mathf.SmoothDamp(position.y, desiredY, ref smoothVelocity.y, verticalSmoothTime);
            transform.position = position;
        }

        /// <summary>
        /// Returns where the camera wants to be on one axis. Inside the dead zone
        /// that is simply where it already is, which is what stops small movements
        /// from moving the camera at all.
        /// </summary>
        private static float ResolveAxis(float current, float focus, float deadZoneSize)
        {
            float half = deadZoneSize * 0.5f;
            float delta = focus - current;

            if (Mathf.Abs(delta) <= half)
            {
                return current;
            }

            // Pull only as far as needed to put the target back on the dead
            // zone's edge, never all the way to centre. Recentring fully would
            // make the camera chase every step.
            return focus - (Mathf.Sign(delta) * half);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(deadZone.x, deadZone.y, 0.1f));

            if (!useBounds)
            {
                return;
            }

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            var centre = new Vector3((boundsMin.x + boundsMax.x) * 0.5f, (boundsMin.y + boundsMax.y) * 0.5f, transform.position.z);
            var size = new Vector3(Mathf.Abs(boundsMax.x - boundsMin.x), Mathf.Abs(boundsMax.y - boundsMin.y), 0.1f);
            Gizmos.DrawWireCube(centre, size);
        }
    }
}
