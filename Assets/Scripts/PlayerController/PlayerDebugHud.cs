using System.Collections.Generic;
using ThinkFast.Combat;
using ThinkFast.Common;
using ThinkFast.Economy;
using UnityEngine;

namespace ThinkFast.Player
{
    /// <summary>
    /// Throwaway on-screen readout for manual testing. Cheap IMGUI on purpose:
    /// this is a tuning aid, not part of the real HUD.
    ///
    /// The panel measures itself from its contents. An earlier fixed-height
    /// version silently clipped every row added past the bottom edge, which looks
    /// exactly like the feature not being implemented.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerDebugHud : MonoBehaviour
    {
        [SplitScreenTodo("This readout is drawn at a hard-coded screen position (top-left, 12,12) in FULL-SCREEN coordinates. Under split screen it will sit over the wrong half. Either offset it into the fighter's viewport rect or just switch it off.")]
        [SerializeField] private bool show = true;

        [SerializeField] private int fontSize = 16;
        [SerializeField] private float panelWidth = 380f;

        private PlayerController player;
        private PlayerInputReader input;
        private PlayerAttack attack;
        private FighterResources resources;

        private GUIStyle style;
        private readonly List<string> lines = new List<string>(16);

        private const float Margin = 12f;
        private const float Padding = 8f;
        private const float BarHeight = 16f;

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            input = GetComponent<PlayerInputReader>();
            attack = GetComponent<PlayerAttack>();
            resources = GetComponent<FighterResources>();
        }

        private void OnGUI()
        {
            if (!show)
            {
                return;
            }

            style ??= new GUIStyle(GUI.skin.label) { fontSize = fontSize, richText = true };

            BuildLines();

            float lineHeight = style.lineHeight + 3f;
            float height = (Padding * 2f) + (lines.Count * lineHeight);

            // Reserve room for the flow bar and the gap above it.
            if (resources != null)
            {
                height += BarHeight + 6f;
            }

            GUILayout.BeginArea(new Rect(Margin, Margin, panelWidth, height));

            foreach (string line in lines)
            {
                GUILayout.Label(line, style);
            }

            if (resources != null)
            {
                DrawFlowBar();
            }

            GUILayout.EndArea();
        }

        private void BuildLines()
        {
            lines.Clear();

            Vector2 velocity = player.Velocity;

            lines.Add(player.IsGrounded
                ? "<color=#66ff66><b>GROUNDED</b></color>"
                : "<color=#ff9966><b>AIRBORNE</b></color>");

            lines.Add($"input.x   {input.MoveX,6:0.00}");
            lines.Add($"velocity  {velocity.x,6:0.00} , {velocity.y,6:0.00}");
            lines.Add($"facing    {(player.Facing > 0f ? "right" : "left")}");
            lines.Add($"coyote    {Flag(player.HasCoyote)}");
            lines.Add($"buffered  {Flag(player.HasBufferedJump)}");
            lines.Add($"fastfall  {Flag(player.IsFastFalling)}");
            lines.Add($"dropping  {Flag(player.IsDroppingThroughPlatform)}");
            lines.Add($"attack    {DescribeAttack()}");

            if (resources != null)
            {
                lines.Add($"AP        {DescribeActionPoints()}");
                lines.Add($"flow      {DescribeFlow()}");
            }
        }

        private static string Flag(bool on)
        {
            return on ? "<color=#66ff66>yes</color>" : "<color=#888888>no</color>";
        }

        private string DescribeAttack()
        {
            if (attack == null || !attack.IsAttacking)
            {
                return "<color=#888888>ready</color>";
            }

            // Active is the only phase that can actually hit, so it is the only
            // one worth making loud.
            string colour = attack.CurrentPhase == AttackRunner.Phase.Active ? "#ff4444" : "#ffcc44";
            return $"<color={colour}>{attack.CurrentAttackName} : {attack.CurrentPhase}</color>";
        }

        private string DescribeActionPoints()
        {
            int ap = resources.ActionPoints;
            string colour = ap > 0 ? "#66ff66" : "#ff6666";

            // Pips rather than a bare number: AP is a small integer, and a row of
            // filled slots is readable without being counted.
            string pips = new string('#', ap) + new string('.', Mathf.Max(0, resources.MaxActionPoints - ap));
            return $"<color={colour}>{pips}</color>  {ap}/{resources.MaxActionPoints}";
        }

        private string DescribeFlow()
        {
            return resources.IsFlowActive
                ? $"<color=#ffcc22><b>FLOW STATE  x{resources.DamageMultiplier:0.#} dmg  x{resources.KnockbackMultiplier:0.#} kb</b></color>"
                : $"<color=#88aaff>{resources.Flow:000} / {resources.MaxFlow:000}</color>";
        }

        /// <summary>
        /// A real filled rect rather than an ASCII bar. The default GUI font is
        /// proportional, so a bar built from '=' characters visibly jitters as it
        /// fills.
        /// </summary>
        private void DrawFlowBar()
        {
            GUILayout.Space(4f);
            Rect rect = GUILayoutUtility.GetRect(panelWidth - (Padding * 2f), BarHeight);

            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = resources.IsFlowActive
                ? new Color(1f, 0.8f, 0.15f)
                : new Color(0.45f, 0.6f, 1f);

            var fill = new Rect(
                rect.x + 1f,
                rect.y + 1f,
                (rect.width - 2f) * resources.FlowNormalised,
                rect.height - 2f);

            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
