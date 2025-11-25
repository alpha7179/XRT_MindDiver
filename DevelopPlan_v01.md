# 🚀 마인드 다이버 - CAVE 프로토타입 v0.3 통합 기술 계획서

본 문서는 '마인드 다이버' 프로젝트의 CAVE 연동 프로토타입(v0.3) 개발을 위한 공식 기술 계획서입니다.
프로젝트의 목표, 흐름, 하드웨어 구성 및 각 Unity 스크립트의 세부 사양을 통합하여 정의합니다.

---

## 1. 프로젝트 개요

### 1.1. 프로토타입 목표

* 물리 운전대와 3면 XR CAVE (터치스크린) 하드웨어를 유니티 엔진과 연동합니다. (VR HMD 미사용)
* 전체 게임 플로우(메인메뉴 → 인트로 → 캐릭터 선택 → 게임 → 아웃트로 → 메인메뉴)를 구현합니다.
* 조건부 터치 입력을 구현합니다:
    * 정면(Screen 0): '캐릭터 선택' 씬에서만 터치 활성화. '게임 스테이지' 중에는 터치 비활성화 (파일럿 뷰).
    * 좌/우(Screen 1, 2): '게임 스테이지' 중(Phase 1~3)에만 터치 활성화 (포수 역할).
* 각 플레이어의 입력(운전대, 터치)이 공유된 게임 환경에 실시간으로 영향을 미치는지 검증합니다.

### 1.2. 하드웨어 환경 및 조작법

* **플랫폼:** Unity PC (Windows Standalone, 3개 디스플레이 확장 모드)
* **시점:** 3면 CAVE 스크린 전체가 '함선 엠파시의 1인칭 조종석 창문'이 됩니다. (모든 플레이어가 동일한 뷰 공유)
* **파일럿 조작 (물리 운전대):**
    * 핸들 조향: 함선 좌/우 회전
    * 페달 (가속): 함선 전진
    * 페달 (브레이크): 함선 후진/감속
    * 휠 버튼 1: 방어막 활성화 (Phase 3)
* **포수 조작 (3면 터치스크린):**
    * 정면: '캐릭터 선택' 씬에서 N초간 터치 홀드하여 캐릭터 선택.
    * 좌/우: '게임 스테이지' 씬에서 타겟(악플 벌레, 파편, 약점)을 직접 터치.

### 1.3. 유니티 씬(Scene) 구성

게임의 흐름을 관리하기 위해 유니티 씬을 다음과 같이 분리합니다. `GameManager`가 이 씬들을 순차적으로 로드합니다.

* **MainMenuScene:** 메인 메뉴 UI. (정면 스크린 사용)
* **VideoScene:** 인트로/아웃트로 영상 재생. (3면 스크린 동시 사용)
* **CharacterSelectScene:** 캐릭터 선택 UI. (정면 스크린 터치 사용)
* **GameStageScene:** 실제 게임(Phase 1, 2, 3)이 진행되는 씬. (운전대, 좌/우 터치 사용)
* **ResultScene:** 게임 클리어/오버 시 결과 화면. (정면 스크린 사용)

---

## 2. 핵심 시스템 아키텍처 및 기술 사양

각 스크립트가 구현해야 할 구체적인 기능, 관리 데이터, 핵심 함수를 정의합니다.

### 2.1. GameManager.cs

* **파일:** `GameManager.cs`
* **씬:** `DontDestroyOnLoad` (싱글톤)
* **역할:** 게임의 최상위 상태(GameState)와 씬(Scene) 전환을 총괄합니다.

#### 핵심 관리 데이터

* `public static GameManager Instance { get; private set; }`
* `public enum GameState { MainMenu, IntroVideo, CharacterSelect, GameStage, OutroVideo, Result }`
* `public GameState currentState;` (현재 게임 상태)
* `[Header("Scene Names")]`
* `public string mainMenuScene = "MainMenuScene";`
* `public string videoScene = "VideoScene";`
* `public string characterSelectScene = "CharacterSelectScene";`
* `public string gameStageScene = "GameStageScene";`
* `public string resultScene = "ResultScene";`

#### 핵심 기능 및 함수

* `private void Awake()`: 싱글톤 인스턴스 설정 및 `DontDestroyOnLoad` 처리.
* `public void Start()`: 게임 시작 시 `ChangeState(GameState.MainMenu)`를 호출하여 메인 메뉴 씬으로 진입.
* `public void ChangeState(GameState newState)`:
    * `currentState = newState;`
    * `switch (newState)` 문을 사용하여 `newState`에 맞는 `SceneManager.LoadSceneAsync()` 호출.
    * (예: `case GameState.MainMenu: SceneManager.LoadSceneAsync(mainMenuScene);`)
