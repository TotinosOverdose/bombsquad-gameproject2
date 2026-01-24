using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource m_Source;
    [SerializeField] AudioClip themeSong;

    [Header("Pitch Shift (use the curve to design how pitch changes over normalized time)")]
    [Tooltip("X-axis = normalized time (0..1), Y-axis = pitch value applied to the AudioSource.")]
    public AnimationCurve pitchCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(0.5f, 0.8f),
        new Keyframe(1f, 1f)
    );

    [Tooltip("Base pitch multiplier applied to the evaluated curve value.")]
    public float basePitch = 1f;

    private Coroutine pitchCoroutine;

    private void Start()
    {
        PlayThemeSong();
        StopAudioPitchShift();
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
        if (m_Source == null || duration <= 0f) return;

        // restart any existing shift
        if (pitchCoroutine != null)
            StopCoroutine(pitchCoroutine);

        pitchCoroutine = StartCoroutine(PitchShiftCoroutine(duration));
    }

    private IEnumerator PitchShiftCoroutine(float duration)
    {
        float elapsed = 0f;

        // guard: if no curve, behave as simple lerp
        bool hasCurve = pitchCurve != null && pitchCurve.length > 0;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float evaluated = hasCurve ? pitchCurve.Evaluate(t) : 1f;
            m_Source.pitch = evaluated * basePitch;

            yield return null;
        }

        // ensure reset to base pitch
        m_Source.pitch = basePitch;
        pitchCoroutine = null;
    }

    public void StopAudioPitchShift()
    {
        if (pitchCoroutine != null)
        {
            StopCoroutine(pitchCoroutine);
            pitchCoroutine = null;
        }

        if (m_Source != null)
            m_Source.pitch = basePitch;
    }

}