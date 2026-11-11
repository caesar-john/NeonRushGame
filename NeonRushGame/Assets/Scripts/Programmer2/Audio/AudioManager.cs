using UnityEngine;

public enum SfxId
{
    Coin,
    Jump,
    Slide,
    Crash,
    Button
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sumber Suara (otomatis dibuat jika kosong)")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Klip")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip slideClip;
    [SerializeField] private AudioClip crashClip;
    [SerializeField] private AudioClip buttonClip;

    [Header("Pengaturan")]
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Tooltip("Centang jika AudioManager harus bertahan saat pindah scene.")]
    [SerializeField] private bool dontDestroyOnLoad = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad && transform.parent == null) DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.volume = bgmVolume;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }
    }

    private void Start()
    {
        PlayBgm();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void PlaySfx(SfxId id)
    {
        if (Instance != null) Instance.PlaySfxInternal(id);
    }

    public static void PlayUiClick()
    {
        PlaySfx(SfxId.Button);
    }

    private void PlaySfxInternal(SfxId id)
    {
        AudioClip clip = GetClip(id);
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    private AudioClip GetClip(SfxId id)
    {
        switch (id)
        {
            case SfxId.Coin: return coinClip != null ? coinClip : LoadFallback("Coin");
            case SfxId.Jump: return jumpClip != null ? jumpClip : LoadFallback("Jump");
            case SfxId.Slide: return slideClip != null ? slideClip : LoadFallback("Slide");
            case SfxId.Crash: return crashClip != null ? crashClip : LoadFallback("Crash");
            case SfxId.Button: return buttonClip != null ? buttonClip : LoadFallback("Button");
            default: return null;
        }
    }

    private static AudioClip LoadFallback(string clipName)
    {
        return Resources.Load<AudioClip>("Audio/" + clipName);
    }

    public void PlayBgm()
    {
        if (bgmClip == null) bgmClip = LoadFallback("Bgm");
        if (bgmClip == null || bgmSource == null) return;
        if (bgmSource.clip == bgmClip && bgmSource.isPlaying) return;

        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null) bgmSource.Stop();
    }
}
