using UnityEngine;

namespace ThinkFast.UI
{
    /// <summary>
    /// Splits the screen between the two halves of the game: the fight on the
    /// left, the quiz on the right.
    ///
    /// One component owns the whole layout so the split ratio exists exactly
    /// once. The fighter camera gets a viewport rect, the quiz panel gets
    /// re-anchored into what is left, and a backdrop fills the quiz side --
    /// which is not decoration: a camera whose viewport covers only part of the
    /// screen never clears the rest, so without something opaque drawn there the
    /// quiz half shows undefined pixels.
    ///
    /// Applied on Start and again whenever the window changes size, so a resized
    /// player (or a WebGL canvas that settles a frame late) cannot leave the two
    /// halves disagreeing about where the seam is.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class SplitScreenLayout : MonoBehaviour
    {
        [Header("Split")]
        [Tooltip("Share of the screen width given to the fight. The quiz gets the rest. The quiz panel scales itself to whatever it is left, so this is safe to drag.")]
        [SerializeField, Range(0.25f, 0.85f)] private float fighterViewportWidth = 0.75f;

        [Header("Fight side")]
        [Tooltip("Left empty, the main camera is used. Its viewport rect is what actually confines the fight to one half.")]
        [SerializeField] private Camera fighterCamera;

        [Tooltip("The HUD's root. Anchored to the fight's share of the window, so bars pinned to the top corners land inside the fight rather than running across the quiz.")]
        [SerializeField] private RectTransform fighterHud;

        [Header("Quiz side")]
        [Tooltip("The quiz panel's container. It is re-anchored to the middle of the quiz half rather than the middle of the screen.")]
        [SerializeField] private RectTransform quizPanel;

        [Tooltip("The width the quiz panel was laid out at. It is never re-flowed to a different width -- only scaled down to fit -- so this must match the panel's authored size or the scale will be wrong.")]
        [SerializeField, Min(1f)] private float quizPanelWidth = 640f;

        [Tooltip("Gap kept between the quiz panel and the edges of its half.")]
        [SerializeField, Min(0f)] private float quizPanelMargin = 24f;

        [Tooltip("Opaque image covering the quiz half. Required: the fighter camera does not clear this area.")]
        [SerializeField] private RectTransform quizBackdrop;

        [Header("Seam")]
        [Tooltip("Optional divider drawn on the boundary, so the two halves read as two panes instead of one broken image.")]
        [SerializeField] private RectTransform seam;

        [SerializeField, Min(0f)] private float seamWidthPixels = 4f;

        private int appliedScreenWidth;
        private int appliedScreenHeight;

        /// <summary>Share of the screen width currently given to the fight.</summary>
        public float FighterViewportWidth => fighterViewportWidth;

        /// <summary>
        /// Re-applies the split to the camera, the backdrop, the seam and the
        /// quiz panel. Safe to call at any time, including from the editor.
        /// </summary>
        [ContextMenu("Apply Layout")]
        public void Apply()
        {
            ApplyCameraRect();
            ApplyFighterHud();
            ApplyBackdrop();
            ApplySeam();
            ApplyQuizPanel();

            appliedScreenWidth = Screen.width;
            appliedScreenHeight = Screen.height;
        }

        private void Awake()
        {
            if (fighterCamera == null)
            {
                fighterCamera = Camera.main;
            }
        }

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            // The quiz panel's width is resolved against the canvas, which is
            // sized from the screen -- so a resize is the one thing that can
            // invalidate the layout. The camera rect is in normalized viewport
            // coordinates and would survive on its own.
            if (Screen.width == appliedScreenWidth && Screen.height == appliedScreenHeight)
            {
                return;
            }

            Apply();
        }

        private void ApplyCameraRect()
        {
            if (fighterCamera == null)
            {
                return;
            }

            fighterCamera.rect = new Rect(0f, 0f, fighterViewportWidth, 1f);
        }

        private void ApplyFighterHud()
        {
            if (fighterHud == null)
            {
                return;
            }

            fighterHud.anchorMin = Vector2.zero;
            fighterHud.anchorMax = new Vector2(fighterViewportWidth, 1f);
            fighterHud.offsetMin = Vector2.zero;
            fighterHud.offsetMax = Vector2.zero;
        }

        private void ApplyBackdrop()
        {
            if (quizBackdrop == null)
            {
                return;
            }

            quizBackdrop.anchorMin = new Vector2(fighterViewportWidth, 0f);
            quizBackdrop.anchorMax = Vector2.one;
            quizBackdrop.offsetMin = Vector2.zero;
            quizBackdrop.offsetMax = Vector2.zero;
        }

        private void ApplySeam()
        {
            if (seam == null)
            {
                return;
            }

            seam.anchorMin = new Vector2(fighterViewportWidth, 0f);
            seam.anchorMax = new Vector2(fighterViewportWidth, 1f);
            seam.pivot = new Vector2(0.5f, 0.5f);
            seam.anchoredPosition = Vector2.zero;
            seam.sizeDelta = new Vector2(seamWidthPixels, 0f);
        }

        private void ApplyQuizPanel()
        {
            if (quizPanel == null)
            {
                return;
            }

            // A point anchor in the middle of the quiz half, rather than a
            // stretch: the panel was styled at a fixed width and stretching it
            // across the half would pull its layout apart.
            float centreX = fighterViewportWidth + ((1f - fighterViewportWidth) * 0.5f);
            quizPanel.anchorMin = new Vector2(centreX, 0.5f);
            quizPanel.anchorMax = new Vector2(centreX, 0.5f);
            quizPanel.pivot = new Vector2(0.5f, 0.5f);
            quizPanel.anchoredPosition = Vector2.zero;

            // Height is driven by the panel's own ContentSizeFitter, so only the
            // width is set here -- and always to the authored width.
            Vector2 size = quizPanel.sizeDelta;
            size.x = quizPanelWidth;
            quizPanel.sizeDelta = size;

            float scale = ResolveQuizPanelScale();
            quizPanel.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// Returns how much the quiz panel has to shrink to fit the half it was
        /// given, as a uniform scale.
        ///
        /// Scaled rather than narrowed on purpose. Re-flowing the panel to a
        /// smaller width keeps the type at full size and takes it out of the
        /// answer rows instead: answers start wrapping to two lines inside
        /// buttons whose height is fixed, so the second line is clipped. Real
        /// place and animal names are long enough to hit that well before the
        /// panel looks too small. Scaling shrinks type and layout together, so
        /// the panel stays exactly the design that was authored -- just smaller
        /// -- and nothing can wrap that did not wrap before.
        /// </summary>
        private float ResolveQuizPanelScale()
        {
            float available = QuizHalfWidth() - (quizPanelMargin * 2f);
            if (available <= 0f || available >= quizPanelWidth)
            {
                return 1f;
            }

            return available / quizPanelWidth;
        }

        /// <summary>
        /// Width of the quiz half in the quiz canvas's own units, which is what
        /// the panel's size is measured in. Returns 0 when there is no canvas to
        /// measure against yet.
        /// </summary>
        private float QuizHalfWidth()
        {
            if (quizPanel == null)
            {
                return 0f;
            }

            Canvas canvas = quizPanel.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return 0f;
            }

            if (canvas.transform is not RectTransform canvasRect)
            {
                return 0f;
            }

            return canvasRect.rect.width * (1f - fighterViewportWidth);
        }
    }
}
