using System.Collections;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    // make a project wide instance
    public static SFXManager Instance { get; private set; }

    [SerializeField] float regularPitch = 1.0f;
    [SerializeField] AudioClip popSound;
    [SerializeField] AudioClip pickUpSound;
    [SerializeField] AudioClip putDownSound;
    [SerializeField] AudioClip throwSound;
    [SerializeField] AudioClip yeetSound;
    [SerializeField] AudioClip correctSound;
    [SerializeField] AudioClip tickingSound;

    // Death Screams
    [SerializeField] AudioClip deathScream1;
    [SerializeField] AudioClip deathScream2;
    [SerializeField] AudioClip deathScream3;

    [SerializeField] AudioSource sfxSource;


    private void OnEnable()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Awake()
    {
        sfxSource = GetComponent<AudioSource>();
    }

    public void PlayPopSound()
    {
        sfxSource.pitch = regularPitch;
        sfxSource.PlayOneShot(popSound);
    }
    public void PlayPickUpSound()
    {
        sfxSource.pitch = regularPitch;
        sfxSource.PlayOneShot(pickUpSound);
    }
    public void PlayPutDownSound()
    {
        sfxSource.pitch = regularPitch;
        sfxSource.PlayOneShot(putDownSound);
    }
    public void PlayThrowSound()
    {
        sfxSource.pitch = regularPitch;
        sfxSource.PlayOneShot(throwSound);
    }
    public void PlayYeetSound()
    {
        // random pitch between 0.8 and 1.2
        sfxSource.pitch = Random.Range(0.9f, 1.1f);
        sfxSource.PlayOneShot(yeetSound);
    }
    public void PlayCorrectSound()
    {
        sfxSource.pitch = regularPitch;
        sfxSource.PlayOneShot(correctSound);
    }

    public void PlayTickingSound(float duration)
    {
        sfxSource.pitch = regularPitch;
        sfxSource.PlayOneShot(tickingSound);
        StartCoroutine(StopTickingAfterDelay(duration));
    }

    private IEnumerator StopTickingAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        sfxSource.Stop();
    }

    public void ExplodeAndScream()
    {
        StartCoroutine(ExplodeAndScreamCoroutine());
    }

    public IEnumerator ExplodeAndScreamCoroutine()
    {
        PlayPopSound();
        yield return new WaitForSeconds(0.2f);
        PlayRandomDeathScream();
    }


    public void PlayRandomDeathScream()
    {
        AudioClip clipToPlay = deathScream1;
        int randomIndex = Random.Range(0, 3);
        switch (randomIndex)
        {
            case 0:
                clipToPlay = deathScream1;
                break;
            case 1:
                clipToPlay = deathScream2;
                break;
            case 2:
                clipToPlay = deathScream3;
                break;
        }
        sfxSource.pitch = Random.Range(0.8f, 1.2f);
        sfxSource.PlayOneShot(clipToPlay);
    }

    public void StopAllSFX()
    {
        sfxSource.Stop();
    }

}
