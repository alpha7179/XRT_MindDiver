using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public enum VideoType { Intro, Outro }
    [Header("Video Settings")]
    public VideoType currentVideoType; // 현재 재생해야 할 비디오 타입

    [Header("In-Game Data")]
    public int teamScore;
    public int shipShield;
    public int maxShipShield = 100;
    public int bufferCharge;
    public int debufferCharge;
    public int maxCharge = 10;

    [Header("Player Info")]
    public int selectedCharacterID;

    [Header("Statistics (Result)")]
    public float totalPlayTime;
    public int totalEnemiesKilled;
    public int totalDamageTaken;

    private bool _isTimerRunning = false;

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

    private void Update()
    {
        if (_isTimerRunning)
        {
            totalPlayTime += Time.deltaTime;
        }
    }

    /// <summary>
    /// 재생할 비디오 타입을 설정합니다. (GameManager에서 호출)
    /// </summary>
    public void SetVideoType(VideoType type)
    {
        currentVideoType = type;
        GameManager.Instance.Log($"[DataManager] Set Video Type: {type}");
    }

    public void InitializeGameData()
    {
        teamScore = 0;
        shipShield = maxShipShield;
        bufferCharge = 0;
        debufferCharge = 0;
        totalPlayTime = 0f;
        totalEnemiesKilled = 0;
        totalDamageTaken = 0;

        _isTimerRunning = true;
        GameManager.Instance.Log("[DataManager] Game Data Initialized");
    }

    public void StopTimer() => _isTimerRunning = false;

    public void AddScore(int amount) => teamScore += amount;

    public void TakeDamage(int amount)
    {
        shipShield -= amount;
        totalDamageTaken += amount;
        if (shipShield <= 0) shipShield = 0;
    }

    public void AddBuffer(int amount) => bufferCharge = Mathf.Min(bufferCharge + amount, maxCharge);
    public void AddDebuffer(int amount) => debufferCharge = Mathf.Min(debufferCharge + amount, maxCharge);
    public void IncrementKillCount() => totalEnemiesKilled++;
    public void SetPlayerCharacter(int id) => selectedCharacterID = id;
}