using Mover; // EnemyMover가 있는 네임스페이스라고 가정
using System;
using UnityEngine;

/// <summary>
/// 게임 내 데이터(점수, 스탯, 진행도 등) 및 통계를 관리하는 클래스
/// - 수정됨: AudioManager 연결 및 SFX 쿨타임 로직 추가
/// </summary>
public class DataManager : MonoBehaviour
{
    #region Singleton
    public static DataManager Instance { get; private set; }
    #endregion

    #region Enums
    public enum VideoType { Intro, Outro }
    public enum LangType { Kor }
    #endregion

    #region Inspector Fields
    [Header("Video Settings")]
    [SerializeField] public VideoType currentVideoType;

    [Header("Setting Data")]
    [SerializeField][Range(0, 100)] private int BGMVolume;
    [SerializeField][Range(0, 100)] private int SFXVolume;
    [SerializeField][Range(0, 100)] private int videoVolume;
    [SerializeField][Range(0, 100)] private int NARVolume;
    [SerializeField] private LangType currentLangType;

    [Header("In-Game Data")]
    [SerializeField][Range(0, 100)] private float progress;
    [SerializeField] private int teamScore;
    [SerializeField][Range(0, 100)] private int shipHealth;
    [SerializeField] public int maxShipHealth = 100;
    [SerializeField][Range(0, 100)] private int shipShield;
    [SerializeField] public int maxShipShield = 100;

    [Header("Skill Settings")]
    [SerializeField][Range(0, 100)] public int bufferUse = 50;
    [SerializeField][Range(0, 100)] private int bufferCharge;
    [SerializeField][Range(0, 10)] public float buffDuration = 10f;
    [SerializeField] public bool isBuffState;

    [SerializeField][Range(0, 100)] public int debufferUse = 50;
    [SerializeField][Range(0, 100)] private int debufferCharge;
    [SerializeField][Range(0, 10)] public float DebuffDuration = 10f;
    [SerializeField] public bool isDebuffState;

    [SerializeField] private int maxCharge = 100;

    [Header("Ammo")]
    [SerializeField][Range(0, 9999)] private int bullet;
    [SerializeField] public int maxBullet = 9999;

    [Header("Statistics")]
    [SerializeField] private float totalPlayTime;
    [SerializeField] private int totalEnemiesKilled;
    [SerializeField] private int totalDamageTaken;

    [Header("Debug Settings")]
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    private bool _isTimerRunning = false;
    private float _bufferTimer = 0f;
    private float _debufferTimer = 0f;
    private float _shieldRegenAccumulator = 0f;

    // [추가됨] 데미지 사운드 중복 재생 방지용 타이머
    private float _lastDamageTime = -10f;
    // [추가됨] 데미지 사운드 최소 간격 (초 단위, 0.1초)
    private const float DamageSoundCooldown = 0.1f;
    #endregion

    #region Events
    public event Action<int, int> OnHealthChanged;
    public event Action<int, int> OnShieldChanged;
    public event Action OnBufferAdded;
    public event Action OnDeBufferAdded;
    #endregion

