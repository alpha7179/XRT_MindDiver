using UnityEngine;
using static GameManager;
using static GamePhaseManager;

/// <summary>
/// 게임 내 배경음악(BGM)과 효과음(SFX) 재생을 총괄하는 오디오 관리 클래스
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Singleton
    public static AudioManager Instance { get; private set; }
    #endregion

    #region Inspector Fields
    [Header("Audio Sources")]
    // 배경음악 재생용 오디오 소스
    public AudioSource bgmSource;
    // 효과음 재생용 오디오 소스
    public AudioSource sfxSource;

    [Header("BGM Clips")]
    // 메인 메뉴 배경음악
    public AudioClip bgm_MainMenu;
    // 캐릭터 선택 화면 배경음악
    public AudioClip bgm_CharacterSelect;
    // 페이즈 1 배경음악
    public AudioClip bgm_Phase1;
    // 페이즈 2 배경음악
    public AudioClip bgm_Phase2;
    // 페이즈 3 배경음악
    public AudioClip bgm_Phase3;
    // 결과 화면 배경음악
    public AudioClip bgm_Result;

    [Header("SFX Clips")]
    // 터치 효과음
    public AudioClip sfx_Touch;
    // 폭발 효과음
    public AudioClip sfx_Explosion;
    // 쉴드 피격 효과음
    public AudioClip sfx_ShieldHit;
    // 아이템 획득 효과음
    public AudioClip sfx_ItemCollect;
    #endregion

    #region Unity Lifecycle
    /*
     * 싱글톤 인스턴스 초기화 및 파괴 방지 설정 수행
     */
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
    #endregion

    #region Public Methods - BGM
    /*
     * 특정 오디오 클립으로 BGM 재생 (중복 재생 방지)
     */
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource.clip == clip) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    /*
     * 게임 상태 및 페이즈에 따른 BGM 자동 재생
     */
    public void PlayBGM(GameState gameState, Phase phaseState = Phase.Null)
    {
        switch (gameState)
        {
            case GameState.MainMenu:
                PlayBGM(bgm_MainMenu);
                break;
            case GameState.CharacterSelect:
                PlayBGM(bgm_CharacterSelect);
                break;
            case GameState.GameStage:
                switch (phaseState)
                {
                    case Phase.Phase1: PlayBGM(bgm_Phase1); break;
                    case Phase.Phase2: PlayBGM(bgm_Phase2); break;
                    case Phase.Phase3: PlayBGM(bgm_Phase3); break;
                }
                break;
            case GameState.Result:
                PlayBGM(bgm_Result);
                break;
        }
    }
    #endregion

    #region Public Methods - SFX
    /*
     * 특정 오디오 클립을 효과음으로 1회 재생
     */
    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    /*
     * 문자열 키워드를 통한 효과음 재생 (편의성 제공)
     */
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
    #endregion
}