* `public void QuitGame()`: `Application.Quit()`을 호출하여 애플리케이션 종료 (메인 메뉴 등에서 사용).
* `public void TakeDamage(int amount)`:
    * `DataManager.Instance.TakeDamage(amount);` // 데미지 처리를 DataManager로 위임
* `public void AddScore(int amount)`:
    * `DataManager.Instance.AddScore(amount);` // 점수 처리를 DataManager로 위임

### 2.2. GunnerInputManager.cs

* **파일:** `GunnerInputManager.cs`
* **씬:** `DontDestroyOnLoad` (싱글톤)
* **역할:** CAVE의 3면 터치스크린 입력을 독점적으로 수신하고, 현재 `GameState`와 `Screen ID`에 따라 입력을 올바른 매니저로 분배합니다.

#### 핵심 관리 데이터

* `public static GunnerInputManager Instance { get; private set; }`
* `[Header("CAVE Cameras")]`
* `public Camera cam_Front;`
* `public Camera cam_Left;`
* `public Camera cam_Right;`

#### 핵심 기능 및 함수

* `private void Awake()`: 싱글톤 인스턴스 설정.
* `public void OnCaveTouchReceived(int screenID, Vector2 touchPosition, TouchPhase touchPhase)`: (가장 중요) CAVE 하드웨어 SDK가 터치를 감지할 때마다 이 함수를 호출해야 합니다.
    * `GameState state = GameManager.Instance.currentState;`
    * `if (state == GameState.CharacterSelect && screenID == 0): FindObjectOfType<CharacterSelectManager>()?.ProcessTouch(touchPosition, touchPhase);` // 캐릭터 선택 씬 (정면)
    * `else if (state == GameState.GameStage && (screenID == 1 || screenID == 2)) : ProcessGameRaycast(screenID == 1 ? cam_Left : cam_Right, touchPosition);` // 게임 중 (좌/우)
    * `else if ((state == GameState.MainMenu || state == GameState.Result) && screenID == 0): ProcessUIRaycast(cam_Front, touchPosition);` // UI 씬 (정면)
* `private void ProcessGameRaycast(Camera cam, Vector2 pos)`:
    * `Ray ray = cam.ScreenPointToRay(pos);`
    * `if (Physics.Raycast(ray, out RaycastHit hit, 100f))`
    * `Target target = hit.collider.GetComponent<Target>();`
    * `if (target != null) { target.OnHit(); }`
* `private void ProcessUIRaycast(Camera cam, Vector2 pos)`:
    * // Unity의 EventSystem을 사용하여 UI 버튼 클릭을 처리하는 로직. PhysicsRaycaster 또는 GraphicRaycaster 활용.

### 2.3. PlayerShipController.cs

* **파일:** `PlayerShipController.cs`
* **씬:** `GameStageScene` (플레이어 함선 프리팹)
* **역할:** 물리 운전대(핸들, 페달, 버튼)의 입력을 받아 함선의 이동, 회전, 방어막을 제어합니다.

#### 핵심 관리 데이터

* `public float moveSpeed = 30f;`
* `public float turnSpeed = 45f;`
* `public GameObject shieldEffect;`
* `public string pedalAxisName = "Vertical";` // (Unity Input Manager 설정 필요)
* `public string wheelAxisName = "Horizontal";` // (Unity Input Manager 설정 필요)
* `public string shieldButtonName = "ShieldButton";` // (Unity Input Manager 설정 필요)

#### 핵심 기능 및 함수

* `private void Start()`: `shieldEffect.SetActive(false);`
* `private void Update()`:
    * `float pedalInput = Input.GetAxis(pedalAxisName);`
    * `float wheelInput = Input.GetAxis(wheelAxisName);`
    * // 함선 이동 로직 (Translate 또는 Rigidbody.MovePosition)
    * `transform.Translate(Vector3.forward * pedalInput * moveSpeed * Time.deltaTime);`
    * `transform.Rotate(Vector3.up * wheelInput * turnSpeed * Time.deltaTime);`
    * `if (Input.GetButtonDown(shieldButtonName)) { ActivateShield(); }`
* `private void ActivateShield()`:
    * // (예: 3초간 방어막 활성화)
    * `StartCoroutine(ShieldRoutine());`
