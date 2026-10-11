using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Background Music")]
    [Tooltip("Ambient / energetic music loop that plays from Menu through Flight.")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;

    [Header("Thruster Audio (Looping)")]
    public AudioSource thrusterSource;
    public AudioClip thrusterLoop;
    [Range(0f, 1f)] public float maxThrusterVolume = 0.8f;

    [Header("One-Shot SFX Clips")]
    public AudioSource sfxSource;
    public AudioClip uiHoverSound;
    public AudioClip uiClickSound;
    public AudioClip goalSuccessSound;
    public AudioClip goalMissSound;
    public AudioClip gameOverSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize Background Music
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        // Initialize Thruster Loop
        if (thrusterSource != null && thrusterLoop != null)
        {
            thrusterSource.clip = thrusterLoop;
            thrusterSource.loop = true;
            thrusterSource.volume = 0f;
            thrusterSource.Play();
        }
    }

    public void SetThrusterVolume(float targetFraction)
    {
        if (thrusterSource == null) return;
        thrusterSource.volume = Mathf.Clamp01(targetFraction) * maxThrusterVolume;
    }

    public void PlayUIHover()
    {
        PlayClip(uiHoverSound, 0.4f);
    }

    public void PlayUIClick()
    {
        PlayClip(uiClickSound, 0.7f);
    }

    public void PlayGoalSuccess()
    {
        PlayClip(goalSuccessSound, 0.9f);
    }

    public void PlayGoalMiss()
    {
        PlayClip(goalMissSound, 0.8f);
    }

    public void PlayGameOver()
    {
        // Stop/fade background music on game over
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        // Mute thruster
        if (thrusterSource != null)
        {
            thrusterSource.volume = 0f;
        }

        PlayClip(gameOverSound, 1f);
    }

    public void ResumeMusic()
    {
        if (musicSource != null && backgroundMusic != null && !musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    private void PlayClip(AudioClip clip, float volume = 1f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}