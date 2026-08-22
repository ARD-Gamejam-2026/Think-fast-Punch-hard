using ThinkFast.Menu;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace ThinkFast.UI
{
    /// <summary>
    /// Drives one mixer channel from a slider, and shows the value the mixer
    /// already holds when the screen opens.
    ///
    /// Reading the current value first is the part that matters: a slider that
    /// always starts at its authored position silently resets the player's volume
    /// the first time they open the menu, which looks like the setting not being
    /// saved.
    ///
    /// Writes go through the existing <see cref="AudioManager"/> asset rather than
    /// to the mixer directly, so the conversion to decibels stays in one place.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Slider))]
    public sealed class VolumeSlider : MonoBehaviour
    {
        /// <summary>Which mixer channel a slider is bound to.</summary>
        public enum Channel
        {
            Music,
            Sfx,
        }

        [SerializeField] private Channel channel = Channel.Music;

        [Tooltip("The shared audio manager asset. Writes go through it.")]
        [SerializeField] private AudioManager audioManager;

        [Tooltip("Read directly, only to show the value the mixer is already set to.")]
        [SerializeField] private AudioMixer mixer;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
        }

        private void Start()
        {
            ShowCurrentValue();
            slider.onValueChanged.AddListener(Write);
        }

        private void OnDestroy()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(Write);
            }
        }

        private void ShowCurrentValue()
        {
            if (mixer == null || !mixer.GetFloat(ParameterName(), out float decibels))
            {
                return;
            }

            // Without notify: this is reporting the existing value, not changing
            // it, and notifying would write it straight back.
            slider.SetValueWithoutNotify(LinearFromDecibels(decibels));
        }

        private void Write(float value)
        {
            if (audioManager == null)
            {
                return;
            }

            if (channel == Channel.Music)
            {
                audioManager.SetMusicVolume(value);
                return;
            }

            audioManager.SetSFXVolume(value);
        }

        private string ParameterName()
        {
            if (channel == Channel.Music)
            {
                return "MusicVolume";
            }

            return "SFXVolume";
        }

        /// <summary>
        /// Converts a mixer level back to the 0..1 the slider works in. Unity
        /// treats -80 dB as silence, and the logarithm would run away below it.
        /// </summary>
        private static float LinearFromDecibels(float decibels)
        {
            if (decibels <= -80f)
            {
                return 0f;
            }

            return Mathf.Pow(10f, decibels / 20f);
        }
    }
}
