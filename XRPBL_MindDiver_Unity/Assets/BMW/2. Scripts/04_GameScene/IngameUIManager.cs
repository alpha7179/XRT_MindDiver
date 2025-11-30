using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// UnityEngine.Rendering.DebugUI는 불필요하면 제거 권장

/// <summary>
/// 인게임 UI 매니저: HUD, 패널 관리 및 데이터 기반 동적 쉐이더 비네팅 제어
/// </summary>
public class IngameUIManager : MonoBehaviour
{
    public static IngameUIManager Instance { get; private set; }

    [Header("HUD Elements")]
    [SerializeField] private Canvas IngameCanvas;

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject manualPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject takenDamagePanel;

    [Header("Panels UI Elements")]
    [SerializeField] public TextMeshProUGUI scoreText;
    [SerializeField] public Slider progressSlider;
    [SerializeField] public TextMeshProUGUI HPText;
    [SerializeField] public Slider HPSlider;
    [SerializeField] public TextMeshProUGUI MPText;
    [SerializeField] public Slider MPSlider;
    [SerializeField] public TextMeshProUGUI BulletText;
    [SerializeField] public Slider BulletSlider;

    [SerializeField] public Image[] CharacterFrontImage;
    [SerializeField] public Image[] CharacterLeftImage;
    [SerializeField] public Image[] CharacterRightImage;

    [SerializeField] public Image pressureImages;

    [Header("UI Fade Settings")]
    [SerializeField] private float panelFadeDuration = 0.2f;
    [SerializeField] private float pulseSpeed = 3.0f;
    [SerializeField] private float minPulseAlpha = 0.2f;

    [Header("Vignette Effect Settings")]
    [SerializeField] private Image vignetteImage;
    private Material vignetteMat;

    private readonly int RadiusProp = Shader.PropertyToID("_Radius");
    private readonly int ColorProp = Shader.PropertyToID("_VignetteColor");

    [Header("Vignette Colors")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color bufferColor = Color.yellow;

    [Header("Vignette Pulse Config")]
    [Tooltip("체력이 100%일 때 비네팅 반지름 (안 보임)")]
    [SerializeField] private float maxRadius = 1;
    [Tooltip("체력이 0%일 때 비네팅 반지름 (매우 좁음)")]
    [SerializeField] private float minRadius = 0f;
    [Tooltip("버퍼 효과 지속 시간")]
    [SerializeField] private float bufferEffectDuration = 2.0f;

    private int currentShield;
    private int maxShield;
    private bool isBufferEffectActive = false;
    private float bufferTimer = 0f;

    // 패널별 실행 중인 코루틴 관리
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    private Dictionary<Image, Coroutine> imageCoroutines = new Dictionary<Image, Coroutine>();
    private float cachedOriginalAlpha = 1.0f;

    [Header("External References")]
    //[SerializeField] private OuttroUIManager outtroManager;

    private bool isDisplayPanel = false;

    [Header("디버그 로그")]
    [SerializeField] private bool isDebugMode = true;

    private void Start()
    {
        InitializePanels();

        if (vignetteImage != null)
        {
            vignetteMat = vignetteImage.material;
            vignetteMat.SetFloat(RadiusProp, maxRadius);
        }

        // 초기 체력 데이터 가져오기
        if (DataManager.Instance != null)
        {
            currentShield = DataManager.Instance.GetShipShield();
            maxShield = DataManager.Instance.maxShipShield;

            // 이벤트 구독
            GameManager.Instance.OnPauseStateChanged += HandlePauseState;
            DataManager.Instance.OnShieldChanged += HandleShieldChange;
            DataManager.Instance.OnBufferAdded += HandleBufferAdded;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPauseStateChanged -= HandlePauseState;
        }

        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnShieldChanged -= HandleShieldChange;
            DataManager.Instance.OnBufferAdded -= HandleBufferAdded;
        }
    }

    private void Update()
    {
        UpdateVignetteState();
    }

    private void HandleShieldChange(int current, int max)
    {
        currentShield = current;
        maxShield = max;
    }

    private void HandleBufferAdded()
    {
        // 버퍼 획득 시 타이머 설정 및 플래그 활성화
        bufferTimer = bufferEffectDuration;
        isBufferEffectActive = true;
    }

