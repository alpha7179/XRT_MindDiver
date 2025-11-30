using System.Linq;
using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class CharaterSelectManager : MonoBehaviour
{
    [Header("Video Players Settings (Order: 0=Front, 1=Left, 2=Right)")]
    public VideoPlayer[] videoPlayers;

    [Header("Video Clips Settings (Order: 0=Front, 1=Left, 2=Right)")]
    public VideoClip[] outroClips;

    [Header("Settings")]
    [Tooltip("동시 입력 유지 시간 (초)")]
    [SerializeField] private float requiredHoldTime = 3.0f;

    [Header("Text 구성요소")]
    [SerializeField] private GameObject FrontButtonText;
    [SerializeField] private GameObject LeftButtonText;
    [SerializeField] private GameObject RightButtonText;

    [Header("UI Fade Settings")]
    [SerializeField] private float panelFadeDuration = 0.2f;

    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();

    [Header("디버그 로그")]
    [SerializeField] private bool isDebugMode = true;

    [Header("디버그 옵션 (테스트용)")]
    [Tooltip("체크 시 정면 버튼만 눌러도 타이머가 작동합니다.")]
    [SerializeField] private bool enableSingleButtonDebug = false;


    // 버튼 입력 상태 관리 (0: Front, 1: Left, 2: Right)
    private bool[] isButtonPressed = new bool[3];
    private float currentHoldTimer = 0f;
    private bool isVideoPlayed = false;

    private void Start()
    {
        if (!CheckArrayValid(videoPlayers, "Video Players")) return;

        if (videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached += OnVideoFinished;
        }

        // 디버그 모드가 켜져있으면 경고 로그 출력
        if (enableSingleButtonDebug)
        {
            Debug.LogWarning("!!! [Test Mode] 정면 버튼만 눌러도 진행되는 모드가 켜져있습니다 !!!");
        }

        AudioManager.Instance.PlayBGM(GameManager.Instance.currentState);
    }

    private void Update()
    {
        if (isVideoPlayed) return;

        if (IsAllButtonsPressed())
        {
            currentHoldTimer += Time.deltaTime;

            if (currentHoldTimer % 1.0f < Time.deltaTime)
                Log($"Charging... {currentHoldTimer:F1} / {requiredHoldTime}");

            if (currentHoldTimer >= requiredHoldTime)
            {
                currentHoldTimer = 0f;
                isVideoPlayed = true;
                FadePanel(FrontButtonText, false);
                FadePanel(RightButtonText, false);
                FadePanel(LeftButtonText, false);
                StartVideoSequence();
            }
        }
        else
        {
            if (currentHoldTimer > 0)
            {
                currentHoldTimer = 0f;
                Log("Button released. Timer reset.");
            }
        }
    }

    private bool IsAllButtonsPressed()
    {

        if (enableSingleButtonDebug)
        {
            return isButtonPressed[0];
        }


        return isButtonPressed.All(x => x);
    }

    private void StartVideoSequence()
    {
        Log($"[VideoManager] {requiredHoldTime}s Hold Complete! Playing Outro Video...");

        if (CheckArrayValid(outroClips, "Outro Clips"))
        {
            PlayPanoramicVideo(outroClips);
        }
    }

    // ---------------------------------------------------------
    //  UI 이벤트 연결 함수
    // ---------------------------------------------------------
    public void OnPointerDownFront() { SetButtonState(0, true); FadePanel(FrontButtonText, false); Log("Front Display Held"); }
    public void OnPointerUpFront() { SetButtonState(0, false); if (!isVideoPlayed) { FadePanel(FrontButtonText, true); } Log("Front Display Released"); }

    public void OnPointerDownLeft() { SetButtonState(1, true); FadePanel(LeftButtonText, false); Log("Left Display Held"); }
    public void OnPointerUpLeft() { SetButtonState(1, false); if (!isVideoPlayed) { FadePanel(LeftButtonText, true); } Log("Left Display Released"); }

    public void OnPointerDownRight() { SetButtonState(2, true); FadePanel(RightButtonText, false); Log("Right Display Held"); }
    public void OnPointerUpRight() { SetButtonState(2, false); if (!isVideoPlayed) { FadePanel(RightButtonText, true); } Log("Right Display Released"); }

    private void SetButtonState(int index, bool isPressed)
    {
        isButtonPressed[index] = isPressed;
        if (isPressed) Log($"Button {index} Pressed");
        else Log($"Button {index} Released");
    }

    private bool CheckArrayValid(System.Array array, string arrayName)
    {
        if (array == null || array.Length < 3)
        {
            Log($"[VideoManager] Error: {arrayName} array must have at least 3 elements.");
            if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
            {
                OnVideoFinished(videoPlayers[0]);
            }
            return false;
        }
        return true;
    }

    private void PlayPanoramicVideo(VideoClip[] clips)
    {
        for (int i = 0; i < 3; i++)
        {
            if (videoPlayers[i] != null && clips[i] != null)
            {
                videoPlayers[i].clip = clips[i];
                videoPlayers[i].Play();
            }
        }

        if (videoPlayers[0] == null || clips[0] == null)
        {
            OnVideoFinished(null);
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        Log("[VideoManager] Video Finished.");
        if (videoPlayers != null && videoPlayers.Length > 0 && videoPlayers[0] != null)
        {
            videoPlayers[0].loopPointReached -= OnVideoFinished;
        }

        if (DataManager.Instance != null && GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.GameStage);
        }
    }

    private void FadePanel(GameObject panel, bool show)
    {
        if (panel == null) return;

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();

        if (panelCoroutines.ContainsKey(panel) && panelCoroutines[panel] != null)
        {
            StopCoroutine(panelCoroutines[panel]);
        }

        panelCoroutines[panel] = StartCoroutine(FadePanelRoutine(panel, cg, show));
    }

    private IEnumerator FadePanelRoutine(GameObject panel, CanvasGroup cg, bool show)
    {
        float targetAlpha = show ? 1.0f : 0.0f;
        float startAlpha = cg.alpha;
        float elapsed = 0f;

        if (show)
        {
            panel.SetActive(true);
            cg.alpha = 0f;
            startAlpha = 0f;
        }

        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
            yield return null;
        }

        cg.alpha = targetAlpha;

        if (!show)
        {
            panel.SetActive(false);
        }
    }

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
}