using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource m_Source;
    [SerializeField] AudioClip themeSong;

    private void Start()
    {
        PlayThemeSong();
    }
    void PlayThemeSong()
    {
        if (m_Source != null && themeSong != null)
        {
            m_Source.clip = themeSong;
            m_Source.Play();
            m_Source.loop = true;
        }
    }

    public void StartAudioPitchShift(float duration)
    {
        // Pitch shift from 1.0 to 0.8 over the specified duration
        StartCoroutine(PitchShiftCoroutine(1.0f, 0.1f, duration));
    }

    private IEnumerator PitchShiftCoroutine(float startPitch, float endPitch, float duration)
    {
        Debug.Log("Starting pitch shift coroutine");
        float elapsed = 0f;
        float slowingDuration = duration / 2f;
        // First half of duration pitch down, second half pitch back up
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed <= slowingDuration)
            {
                // Pitch down
                m_Source.pitch = Mathf.Lerp(startPitch, endPitch, elapsed / slowingDuration);
            }
            else
            {
                // Pitch back up
                m_Source.pitch = Mathf.Lerp(endPitch, startPitch, (elapsed - slowingDuration) / slowingDuration);
            }
            yield return null;
        }
        m_Source.pitch = 1.0f; // Ensure pitch resets to normal at the end
    }

    public void StopAudioPitchShift()
    {
        // Stop any ongoing pitch shift and reset pitch to normal
        StopAllCoroutines();
        m_Source.pitch = 1.0f;
    }
}
