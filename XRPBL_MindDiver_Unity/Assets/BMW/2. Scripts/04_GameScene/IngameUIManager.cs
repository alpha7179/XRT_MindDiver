using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD, 패널 관리 및 데이터 기반 동적 쉐이더 비네팅 제어 클래스
/// </summary>
public class IngameUIManager : MonoBehaviour
{
    #region Singleton
    public static IngameUIManager Instance { get; private set; }
    #endregion

    #region Inspector Fields - HUD & Panels
    [Header("HUD Elements")]
    // 인게임 캔버스 참조
    [SerializeField] private Canvas IngameCanvas;

    [Header("Panels")]
    // 메인 패널 참조
    [SerializeField] private GameObject mainPanel;
    // 정보 패널 참조
    [SerializeField] private GameObject infoPanel;
    // 설명 패널 참조
    [SerializeField] private GameObject instructionPanel;
    // 매뉴얼 패널 참조
    [SerializeField] private GameObject manualPanel;
    // 일시정지 패널 참조
    [SerializeField] private GameObject pausePanel;
    // 피격 효과 패널 참조
    [SerializeField] private GameObject takenDamagePanel;

    [Header("Panels UI Elements")]
    // 점수 텍스트
    [SerializeField] public TextMeshProUGUI scoreText;
    // 진행도 슬라이더
    [SerializeField] public Slider progressSlider;
    // 체력(HP) 텍스트
    [SerializeField] public TextMeshProUGUI HPText;
    // 체력 슬라이더
    [SerializeField] public Slider HPSlider;
    // 마력(MP)/버퍼 텍스트
    [SerializeField] public TextMeshProUGUI MPText;
    // 마력/버퍼 슬라이더
    [SerializeField] public Slider MPSlider;
    // 총알 개수 텍스트
    [SerializeField] public TextMeshProUGUI BulletText;
    // 총알 슬라이더
    [SerializeField] public Slider BulletSlider;

    // 캐릭터 정면 이미지 배열
    [SerializeField] public Image[] CharacterFrontImage;
    // 캐릭터 좌측면 이미지 배열
    [SerializeField] public Image[] CharacterLeftImage;
    // 캐릭터 우측면 이미지 배열
    [SerializeField] public Image[] CharacterRightImage;

    // 압력 게이지 이미지
    [SerializeField] public Image pressureImages;
    #endregion

    #region Inspector Fields - Settings
    [Header("UI Fade Settings")]
    // 패널 페이드 효과 지속 시간
    [SerializeField] private float panelFadeDuration = 0.2f;
    // 맥박 효과 속도
    [SerializeField] private float pulseSpeed = 3.0f;
    // 맥박 효과 최소 투명도
    [SerializeField] private float minPulseAlpha = 0.2f;

    [Header("Vignette Effect Settings")]
    // 비네팅 효과 적용 이미지
    [SerializeField] private Image vignetteImage;
    // 비네팅 효과 적용 재질
    private Material vignetteMat;

    // 쉐이더 프로퍼티 ID 캐싱
    private readonly int RadiusProp = Shader.PropertyToID("_Radius");
    private readonly int ColorProp = Shader.PropertyToID("_VignetteColor");

    [Header("Vignette Colors")]
    // 피격/위험 시 비네팅 색상
    [SerializeField] private Color damageColor = Color.red;
    // 버퍼 획득 시 비네팅 색상
    [SerializeField] private Color bufferColor = Color.yellow;

    [Header("Vignette Pulse Config")]
    [Tooltip("체력이 100%일 때 비네팅 반지름 (안 보임)")]
    [SerializeField] private float maxRadius = 1;
    [Tooltip("체력이 0%일 때 비네팅 반지름 (매우 좁음)")]
    [SerializeField] private float minRadius = 0f;
    [Tooltip("버퍼 효과 지속 시간")]
    [SerializeField] private float bufferEffectDuration = 2.0f;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    // 현재 쉴드량 저장
    private int currentShield;
    // 최대 쉴드량 저장
    private int maxShield;
    // 버퍼 효과 활성화 여부
    private bool isBufferEffectActive = false;
    // 버퍼 효과 타이머
    private float bufferTimer = 0f;
    // 패널 활성화 상태 플래그
    private bool isDisplayPanel = false;

    // 패널별 실행 중인 코루틴 관리 딕셔너리
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    // 이미지별 실행 중인 코루틴 관리 딕셔너리
    private Dictionary<Image, Coroutine> imageCoroutines = new Dictionary<Image, Coroutine>();
    #endregion

    #region Unity Lifecycle
    /*
     * 패널 초기화, 쉐이더 설정 및 이벤트 구독 수행
     */
    private void Start()
    {
        InitializePanels();

        if (vignetteImage != null)
        {
            vignetteMat = vignetteImage.material;
            vignetteMat.SetFloat(RadiusProp, maxRadius);
        }

        // 초기 데이터 로드 및 이벤트 구독 수행
        if (DataManager.Instance != null)
        {
            currentShield = DataManager.Instance.GetShipShield();
            maxShield = DataManager.Instance.maxShipShield;

            GameManager.Instance.OnPauseStateChanged += HandlePauseState;
            DataManager.Instance.OnShieldChanged += HandleShieldChange;
            DataManager.Instance.OnBufferAdded += HandleBufferAdded;
        }
    }

    /*
     * 이벤트 구독 해제 수행
     */
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

    /*
     * 비네팅 효과 상태 갱신 수행
     */
    private void Update()
    {
        UpdateVignetteState();
    }
    #endregion

