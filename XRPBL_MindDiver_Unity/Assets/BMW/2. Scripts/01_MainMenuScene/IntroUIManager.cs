using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;
using static GamePhaseManager;

public class IntroUIManager : MonoBehaviour
{
    [Header("UI 패널 (Parents)")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject choicePanel;

    [Header("StartPanel의 하위 패널들")]
    [SerializeField] private GameObject placePanel;
    [SerializeField] private GameObject manualPanel;
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject endPanel;
    

    [Header("Setting 패널 구성요소")]
    [SerializeField] private TextMeshProUGUI BGMText;
    [SerializeField] private Slider BGMSlider;
    [SerializeField] private TextMeshProUGUI SFXText;
    [SerializeField] private Slider SFXSlider;

    [Header("디버그 로그")]
    [SerializeField] private bool isDebugMode = true;

    private GameObject currentTopPanel;
    private GameObject currentMainPanel;

    private int BGMValue;
    private int SFXValue;

    void Start()
    {

        // 오디오 슬라이더
        BGMSlider.minValue = 0;
        BGMSlider.maxValue = 100;
        BGMSlider.wholeNumbers = true;

        BGMValue = 100;
        BGMSlider.value = BGMValue;
        BGMSlider.onValueChanged.AddListener(OnBGMSliderValueChanged);

        OnBGMSliderValueChanged(BGMValue);

        SFXSlider.minValue = 0;
        SFXSlider.maxValue = 100;
        SFXSlider.wholeNumbers = true;

        SFXValue = 100;
        SFXSlider.value = SFXValue;
        SFXSlider.onValueChanged.AddListener(OnSFXSliderValueChanged);

        OnSFXSliderValueChanged(SFXValue);


        // 모든 하위 패널을 끈 상태로 시작
        if (placePanel) placePanel.SetActive(false);
        if (manualPanel) manualPanel.SetActive(false);
        if (settingPanel) settingPanel.SetActive(false);
        if (endPanel) endPanel.SetActive(false);

        // 최상위 패널(인트로)만 활성화
        if (choicePanel) choicePanel.SetActive(false);
        if (introPanel) introPanel.SetActive(true);

        currentTopPanel = introPanel;
        currentMainPanel = null;

        AudioManager.Instance.PlayBGM(GameManager.Instance.currentState);
    }

    // 최상위 패널(Intro <-> Choice)을 전환
    private void SwitchTopPanel(GameObject panelToActivate)
    {
        if (currentTopPanel == panelToActivate) return;

        if (currentTopPanel != null) currentTopPanel.SetActive(false);

        panelToActivate.SetActive(true);
        currentTopPanel = panelToActivate;
    }

    // StartPanel 내부의 메인 패널(Place, Manual 등)을 전환
    private void SwitchMainPanel(GameObject panelToActivate)
    {
        if (currentMainPanel == panelToActivate) return; 

        if (currentMainPanel != null) currentMainPanel.SetActive(false);

        panelToActivate.SetActive(true);
        currentMainPanel = panelToActivate;
    }

    // [IntroButton]에 연결: 인트로를 닫고 메인 메뉴(chicePanel)를 염
    public void OnClickIntroButton()
    {
        Log("IntroButton Clicked");
        SwitchTopPanel(choicePanel);

        // StartPanel이 열릴 때 기본으로 보여줄 하위 패널
        SwitchMainPanel(placePanel);
    }

    // [PlaceButton]에 연결: 장소 선택 패널을 염
    public void OnClickPlaceButton()
    {
        Log("PlaceButton Clicked");
        SwitchMainPanel(placePanel);
    }

    // [ManualButton]에 연결: 매뉴얼 패널을 염
    public void OnClickManualButton()
    {
        Log("ManualButton Clicked");
        SwitchMainPanel(manualPanel);
    }

    // [SettingButton]에 연결: 설정 패널을 염
    public void OnClickSettingButton()
    {
        Log("SettingButton Clicked");
        SwitchMainPanel(settingPanel);
    }

    // [EndButton]에 연결: 팁 패널을 염
    public void OnClickEndButton()
    {
        Log("EndButton Clicked");
        SwitchMainPanel(endPanel);
    }

    // [QuitButton]에 연결: 게임 종료
    public void OnClickQuitButton()
    {
        Log("QuitButton Clicked");
        GameManager.Instance.QuitGame();
    }

    // [PlayButton]에 연결: 체험을 시작
    public void OnClickPlayButton()
    {
        Log("체험을 시작합니다.");

        // GameManager를 통해 씬 전환
        if (GameManager.Instance != null) GameManager.Instance.ChangeState(GameManager.GameState.IntroVideo);
    }

    // BGM 슬라이더 값이 변경될 때 호출
    private void OnBGMSliderValueChanged(float value)
    {
        BGMValue = Mathf.RoundToInt(value);
        if (BGMText != null)
            BGMText.text = BGMValue.ToString();

    }

    // SFX 슬라이더 값이 변경될 때 호출
    private void OnSFXSliderValueChanged(float value)
    {
        SFXValue = Mathf.RoundToInt(value);
        if (SFXText != null)
            SFXText.text = SFXValue.ToString();

    }

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }

}