    #region Unity Lifecycle
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
        InitializeSettingData();
    }

    private void Update()
    {
        if (_isTimerRunning)
        {
            totalPlayTime += Time.deltaTime;
        }

        // --- 버퍼(무적 + 쉴드 회복) 지속시간 체크 ---
        if (isBuffState)
        {
            _bufferTimer -= Time.deltaTime;

            if (shipShield < maxShipShield)
            {
                float totalRegenAmount = maxShipShield * 0.5f;
                float regenPerSecond = totalRegenAmount / buffDuration;
                _shieldRegenAccumulator += regenPerSecond * Time.deltaTime;

                if (_shieldRegenAccumulator >= 1f)
                {
                    int amountToAdd = (int)_shieldRegenAccumulator;
                    _shieldRegenAccumulator -= amountToAdd;
                    SetShipShield(Mathf.Min(shipShield + amountToAdd, maxShipShield));
                }
            }

            if (_bufferTimer <= 0)
            {
                isBuffState = false;
                _shieldRegenAccumulator = 0f;
                Log("[DataManager] Buffer Effect Ended (Invincibility OFF)");
            }
        }

        // --- 디버퍼 지속시간 체크 ---
        if (isDebuffState)
        {
            _debufferTimer -= Time.deltaTime;

            if (_debufferTimer <= 0)
            {
                isDebuffState = false;
                EnemyMover[] enemies = FindObjectsByType<EnemyMover>(FindObjectsSortMode.None);
                foreach (var enemy in enemies)
                {
                    if (enemy != null) enemy.DebuffEnd();
                }
                Log("[DataManager] Debuffer Effect Ended (All Enemies Restored)");
            }
        }
    }
    #endregion

    #region Public Methods - Control
    public void SetVideoType(VideoType type)
    {
        currentVideoType = type;
        Log($"[DataManager] Set Video Type: {type}");
    }

    public void InitializeSettingData()
    {
        SetBGMVolume(100);
        SetSFXVolume(100);
        SetVideoVolume(100);
        SetNARVolume(100);

        _isTimerRunning = true;
        Log("[DataManager] Setting Data Initialized");
    }

    public void InitializeGameData()
    {
        if (IngameUIManager.Instance != null)
            IngameUIManager.Instance.InitializeSliders();

        SetProgress(0);
        SetScore(0);
        SetShipHealth(maxShipHealth);
        SetShipShield(maxShipShield);

        SetBuffer(0);
        SetBuffState(false);
        _bufferTimer = 0f;
        _shieldRegenAccumulator = 0f;

        SetDeBuffer(0);
        SetDebuffState(false);
        _debufferTimer = 0f;

        SetBullet(maxBullet);
        SetIncrementKillCount(0);
        SetTotalDamageTaken(0);
        SetTotalPlayTime(0);

        _isTimerRunning = true;
        Log("[DataManager] Game Data Initialized");
    }

    public void StopTimer() => _isTimerRunning = false;
    #endregion

    #region Public Methods - Data Access & Logic

    // ... (Volume, Progress, Score Getter/Setters 생략 - 기존과 동일) ...
    public int GetBGMVolume() { return BGMVolume; }
    public void SetBGMVolume(int value) { BGMVolume = value; }
    public int GetSFXVolume() { return SFXVolume; }
    public void SetSFXVolume(int value) { SFXVolume = value; }
    public int GetVideoVolume() { return videoVolume; }
    public void SetVideoVolume(int value) { videoVolume = value; }
    public int GetNARVolume() { return NARVolume; }
    public void SetNARVolume(int value) { NARVolume = value; }
    public float GetProgress() { return progress; }
    public void SetProgress(float value) { progress = value; if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateProgress(progress); }
    public void AddScore(int amount) { teamScore += amount; if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateScore(teamScore); }
    public int GetScore() { return teamScore; }
    public void SetScore(int value) { teamScore = value; if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateScore(teamScore); }

    // --- Shield / Health (Damage Logic) ---
    public void TakeDamage(int amount)
    {
        // 1. 버퍼 상태(무적) 확인
        if (isBuffState)
        {
            Log("[DataManager] Damage Ignored (Buff Active)");
            return;
        }

        bool isShieldHit = false;

        // 2. 데미지 계산 로직
        if (shipShield > 0)
        {
            isShieldHit = true; // 쉴드가 남아있을 때 맞음
            if (shipShield >= amount)
            {
                shipShield -= amount;
            }
            else if ((shipShield + shipHealth) >= amount)
            {
                // 쉴드가 깨지면서 체력도 깎임 (관통) -> 이 경우 소리는 '깨짐' or '피격' 중 선택. 여기선 쉴드 피격음 우선
                shipHealth -= (amount - shipShield);
                shipShield = 0;
            }
            else
            {
                shipHealth = 0;
                shipShield = 0;
                if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateHP(shipHealth);
            }

            if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateShield(shipShield);
            OnShieldChanged?.Invoke(shipShield, maxShipShield);
        }
        else
        {
            isShieldHit = false; // 쉴드 없음, 맨몸 피격
            if (shipHealth >= amount) shipHealth -= amount;
            else shipHealth = 0;

            if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateHP(shipHealth);
            OnHealthChanged?.Invoke(shipHealth, maxShipHealth);
        }

        totalDamageTaken += amount;

        // 3. [추가됨] 데미지 사운드 처리 (쿨타임 적용)
        if (AudioManager.Instance != null && Time.time - _lastDamageTime >= DamageSoundCooldown)
        {
            _lastDamageTime = Time.time; // 마지막 재생 시간 갱신

            if (isShieldHit)
            {
                AudioManager.Instance.PlaySFX(SFXType.Damage_Shield);
            }
            else
            {
                AudioManager.Instance.PlaySFX(SFXType.Damage_Player);
            }
        }
    }

    public int GetShipHealth() { return shipHealth; }
    public void SetShipHealth(int value)
    {
        shipHealth = value;
        if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateHP(shipHealth);
        OnHealthChanged?.Invoke(shipHealth, maxShipHealth);
    }

    public int GetShipShield() { return shipShield; }
    public void SetShipShield(int value)
    {
        shipShield = value;
        if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateShield(shipShield);
        OnShieldChanged?.Invoke(shipShield, maxShipShield);
    }

    public int GetTotalDamageTaken() { return totalDamageTaken; }
    public void SetTotalDamageTaken(int value) { totalDamageTaken = value; }

    // --- Buffer (Skill) ---
    public void AddBuffer(int amount)
    {
        bufferCharge = Mathf.Min(bufferCharge + amount, maxCharge);
        if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateBuff(bufferCharge);

        // [추가됨] 아이템 획득(충전) 시 사운드 (amount가 양수일 때만)
        if (amount > 0 && AudioManager.Instance != null)
        {
            //AudioManager.Instance.PlaySFX(SFXType.Collect_Energy);
        }
    }
    public int GetBuffer() { return bufferCharge; }
    public void SetBuffer(int value) { bufferCharge = value; if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateBuff(bufferCharge); }

    public bool GetBuffState() { return isBuffState; }
    public void SetBuffState(bool value) { isBuffState = value; }

    public void ActivateBuff()
    {
        if (bufferCharge >= bufferUse)
        {
            isBuffState = true;
            _bufferTimer = buffDuration;
            _shieldRegenAccumulator = 0f;
            AddBuffer(-bufferUse); // 사용 시 차감 (사운드 재생 안됨, amount가 음수이므로)

            // [추가됨] 버프 스킬 사운드
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(SFXType.Skill_Buff);

            Log("[DataManager] Buffer Activated: Invincible + Shield Regen");
        }
        else
        {
            Log("[DataManager] Not enough Buffer Charge");
        }
    }

    // --- Debuffer (Skill) ---
    public void AddDeBuffer(int amount)
    {
        debufferCharge = Mathf.Min(debufferCharge + amount, maxCharge);
        if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateDeBuff(debufferCharge);

        // [추가됨] 아이템 획득 사운드
        if (amount > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Collect_Energy);
        }
    }
    public int GetDeBuffer() { return debufferCharge; }
    public void SetDeBuffer(int value) { debufferCharge = value; if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateDeBuff(debufferCharge); }

    public bool GetDebuffState() { return isDebuffState; }
    public void SetDebuffState(bool value) { isDebuffState = value; }

    public void ActivateDebuff()
    {
        if (debufferCharge >= debufferUse)
        {
            isDebuffState = true;
            _debufferTimer = DebuffDuration;
            AddDeBuffer(-debufferUse);

            EnemyMover[] enemies = FindObjectsByType<EnemyMover>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null) enemy.DebuffStart();
            }

            // [추가됨] 디버프 스킬 사운드
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(SFXType.Skill_Debuff);

            Log("[DataManager] Debuffer Activated (All Enemies Weakened)");
        }
        else
        {
            Log("[DataManager] Not enough Debuffer Charge");
        }
    }

    // --- Bullet ---
    public void AddBullet(int amount)
    {
        bullet += amount;
        if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateBullet(bullet);

        // [추가됨] 총알 아이템 획득 시에도 에너지 수집 사운드 재생 (원하시면 변경 가능)
        if (amount > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXType.Collect_Energy);
        }
    }
    public int GetBullet() { return bullet; }
    public void SetBullet(int value) { bullet = value; if (IngameUIManager.Instance) IngameUIManager.Instance.UpdateBullet(bullet); }

    // --- Statistics ---
    public void IncrementKillCount() => totalEnemiesKilled++;
    public int GetIncrementKillCount() { return totalEnemiesKilled; }
    public void SetIncrementKillCount(int value) { totalEnemiesKilled = value; }

    public int GetTotalPlayTime() { return (int)totalPlayTime; }
    public void SetTotalPlayTime(int value) { totalPlayTime = value; }
    #endregion

    #region Helper Methods
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}