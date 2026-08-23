using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ThinkFast.UI
{
    /// <summary>
    /// A menu tile that responds to being pointed at, pressed and focused.
    ///
    /// Everything is driven toward a single target each frame -- scale, glow,
    /// fill, border, label -- rather than being set on each event. Events only
    /// change which state is wanted. That is what stops the usual bug where the
    /// pointer leaves during a press, or focus and hover disagree, and a tile is
    /// left stuck bright: there is no path that sets a visual without the state
    /// that justifies it.
    ///
    /// Hover and keyboard focus are treated as the same state on purpose. A tile
    /// that lights up under the mouse but not under the arrow keys tells a
    /// keyboard player nothing about where they are.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MenuButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        [Header("Parts")]
        [Tooltip("The card itself. Its colour washes toward the accent when highlighted.")]
        [SerializeField] private Image background;

        [Tooltip("Soft accent halo behind the card, faded in on highlight. Optional.")]
        [SerializeField] private Image glow;

        [Tooltip("Scaled and recoloured with the card. Optional.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Icon or glyph above the label. Recoloured with the label. Optional.")]
        [SerializeField] private TMP_Text icon;

        [Header("Colours")]
        [Tooltip("Resting fill. Left at the surface white this is the quiet card every menu tile uses; set to an accent it becomes a filled call-to-action.")]
        [SerializeField] private Color restColour = MenuTheme.Surface;

        [Tooltip("Fill while hovered or focused.")]
        [SerializeField] private Color highlightColour = MenuTheme.AccentWash;

        [Tooltip("Label colour at rest. Must be readable on the resting fill -- nothing here checks that for you.")]
        [SerializeField] private Color restTextColour = MenuTheme.TextPrimary;

        [Tooltip("Label colour while hovered or focused.")]
        [SerializeField] private Color highlightTextColour = MenuTheme.Accent;

        [Header("Sound")]
        [Tooltip("Off for a tile that is already loud, like a confirm inside a dialog that just opened.")]
        [SerializeField] private bool playSounds = true;

        private Button button;
        private RectTransform card;

        private bool pointerInside;
        private bool focused;
        private bool held;

        private float scale = 1f;
        private float highlight;

        private void Awake()
        {
            button = GetComponent<Button>();

            // The card is scaled, not this object: scaling the object that owns
            // the layout element would fight the layout group it sits in.
            card = background != null ? background.rectTransform : (RectTransform)transform;

            ApplyImmediate();
        }

        private void OnDisable()
        {
            // A tile disabled mid-hover would come back still lit, because no
            // exit event is delivered to a disabled object.
            pointerInside = false;
            focused = false;
            held = false;
            ApplyImmediate();
        }

        private void Update()
        {
            // Unscaled: menus have to keep animating while the game is paused,
            // and a pause is exactly when a menu is open.
            float step = Time.unscaledDeltaTime / Mathf.Max(0.0001f, MenuTheme.StateDuration);

            highlight = Mathf.MoveTowards(highlight, TargetHighlight(), step);
            scale = Mathf.MoveTowards(scale, TargetScale(), step * 0.5f);

            Apply();
        }

        private float TargetHighlight()
        {
            if (!IsInteractable())
            {
                return 0f;
            }

            if (pointerInside || focused)
            {
                return 1f;
            }

            return 0f;
        }

        private float TargetScale()
        {
            if (!IsInteractable())
            {
                return 1f;
            }

            if (held)
            {
                return MenuTheme.PressScale;
            }

            if (pointerInside || focused)
            {
                return MenuTheme.HoverScale;
            }

            return 1f;
        }

        private bool IsInteractable()
        {
            return button != null && button.IsInteractable();
        }

        private void ApplyImmediate()
        {
            highlight = TargetHighlight();
            scale = TargetScale();
            Apply();
        }

        private void Apply()
        {
            if (card != null)
            {
                card.localScale = new Vector3(scale, scale, 1f);
            }

            if (background != null)
            {
                background.color = Color.Lerp(restColour, highlightColour, highlight);
            }

            if (glow != null)
            {
                Color glowColour = MenuTheme.Accent;
                glowColour.a = highlight;
                glow.color = glowColour;
            }

            Color textColour = Color.Lerp(restTextColour, highlightTextColour, highlight);

            if (label != null)
            {
                label.color = textColour;
            }

            if (icon != null)
            {
                icon.color = textColour;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (pointerInside)
            {
                return;
            }

            pointerInside = true;

            // Only the pointer arriving makes a sound. Focus does too, but via
            // OnSelect -- and the two are separated so that clicking a tile, which
            // both hovers and focuses it, cannot tick twice.
            if (!focused)
            {
                PlayHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;

            // Releasing outside the tile never delivers OnPointerUp here, so the
            // held flag has to be cleared on the way out too.
            held = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            held = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            held = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (focused)
            {
                return;
            }

            focused = true;

            if (!pointerInside)
            {
                PlayHover();
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            focused = false;
        }

        private void PlayHover()
        {
            if (!playSounds || !IsInteractable() || MenuSounds.Instance == null)
            {
                return;
            }

            MenuSounds.Instance.PlayHover();
        }
    }
}
