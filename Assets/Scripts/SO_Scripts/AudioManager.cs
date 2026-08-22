using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioManager", menuName = "Scriptable Objects/AudioManager")]
public class AudioManager : ScriptableObject
{
    [SerializeField] private AudioMixer mainMixer;

    public void SetSFXVolume(float volume)
    {
        mainMixer.SetFloat("SFXVolume", CalcDBVolume(volume));
    }

    public void SetMusicVolume(float volume)
    {
        mainMixer.SetFloat("MusicVolume", CalcDBVolume(volume));
    }

    private float CalcDBVolume(float volume)
    {
        if (volume <= 0)
        {
            //volume: 0.0001f == -80dB
            return -80f;
        }
        return Mathf.Log10(volume) * 20; // Convert linear volume to decibels (dB is logarithmic)
    }
}