    #region Event Handlers
    /*
     * 쉴드 수치 변경 시 내부 데이터 갱신
     */
    private void HandleShieldChange(int current, int max)
    {
        currentShield = current;
        maxShield = max;
    }

    /*
     * 버퍼 획득 시 효과 타이머 및 플래그 설정
     */
    private void HandleBufferAdded()
    {
        bufferTimer = bufferEffectDuration;
        isBufferEffectActive = true;
    }

    /*
     * 일시정지 상태 변경에 따른 패널 제어
     */
    private void HandlePauseState(bool isPaused)
    {
        if (pausePanel) pausePanel.SetActive(isPaused);
    }
    #endregion

    #region Vignette Logic
    /*
     * 체력 및 버퍼 상태에 따른 비네팅 쉐이더 효과 갱신
     */
    private void UpdateVignetteState()
    {
        if (vignetteMat == null) return;

        // 1. 버퍼 효과 (노란색) - 우선순위 높음
        if (isBufferEffectActive)
        {
            bufferTimer -= Time.deltaTime;

            if (bufferTimer <= 0)
            {
                // 시간 종료 시 효과 해제
                isBufferEffectActive = false;
            }
            else
            {
                // 색상 설정 (노란색)
                vignetteMat.SetColor(ColorProp, bufferColor);

                // 빠르고 강한 맥박 효과 계산
                float pulse = Mathf.Sin(Time.time * 10.0f) * 0.1f;
                // 반지름 요동 적용 (0.4 ~ 0.6)
                float targetRadius = 0.5f + pulse;

                vignetteMat.SetFloat(RadiusProp, targetRadius);
                return; // 버퍼 효과 중에는 체력 비례 효과 무시
            }
        }

        // 2. 체력 비례 효과 (붉은색)
        float hpRatio = (float)currentShield / maxShield;

        // 체력 100% 미만 시 효과 적용
        if (hpRatio < 1.0f)
        {
            vignetteMat.SetColor(ColorProp, damageColor);

            // 체력 반비례 위험도 수치 계산
            float dangerLevel = 1.0f - hpRatio;

            // 기본 반지름 계산 (체력이 낮을수록 시야 좁아짐)
            float baseRadius = Mathf.Lerp(maxRadius, minRadius, dangerLevel);

            // 맥박 속도 계산 (체력이 낮을수록 속도 증가)
            float currentPulseSpeed = Mathf.Lerp(2.0f, 15.0f, dangerLevel);

            // 맥박 강도 계산 (체력이 낮을수록 강도 증가)
            float pulseAmplitude = Mathf.Lerp(0.02f, 0.08f, dangerLevel);

            // Sine 파동 적용
            float pulse = Mathf.Sin(Time.time * currentPulseSpeed) * pulseAmplitude;

            vignetteMat.SetFloat(RadiusProp, baseRadius + pulse);
        }
        else
        {
            // 체력 100% 시 효과 제거
            vignetteMat.SetFloat(RadiusProp, maxRadius);
        }
    }
    #endregion

    #region UI Control Methods
    /*
     * 모든 패널 비활성화 및 초기 UI 값 설정
     */
    private void InitializePanels()
    {
        if (mainPanel) mainPanel.SetActive(false);
        if (infoPanel) infoPanel.SetActive(false);
        if (instructionPanel) instructionPanel.SetActive(false);
        if (manualPanel) manualPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (takenDamagePanel) takenDamagePanel.SetActive(false);

        // 초기 데이터 UI 반영
        UpdateProgress(DataManager.Instance.GetProgress());
        UpdateScore(DataManager.Instance.GetScore());
        UpdateBullet(DataManager.Instance.GetBullet());
        UpdateHP(DataManager.Instance.GetShipShield());
        UpdateMP(DataManager.Instance.GetBuffer());
    }

    // --- Button Event Handlers ---
    /*
     * 일시정지 버튼 클릭 처리
     */
    public void OnClickPauseButton()
    {
        Log("PauseButton Clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
            OpenPausePanel();
        }
    }

    /*
     * 계속하기 버튼 클릭 처리
     */
    public void OnClickContinueButton()
    {
        Log("ContinueButton Clicked");
        if (pausePanel != null && pausePanel.activeSelf)
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
            ClosePausePanel();
        }
    }

    /*
     * 뒤로가기(메인 메뉴) 버튼 클릭 처리
     */
    public void OnClickBackButton()
    {
        Log("BackButton Clicked");
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
    }

    // --- Panel Open/Close Wrappers ---
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

    // --- UI Element Updates ---
    public void UpdateScore(int value) { scoreText.text = value.ToString(); HPSlider.value = value; }
    public void UpdateHP(int value) { HPText.text = value.ToString(); HPSlider.value = value; }
    public void UpdateMP(int value) { MPText.text = value.ToString(); MPSlider.value = value; }
    public void UpdateBullet(int value) { BulletText.text = value.ToString(); BulletSlider.value = value; }
    public void UpdateProgress(float value) { progressSlider.value = value; }

    // --- State Accessors ---
    public void SetDisplayPanel(bool state) { isDisplayPanel = state; }
    public bool GetDisplayPanel() { return isDisplayPanel; }
    #endregion

    #region Coroutines & Animations
    /*
     * 패널 페이드 효과 코루틴 실행 및 관리
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

    /*
     * 알파값 조정을 통한 페이드 인/아웃 처리 코루틴
     */
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
    #endregion

    #region Utils
    /*
     * 디버그 모드 시 로그 출력 수행
     */
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}