* `private IEnumerator ShieldRoutine()`:
    * `shieldEffect.SetActive(true);`
    * `yield return new WaitForSeconds(3f);`
    * `shieldEffect.SetActive(false);`
* `private void OnTriggerEnter(Collider other)`:
    * `if (other.CompareTag("LargeSpike"))` // 'LargeSpike' 태그가 있는 장애물
    * `GameManager.Instance.TakeDamage(20);` // DataManager가 처리

### 2.4. StageManager.cs

* **파일:** `StageManager.cs`
* **씬:** `GameStageScene`
* **역할:** `GameStageScene` 내부의 페이즈(Phase 1, 2, 3) 진행과 전환 조건을 관리하는 '서브 매니저'.

#### 핵심 관리 데이터

* `public enum TransitionCondition { Time, Kills, TimeOrKills }`
* `public enum GamePhase { Phase1, Phase2, Phase3_Boss, None }`
* `private GamePhase currentGamePhase = GamePhase.None;`
* `[Header("Phase 1 Settings")]`
* `public TransitionCondition phase1_ConditionType = TransitionCondition.Time;`
* `public float phase1_TimeLimit = 80f;`
* `public int phase1_KillTarget = 50;`
* `[Header("Phase 2 Settings")]`
* `public TransitionCondition phase2_ConditionType = TransitionCondition.Time;`
* `public float phase2_TimeLimit = 110f;`
* `public int phase2_KillTarget = 100;`
* `[Header("References")]`
* `public EnemyManager enemyManager;`
* `public AudioManager audioManager;`
* `public BossAI bossAI;` // (보스 AI 참조)
* `private float phaseTimer;`
* `private int currentPhaseKills;`

#### 핵심 기능 및 함수

* `public void Start()`:
    * `DataManager.Instance.StartGameTimerAndResetStats();`
    * `StartPhase(GamePhase.Phase1);`
* `public void StartPhase(GamePhase phase)`:
    * `currentGamePhase = phase;`
    * `phaseTimer = 0f;`
    * `currentPhaseKills = 0;`
    * `switch (phase):`
        * `case GamePhase.Phase1:`
            * `enemyManager.StartSpawning(TargetType.SpamMite);`
            * `audioManager.ChangeBGM(audioManager.bgm_Phase1);` // (AudioManager에서 BGM 참조)
            * `break;`
        * `case GamePhase.Phase2:`
            * `enemyManager.StopSpawning();`
            * `enemyManager.StartSpawning(TargetType.SmallSpike);`
            * `enemyManager.StartSpawning(TargetType.LargeSpike);`
            * `audioManager.ChangeBGM(audioManager.bgm_Phase2);`
            * `break;`
        * `case GamePhase.Phase3_Boss:`
            * `enemyManager.StopSpawning();`
            * `enemyManager.SpawnBoss();`
            * `bossAI = FindObjectOfType<BossAI>();` // (스폰된 보스 참조)
            * `audioManager.ChangeBGM(audioManager.bgm_Boss);`
            * `break;`
* `public void IncrementPhaseKillCount()`: `currentPhaseKills++;`
* `private void Update()`:
    * `if (currentGamePhase == GamePhase.Phase1): CheckPhaseTransition(phase1_ConditionType, phase1_TimeLimit, phase1_KillTarget, GamePhase.Phase2);`
    * `else if (currentGamePhase == GamePhase.Phase2): CheckPhaseTransition(phase2_ConditionType, phase2_TimeLimit, phase2_KillTarget, GamePhase.Phase3_Boss);`
    * `else if (currentGamePhase == GamePhase.Phase3_Boss):`
        * `if (bossAI != null && bossAI.IsDead())` // IsDead()는 BossAI에 구현 필요
        * `DataManager.Instance.StopGameTimer();`
        * `GameManager.Instance.ChangeState(GameState.OutroVideo);`
* `private void CheckPhaseTransition(TransitionCondition type, float timeLimit, int killTarget, GamePhase nextPhase)`:
    * `bool timeMet = (phaseTimer += Time.deltaTime) >= timeLimit;`
    * `bool killsMet = currentPhaseKills >= killTarget;`
    * `if ((type == TransitionCondition.Time && timeMet) || (type == TransitionCondition.Kills && killsMet) || (type == TransitionCondition.TimeOrKills && (timeMet || killsMet)))`
    * `StartPhase(nextPhase);`

