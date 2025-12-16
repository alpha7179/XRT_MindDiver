using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IngameUIManager : MonoBehaviour
{
    #region Singleton
    public static IngameUIManager Instance { get; private set; }
    #endregion

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        RadiusProp = Shader.PropertyToID("_Radius");
        ColorProp = Shader.PropertyToID("_VignetteColor");
    }

    #region Inspector Fields - HUD & Panels
    // ... (기존 필드 동일) ...
    [Header("XR CAVE Elements")]
    [SerializeField] private Canvas[] ingameCanvases;

    [Header("Panels (Multi-Screen Support)")]
    [SerializeField] private List<GameObject> mainPanels;
    [SerializeField] private List<GameObject> infoPanels;
    [SerializeField] private List<GameObject> instructionPanels;
    [SerializeField] private List<GameObject> manualPanels;
    [SerializeField] private List<GameObject> pausePanels;
    [SerializeField] private List<GameObject> failPanels;
    [SerializeField] private List<GameObject> blackoutPanels;
    [SerializeField] private List<GameObject> takenDamagePanels;

    [Header("Panels UI Elements")]
    [SerializeField] public List<TextMeshProUGUI> scoreTexts;
    [SerializeField] public TextMeshProUGUI progressText;
    [SerializeField] public Image progressSlider;
    [SerializeField] public List<TextMeshProUGUI> HPTexts;
    [SerializeField] public List<Slider> HPSliders;
    [SerializeField] public List<TextMeshProUGUI> ShieldTexts;
    [SerializeField] public List<Slider> ShieldSliders;
    [SerializeField] public TextMeshProUGUI BulletText;
    [SerializeField] public Slider BulletSlider;
    [SerializeField] public TextMeshProUGUI BuffText;
    [SerializeField] public Slider BuffSlider;
    [SerializeField] public TextMeshProUGUI DeBuffText;
    [SerializeField] public Slider DeBuffSlider;
    [SerializeField] private List<GameObject> arrowPanels;

    [Header("Character Display Settings")]
    [SerializeField] public bool useVideoMode = false;
    [SerializeField] public List<Image> CharacterFrontImage;
    [SerializeField] public List<Image> CharacterLeftImage;
    [SerializeField] public List<Image> CharacterRightImage;
    [SerializeField] public List<VideoClip> CharacterFrontVideo;
    [SerializeField] public List<VideoClip> CharacterLeftVideo;
    [SerializeField] public List<VideoClip> CharacterRightVideo;
    [SerializeField] public VideoPlayer frontVideoPlayer;
    [SerializeField] public VideoPlayer leftVideoPlayer;
    [SerializeField] public VideoPlayer rightVideoPlayer;
    [SerializeField] public RawImage frontRawImage;
    [SerializeField] public RawImage leftRawImage;
    [SerializeField] public RawImage rightRawImage;
    [SerializeField] public float videoPlaybackSpeed = 1.0f;
    #endregion

    #region Inspector Fields - Settings
    [Header("UI Fade Settings")]
    [SerializeField] private float panelFadeDuration = 0.2f;

    [Header("Vignette Effect Settings")]
    [SerializeField] private List<Image> vignetteImages;
    private List<Material> vignetteMats = new List<Material>();

    private int RadiusProp;
    private int ColorProp;

    [Header("Vignette Colors")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private Color shieldDamageColor = Color.blue;
    [SerializeField] private Color bufferColor = Color.yellow; // 노란색 (버프)
    [SerializeField] private Color debufferColor = new Color(0.6f, 0f, 0.8f, 1f); // 보라색 (디버프)

    [Header("Vignette Configuration")]
    [SerializeField] private float maxRadius = 0.6f;
    [SerializeField] private float minRadius = -0.3f;

    [Header("Skill Effect Settings")]
    [SerializeField] private float skillTargetRadius = 0.35f; // 스킬 발동 시 비네팅 크기

    [Header("Damage Feedback Settings")]
    [SerializeField] private float damageImageDuration = 2.0f;
    [SerializeField] private float hitVignetteDuration = 2.0f;

    [Header("Debug Settings")]
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields - Logic
    private OuttroUIManager outtroUIManager;
    private int currentHealth;
    private int maxHealth;
    private int currentShield;
    private int maxShield;

    // [변경] 불필요해진 타이머 변수 제거 (DataManager 상태를 직접 참조)
    private bool isHitEffectActive = false;
    private float hitEffectTimer = 0f;

    private Coroutine characterResetRoutine;
    private bool isDisplayPanel = false;
    private Dictionary<GameObject, Coroutine> panelCoroutines = new Dictionary<GameObject, Coroutine>();
    #endregion

    #region Private Fields - Seamless Video
    private class SeamlessVideoChannel
    {
        public VideoPlayer playerA;
        public VideoPlayer playerB;
        public RawImage displayImage;
        public Coroutine transitionRoutine;
        public bool isUsingA = true;
    }
    private SeamlessVideoChannel frontChannel;
    private SeamlessVideoChannel leftChannel;
    private SeamlessVideoChannel rightChannel;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        outtroUIManager = GetComponent<OuttroUIManager>();

        foreach (var img in vignetteImages)
        {
            if (img != null)
            {
                Material mat = img.material;
                mat.SetFloat(RadiusProp, maxRadius);
                vignetteMats.Add(mat);
            }
        }

        InitializeCharacterDisplayMode();

        if (useVideoMode)
        {
            frontChannel = InitializeVideoChannel(frontVideoPlayer, frontRawImage);
            leftChannel = InitializeVideoChannel(leftVideoPlayer, leftRawImage);
            rightChannel = InitializeVideoChannel(rightVideoPlayer, rightRawImage);
        }

        SetCharacterState(0);

        if (DataManager.Instance != null)
        {
            currentHealth = DataManager.Instance.GetShipHealth();
            maxHealth = DataManager.Instance.maxShipHealth;
            currentShield = DataManager.Instance.GetShipShield();
            maxShield = DataManager.Instance.maxShipShield;

            GameManager.Instance.OnPauseStateChanged += HandlePauseState;
            GameManager.Instance.OnFailStateChanged += HandleFailState;
            DataManager.Instance.OnHealthChanged += HandleHealthChange;
            DataManager.Instance.OnShieldChanged += HandleShieldChange;
            DataManager.Instance.OnDeBufferAdded += HandleDeBufferAdded;
            DataManager.Instance.OnBufferAdded += HandleBufferAdded;
        }

        InitializeSliders();
    }

    private void OnDestroy()
    {
        // ... (기존과 동일)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPauseStateChanged -= HandlePauseState;
            GameManager.Instance.OnFailStateChanged -= HandleFailState;
        }
        if (DataManager.Instance != null)
        {
            DataManager.Instance.OnHealthChanged -= HandleHealthChange;
            DataManager.Instance.OnShieldChanged -= HandleShieldChange;
            DataManager.Instance.OnDeBufferAdded -= HandleDeBufferAdded;
            DataManager.Instance.OnBufferAdded -= HandleBufferAdded;
        }
    }

    private void Update()
    {
        UpdateVignetteState();
    }
    #endregion

    #region Event Handlers
    private void HandleShieldChange(int current, int max)
    {
        if (current < currentShield) TriggerDamageEffect();
        currentShield = current;
        maxShield = max;
        UpdateShield(current);
    }

    private void HandleHealthChange(int current, int max)
    {
        if (current < currentHealth) TriggerDamageEffect();
        currentHealth = current;
        maxHealth = max;
        UpdateHP(current);
    }

    private void HandleBufferAdded()
    {
        // [수정] 아이템 획득 시에는 UI 슬라이더만 갱신 (비주얼 효과는 UpdateVignetteState에서 상태 기반으로 처리)
        if (DataManager.Instance != null) UpdateBuff(DataManager.Instance.GetBuffer());
    }

    private void HandleDeBufferAdded()
    {
        // [수정] 아이템 획득 시에는 UI 슬라이더만 갱신
        if (DataManager.Instance != null) UpdateDeBuff(DataManager.Instance.GetDeBuffer());
    }

    private void HandlePauseState(bool isPaused)
    {
        if (isPaused)
        {
            OpenPausePanel();
            PlayUISound(SFXType.Popup_Pause);
        }
        else
        {
            ClosePausePanel();
        }

        if (useVideoMode)
        {
            if (isPaused) PauseAllVideos();
            else ResumeAllVideos();
        }
    }

    private void HandleFailState(bool isFailed)
    {
        if (isFailed)
        {
            OpenFailPanel();
            SetCharacterState(2);
            PlayUISound(SFXType.Popup_Fail);
        }
        else
        {
            CloseFailPanel();
        }
    }
    #endregion

    #region Vignette Logic (Updated Priority)
    /// <summary>
    /// 비네팅 효과 상태 업데이트 (매 프레임 호출)
    /// - 수정됨: 스킬(버프/디버프) > 피격 > 체력 순으로 우선순위 변경
    /// </summary>
    private void UpdateVignetteState()
    {
        if (vignetteMats.Count == 0) return;

        float targetRadius = maxRadius;
        Color targetColor = damageColor;

        // DataManager 상태 확인
        bool isBuff = (DataManager.Instance != null && DataManager.Instance.isBuffState);
        bool isDebuff = (DataManager.Instance != null && DataManager.Instance.isDebuffState);

        // [우선순위 1] 버프 상태 (가장 높은 우선순위, 다른 효과 무시)
        if (isBuff)
        {
            targetColor = bufferColor;
            targetRadius = skillTargetRadius;

            // 참고: 버프 중에도 피격 타이머를 줄이고 싶다면 여기서 hitEffectTimer 처리를 할 수도 있습니다.
            // 현재 로직은 버프가 끝나면 맞았던 효과가 뒤늦게 나올 수 있습니다. (원치 않으면 TriggerDamageEffect 수정 필요)
        }
        // [우선순위 2] 디버프 상태
        else if (isDebuff)
        {
            targetColor = debufferColor;
            targetRadius = skillTargetRadius;
        }
        // [우선순위 3] 피격 효과 (스킬 상태가 아닐 때만 표시됨)
        else if (isHitEffectActive)
        {
            targetColor = (currentShield > 0) ? shieldDamageColor : damageColor;
            hitEffectTimer -= Time.deltaTime;

            if (hitEffectTimer <= 0) isHitEffectActive = false;
            else
            {
                float hitProgress = 1.0f - (hitEffectTimer / hitVignetteDuration);
                targetRadius = Mathf.Lerp(minRadius, maxRadius, hitProgress);
            }
        }
        // [우선순위 4] 체력 경고 (가장 낮은 순위)
        else
        {
            float healthRatio = (maxHealth > 0) ? (float)currentHealth / maxHealth : 0f;
            if (healthRatio <= 0.5f)
            {
                targetColor = damageColor;
                float t = 1.0f - (healthRatio / 0.5f);
                float healthBasedRadius = Mathf.Lerp(maxRadius, minRadius, t);
                float pulseSpeed = Mathf.Lerp(2.0f, 8.0f, t);
                float pulseAmp = Mathf.Lerp(0.0f, 0.05f, t);
                targetRadius = healthBasedRadius + Mathf.Sin(Time.time * pulseSpeed) * pulseAmp;
            }
            else
            {
                targetRadius = maxRadius;
            }
        }

        // 쉐이더(혹은 이미지 컬러) 적용
        foreach (var mat in vignetteMats)
        {
            if (mat != null)
            {
                // 쉐이더 프로퍼티 사용 시
                mat.SetColor(ColorProp, targetColor);
                mat.SetFloat(RadiusProp, targetRadius);
            }
        }

        // (만약 쉐이더 대신 Image.color를 쓰는 방식을 적용 중이라면 아래 코드를 사용하세요)
        /*
        foreach (var img in vignetteImages)
        {
            if (img != null)
            {
                Color finalColor = targetColor;
                float alpha = Mathf.InverseLerp(maxRadius, skillTargetRadius, targetRadius);
                finalColor.a = Mathf.Clamp(alpha, 0.0f, 0.8f);
                img.color = finalColor;
            }
        }
        */
    }
    #endregion

    #region Character Logic & Seamless Video & UI Control
    // ... (이 아래 코드는 기존과 동일하므로 생략하지 않고 유지합니다) ...
    // 편의를 위해 UI Control 부분 등 나머지 코드는 변경 사항이 없으므로 그대로 두시면 됩니다.

    private void InitializeCharacterDisplayMode()
    {
        if (useVideoMode)
        {
            if (frontRawImage) frontRawImage.gameObject.SetActive(true);
            if (leftRawImage) leftRawImage.gameObject.SetActive(true);
            if (rightRawImage) rightRawImage.gameObject.SetActive(true);

            if (frontVideoPlayer) frontVideoPlayer.gameObject.SetActive(true);
            if (leftVideoPlayer) leftVideoPlayer.gameObject.SetActive(true);
            if (rightVideoPlayer) rightVideoPlayer.gameObject.SetActive(true);

            HideAllCharacterImages();
        }
        else
        {
            if (frontRawImage) frontRawImage.gameObject.SetActive(false);
            if (leftRawImage) leftRawImage.gameObject.SetActive(false);
            if (rightRawImage) rightRawImage.gameObject.SetActive(false);
        }
    }

    private void TriggerDamageEffect()
    {
        isHitEffectActive = true;
        hitEffectTimer = hitVignetteDuration;
        SetCharacterState(1);
    }

    public void SetCharacterState(int index)
    {
        if (characterResetRoutine != null) StopCoroutine(characterResetRoutine);
        if (useVideoMode) UpdateCharacterVideo(index);
        else UpdateCharacterImage(index);
    }

    private void UpdateCharacterImage(int index)
    {
        UpdateImageList(CharacterFrontImage, index);
        UpdateImageList(CharacterLeftImage, index);
        UpdateImageList(CharacterRightImage, index);
        if (index == 1) characterResetRoutine = StartCoroutine(ResetCharacterStateRoutine(damageImageDuration));
    }

    private void UpdateImageList(List<Image> images, int targetIndex)
    {
        if (images == null) return;
        for (int i = 0; i < images.Count; i++)
            if (images[i] != null) images[i].gameObject.SetActive(i == targetIndex);
    }

    private void HideAllCharacterImages()
    {
        foreach (var img in CharacterFrontImage) if (img) img.gameObject.SetActive(false);
        foreach (var img in CharacterLeftImage) if (img) img.gameObject.SetActive(false);
        foreach (var img in CharacterRightImage) if (img) img.gameObject.SetActive(false);
    }

    private SeamlessVideoChannel InitializeVideoChannel(VideoPlayer originalPlayer, RawImage targetImage)
    {
        if (originalPlayer == null || targetImage == null) return null;
        SeamlessVideoChannel channel = new SeamlessVideoChannel();
        channel.playerA = originalPlayer;
        channel.displayImage = targetImage;

        GameObject ghostObj = new GameObject(originalPlayer.name + "_Ghost");
        ghostObj.transform.SetParent(originalPlayer.transform.parent);
        ghostObj.transform.localPosition = originalPlayer.transform.localPosition;
        ghostObj.transform.localRotation = originalPlayer.transform.localRotation;
        ghostObj.transform.localScale = originalPlayer.transform.localScale;

        VideoPlayer ghostPlayer = ghostObj.AddComponent<VideoPlayer>();
        ghostPlayer.playOnAwake = false;
        ghostPlayer.waitForFirstFrame = true;
        ghostPlayer.isLooping = originalPlayer.isLooping;
        ghostPlayer.source = VideoSource.VideoClip;
        ghostPlayer.audioOutputMode = originalPlayer.audioOutputMode;
        channel.playerA.renderMode = VideoRenderMode.APIOnly;
        ghostPlayer.renderMode = VideoRenderMode.APIOnly;
        channel.playerA.targetTexture = null;
        ghostPlayer.targetTexture = null;
        channel.playerB = ghostPlayer;
        channel.isUsingA = true;
        return channel;
    }

    private void UpdateCharacterVideo(int index)
    {
        VideoClip frontClip = (index < CharacterFrontVideo.Count) ? CharacterFrontVideo[index] : null;
        VideoClip leftClip = (index < CharacterLeftVideo.Count) ? CharacterLeftVideo[index] : null;
        VideoClip rightClip = (index < CharacterRightVideo.Count) ? CharacterRightVideo[index] : null;
        bool isLooping = (index != 1);

        PlaySeamless(frontChannel, frontClip, isLooping);
        PlaySeamless(leftChannel, leftClip, isLooping);
        PlaySeamless(rightChannel, rightClip, isLooping);

        if (index == 1 && frontClip != null)
        {
            float waitTime = (float)frontClip.length / videoPlaybackSpeed;
            characterResetRoutine = StartCoroutine(ResetCharacterStateRoutine(waitTime));
        }
    }

    private void PlaySeamless(SeamlessVideoChannel channel, VideoClip clip, bool loop)
    {
        if (channel == null || clip == null) return;
        if (channel.transitionRoutine != null) StopCoroutine(channel.transitionRoutine);
        channel.transitionRoutine = StartCoroutine(SeamlessSwitchRoutine(channel, clip, loop));
    }

    private IEnumerator SeamlessSwitchRoutine(SeamlessVideoChannel channel, VideoClip nextClip, bool loop)
    {
        VideoPlayer activePlayer = channel.isUsingA ? channel.playerA : channel.playerB;
        VideoPlayer backPlayer = channel.isUsingA ? channel.playerB : channel.playerA;

        backPlayer.gameObject.SetActive(true);
        backPlayer.source = VideoSource.VideoClip;
        backPlayer.clip = nextClip;
        backPlayer.isLooping = loop;
        backPlayer.playbackSpeed = videoPlaybackSpeed;
        backPlayer.Prepare();
        while (!backPlayer.isPrepared) yield return null;

        if (channel.displayImage != null)
        {
            channel.displayImage.texture = backPlayer.texture;
            channel.displayImage.color = Color.white;
        }
        backPlayer.Play();
        yield return null;
        activePlayer.Stop();
        activePlayer.gameObject.SetActive(false);
        channel.isUsingA = !channel.isUsingA;
        channel.transitionRoutine = null;
    }

    private void PauseAllVideos()
    {
        if (frontChannel != null) PauseChannel(frontChannel);
        if (leftChannel != null) PauseChannel(leftChannel);
        if (rightChannel != null) PauseChannel(rightChannel);
    }
    private void PauseChannel(SeamlessVideoChannel channel)
    {
        if (channel.playerA.isPlaying) channel.playerA.Pause();
        if (channel.playerB.isPlaying) channel.playerB.Pause();
    }
    private void ResumeAllVideos()
    {
        if (frontChannel != null) ResumeChannel(frontChannel);
        if (leftChannel != null) ResumeChannel(leftChannel);
        if (rightChannel != null) ResumeChannel(rightChannel);
    }
    private void ResumeChannel(SeamlessVideoChannel channel)
    {
        if (channel.isUsingA) channel.playerA.Play();
        else channel.playerB.Play();
    }
    private IEnumerator ResetCharacterStateRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentHealth > 0 && GameManager.Instance.currentState == GameManager.GameState.GameStage)
            SetCharacterState(0);
    }

    private void PlayUISound(SFXType type)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(type);
    }

    public void InitializePanels()
    {
        SetPanelsActive(mainPanels, false);
        SetPanelsActive(infoPanels, false);
        SetPanelsActive(instructionPanels, false);
        SetPanelsActive(manualPanels, false);
        SetPanelsActive(pausePanels, false);
        SetPanelsActive(failPanels, false);
        SetPanelsActive(takenDamagePanels, true);
        if (DataManager.Instance != null)
        {
            UpdateProgress(DataManager.Instance.GetProgress());
            UpdateScore(DataManager.Instance.GetScore());
            UpdateBullet(DataManager.Instance.GetBullet());
            UpdateHP(DataManager.Instance.GetShipHealth());
            UpdateShield(DataManager.Instance.GetShipShield());
            UpdateBuff(DataManager.Instance.GetBuffer());
            UpdateDeBuff(DataManager.Instance.GetDeBuffer());
        }
    }
    public void InitializeSliders()
    {
        foreach (var sliders in HPSliders) if (sliders) { sliders.minValue = 0; sliders.maxValue = 100; sliders.wholeNumbers = true; }
        foreach (var sliders in ShieldSliders) if (sliders) { sliders.minValue = 0; sliders.maxValue = 100; sliders.wholeNumbers = true; }
        BulletSlider.minValue = 0; BulletSlider.maxValue = 100; BulletSlider.wholeNumbers = true;
        BuffSlider.minValue = 0; BuffSlider.maxValue = 100; BuffSlider.wholeNumbers = true;
        DeBuffSlider.minValue = 0; DeBuffSlider.maxValue = 100; DeBuffSlider.wholeNumbers = true;
    }
    private void SetPanelsActive(List<GameObject> panels, bool isActive)
    {
        foreach (var panel in panels) if (panel != null) panel.SetActive(isActive);
    }
    public void OnClickPauseButton()
    {
        PlayUISound(SFXType.UI_Touch);
        if (GameManager.Instance != null && !GetDisplayPanel())
        {
            OpenPausePanel();
            GameManager.Instance.TogglePause();
        }
    }
    public void OnClickFailButton()
    {
        PlayUISound(SFXType.UI_Touch);
        if (GameManager.Instance != null)
        {
            OpenFailPanel();
            GameManager.Instance.ToggleFail();
        }
    }
    public void OnClickContinueButton()
    {
        PlayUISound(SFXType.UI_Touch);
        if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        ClosePausePanel();
    }
    public void OnClickBackButton()
    {
        PlayUISound(SFXType.UI_Touch);
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.MainMenu);
    }
    public void OnClickRetryButton()
    {
        PlayUISound(SFXType.UI_Touch);
        Time.timeScale = 1f;
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.GameStage);
    }
    public void OpenInstructionPanel() { FadePanels(instructionPanels, true); SetDisplayPanel(true); }
    public void CloseInstructionPanel() { FadePanels(instructionPanels, false); SetDisplayPanel(false); }
    public void OpenManualPanel() { FadePanels(manualPanels, true); SetDisplayPanel(true); }
    public void CloseManualPanel() { FadePanels(manualPanels, false); SetDisplayPanel(false); }
    public void OpenPausePanel() { foreach (var panel in pausePanels) panel.SetActive(true); SetDisplayPanel(true); }
    public void ClosePausePanel() { foreach (var panel in pausePanels) panel.SetActive(false); SetDisplayPanel(false); }
    public void OpenFailPanel() { foreach (var panel in failPanels) panel.SetActive(true); SetDisplayPanel(true); }
    public void CloseFailPanel() { foreach (var panel in failPanels) panel.SetActive(false); SetDisplayPanel(false); }
    public void OpenMainPanel() { FadePanels(mainPanels, true); }
    public void CloseMainPanel() { FadePanels(mainPanels, false); }
    public void OpenInfoPanel() { FadePanels(infoPanels, true); }
    public void CloseInfoPanel() { FadePanels(infoPanels, false); }
    public void OpenTakenDamagePanel() { FadePanels(takenDamagePanels, true); }
    public void CloseTakenDamagePanel() { FadePanels(takenDamagePanels, false); }
    public void OpenArrowPanel(int value) { FadePanels(arrowPanels, true, true, value); }
    public void CloseArrowPanel() { FadePanels(arrowPanels, false); }
    public void UpdateScore(int value) { foreach (var text in scoreTexts) if (text) text.text = value.ToString(); }
    public void UpdateHP(int value) { foreach (var text in HPTexts) if (text) text.text = value.ToString(); foreach (var slider in HPSliders) if (slider) slider.value = value; }
    public void UpdateShield(int value) { foreach (var text in ShieldTexts) if (text) text.text = value.ToString(); foreach (var slider in ShieldSliders) if (slider) slider.value = value; }
    public void UpdateBullet(int value) { BulletText.text = value.ToString(); BulletSlider.value = value; }
    public void UpdateBuff(int value) { BuffText.text = value.ToString(); BuffSlider.value = value; }
    public void UpdateDeBuff(int value) { DeBuffText.text = value.ToString(); DeBuffSlider.value = value; }
    public void UpdateProgress(float value)
    {
        float clampedValue = Mathf.Clamp(value, 0f, 100f);
        if (progressSlider) progressSlider.fillAmount = clampedValue / 100f;
        if (progressText) progressText.text = $"{((int)clampedValue)} %";
    }
    public void SetDisplayPanel(bool state) { isDisplayPanel = state; }
    public bool GetDisplayPanel() { return isDisplayPanel; }
    private void FadePanels(List<GameObject> panels, bool show, bool onlyOne = false, int onlyOneChoice = 0)
    {
        int count = 0;
        foreach (var panel in panels)
        {
            count++;
            if (panel == null) continue;
            if (onlyOne) { if (onlyOneChoice != count) continue; }
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            if (panelCoroutines.ContainsKey(panel) && panelCoroutines[panel] != null) StopCoroutine(panelCoroutines[panel]);
            panelCoroutines[panel] = StartCoroutine(FadePanelRoutine(panel, cg, show));
        }
    }
    private IEnumerator FadePanelRoutine(GameObject panel, CanvasGroup cg, bool show)
    {
        float targetAlpha = show ? 1.0f : 0.0f;
        float startAlpha = cg.alpha;
        float elapsed = 0f;
        if (show) { panel.SetActive(true); cg.alpha = 0f; startAlpha = 0f; }
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / panelFadeDuration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        if (!show) panel.SetActive(false);
    }
    public void Log(string message) { if (isDebugMode) Debug.Log(message); }
    public void ShowOuttroUI()
    {
        PlayUISound(SFXType.Popup_Success);
        if (outtroUIManager != null)
        {
            outtroUIManager.ShowResult();
            foreach (var Panel in blackoutPanels) { if (Panel) Panel.SetActive(true); }
        }
        else Debug.LogError("OuttroUIManager가 씬에 없습니다!");
    }
    #endregion
}