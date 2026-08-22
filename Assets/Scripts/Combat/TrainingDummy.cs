using ThinkFast.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThinkFast.Combat
{
    /// <summary>
    /// A punching bag. Takes damage, gets launched, flashes, and picks itself
    /// back up so you can keep hitting it without restarting Play mode.
    ///
    /// This is a stand-in for the AI opponent: it deliberately does nothing on
    /// its own, so anything that feels wrong while hitting it is the attack's
    /// fault and not the opponent's.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TrainingDummy : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private int maxHealth = 100;

        [Tooltip("Seconds after being KO'd before it stands back up at its spawn point.")]
        [SerializeField] private float respawnDelay = 1.5f;

        [Header("Feedback")]
        [SplitScreenTodo("The floating health label converts world space to screen space via Camera.main and Screen.height. Both assume ONE full-screen camera: with a split viewport the label lands in the wrong place. Needs the fighter camera's viewport rect applied, or turn it off and read health from the real HUD instead.")]
        [SerializeField] private bool showHealthLabel = true;

        [Tooltip("Colour flashed on hit. The flash lasts as long as the hitstun, so you can see exactly how long the stun is.")]
        [SerializeField] private Color hitFlashColour = new Color(1f, 0.35f, 0.35f);

        [SerializeField] private Renderer[] renderers;

        private Rigidbody2D body;
        private Vector2 spawnPosition;
        private MaterialPropertyBlock propertyBlock;
        private Color[] baseColours;

        private float hitstunTimer;
        private float respawnTimer;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public int Health { get; private set; }

        public bool IsStunned => hitstunTimer > 0f;

        public bool IsDown => respawnTimer > 0f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spawnPosition = transform.position;
            Health = maxHealth;

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            propertyBlock = new MaterialPropertyBlock();
            CacheBaseColours();
        }

        private void CacheBaseColours()
        {
            baseColours = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i] != null ? renderers[i].sharedMaterial : null;
                baseColours[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }
        }

        public void TakeHit(in HitInfo hit)
        {
            if (IsDown)
            {
                return;
            }

            Health = Mathf.Max(0, Health - hit.Damage);

            // Assigned rather than added, so a hit always launches by the same
            // amount regardless of what the dummy was already doing. Fixed
            // knockback needs to look fixed.
            body.linearVelocity = hit.Knockback;

            hitstunTimer = hit.Hitstun;
            SetFlash(true);

            if (Health <= 0)
            {
                respawnTimer = respawnDelay;
            }
        }

        private void Update()
        {
            // Debug affordance: a punched dummy ends up wherever the knockback
            // put it, which is rarely where you want it. Read straight from the
            // device rather than through the action asset, since this is a
            // testing convenience and not a game binding.
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                Respawn();
            }

            if (hitstunTimer > 0f)
            {
                hitstunTimer -= Time.deltaTime;
                if (hitstunTimer <= 0f)
                {
                    SetFlash(false);
                }
            }

            if (respawnTimer > 0f)
            {
                respawnTimer -= Time.deltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
        }

        private void Respawn()
        {
            Health = maxHealth;
            hitstunTimer = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            transform.position = spawnPosition;
            SetFlash(false);
        }

        /// <summary>
        /// Tints via a property block rather than touching the material, so this
        /// never leaks a material instance and never bleeds onto anything else
        /// sharing the same one.
        /// </summary>
        private void SetFlash(bool on)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, on ? hitFlashColour : baseColours[i]);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnGUI()
        {
            if (!showHealthLabel)
            {
                return;
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 1.6f);
            if (screen.z < 0f)
            {
                return;
            }

            string label = IsDown ? "DOWN" : $"{Health} / {maxHealth}";
            var rect = new Rect(screen.x - 60f, Screen.height - screen.y - 12f, 120f, 24f);

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                richText = true,
            };

            GUI.Label(rect, IsStunned ? $"<color=#ff6666>{label}</color>" : label, style);
        }
    }
}
