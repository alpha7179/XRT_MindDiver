using System.Collections;
using UnityEngine;
using static GameManager;     // GameState 사용
using static GamePhaseManager; // Phase 사용

/// <summary>
/// 효과음 종류 정의 (문자열 오타 방지용)
/// </summary>
public enum SFXType
{
    Touch,
    Explosion,
    ShieldHit,
    ItemCollect,
    None
}

/// <summary>
/// 게임 내 오디오(BGM, SFX)를 총괄 관리하는 클래스
/// - 수정됨: 실시간 볼륨 조절 기능 추가 (Update 루프 활용)
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM Clips")]
    public AudioClip bgm_MainMenu;
    public AudioClip bgm_CharacterSelect;
    public AudioClip bgm_PrePhase;
    public AudioClip bgm_Phase1;
    public AudioClip bgm_Phase2;
    public AudioClip bgm_Phase3;
    public AudioClip bgm_Result;

    [Header("SFX Clips")]
    public AudioClip sfx_Touch;
    public AudioClip sfx_Explosion;
    public AudioClip sfx_ShieldHit;
    public AudioClip sfx_ItemCollect;

    [Header("Settings")]
    [Tooltip("BGM이 전환될 때 페이드 아웃/인 되는 시간")]
    [SerializeField] private float bgmTransitionTime = 1.0f;
    #endregion

    #region Private Fields
    // 현재 실행 중인 BGM 전환/페이드 코루틴 (null이면 페이드 중이 아님)
    private Coroutine _bgmCoroutine;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // [추가됨] 실시간 볼륨 동기화를 위한 Update 로직
    private void Update()
    {
        if (DataManager.Instance == null) return;

        // 1. BGM 볼륨 실시간 동기화
        // 중요: 페이드 인/아웃(코루틴)이 진행 중일 때는 볼륨을 건드리지 않아야 자연스러운 전환이 유지됩니다.
        if (_bgmCoroutine == null && bgmSource.isPlaying)
        {
            bgmSource.volume = GetSavedBGMVolume();
        }

        // 2. SFX 볼륨 실시간 동기화
        // SFX는 보통 OneShot으로 재생되지만, Source의 볼륨을 바꾸면 전체 크기에 영향을 줍니다.
        sfxSource.volume = (float)DataManager.Instance.GetSFXVolume() / 100f;
    }
    #endregion

    #region Public Methods - BGM Logic

    /// <summary>
    /// BGM을 재생합니다. (페이드 효과 자동 적용)
    /// </summary>
    public void PlayBGM(AudioClip clip)
    {
        // 1. 이미 같은 곡이 재생 중이고, 잘 나오고 있다면 무시
        if (bgmSource.clip == clip && bgmSource.isPlaying && _bgmCoroutine == null) return;

        // 2. 이전 전환 작업이 진행 중이라면 중단
        if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);

        // 3. 부드러운 전환 시작 (Fade Out -> Swap -> Fade In)
        _bgmCoroutine = StartCoroutine(ChangeBGMRoutine(clip, bgmTransitionTime));
    }

    /// <summary>
    /// 게임 상태에 따라 적절한 BGM을 자동으로 재생합니다.
    /// </summary>
    public void PlayBGM(GameState gameState, Phase phaseState = Phase.Null)
    {
        AudioClip targetClip = null;

        switch (gameState)
        {
            case GameState.MainMenu: targetClip = bgm_MainMenu; break;
            case GameState.CharacterSelect: targetClip = bgm_CharacterSelect; break;
            case GameState.Result: targetClip = bgm_Result; break;
            case GameState.GameStage:
                switch (phaseState)
                {
                    case Phase.PrePhase: targetClip = bgm_PrePhase; break;
                    case Phase.Phase1: targetClip = bgm_Phase1; break;
                    case Phase.Phase2: targetClip = bgm_Phase2; break;
                    case Phase.Phase3: targetClip = bgm_Phase3; break;
                }
                break;
        }

        if (targetClip != null)
        {
            PlayBGM(targetClip);
        }
    }

    /// <summary>
    /// BGM을 서서히 멈춥니다.
    /// </summary>
    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);
        _bgmCoroutine = StartCoroutine(FadeOutAndStopRoutine(fadeDuration));
    }

    #endregion

    #region Private Methods - BGM Coroutines

    // [핵심 개선] BGM 교체 시 페이드 아웃 -> 교체 -> 페이드 인 처리
    private IEnumerator ChangeBGMRoutine(AudioClip nextClip, float duration)
    {
        float targetVolume = GetSavedBGMVolume();
        float halfDuration = duration * 0.5f;

        // 1. 재생 중이라면 페이드 아웃
        if (bgmSource.isPlaying && bgmSource.volume > 0)
        {
            float startVol = bgmSource.volume;
            float timer = 0f;

            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                // 여기서는 실시간 볼륨보다 페이드 효과가 우선됩니다.
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / halfDuration);
                yield return null;
            }
            bgmSource.volume = 0f;
            bgmSource.Stop();
        }

        // 2. 클립 교체 및 재생 시작
        bgmSource.clip = nextClip;
        bgmSource.Play();

        // 3. 페이드 인
        // 페이드 인을 시작할 때 최신 볼륨값을 다시 가져옵니다.
        targetVolume = GetSavedBGMVolume();

        if (targetVolume > 0)
        {
            float timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, timer / halfDuration);
                yield return null;
            }
        }

        // 최종 볼륨 확정 후 코루틴 종료 표시 (null)
        // 이 시점부터 Update문에서 실시간 볼륨 조절이 활성화됩니다.
        bgmSource.volume = targetVolume;
        _bgmCoroutine = null;
    }

    private IEnumerator FadeOutAndStopRoutine(float duration)
    {
        if (bgmSource.isPlaying)
        {
            float startVol = bgmSource.volume;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                yield return null;
            }
        }

        bgmSource.volume = 0f;
        bgmSource.Stop();
        _bgmCoroutine = null;
    }

    // DataManager에서 현재 BGM 볼륨(0~1) 가져오기
    private float GetSavedBGMVolume()
    {
        if (DataManager.Instance != null)
            return (float)DataManager.Instance.GetBGMVolume() / 100f;
        return 1f; // 기본값
    }

    #endregion

    #region Public Methods - SFX Logic (Enum Improved)

    public void PlaySFX(SFXType type)
    {
        AudioClip clipToPlay = null;

        switch (type)
        {
            case SFXType.Touch: clipToPlay = sfx_Touch; break;
            case SFXType.Explosion: clipToPlay = sfx_Explosion; break;
            case SFXType.ShieldHit: clipToPlay = sfx_ShieldHit; break;
            case SFXType.ItemCollect: clipToPlay = sfx_ItemCollect; break;
        }

        if (clipToPlay != null)
        {
            PlaySFX(clipToPlay);
        }
        else
        {
            if (type != SFXType.None)
                Debug.LogWarning($"[AudioManager] SFX Clip is missing for type: {type}");
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        // PlayOneShot은 Source의 Volume 설정을 따라가므로 
        // 여기서 굳이 volume을 세팅하지 않아도 Update에서 처리됩니다.
        // 하지만 즉시 반응성을 위해 남겨둡니다.
        float sfxVol = 1f;
        if (DataManager.Instance != null)
            sfxVol = (float)DataManager.Instance.GetSFXVolume() / 100f;

        sfxSource.volume = sfxVol;
        sfxSource.PlayOneShot(clip);
    }

    public void StopSFX()
    {
        if (sfxSource != null) sfxSource.Stop();
    }

    #endregion

    #region Public Methods - All control
    public void StopAllAudio()
    {
        StopBGM(0.5f);
        StopSFX();
    }
    #endregion
}