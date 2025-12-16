using System.Collections;
using UnityEngine;
using static GameManager;      // GameState 사용
using static GamePhaseManager; // Phase 사용

/// <summary>
/// 효과음 종류 정의
/// 네이밍 규칙: [Category]_[Action]
/// </summary>
public enum SFXType
{
    // 1. UI & Popup
    UI_Touch,           // 일반 터치/버튼음
    Popup_Success,      // 성공 팝업
    Popup_Fail,         // 실패 팝업
    Popup_Pause,        // 일시정지 팝업

    // 2. Player Action
    Attack_Player,      // 플레이어 공격
    Skill_Buff,         // 버프 스킬 사용
    Skill_Debuff,       // 디버프 스킬 사용

    // 3. Damage & Combat
    Damage_Player,      // 플레이어 피격
    Damage_Shield,      // 실드 피격
    Damage_Enemy,       // 적 피격
    Die_Enemy,          // 폭발 (적 사망 등)

    // 4. Items
    Collect_Energy,     // (구 ItemCollect) 에너지 수집

    None
}

/// <summary>
/// 게임 내 오디오(BGM, SFX)를 총괄 관리하는 클래스
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

    [Header("SFX Clips - UI")]
    public AudioClip sfx_UI_Touch;          // 기존 sfx_Touch 연결
    public AudioClip sfx_Popup_Success;     // [신규]
    public AudioClip sfx_Popup_Fail;        // [신규]
    public AudioClip sfx_Popup_Pause;       // [신규]

    [Header("SFX Clips - Player & Skill")]
    public AudioClip sfx_Attack_Player;     // [신규]
    public AudioClip sfx_Skill_Buff;        // [신규]
    public AudioClip sfx_Skill_Debuff;      // [신규]

    [Header("SFX Clips - Combat & Damage")]
    public AudioClip sfx_Damage_Player;     // [신규]
    public AudioClip sfx_Damage_Shield;     // 기존 sfx_ShieldHit 연결
    public AudioClip sfx_Damage_Enemy;      // [신규]
    public AudioClip sfx_Die_Enemy;         // 기존 유지

    [Header("SFX Clips - Item")]
    public AudioClip sfx_Collect_Energy;    // 기존 sfx_ItemCollect 연결

    [Header("Settings")]
    [Tooltip("BGM이 전환될 때 페이드 아웃/인 되는 시간")]
    [SerializeField] private float bgmTransitionTime = 1.0f;
    #endregion

    #region Private Fields
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

    private void Update()
    {
        if (DataManager.Instance == null) return;

        // 1. BGM 볼륨 실시간 동기화 (페이드 중이 아닐 때만)
        if (_bgmCoroutine == null && bgmSource.isPlaying)
        {
            bgmSource.volume = GetSavedBGMVolume();
        }

        // 2. SFX 볼륨 실시간 동기화
        sfxSource.volume = (float)DataManager.Instance.GetSFXVolume() / 100f;
    }
    #endregion

    #region Public Methods - BGM Logic
    // (BGM 관련 로직은 기존과 동일하므로 생략 없이 유지)

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying && _bgmCoroutine == null) return;
        if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);
        _bgmCoroutine = StartCoroutine(ChangeBGMRoutine(clip, bgmTransitionTime));
    }

    public void PlayBGM(GameState gameState, Phase phaseState = Phase.Null)
    {
        AudioClip targetClip = null;

        switch (gameState)
        {
            case GameState.MainMenu: targetClip = bgm_MainMenu; break;
            case GameState.CharacterSelect: targetClip = bgm_CharacterSelect; break;
            case GameState.Result: targetClip = bgm_Result; break;
            case GameState.GameStage:
                {
                    switch (phaseState)
                    {
                        case Phase.PrePhase: targetClip = bgm_PrePhase; break;
                        case Phase.Phase1: targetClip = bgm_Phase1; break;
                        case Phase.Phase2: targetClip = bgm_Phase2; break;
                        case Phase.Phase3: targetClip = bgm_Phase3; break;
                    }
                }
                break;
        }

        if (targetClip != null) PlayBGM(targetClip);
    }

    public void StopBGM(float fadeDuration = 1.0f)
    {
        if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);
        _bgmCoroutine = StartCoroutine(FadeOutAndStopRoutine(fadeDuration));
    }
    #endregion

    #region Private Methods - BGM Coroutines
    private IEnumerator ChangeBGMRoutine(AudioClip nextClip, float duration)
    {
        float targetVolume = GetSavedBGMVolume();
        float halfDuration = duration * 0.5f;

        if (bgmSource.isPlaying && bgmSource.volume > 0)
        {
            float startVol = bgmSource.volume;
            float timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0f, timer / halfDuration);
                yield return null;
            }
            bgmSource.volume = 0f;
            bgmSource.Stop();
        }

        bgmSource.clip = nextClip;
        bgmSource.Play();
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

    private float GetSavedBGMVolume()
    {
        if (DataManager.Instance != null)
            return (float)DataManager.Instance.GetBGMVolume() / 100f;
        return 1f;
    }
    #endregion

    #region Public Methods - SFX Logic (Updated)

    /// <summary>
    /// SFXType에 따라 적절한 클립을 재생합니다.
    /// </summary>
    public void PlaySFX(SFXType type)
    {
        AudioClip clipToPlay = null;

        switch (type)
        {
            // UI
            case SFXType.UI_Touch: clipToPlay = sfx_UI_Touch; break;
            case SFXType.Popup_Success: clipToPlay = sfx_Popup_Success; break;
            case SFXType.Popup_Fail: clipToPlay = sfx_Popup_Fail; break;
            case SFXType.Popup_Pause: clipToPlay = sfx_Popup_Pause; break;

            // Player Action
            case SFXType.Attack_Player: clipToPlay = sfx_Attack_Player; break;
            case SFXType.Skill_Buff: clipToPlay = sfx_Skill_Buff; break;
            case SFXType.Skill_Debuff: clipToPlay = sfx_Skill_Debuff; break;

            // Damage & Combat
            case SFXType.Damage_Player: clipToPlay = sfx_Damage_Player; break;
            case SFXType.Damage_Shield: clipToPlay = sfx_Damage_Shield; break;
            case SFXType.Damage_Enemy: clipToPlay = sfx_Damage_Enemy; break;
            case SFXType.Die_Enemy: clipToPlay = sfx_Die_Enemy; break;

            // Items
            case SFXType.Collect_Energy: clipToPlay = sfx_Collect_Energy; break;
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