### 2.5. VideoPlayerManager.cs

* **파일:** `VideoPlayerManager.cs`
* **씬:** `VideoScene`
* **역할:** 인트로/아웃트로 비디오를 3면 스크린에 동기화하여 재생합니다.

#### 핵심 관리 데이터

* `public VideoPlayer videoPlayer_Front, videoPlayer_Left, videoPlayer_Right;`
* `public VideoClip introClip, outroClip;`

#### 핵심 기능 및 함수

* `private void Start()`:
    * `videoPlayer_Front.loopPointReached += OnVideoEnd;` // 메인 비디오 플레이어에 이벤트 구독
    * `VideoClip clipToPlay = (GameManager.Instance.currentState == GameState.IntroVideo) ? introClip : outroClip;`
    * `PlayVideo(clipToPlay);`
* `private void PlayVideo(VideoClip clip)`:
    * `videoPlayer_Front.clip = clip; videoPlayer_Left.clip = clip; videoPlayer_Right.clip = clip;`
    * `videoPlayer_Front.Play(); videoPlayer_Left.Play(); videoPlayer_Right.Play();`
* `private void OnVideoEnd(VideoPlayer vp)`:
    * `if (GameManager.Instance.currentState == GameState.IntroVideo):`
        * `GameManager.Instance.ChangeState(GameState.CharacterSelect);`
    * `else if (GameManager.Instance.currentState == GameState.OutroVideo):`
        * `GameManager.Instance.ChangeState(GameState.Result);`

### 2.6. CharacterSelectManager.cs

* **파일:** `CharacterSelectManager.cs`
* **씬:** `CharacterSelectScene`
* **역할:** 정면 스크린의 터치 입력을 받아 'N초간 홀드'하여 캐릭터를 선택합니다.

#### 핵심 관리 데이터

* `public float touchHoldTime = 0f;`
* `public float requiredHoldTime = 3f;`
* `public int selectedCharacterID = 0;`
* `public Slider holdSlider;` // (터치 홀드 진행률을 표시할 UI 슬라이더)

#### 핵심 기능 및 함수

* `public void ProcessTouch(Vector2 pos, TouchPhase phase)`: (GunnerInputManager로부터 호출됨)
    * // Raycast를 통해 터치된 대상이 '캐릭터 선택 버튼'인지 확인
    * `if (phase == TouchPhase.Began): touchHoldTime = 0f;`
    * `if (phase == TouchPhase.Stationary || phase == TouchPhase.Moved):`
        * `touchHoldTime += Time.deltaTime;`
        * `holdSlider.value = touchHoldTime / requiredHoldTime;`
        * `if (touchHoldTime >= requiredHoldTime): ConfirmSelection();`
    * `if (phase == TouchPhase.Ended): touchHoldTime = 0f; holdSlider.value = 0f;`
* `public void SelectCharacter(int id)`: `selectedCharacterID = id;` (UI 버튼에서 호출)
* `private void ConfirmSelection()`:
    * `DataManager.Instance.SetPlayerCharacter(selectedCharacterID);`
    * `GameManager.Instance.ChangeState(GameState.GameStage);`

### 2.7. Target.cs

* **파일:** `Target.cs`
* **씬:** `GameStageScene` (적, 자원 프리팹)
* **역할:** 포수가 터치(파괴/수집)할 수 있는 모든 오브젝트의 기반 스크립트.

#### 핵심 관리 데이터

* `public enum TargetType { SpamMite, BufferResource, DebufferResource, SmallSpike, BossWeakpoint }`
* `public TargetType type;`
* `private GameManager gameManager;`
* `private DataManager dataManager;`
* `private StageManager stageManager;`
* `private AudioManager audioManager;`

#### 핵심 기능 및 함수

