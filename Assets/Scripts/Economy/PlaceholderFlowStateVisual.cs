using UnityEngine;

namespace ThinkFast.Economy
{
    /// <summary>
    /// THROWAWAY. A stand-in "powered up" cue for Flow state -- the fighter turns
    /// gold and pulses, with a ring of orbiting motes around it.
    ///
    /// Everything is opaque primitives and property blocks on purpose. Doing a
    /// real glowing aura means transparent materials and render-queue fiddling in
    /// URP, which is a lot of setup for something that will be thrown away the
    /// moment there is real art. This reads clearly from across the screen, which
    /// is all a placeholder has to do.
    ///
    /// Listens to <see cref="FighterResources"/> events and is never called into,
    /// so deleting this file and its component removes it completely.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FighterResources))]
    public sealed class PlaceholderFlowStateVisual : MonoBehaviour
    {
        [Header("Body tint")]
        [SerializeField] private Color flowTint = new Color(1f, 0.85f, 0.2f);

        [Tooltip("Renderers to tint. Left empty, every renderer under this object is used.")]
        [SerializeField] private Renderer[] bodyRenderers;

        [Header("Pulse")]
        [Tooltip("How much the fighter swells at the peak of the pulse.")]
        [SerializeField] private float pulseAmount = 0.12f;

        [SerializeField] private float pulseSpeed = 9f;

        [Header("Motes")]
        [SerializeField] private int moteCount = 6;
        [SerializeField] private float moteRadius = 0.85f;
        [SerializeField] private float moteSize = 0.16f;
        [SerializeField] private float moteSpinSpeed = 220f;

        private FighterResources resources;
        private MaterialPropertyBlock propertyBlock;
        private Color[] baseColours;

        private Transform moteRoot;
        private Transform pulseTarget;
        private Vector3 pulseBaseScale;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            resources = GetComponent<FighterResources>();

            if (bodyRenderers == null || bodyRenderers.Length == 0)
            {
                bodyRenderers = GetComponentsInChildren<Renderer>();
            }

            propertyBlock = new MaterialPropertyBlock();
            CacheBaseColours();

            // Pulse the mesh, not the root: scaling the root would scale the
            // collider with it and quietly change the fighter's hurtbox.
            if (bodyRenderers.Length > 0 && bodyRenderers[0] != null)
            {
                pulseTarget = bodyRenderers[0].transform;
                pulseBaseScale = pulseTarget.localScale;
            }
        }

        private void CacheBaseColours()
        {
            baseColours = new Color[bodyRenderers.Length];
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                Material material = bodyRenderers[i] != null ? bodyRenderers[i].sharedMaterial : null;
                baseColours[i] = material != null && material.HasProperty(BaseColorId)
                    ? material.GetColor(BaseColorId)
                    : Color.white;
            }
        }

        private void OnEnable()
        {
            resources.FlowStateEntered += HandleFlowStateEntered;
            resources.FlowStateExited += HandleFlowStateExited;
        }

        private void OnDisable()
        {
            resources.FlowStateEntered -= HandleFlowStateEntered;
            resources.FlowStateExited -= HandleFlowStateExited;
            HandleFlowStateExited();
        }

        private void HandleFlowStateEntered()
        {
            SetTint(true);
            BuildMotes();
        }

        private void HandleFlowStateExited()
        {
            SetTint(false);

            if (moteRoot != null)
            {
                Destroy(moteRoot.gameObject);
                moteRoot = null;
            }

            if (pulseTarget != null)
            {
                pulseTarget.localScale = pulseBaseScale;
            }
        }

        private void Update()
        {
            if (moteRoot == null || !resources.IsFlowActive)
            {
                return;
            }

            moteRoot.Rotate(Vector3.up, moteSpinSpeed * Time.deltaTime, Space.Self);

            if (pulseTarget != null)
            {
                float pulse = 1f + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * pulseAmount;
                pulseTarget.localScale = pulseBaseScale * pulse;
            }
        }

        private void BuildMotes()
        {
            if (moteRoot != null)
            {
                return;
            }

            var root = new GameObject("FlowMotes");
            moteRoot = root.transform;
            moteRoot.SetParent(transform, worldPositionStays: false);
            moteRoot.localPosition = Vector3.zero;

            for (int i = 0; i < moteCount; i++)
            {
                float angle = (360f / Mathf.Max(1, moteCount)) * i;
                GameObject mote = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mote.name = "Mote";

                Collider collider = mote.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                mote.transform.SetParent(moteRoot, worldPositionStays: false);
                mote.transform.localPosition = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * moteRadius);
                mote.transform.localScale = Vector3.one * moteSize;

                // Tinted through a property block so no material instance is
                // created and nothing else sharing the material is affected.
                var renderer = mote.GetComponent<MeshRenderer>();
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, flowTint);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void SetTint(bool on)
        {
            for (int i = 0; i < bodyRenderers.Length; i++)
            {
                Renderer renderer = bodyRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, on ? flowTint : baseColours[i]);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
