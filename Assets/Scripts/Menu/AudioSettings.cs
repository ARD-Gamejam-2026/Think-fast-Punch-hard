using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace ThinkFast.Menu
{
    public class AudioSettings : MonoBehaviour
    {
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider musicSlider;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (mainMixer == null)
            {
                Debug.LogWarning("AudioSettings: mainMixer is not assigned.");
                return;
            }

            // Read SFX volume (exposed parameter is in dB) and set slider (linear 0..1)
            if (sfxSlider != null && mainMixer.GetFloat("SFXVolume", out float sfxDb))
            {
                sfxSlider.SetValueWithoutNotify(DBToLinear(sfxDb));
            }

            // Read Music volume (exposed parameter is in dB) and set slider (linear 0..1)
            if (musicSlider != null && mainMixer.GetFloat("MusicVolume", out float musicDb))
            {
                musicSlider.SetValueWithoutNotify(DBToLinear(musicDb));
            }
        }
        // Convert decibels (dB) from AudioMixer to linear 0..1 for UI sliders
        private float DBToLinear(float dB)
        {
            if (dB <= -80f) return 0f; // Unity commonly uses -80 dB as silence
            return Mathf.Pow(10f, dB / 20f);
        }

        // Convert linear 0..1 to decibels for applying to the AudioMixer if needed
        private float LinearToDB(float linear)
        {
            if (linear <= 0f) return -80f;
            return Mathf.Log10(Mathf.Clamp(linear, 0.000001f, 1f)) * 20f;
        }
    }
}
