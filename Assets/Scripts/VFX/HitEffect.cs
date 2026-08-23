using UnityEngine;

namespace ThinkFast.VFX
{
    /// <summary>
    /// One-shot hit sparks at a world position. A scene singleton so combat code
    /// can call <see cref="PlayHitEffect"/> without wiring a reference.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem defaultEffect;
        [SerializeField] private ParticleSystem strongEffect;

        private static HitEffect instance;

        /// <summary>
        /// The hit effect in the current scene, or null when none exists.
        /// </summary>
        public static HitEffect Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<HitEffect>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            instance = this;
        }

        /// <summary>
        /// Moves this effect to <paramref name="position"/> and emits one particle.
        /// </summary>
        /// <param name="position">World-space contact point.</param>
        /// <param name="strong">When true, uses <see cref="strongEffect"/> instead of <see cref="defaultEffect"/>.</param>
        public void PlayHitEffect(Vector3 position, bool strong = false)
        {
            transform.position = position;
            
            if (strong)
            {
                strongEffect?.Emit(1);
                defaultEffect?.Emit(1);
            }
            else
            {
                defaultEffect?.Emit(1);
            }
        }
    }
}
