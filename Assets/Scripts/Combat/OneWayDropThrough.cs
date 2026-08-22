using System.Collections.Generic;
using UnityEngine;

namespace ThinkFast.Combat
{
    /// <summary>
    /// Lets one fighter fall through the one-way platforms it is standing on.
    ///
    /// Plain C#, shared by both fighters. The subtlety here is entirely in the
    /// restore rules, and getting them wrong is invisible until it is not, so
    /// there is exactly one copy.
    ///
    /// Collision is disabled per collider PAIR rather than by rotating the
    /// <see cref="PlatformEffector2D"/>: rotating the effector would let
    /// everything in the scene through, not just whoever pressed down.
    /// </summary>
    public sealed class OneWayDropThrough
    {
        private struct DroppedPlatform
        {
            public Collider2D Collider;
            public float EarliestRestore;
            public float ForcedRestore;
        }

        private readonly List<DroppedPlatform> dropped = new List<DroppedPlatform>(4);
        private readonly Collider2D self;
        private readonly float minDuration;
        private readonly float maxDuration;

        /// <param name="self">The fighter collider that should pass through.</param>
        /// <param name="minDuration">How long collision stays disabled before a restore is even considered. Must be long enough to fall clear.</param>
        /// <param name="maxDuration">Hard cap, in case we somehow never stop overlapping. Prevents a platform being ignored forever.</param>
        public OneWayDropThrough(Collider2D self, float minDuration, float maxDuration)
        {
            this.self = self;
            this.minDuration = minDuration;
            this.maxDuration = maxDuration;
        }

        /// <summary>True while at least one platform is being fallen through.</summary>
        public bool Active => dropped.Count > 0;

        /// <summary>
        /// True if this collider is currently being fallen through.
        ///
        /// Ground checks must consult this by hand: OverlapBox is a query and
        /// does not respect IgnoreCollision, so a dropped platform still shows up
        /// in the feet probe and would otherwise keep refreshing coyote time all
        /// the way down.
        /// </summary>
        public bool IsDroppingThrough(Collider2D candidate)
        {
            for (int i = 0; i < dropped.Count; i++)
            {
                if (dropped[i].Collider == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Starts falling through whichever of the given colliders are one-way
        /// platforms. Solid ground is left alone: pressing down on the floor
        /// should do nothing.
        /// </summary>
        public void Drop(List<Collider2D> standingOn)
        {
            for (int i = 0; i < standingOn.Count; i++)
            {
                Collider2D ground = standingOn[i];
                if (ground == null || ground.GetComponent<PlatformEffector2D>() == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(self, ground, true);
                dropped.Add(new DroppedPlatform
                {
                    Collider = ground,
                    EarliestRestore = Time.time + minDuration,
                    ForcedRestore = Time.time + maxDuration,
                });
            }
        }

        /// <summary>
        /// Re-enables collision once we are genuinely clear of a platform.
        /// Restoring while still overlapping would have the solver shove us out,
        /// which reads as being spat back onto the platform we just left.
        /// </summary>
        public void RestoreCleared()
        {
            for (int i = dropped.Count - 1; i >= 0; i--)
            {
                DroppedPlatform platform = dropped[i];

                if (platform.Collider == null || self == null)
                {
                    dropped.RemoveAt(i);
                    continue;
                }

                bool waitedLongEnough = Time.time >= platform.EarliestRestore;
                bool outOfPatience = Time.time >= platform.ForcedRestore;
                bool stillOverlapping = Physics2D.Distance(self, platform.Collider).isOverlapped;

                if (outOfPatience || (waitedLongEnough && !stillOverlapping))
                {
                    Physics2D.IgnoreCollision(self, platform.Collider, false);
                    dropped.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Restores every pair immediately. Call from OnDisable: IgnoreCollision
        /// is a persistent property of the collider pair, so leaving it set would
        /// survive the component being switched off.
        /// </summary>
        public void RestoreAll()
        {
            for (int i = 0; i < dropped.Count; i++)
            {
                if (dropped[i].Collider != null && self != null)
                {
                    Physics2D.IgnoreCollision(self, dropped[i].Collider, false);
                }
            }

            dropped.Clear();
        }
    }
}
