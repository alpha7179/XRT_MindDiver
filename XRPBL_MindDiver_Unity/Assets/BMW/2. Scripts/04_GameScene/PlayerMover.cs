using Logitech;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// 플레이어 우주선의 평행 이동 및 속도 제어 클래스 (회전 없음) + 로지텍 휠 지원
/// </summary>
public class PlayerMover : MonoBehaviour
{
    public static PlayerMover Instance { get; private set; }

    #region Inspector Fields
    [Header("Control Settings")]
    [Tooltip("이 값이 True여야만 움직일 수 있습니다.")]
    [SerializeField] private bool canMove;

    [Header("Speed Settings")]
    [Tooltip("기본 전진 속도")]
    [SerializeField] private float defaultSpeed = 0f;
    [Tooltip("W키 누를 시 최대 속도")]
    [SerializeField] private float maxSpeed = 40f;
    [Tooltip("S키 누를 시 최소 속도 (감속)")]
    [SerializeField] private float minSpeed = 0.001f;
    [Tooltip("속도 변경 반응 속도")]
    [SerializeField] private float acceleration = 5f;

    [Header("Movement Settings")]
    [Tooltip("좌우(A/D) 이동 속도")]
    [SerializeField] private float strafeSpeed = 15f;
    [Tooltip("좌우 이동 범위 제한 (X축)")]
    [SerializeField] private float xLimit = 10f;

    [Header("Logitech Wheel Settings")]
    [Tooltip("휠 입력 데드존 (미세한 떨림 방지)")]
    [SerializeField] private float wheelDeadzone = 0.05f;
    [Tooltip("휠 입력을 사용할지 여부")]
    [SerializeField] private bool useLogitechWheel = true;

    [Header("References")]
    [SerializeField] private GameObject shieldEffect;
    #endregion

    #region Private Fields
    private Rigidbody _rb;
    private Vector2 _input;
    private float _currentForwardSpeed;

    // 로그 중복 출력을 막기 위한 상태 플래그들
    private bool _loggedForward = false;
    private bool _loggedBackward = false;
    private bool _loggedLeft = false;
    private bool _loggedRight = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _rb = GetComponent<Rigidbody>();
        _currentForwardSpeed = defaultSpeed;

