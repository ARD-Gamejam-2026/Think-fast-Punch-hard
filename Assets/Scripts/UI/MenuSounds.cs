using UnityEngine;
using UnityEngine.Audio;

namespace ThinkFast.UI
{
    /// <summary>
    /// The menu's voice: a soft tick when something is hovered, a lower bloop
    /// when it is pressed, and a shorter one for going back.
    ///
    /// Recorded clips play when they are assigned, and synthesised tones stand in
    /// when they are not -- so a screen built without any audio assets still has a
    /// voice, and dropping clips in is a one-field change per sound rather than a
    /// rewrite. Output goes through the SFX mixer group, so the existing volume
    /// slider already controls it.
    ///
    /// The fallback tones do not use
    /// <see cref="ThinkFast.Common.PlaceholderFxKit"/>, which deliberately has no
    /// attack: a sine that starts at full amplitude begins with a step, and a step
    /// is a click. That reads as impact on a punch, which is what the kit is for,
    /// and as a fault on a button. These have a short attack ramp and a smooth
    /// release instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuSounds : MonoBehaviour
    {
        private const int SampleRate = 44100;

        [Header("Output")]
        [Tooltip("Mixer group the interface plays through. The SFX group, so the menu's own volume slider applies to it.")]
        [SerializeField] private AudioMixerGroup output;

        [Header("Recorded clips")]
        [Tooltip("Played instead of the synthesised tick when set. Left empty, the tone below is used, so a screen still has a voice with no audio assets at all.")]
        [SerializeField] private AudioClip hoverSound;

        [SerializeField] private AudioClip pressSound;

        [SerializeField] private AudioClip backSound;

        [Header("Hover")]
        [Tooltip("A short, high, quiet tick. It fires on every hover, so anything longer or louder becomes noise while the pointer crosses the screen.")]
        [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.22f;

        [SerializeField] private float hoverHz = 1180f;

        [Header("Press")]
        [Tooltip("Lower and longer than the hover, and falling rather than flat: a press is an answer to the hover, so it sits below it.")]
        [SerializeField, Range(0f, 1f)] private float pressVolume = 0.36f;

        [SerializeField] private float pressStartHz = 660f;

        [SerializeField] private float pressEndHz = 392f;

        private AudioSource source;
        private AudioClip hoverClip;
        private AudioClip pressClip;
        private AudioClip backClip;

        private static MenuSounds instance;

        /// <summary>
        /// The menu sounds in the current scene, or null when a screen was built
        /// without them. Resolved lazily so buttons do not each need a reference
        /// wired to it.
        /// </summary>
        public static MenuSounds Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<MenuSounds>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            instance = this;

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = output;

            // A tick with almost no body. Flat pitch: a sweep this short reads as
            // a chirp rather than a tap.
            hoverClip = CreateBlip("UI Hover", 0.055f, hoverHz, hoverHz, 0.004f);

            // Falling, so it lands rather than hangs.
            pressClip = CreateBlip("UI Press", 0.14f, pressStartHz, pressEndHz, 0.006f);

            // The press inverted: rising and shorter, which reads as undoing.
            backClip = CreateBlip("UI Back", 0.11f, pressEndHz, pressStartHz, 0.006f);

            // The synthesised clips are built either way. They cost a few
            // milliseconds at load and mean an unwired screen is never silent,
            // which is worth more than the saving.
        }

        /// <summary>
        /// Returns the recorded clip when one is assigned, and the synthesised
        /// tone when it is not.
        /// </summary>
        private static AudioClip Pick(AudioClip recorded, AudioClip synthesised)
        {
            if (recorded != null)
            {
                return recorded;
            }

            return synthesised;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            Destroy(hoverClip);
            Destroy(pressClip);
            Destroy(backClip);
        }

        /// <summary>Plays the hover tick.</summary>
        public void PlayHover()
        {
            Play(Pick(hoverSound, hoverClip), hoverVolume);
        }

        /// <summary>Plays the press bloop.</summary>
        public void PlayPress()
        {
            Play(Pick(pressSound, pressClip), pressVolume);
        }

        /// <summary>Plays the rising note used for closing or going back.</summary>
        public void PlayBack()
        {
            Play(Pick(backSound, backClip), pressVolume);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (source == null || clip == null)
            {
                return;
            }

            // PlayOneShot rather than Play, so a fast pointer crossing several
            // tiles layers ticks instead of cutting each one off mid-note.
            source.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Builds one sine blip: a short attack ramp, then a curved decay to
        /// silence. Both ends matter -- an abrupt start clicks, and an abrupt end
        /// leaves a tail cut off square, which also clicks.
        /// </summary>
        private static AudioClip CreateBlip(string name, float duration, float startHz, float endHz, float attack)
        {
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * duration));
            var samples = new float[sampleCount];

            int attackSamples = Mathf.Max(1, Mathf.RoundToInt(SampleRate * attack));

            // Phase is accumulated rather than derived from t, so sweeping the
            // frequency cannot introduce a discontinuity.
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float progress = (float)i / sampleCount;
                float hz = Mathf.Lerp(startHz, endHz, progress);
                phase += 2f * Mathf.PI * hz / SampleRate;

                float envelope = Envelope(i, sampleCount, attackSamples);
                samples[i] = Mathf.Sin(phase) * envelope;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Amplitude at one sample: a linear ramp in, then a cosine fall to zero
        /// across the remainder. The cosine is what keeps the tail soft -- an
        /// exponential never actually reaches zero, so the clip still ends on a
        /// step.
        /// </summary>
        private static float Envelope(int index, int sampleCount, int attackSamples)
        {
            if (index < attackSamples)
            {
                return (float)index / attackSamples;
            }

            int released = index - attackSamples;
            int releaseSamples = Mathf.Max(1, sampleCount - attackSamples);
            float t = (float)released / releaseSamples;

            return Mathf.Cos(t * Mathf.PI * 0.5f);
        }
    }
}
