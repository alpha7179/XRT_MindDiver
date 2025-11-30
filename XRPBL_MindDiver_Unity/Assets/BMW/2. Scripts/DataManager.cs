using System;
using UnityEditor;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public enum VideoType { Intro, Outro }
    [Header("Video Settings")]
    public VideoType currentVideoType;

    [Header("In-Game Data")]
    [SerializeField] private float progress;
    [SerializeField] private int teamScore;
    [SerializeField] private int bullet;
    [SerializeField] public int maxBullet = 9999;
    [SerializeField] private int shipShield;
    [SerializeField] public int maxShipShield = 100;
    [SerializeField] private int bufferCharge;
    [SerializeField] private int debufferCharge;
    [SerializeField] private int maxCharge = 10;

    [Header("Statistics")]
    [SerializeField] private float totalPlayTime;
    [SerializeField] private int totalEnemiesKilled;
    [SerializeField] private int totalDamageTaken;

    private bool _isTimerRunning = false;

    public event Action<int, int> OnShieldChanged;
    public event Action OnBufferAdded;
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

    public void SetVideoType(VideoType type)
    {
        currentVideoType = type;
        GameManager.Instance.Log($"[DataManager] Set Video Type: {type}");
    }

    public void InitializeGameData()
    {
        SetProgress(0);
        SetScore(0);
        SetBullet(maxBullet);
        SetShipShield(maxShipShield);
        SetTotalDamageTaken(0);
        SetBuffer(0);
        SetDebuffer(0);
        SetIncrementKillCount(0);
        SetTotalDamageTaken(0);
        SetTotalPlayTime(0);

        _isTimerRunning = true;
        GameManager.Instance.Log("[DataManager] Game Data Initialized");
    }

    public void StopTimer() => _isTimerRunning = false;

    public void AddProgress(float amount) => progress += amount;
    public float GetProgress() { return progress; }
    public void SetProgress(int value) { progress = value; }

    public void AddScore(int amount) => teamScore += amount;
    public int GetScore() { return teamScore; }
    public void SetScore(int value) { teamScore = value; }

    public void AddBullet (int amount) => bullet += amount;
    public int GetBullet() { return bullet; }
    public void SetBullet(int value) { bullet = value; }

    public void TakeDamage(int amount)
    {
        shipShield -= amount;
        totalDamageTaken += amount;
        OnShieldChanged?.Invoke(shipShield, maxShipShield);
    }

    public int GetShipShield() { return shipShield; }

    public void SetShipShield(int value)
    {
        shipShield = value;
        OnShieldChanged?.Invoke(shipShield, maxShipShield);
    }

    public int GetTotalDamageTaken() { return totalDamageTaken; }
    public void SetTotalDamageTaken(int value) { totalDamageTaken = value; }

    public void AddBuffer(int amount)
    {
        int oldBuffer = bufferCharge;
        bufferCharge += bufferCharge;
        shipShield += amount;

        if (bufferCharge > oldBuffer)
        {
            OnBufferAdded?.Invoke();
        }
    }

    public int GetBuffer() { return bufferCharge; }
    public void SetBuffer(int value) { bufferCharge = value; }

    public void AddDebuffer(int amount) => debufferCharge = Mathf.Min(debufferCharge + amount, maxCharge);
    public int GetDebuffer() { return debufferCharge; }
    public void SetDebuffer(int value) { debufferCharge = value; }

    public void IncrementKillCount() => totalEnemiesKilled++;
    public int GetIncrementKillCount() { return totalEnemiesKilled; }
    public void SetIncrementKillCount(int value) { totalEnemiesKilled = value; }

    public int GetTotalPlayTime() { return (int)totalPlayTime; }
    public void SetTotalPlayTime(int value) { totalPlayTime = value; }
}