        SetMoveAction(false);
    }

    private void Start()
    {
        // 로지텍 SDK 초기화
        if (useLogitechWheel)
        {
            Debug.Log("Initializing Logitech Steering Wheel SDK...");
            bool init = LogitechGSDK.LogiSteeringInitialize(false);
            if (init) Debug.Log("Logitech SDK Initialized.");
            else Debug.LogError("Logitech SDK Initialization Failed!");
        }
    }

    private void Update()
    {
        // 1. 움직임이 불가능한 상태면 입력 처리도 하지 않음
        if (!canMove)
        {
            _input = Vector2.zero;
            return;
        }

        // 2. 이동 입력 처리 (키보드 + 휠)
        float h = GetCombinedHorizontalInput(); // 함수로 분리함
        float v = Input.GetAxis("Vertical");

        _input = new Vector2(h, v);

        // 3. 로그 출력 및 화살표 UI 처리
        HandleInputLogging(h, v);

        // 4. 스킬(버프/디버프) 입력 처리
        HandleSkillInput();

        // 방어막 테스트 (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateShield();
        }
    }

    private void FixedUpdate()
    {
        if (!canMove) return;

        // 1. 전진 속도 계산
        float targetSpeed = defaultSpeed;

        if (_input.y > 0) targetSpeed = maxSpeed;
        else if (_input.y < 0) targetSpeed = minSpeed;

        _currentForwardSpeed = Mathf.Lerp(_currentForwardSpeed, targetSpeed, Time.fixedDeltaTime * acceleration);

        // 2. 이동 벡터 계산
        float moveZ = _currentForwardSpeed * Time.fixedDeltaTime;
        float moveX = _input.x * strafeSpeed * Time.fixedDeltaTime;

        // 3. 다음 위치 계산 및 제한
        Vector3 nextPosition = _rb.position + new Vector3(moveX, 0f, moveZ);
        nextPosition.x = Mathf.Clamp(nextPosition.x, -xLimit, xLimit);

        // 4. 리지드바디 갱신
        _rb.MovePosition(nextPosition);
        _rb.rotation = Quaternion.identity;
    }

    private void OnApplicationQuit()
    {
        // 종료 시 SDK 해제
        if (useLogitechWheel)
        {
            LogitechGSDK.LogiSteeringShutdown();
        }
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// 키보드 입력과 로지텍 휠 입력을 합쳐서 반환합니다.
    /// </summary>
    private float GetCombinedHorizontalInput()
    {
        // 1. 키보드 입력
        float keyboardInput = Input.GetAxis("Horizontal");

        // 2. 휠 입력 (SDK 사용 시)
        float wheelInput = 0f;

        // [수정] LogiGetState -> LogiGetStateUnity 로 변경
        if (useLogitechWheel && LogitechGSDK.LogiUpdate() && LogitechGSDK.LogiIsConnected(0))
        {
            // Unity Wrapper에서는 함수 이름 뒤에 'Unity'가 붙는 경우가 많습니다.
            LogitechGSDK.DIJOYSTATE2ENGINES state = LogitechGSDK.LogiGetStateUnity(0);

            // lX는 휠의 회전축 (-32768 ~ 32768 범위)
            float rawValue = state.lX;

            // 정규화: -1.0 ~ 1.0 사이로 변환
            wheelInput = rawValue / 32768f;

            // 데드존 처리
            if (Mathf.Abs(wheelInput) < wheelDeadzone)
            {
                wheelInput = 0f;
            }
        }

        // 3. 입력 합산 및 제한
        float combined = keyboardInput + wheelInput;
        return Mathf.Clamp(combined, -1f, 1f);
    }

    public void SetMoveAction(bool value) { canMove = value; Debug.Log($"Change MoveState : {canMove}"); }

    private void HandleSkillInput()
    {
        if (DataManager.Instance == null) return;

        // --- 버프 스킬 (X 키) ---
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (DataManager.Instance.GetBuffer() >= DataManager.Instance.bufferUse)
            {
                Debug.Log("[PlayerMover] Buff Skill Activated!");
                int cost = DataManager.Instance.bufferUse;
                int remain = DataManager.Instance.GetBuffer() - cost;
                DataManager.Instance.SetBuffer(Mathf.Max(0, remain));

                if (IngameUIManager.Instance != null)
                {
                    IngameUIManager.Instance.Log("Triggering Buff Vignette");
                }
            }
            else
            {
                Debug.Log("Not enough Buff charge!");
            }
        }

        // --- 디버프 스킬 (C 키) ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (DataManager.Instance.GetDeBuffer() >= DataManager.Instance.debufferUse)
            {
                Debug.Log("[PlayerMover] Debuff Skill Activated!");
                int cost = DataManager.Instance.debufferUse;
                int remain = DataManager.Instance.GetDeBuffer() - cost;
                DataManager.Instance.SetDeBuffer(Mathf.Max(0, remain));

                if (IngameUIManager.Instance != null)
                {
                    IngameUIManager.Instance.Log("Triggering Debuff Vignette");
                }
            }
            else
            {
                Debug.Log("Not enough Debuff charge!");
            }
        }
    }

    private void HandleInputLogging(float h, float v)
    {
        float threshold = 0.1f;

        // Forward
        if (v > threshold)
        {
            if (!_loggedForward)
            {
                Debug.Log("Forward Input (W) Started");
                IngameUIManager.Instance.CloseArrowPanel();
                IngameUIManager.Instance.OpenArrowPanel(1);
                _loggedForward = true;
            }
        }
        else _loggedForward = false;

        // Backward
        if (v < -threshold)
        {
            if (!_loggedBackward)
            {
                Debug.Log("Backward Input (S) Started");
                IngameUIManager.Instance.CloseArrowPanel();
                _loggedBackward = true;
            }
        }
        else _loggedBackward = false;

        // Left
        if (h < -threshold)
        {
            if (!_loggedLeft)
            {
                Debug.Log("Left Input (A) or Wheel Left Started");
                IngameUIManager.Instance.CloseArrowPanel();
                IngameUIManager.Instance.OpenArrowPanel(2);
                _loggedLeft = true;
            }
        }
        else _loggedLeft = false;

        // Right
        if (h > threshold)
        {
            if (!_loggedRight)
            {
                Debug.Log("Right Input (D) or Wheel Right Started");
                IngameUIManager.Instance.CloseArrowPanel();
                IngameUIManager.Instance.OpenArrowPanel(3);
                _loggedRight = true;
            }
        }
        else _loggedRight = false;
    }

    public void SetControlState(bool state)
    {
        canMove = state;
        if (!state)
        {
            _currentForwardSpeed = 0f;
            _input = Vector2.zero;
        }
    }

    private void ActivateShield()
    {
        if (shieldEffect != null)
        {
            shieldEffect.SetActive(true);
            CancelInvoke(nameof(DeactivateShield));
            Invoke(nameof(DeactivateShield), 3f);
        }
    }

    private void DeactivateShield()
    {
        if (shieldEffect != null) shieldEffect.SetActive(false);
    }
    #endregion

    #region Collision Handling
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (DataManager.Instance != null) DataManager.Instance.TakeDamage(20);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ShieldHit");
            Destroy(other.gameObject);
        }
    }
    #endregion
}