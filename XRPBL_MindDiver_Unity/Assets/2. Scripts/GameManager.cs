using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Debug Settings")]
    public bool isDebugMode = true;

    [Header("Game State")]
    public GameState currentState;

    public enum GameState
    {
        MainMenu,
        IntroVideo,
        CharacterSelect,
        GameStage,
        OutroVideo,
        Result
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Log($"[GameManager] State Changed to: {newState}");

        switch (newState)
        {
            case GameState.MainMenu:
                SceneManager.LoadScene("MainMenuScene");
                break;

            case GameState.IntroVideo:
                // [수정] 인트로 타입 설정 후 씬 로드
                DataManager.Instance.SetVideoType(DataManager.VideoType.Intro);
                SceneManager.LoadScene("VideoScene");
                break;

            case GameState.CharacterSelect:
                SceneManager.LoadScene("CharacterSelectScene");
                break;

            case GameState.GameStage:
                SceneManager.LoadScene("GameStageScene");
                break;

            case GameState.OutroVideo:
                // [수정] 아웃트로 타입 설정 후 씬 로드
                DataManager.Instance.SetVideoType(DataManager.VideoType.Outro);
                SceneManager.LoadScene("VideoScene");
                break;

            case GameState.Result:
                SceneManager.LoadScene("ResultScene");
                break;
        }
    }

    public void QuitGame() => Application.Quit();

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
}