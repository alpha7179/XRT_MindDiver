using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 씬 전환 흐름 및 화면/오디오 페이드 효과를 제어하는 관리자 클래스
/// - 개선: 화면이 어두워질 때 BGM과 SFX도 함께 페이드 아웃 처리
/// - 개선: 매니저 파괴 시점을 페이드 아웃 이후로 변경하여 안정성 확보
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    #region Singleton
    public static SceneTransitionManager Instance;
    #endregion

    #region Inspector Fields
    [Header("Components")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Canvas fadeCanvas;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private int sortingOrder = 30000;
    #endregion

    #region Private Fields
    private bool isFading = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null, false);
            DontDestroyOnLoad(gameObject);
            SetupCanvasOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Public Methods
    public void LoadScene(string sceneName)
    {
        if (isFading) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }
    #endregion

    #region Coroutines
    private IEnumerator TransitionRoutine(string sceneName)
    {
        isFading = true;

        // [핵심 개선] 1. 오디오 페이드 아웃 시작 (화면 페이드와 동시에 진행)
        if (AudioManager.Instance != null)
        {
            // BGM은 AudioManager에 내장된 페이드 기능 사용
            AudioManager.Instance.StopBGM(fadeDuration);

            // SFX는 여기서 직접 볼륨을 줄여줌
            StartCoroutine(FadeOutSFX(fadeDuration));
        }

        // 2. 화면 페이드 아웃 (투명 -> 검정)
        yield return StartCoroutine(Fade(0f, 1f));

        // [핵심 개선] 3. 매니저 정리 로직 이동
        // 페이드가 끝난 후(화면이 검을 때) 정리해야 자연스럽고 오류가 없음
        if (sceneName == "IntroScene" || sceneName == "01_MainMenuScene") // 인트로 혹은 메인으로 갈 때 초기화
        {
            Debug.Log("[SceneTransitionManager] 씬 로드 전 매니저 인스턴스 정리");
            if (DataManager.Instance != null) Destroy(DataManager.Instance.gameObject);
            if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        }

        // 4. 비동기 씬 로딩
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }

        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            yield return null;
        }

        // 5. 화면 페이드 인 (검정 -> 투명)
        yield return StartCoroutine(Fade(1f, 0f));

        isFading = false;
    }

    /// <summary>
    /// SFX 오디오 소스의 볼륨을 서서히 0으로 줄이는 코루틴
    /// </summary>
    private IEnumerator FadeOutSFX(float duration)
    {
        // AudioManager의 sfxSource에 접근
        if (AudioManager.Instance == null || AudioManager.Instance.sfxSource == null) yield break;

        AudioSource sfxSource = AudioManager.Instance.sfxSource;
        float startVolume = sfxSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            // 재생 중일 때만 볼륨 조절
            if (sfxSource.isPlaying)
            {
                sfxSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
            }
            yield return null;
        }

        // 완전히 끄기
        sfxSource.Stop();
        sfxSource.volume = startVolume; // 다음 재생을 위해 볼륨 값은 복구하지 않아도 됨 (AudioManager.PlaySFX에서 재설정함)
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsedTime = 0f;

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            fadeCanvasGroup.alpha = startAlpha;
        }

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);

            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = newAlpha;

            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = endAlpha;
            // 화면이 다 밝아졌을 때(0)만 입력 허용, 어두울 땐(1) 차단
            fadeCanvasGroup.blocksRaycasts = (endAlpha > 0.9f);
        }
    }
    #endregion

    #region Helper Methods
    private void SetupCanvasOverlay()
    {
        if (fadeCanvas != null)
        {
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = sortingOrder;

            if (fadeCanvas.TryGetComponent<UnityEngine.UI.GraphicRaycaster>(out var raycaster))
            {
                raycaster.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.All;
            }
        }
    }
    #endregion
}