* `private void Start()`: `gameManager = GameManager.Instance; dataManager = DataManager.Instance; stageManager = FindObjectOfType<StageManager>(); audioManager = AudioManager.Instance;` (모든 매니저 참조 캐싱)
* `public void OnHit()`: (GunnerInputManager로부터 호출됨)
    * `switch (type):`
        * `case SpamMite:`
            * `gameManager.AddScore(10);`
            * `dataManager.IncrementKillCount();`
            * `stageManager.IncrementPhaseKillCount();`
            * `audioManager.PlaySound("Splat");`
            * `Destroy(gameObject);`
            * `break;`
        * `case BufferResource:`
            * `dataManager.AddBuffer(1);`
            * `audioManager.PlaySound("Collect");`
            * `Destroy(gameObject);`
            * `break;`
        * `case DebufferResource:`
            * `dataManager.AddDebuffer(1);`
            * `audioManager.PlaySound("Collect");`
            * `Destroy(gameObject);`
            * `break;`
        * `case SmallSpike:`
            * `gameManager.AddScore(5);`
            * `dataManager.IncrementKillCount();`
            * `stageManager.IncrementPhaseKillCount();`
            * `audioManager.PlaySound("Splat");`
            * `Destroy(gameObject);`
            * `break;`
        * `case BossWeakpoint:`
            * `stageManager.bossAI?.TakeDamage(10);` // BossAI에 TakeDamage(int) 구현 필요
            * `audioManager.PlaySound("Hit_Boss");`
            * `break;`

### 2.8. EnemyManager.cs

* **파일:** `EnemyManager.cs`
* **씬:** `GameStageScene`
* **역할:** `StageManager`의 지시에 따라 적/자원/장애물을 스폰합니다.

#### 핵심 관리 데이터

* `public GameObject spamMitePrefab, largeSpikePrefab, smallSpikePrefab, bufferPrefab, debufferPrefab, bossPrefab;`
* `public Transform[] spawnPoints_Left, spawnPoints_Right;`
* `private bool isSpawning = false;`

#### 핵심 기능 및 함수

* `public void StartSpawning(TargetType type)`:
    * `isSpawning = true;`
    * `StartCoroutine(SpawnRoutine(type));`
* `public void StopSpawning()`: `isSpawning = false; StopAllCoroutines();`
* `private IEnumerator SpawnRoutine(TargetType type)`:
    * `while (isSpawning)`
    * `GameObject prefab = GetPrefab(type);`
    * `Transform[] points = (Random.Range(0, 2) == 0) ? spawnPoints_Left : spawnPoints_Right;`
    * `Transform spawnPoint = points[Random.Range(0, points.Length)];`
    * `Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);`
    * `yield return new WaitForSeconds(GetSpawnDelay(type));`
* `public void SpawnBoss()`: `Instantiate(bossPrefab, ...);`
* `private GameObject GetPrefab(TargetType type)`: `switch` 문으로 프리팹 반환.
* `private float GetSpawnDelay(TargetType type)`: `switch` 문으로 스폰 딜레이 반환.

### 2.9. DataManager.cs

* **파일:** `DataManager.cs`
* **씬:** `DontDestroyOnLoad` (싱글톤)
* **역할:** 게임 재화(버프/디버프)와 게임 플레이 성과(통계)를 관리하고 씬 간에 유지합니다.

#### 핵심 관리 데이터

* `public static DataManager Instance { get; private set; }`
* `[Header("Game Stats (for Result)")]`
* `public float totalPlayTime;`
* `public int totalEnemiesKilled;`
* `public int totalBuffersAcquired;`
* `public int totalDebuffersAcquired;`
* `public int totalDamageTaken;`
* `public int selectedCharacterID;`
* `private bool isTimerRunning = false;`
* `[Header("In-Game Currency")]`
* `public int bufferCharge;`
* `public int debufferCharge;`
* `public int maxCharge = 10;`
* `public int shipShield = 100;`
* `public int maxShipShield = 100;`
* `public int teamScore = 0;`

#### 핵심 기능 및 함수

* `private void Awake()`: 싱글톤 인스턴스 설정.
* `public void StartGameTimerAndResetStats()`:
    * `totalPlayTime = 0f; totalEnemiesKilled = 0; totalBuffersAcquired = 0; ...` (모든 통계 0으로 초기화)
    * `bufferCharge = 0; debufferCharge = 0; teamScore = 0; shipShield = maxShipShield;`
    * `isTimerRunning = true;`
* `public void StopGameTimer()`: `isTimerRunning = false;`
* `private void Update()`: `if (isTimerRunning) { totalPlayTime += Time.deltaTime; }`
* `public void IncrementKillCount()`: `totalEnemiesKilled++;`
* `public void AddBuffer(int amount)`:
    * `bufferCharge = Mathf.Min(bufferCharge + amount, maxCharge);`
    * `totalBuffersAcquired++;`
    * `FindObjectOfType<UIManager_GameStage>()?.UpdateBufferUI(bufferCharge, maxCharge);`
