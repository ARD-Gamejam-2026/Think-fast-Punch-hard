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

        [Tooltip("Framing offset from the target. A little upward bias means the fighter sits slightly low, leaving room to see what is above. The quiz has its own viewport rather than sitting on top of this one, so nothing needs biasing out from under it.")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

        [Header("Dead zone")]
        [Tooltip("Size of the central box the target can move inside without the camera following. Wider means calmer; too wide and the camera feels like it is lagging behind you. Authored against a full-screen view -- see the adaptation setting below.")]
        [SerializeField] private Vector2 deadZone = new Vector2(3f, 2.4f);

        [Tooltip("Scales the dead zone's width with how much world the camera can actually see. A split-screen viewport is narrower, so an unscaled box covers a larger share of the screen and the camera starts to feel like it is lagging behind you.")]
        [SerializeField] private bool adaptDeadZoneToViewport = true;

        [Tooltip("Visible half-width the dead zone width was authored against: a 60 degree camera 9 units from the fighters on a full-screen 16:9 view. Only used to scale the dead zone, never the framing.")]
        [SerializeField, Min(0.01f)] private float deadZoneReferenceHalfWidth = 9.24f;

        [Header("Smoothing")]
        [Tooltip("Roughly how long the camera takes to catch up horizontally.")]
        [SerializeField] private float horizontalSmoothTime = 0.22f;

        [Tooltip("Deliberately slower than horizontal, so jumping does not bounce the whole screen.")]
        [SerializeField] private float verticalSmoothTime = 0.45f;

        [Header("Look ahead")]
        [Tooltip("How far the camera leads at full running speed. Left at its full-screen value on purpose: a narrower viewport shows less world, so the same 2 units already lead across a larger share of the screen.")]
        [SerializeField] private float lookAheadDistance = 2f;

        [SerializeField] private float lookAheadSmoothTime = 0.35f;

        [Tooltip("Speed treated as 'full' for look-ahead. Should match the fighter's top running speed.")]
        [SerializeField] private float lookAheadSpeedReference = 9f;

        [Header("Bounds")]
        [Tooltip("Stops the camera showing past the edges of the stage.")]
        [SerializeField] private bool useBounds = true;

        [Tooltip("Works out the horizontal limits from how wide the camera actually sees, instead of taking them from the values below. This is what makes the bounds survive split screen: a narrower viewport shows less world, so the camera can travel further before the stage edge comes into view, and a fixed pair of numbers would pin it far too tightly.")]
        [SerializeField] private bool deriveHorizontalBounds = true;

        [Tooltip("Distance from the centre of the stage to its edge. The camera is kept this far in, minus however much world it can see.")]
        [SerializeField] private float stageHalfWidth = 15f;

        [Tooltip("Z the fighters stand on. How far the camera is from this plane is what decides how much world it sees.")]
        [SerializeField] private float gameplayPlaneZ;

        [Tooltip("Horizontal limits used only when the derivation above is switched off. Vertical limits are always taken from here -- split screen changes the width of the viewport, never its height.")]
        [SerializeField] private Vector2 boundsMin = new Vector2(-5.8f, 3f);

        [SerializeField] private Vector2 boundsMax = new Vector2(5.8f, 5.5f);

        private Vector2 smoothVelocity;
        private float lookAhead;
        private float lookAheadVelocity;
        private Vector3 previousTargetPosition;
        private bool hasPreviousPosition;
        private Camera view;

        /// <summary>
        /// The camera being driven. Resolved lazily rather than in Awake so the
        /// gizmos draw the real dead zone and bounds in edit mode too.
        /// </summary>
        private Camera View
        {
            get
            {
                if (view == null)
                {
                    view = GetComponent<Camera>();
                }

                return view;
            }
        }

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
            float visibleHalfWidth = VisibleHalfWidth();

            float desiredX = ResolveAxis(position.x, focus.x, ResolveDeadZoneWidth(visibleHalfWidth));
            float desiredY = ResolveAxis(position.y, focus.y, deadZone.y);

            if (useBounds)
            {
                ResolveHorizontalLimits(visibleHalfWidth, out float limitMin, out float limitMax);
                desiredX = Mathf.Clamp(desiredX, limitMin, limitMax);
                desiredY = Mathf.Clamp(desiredY, boundsMin.y, boundsMax.y);
            }

            position.x = Mathf.SmoothDamp(position.x, desiredX, ref smoothVelocity.x, horizontalSmoothTime);
            position.y = Mathf.SmoothDamp(position.y, desiredY, ref smoothVelocity.y, verticalSmoothTime);
            transform.position = position;
        }

        /// <summary>
        /// How much world the camera can see to either side of itself, at the
        /// distance the fighters actually stand. This is the number every
        /// viewport-dependent setting is derived from, and it already accounts
        /// for a viewport rect: a camera confined to half the screen reports
        /// half the aspect, so it sees half as wide.
        /// </summary>
        private float VisibleHalfWidth()
        {
            Camera camera = View;
            if (camera == null)
            {
                return 0f;
            }

            if (camera.orthographic)
            {
                return camera.orthographicSize * camera.aspect;
            }

            float distance = Mathf.Abs(transform.position.z - gameplayPlaneZ);
            float halfHeight = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distance;
            return halfHeight * camera.aspect;
        }

        /// <summary>
        /// Returns the dead zone width to use, scaled so the box stays the same
        /// share of what the player can see however wide the viewport is.
        /// </summary>
        private float ResolveDeadZoneWidth(float visibleHalfWidth)
        {
            if (!adaptDeadZoneToViewport || visibleHalfWidth <= 0f)
            {
                return deadZone.x;
            }

            return deadZone.x * (visibleHalfWidth / deadZoneReferenceHalfWidth);
        }

        /// <summary>
        /// Returns how far the camera may travel horizontally. Derived, the limit
        /// is the stage edge minus however much world is on screen, so the view
        /// stops exactly as the edge would come into frame -- which is why it
        /// holds at any viewport width instead of needing a second set of
        /// hand-tuned numbers per layout.
        /// </summary>
        private void ResolveHorizontalLimits(float visibleHalfWidth, out float limitMin, out float limitMax)
        {
            if (!deriveHorizontalBounds)
            {
                limitMin = boundsMin.x;
                limitMax = boundsMax.x;
                return;
            }

            // Clamped at zero: a camera that sees wider than the whole stage has
            // nowhere legal to go, and should sit in the middle rather than
            // invert its own limits.
            limitMax = Mathf.Max(0f, stageHalfWidth - visibleHalfWidth);
            limitMin = -limitMax;
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
            // Drawn from the same helpers the camera actually uses, so the boxes
            // shown in the editor are the ones in force under the current
            // viewport rather than the authored values.
            float visibleHalfWidth = VisibleHalfWidth();

            Gizmos.color = new Color(1f, 1f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(ResolveDeadZoneWidth(visibleHalfWidth), deadZone.y, 0.1f));

            if (!useBounds)
            {
                return;
            }

            ResolveHorizontalLimits(visibleHalfWidth, out float limitMin, out float limitMax);

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.6f);
            var centre = new Vector3((limitMin + limitMax) * 0.5f, (boundsMin.y + boundsMax.y) * 0.5f, transform.position.z);
            var size = new Vector3(Mathf.Abs(limitMax - limitMin), Mathf.Abs(boundsMax.y - boundsMin.y), 0.1f);
            Gizmos.DrawWireCube(centre, size);
        }
    }
}
