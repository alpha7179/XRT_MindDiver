using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip bgm_MainMenu;
    public AudioClip bgm_Phase1;
    public AudioClip bgm_Phase2;
    public AudioClip bgm_Boss;

    [Header("SFX Clips")]
    public AudioClip sfx_Touch;
    public AudioClip sfx_Explosion;
    public AudioClip sfx_ShieldHit;
    public AudioClip sfx_ItemCollect;

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
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    // 이름으로 재생 (편의성)
    public void PlaySFX(string sfxName)
    {
        switch (sfxName)
        {
            case "Touch": PlaySFX(sfx_Touch); break;
            case "Explosion": PlaySFX(sfx_Explosion); break;
            case "ShieldHit": PlaySFX(sfx_ShieldHit); break;
            case "Collect": PlaySFX(sfx_ItemCollect); break;
        }
    }
}