* `public void AddDebuffer(int amount)`: (AddBuffer와 유사)
* `public void TakeDamage(int amount)`:
    * `shipShield -= amount;`
    * `totalDamageTaken += amount;`
    * `FindObjectOfType<UIManager_GameStage>()?.UpdateShieldUI(shipShield, maxShipShield);`
    * `if (shipShield <= 0) { GameManager.Instance.ChangeState(GameState.Result); }` // (게임 오버)
* `public void AddScore(int amount)`: `teamScore += amount;`
* `public void SetPlayerCharacter(int id)`: `selectedCharacterID = id;`

### 2.10. UIManager_...cs (씬별 UI 매니저)

* **파일:** `UIManager_MainMenu.cs`, `UIManager_GameStage.cs`, `UIManager_Result.cs` 등
* **씬:** 각자의 씬 (`MainMenuScene`, `GameStageScene`, `ResultScene`)
* **역할:** 각 씬에 필요한 UI 요소를 제어합니다.

#### 핵심 기능 (UIManager\_GameStage.cs)

* `public Slider shieldSlider;, public Slider bufferSlider;, public Slider debufferSlider;, public TextMeshProUGUI scoreText;`
* `public void UpdateShieldUI(int current, int max)`: `shieldSlider.value = (float)current / max;`
* `public void UpdateBufferUI(int current, int max)`: `bufferSlider.value = (float)current / max;`
* `public void UpdateScoreUI(int score)`: `scoreText.text = "SCORE: " + score;`

#### 핵심 기능 (UIManager\_Result.cs)

* `public TextMeshProUGUI playTimeText, killsText, buffsText, damageText;`
* `public Button backToMenuButton;`
* `private void Start()`:
    * `DataManager data = DataManager.Instance;`
    * `playTimeText.text = "플레이 시간: " + data.totalPlayTime.ToString("F0") + "초";`
    * `killsText.text = "처치한 적: " + data.totalEnemiesKilled.ToString();`
    * `buffsText.text = "획득한 버프: " + data.totalBuffersAcquired.ToString();`
    * `damageText.text = "받은 데미지: " + data.totalDamageTaken.ToString();`
    * `backToMenuButton.onClick.AddListener(() => GameManager.Instance.ChangeState(GameState.MainMenu));`

### 2.11. CAVE_DisplayManager.cs

* **파일:** `CAVE_DisplayManager.cs`
* **씬:** `DontDestroyOnLoad` (싱글톤)
* **역할:** 3개의 유니티 카메라를 3개의 물리적 디스플레이(모니터)에 매핑합니다.

#### 핵심 관리 데이터

* `public Camera cam_Front, cam_Left, cam_Right;`

#### 핵심 기능 및 함수

* `private void Start()`:
    * `if (Display.displays.Length > 0) cam_Front.targetDisplay = 0;` // 1번 디스플레이 (정면)
    * `if (Display.displays.Length > 1) cam_Left.targetDisplay = 1;` // 2번 디스플레이 (좌측)
    * `if (Display.displays.Length > 2) cam_Right.targetDisplay = 2;` // 3번 디스플레이 (우측)
    * // (필요 시) 각 카메라의 FOV, 각도를 CAVE 환경에 맞게 조정.

### 2.12. AudioManager.cs

* **파일:** `AudioManager.cs`
* **씬:** `DontDestroyOnLoad` (싱글톤)
* **역할:** 모든 배경음악(BGM)과 효과음(SFX)을 재생합니다.

#### 핵심 관리 데이터

* `public static AudioManager Instance { get; private set; }`
* `public AudioSource bgmSource, sfxSource;`
* `[Header("Audio Clips")]`
* `public AudioClip bgm_Menu, bgm_Phase1, bgm_Phase2, bgm_Boss;`
* `public AudioClip sfx_Splat, sfx_Collect, sfx_ShieldHit, sfx_Click, sfx_Hit_Boss;`

#### 핵심 기능 및 함수

* `private void Awake()`: 싱글톤 인스턴스 설정.
* `public void PlaySound(string soundName)`:
    * `switch (soundName):`
        * `case "Splat": sfxSource.PlayOneShot(sfx_Splat); break;`
        * `case "Collect": sfxSource.PlayOneShot(sfx_Collect); break;`
        * `case "Hit_Boss": sfxSource.PlayOneShot(sfx_Hit_Boss); break;`
        * // ... (다른 효과음 케이스 추가) ...
* `public void ChangeBGM(AudioClip clip)`:
    * `bgmSource.Stop();`
    * `bgmSource.clip = clip;`
    * `bgGAmSource.Play();`