    private void UpdateVignetteState()
    {
        if (vignetteMat == null) return;

        // 1. 버퍼 효과 (노란색) - 우선순위 높음
        if (isBufferEffectActive)
        {
            bufferTimer -= Time.deltaTime;

            if (bufferTimer <= 0)
            {
                isBufferEffectActive = false; // 시간 종료 시 해제
            }
            else
            {
                // 노란색 설정
                vignetteMat.SetColor(ColorProp, bufferColor);

                // 빠르고 강한 맥박
                float pulse = Mathf.Sin(Time.time * 10.0f) * 0.1f;
                float targetRadius = 0.5f + pulse; // 0.4 ~ 0.6 사이 요동

                vignetteMat.SetFloat(RadiusProp, targetRadius);
                return; // 버퍼 효과 중에는 아래 체력 로직 무시
            }
        }

        // 2. 체력 비례 효과 (붉은색)
        float hpRatio = (float)currentShield / maxShield;

        // 체력이 100%가 아닐 때만 작동
        if (hpRatio < 1.0f)
        {
            vignetteMat.SetColor(ColorProp, damageColor);

            // 위험도 (체력이 낮을수록 1에 가까움)
            float dangerLevel = 1.0f - hpRatio;

            // 기본 반지름: 체력이 낮을수록 구멍(Radius)이 작아짐
            // 예: 100% -> 1.5 (안보임), 0% -> 0.4 (많이 가림)
            float baseRadius = Mathf.Lerp(maxRadius, minRadius, dangerLevel);

            // 맥박 속도: 체력이 낮을수록 심장이 빨리 뜀 (2배속 ~ 15배속)
            float currentPulseSpeed = Mathf.Lerp(2.0f, 15.0f, dangerLevel);

            // 맥박 강도: 체력이 낮을수록 더 크게 두근거림
            float pulseAmplitude = Mathf.Lerp(0.02f, 0.08f, dangerLevel);

            // Sine 파동 계산
            float pulse = Mathf.Sin(Time.time * currentPulseSpeed) * pulseAmplitude;

            vignetteMat.SetFloat(RadiusProp, baseRadius + pulse);
        }
        else
        {
            // 체력이 가득 찼으면 비네팅 제거
            vignetteMat.SetFloat(RadiusProp, maxRadius);
        }
    }

    private void InitializePanels()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (infoPanel) infoPanel.SetActive(false);
        if (instructionPanel) instructionPanel.SetActive(false);
        if (manualPanel) manualPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (takenDamagePanel) takenDamagePanel.SetActive(false);

        UpdateProgress(DataManager.Instance.GetProgress());
        UpdateScore(DataManager.Instance.GetScore());
        UpdateBullet(DataManager.Instance.GetBullet());
        UpdateHP(DataManager.Instance.GetShipShield());
        UpdateMP(DataManager.Instance.GetBuffer());

    }

    public void OnClickPauseButton()
    {
        Log("PauseButton Clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
            OpenPausePanel();
        }
    }

    public void OnClickContinueButton()
    {
        Log("ContinueButton Clicked");
        if (pausePanel != null && pausePanel.activeSelf)
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
            ClosePausePanel();
        }
    }

    public void OnClickBackButton()
    {
        Log("BackButton Clicked");
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
    }

    public void OpenInstructionPanel() { FadePanel(instructionPanel, true); SetDisplayPanel(true); }
    public void CloseInstructionPanel() { FadePanel(instructionPanel, false); SetDisplayPanel(false); }
    public void OpenManualPanel() { FadePanel(manualPanel, true); SetDisplayPanel(true); }
    public void CloseManualPanel() { FadePanel(manualPanel, false); SetDisplayPanel(false); }
    public void OpenPausePanel() { FadePanel(pausePanel, true); SetDisplayPanel(true); }
    public void ClosePausePanel() { FadePanel(pausePanel, false); SetDisplayPanel(false); }
    public void OpenMainPanel() { FadePanel(mainPanel, true); }
    public void CloseMainPanel() { FadePanel(mainPanel, false); }
    public void OpenInfoPanel() { FadePanel(infoPanel, true); }
    public void CloseInfoPanel() { FadePanel(infoPanel, false); }
    public void OpenTakenDamagePanel() { FadePanel(takenDamagePanel, true); }
    public void CloseTakenDamagePanel() { FadePanel(takenDamagePanel, false); }

    public void UpdateScore(int value) { scoreText.text = value.ToString(); HPSlider.value = value; }
    public void UpdateHP(int value) { HPText.text = value.ToString(); HPSlider.value = value; }
    public void UpdateMP(int value) { MPText.text = value.ToString(); MPSlider.value = value; }
    public void UpdateBullet(int value) { BulletText.text = value.ToString(); BulletSlider.value = value; }
    public void UpdateProgress(float value) { progressSlider.value = value; }

    public void SetDisplayPanel(bool state) { isDisplayPanel = state; }
    public bool GetDisplayPanel() { return isDisplayPanel; }

    private void HandlePauseState(bool isPaused)
    {
        if (pausePanel) pausePanel.SetActive(isPaused);
    }

    /*
    public void ShowOuttroUI()
    {
        if (IngameCanvas) IngameCanvas.enabled = false;
        if (outtroManager)
        {
            outtroManager.gameObject.SetActive(true);
            StartCoroutine(outtroManager.InitializeRoutine());
        }
    }
    */

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

        if (!show) panel.SetActive(false);
    }

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
}