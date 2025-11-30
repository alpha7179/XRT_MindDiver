using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("Components")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Canvas fadeCanvas;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private int sortingOrder = 30000; // 모든 카메라/UI보다 위에 그리도록 매우 높은 값 설정

    private bool isFading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.parent = null;
            DontDestroyOnLoad(gameObject);

            // [해결책] 카메라 개수/설정과 무관하게 화면 전체를 덮도록 강제 설정
            SetupCanvasOverlay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupCanvasOverlay()
    {
        if (fadeCanvas != null)
        {
            // 1. 모드를 Overlay로 강제 변경 (카메라가 필요 없음)
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // 2. 그리기 순서를 최상위로 설정
            fadeCanvas.sortingOrder = sortingOrder;

            // 3. 물리적 레이캐스트 차단 (혹시 모를 터치 방지)
            if (fadeCanvas.TryGetComponent<UnityEngine.UI.GraphicRaycaster>(out var raycaster))
            {
                raycaster.blockingObjects = UnityEngine.UI.GraphicRaycaster.BlockingObjects.All;
            }
        }
    }

    // 더 이상 OnEnable, OnDisable, Camera.onPreCull, ForceAssignCamera 등이 필요 없습니다.
    // Overlay 모드는 카메라에 의존하지 않고 화면 버퍼에 직접 그리기 때문입니다.

    public void LoadScene(string sceneName)
    {
        if (isFading) return;

        // IntroScene으로 갈 때 매니저 초기화 로직
        if (sceneName == "IntroScene")
        {
            Debug.Log("[SceneTransitionManager] Destroying Manager instance before loading Intro scene.");
            if (DataManager.Instance != null) Destroy(DataManager.Instance.gameObject);
            if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        }

        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        isFading = true;

        // 1. 페이드 아웃 (투명 -> 검정)
        yield return StartCoroutine(Fade(0f, 1f));

        // 2. 씬 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 로딩 대기
        while (op.progress < 0.9f)
        {
            yield return null;
        }

        // 3. 씬 전환 승인
        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            yield return null;
        }

        // 씬이 바뀌어도 Overlay 모드이므로 카메라를 다시 찾을 필요가 없습니다.

        // 4. 페이드 인 (검정 -> 투명)
        yield return StartCoroutine(Fade(1f, 0f));

        isFading = false;
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
            fadeCanvasGroup.blocksRaycasts = (endAlpha > 0.9f);
        }
    }
}