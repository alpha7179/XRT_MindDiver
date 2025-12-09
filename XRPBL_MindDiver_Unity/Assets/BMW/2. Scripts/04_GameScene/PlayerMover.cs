using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

/// <summary>
/// 플레이어 우주선의 평행 이동 및 속도 제어 클래스 (회전 없음)
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

    private void Update()
    {
        // 1. 움직임이 불가능한 상태면 입력 처리도 하지 않음
        if (!canMove)
        {
            _input = Vector2.zero;
            return;
        }

        // 2. 이동 입력 처리
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        _input = new Vector2(h, v);

        // 3. 로그 출력 및 화살표 UI 처리
        HandleInputLogging(h, v);

        // 4. 스킬(버프/디버프) 입력 처리 [추가됨]
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
    #endregion

    #region Helper Methods
    public void SetMoveAction(bool value) { canMove = value; Debug.Log($"Change MoveState : {canMove}"); }

    // [추가] 스킬 입력 처리 함수
    private void HandleSkillInput()
    {
        if (DataManager.Instance == null) return;

        // --- 버프 스킬 (X 키) ---
        if (Input.GetKeyDown(KeyCode.X))
        {
            // 1. 임계값(Max) 체크
            // (DataManager의 maxCharge가 public이라고 가정합니다)
            if (DataManager.Instance.GetBuffer() >= DataManager.Instance.bufferUse)
            {
                Debug.Log("[PlayerMover] Buff Skill Activated!");

                // 2. 활성화 코드 (기능 구현부)
                /*
                 * 예: PlayerAttack.Instance.EnablePowerUp();
                 */

                // 3. 사용량 차감 (현재값 - Max값, 즉 0으로 초기화)
                int cost = DataManager.Instance.bufferUse;
                int remain = DataManager.Instance.GetBuffer() - cost;
                DataManager.Instance.SetBuffer(Mathf.Max(0, remain));

                // 4. 비네팅 효과 활성화 (UI Manager 연동) [cite: 2]
                // (IngameUIManager에 해당 이벤트를 트리거하는 public 메서드가 필요합니다)
                if (IngameUIManager.Instance != null)
                {
                    // IngameUIManager.Instance.HandleBufferAdded(); // 메서드 접근 제어자가 public이어야 함
                    // 혹은 아래처럼 별도의 트리거 메서드를 만들어서 호출
                    IngameUIManager.Instance.Log("Triggering Buff Vignette");
                    // 기존 로직을 활용하기 위해 임시로 AddBuffer 이벤트를 활용하거나, 
                    // IngameUIManager에 'TriggerBuffEffect()' Public 함수를 추가하여 호출해야 합니다.
                    // 여기서는 DataManager의 SetBuffer가 UI 업데이트를 하겠지만, 
                    // '스킬 사용 효과'를 위해 UI 매니저의 메서드를 직접 호출하는 것이 좋습니다.
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
            // 1. 임계값(Max) 체크 [cite: 1]
            if (DataManager.Instance.GetDeBuffer() >= DataManager.Instance.debufferUse)
            {
                Debug.Log("[PlayerMover] Debuff Skill Activated!");

                // 2. 활성화 코드 (기능 구현부)
                /* * [Active Code Here]
                 * 예: EnemyManager.Instance.ApplySlowDown();
                 */

                // 3. 사용량 차감
                int cost = DataManager.Instance.debufferUse;
                int remain = DataManager.Instance.GetDeBuffer() - cost;
                DataManager.Instance.SetDeBuffer(Mathf.Max(0, remain));

                // 4. 비네팅 효과 활성화
                if (IngameUIManager.Instance != null)
                {
                    IngameUIManager.Instance.Log("Triggering Debuff Vignette");
                    // IngameUIManager.Instance.TriggerDeBuffEffect(); // Public 메서드 필요
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
                Debug.Log("Left Input (A) Started");
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
                Debug.Log("Right Input (D) Started");
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