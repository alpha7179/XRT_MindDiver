using UnityEngine;

/// <summary>
/// 플레이어 우주선의 물리 기반 이동, 연출 및 상호작용을 제어하는 클래스
/// </summary>
public class PlayerShipController : MonoBehaviour
{
    #region Inspector Fields
    [Header("Movement Settings")]
    // 전진 자동 이동 속도 설정
    public float forwardSpeed = 20f;
    // 좌우 및 상하 이동 속도 설정
    public float strafeSpeed = 15f;

    [Header("Visual Settings")]
    // 이동 시 기체 기울임 연출 각도 설정 (0일 경우 기울임 없음)
    public float leanAngle = 20f;
    // 이동 가능 범위 제한 설정 (X: 좌우, Y: 상하)
    public Vector2 moveLimits = new Vector2(10f, 5f);

    [Header("References")]
    // 쉴드 이펙트 오브젝트 참조
    public GameObject shieldEffect;
    #endregion

    #region Private Fields
    // 물리 연산을 위한 리지드바디 컴포넌트
    private Rigidbody _rb;
    // 사용자 입력 벡터 저장
    private Vector2 _input;
    #endregion

    #region Unity Lifecycle
    /*
     * 리지드바디 컴포넌트 초기화 수행
     */
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    /*
     * 사용자 입력 감지 및 테스트 기능 수행
     */
    private void Update()
    {
        // 1. 입력 처리
        // Horizontal(A/D): 좌우 이동 입력
        // Vertical(W/S): 상하 이동 입력
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        _input = new Vector2(h, v);

        // 방어막 기능 테스트 수행
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateShield();
        }
    }

    /*
     * 물리 기반 이동 로직 및 기체 기울임 연출 처리 수행
     */
    private void FixedUpdate()
    {
        // 1. 이동 벡터 계산
        // Z축: 자동 전진 속도 적용
        float moveZ = forwardSpeed * Time.fixedDeltaTime;

        // X축(좌우): 입력값에 따른 좌우 이동 속도 적용
        float moveX = _input.x * strafeSpeed * Time.fixedDeltaTime;

        // Y축(상하): 입력값에 따른 상하 이동 속도 적용
        float moveY = _input.y * strafeSpeed * Time.fixedDeltaTime;

        // 2. 다음 위치 계산 (회전이 아닌 좌표 이동)
        Vector3 nextPosition = _rb.position + new Vector3(moveX, moveY, moveZ);

        // 3. 이동 범위 제한 (터널 이탈 방지)
        // X축(좌우) 위치 제한 적용
        nextPosition.x = Mathf.Clamp(nextPosition.x, -moveLimits.x, moveLimits.x);
        // Y축(상하) 위치 제한 적용
        nextPosition.y = Mathf.Clamp(nextPosition.y, -moveLimits.y, moveLimits.y);

        // 4. 리지드바디 위치 갱신 수행
        _rb.MovePosition(nextPosition);

        // [연출] 이동 방향에 따른 기체 회전 처리
        // Pitch: 상하 이동 시 끄덕임 연출
        // Yaw: 0으로 고정 (항상 정면 응시)
        // Roll: 좌우 이동 시 날개 기울임 연출
        Quaternion targetRotation = Quaternion.Euler(
            -_input.y * (leanAngle / 2),
            0,
            -_input.x * leanAngle
        );

        // 부드러운 회전 적용 (보간 사용)
        _rb.rotation = Quaternion.Lerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * 5f);
    }
    #endregion

    #region Helper Methods
    /*
     * 쉴드 이펙트 활성화 및 자동 비활성화 예약 수행
     */
    private void ActivateShield()
    {
        if (shieldEffect != null)
        {
            shieldEffect.SetActive(true);
            CancelInvoke(nameof(DeactivateShield));
            Invoke(nameof(DeactivateShield), 3f);
        }
    }

    /*
     * 쉴드 이펙트 비활성화 수행
     */
    private void DeactivateShield()
    {
        if (shieldEffect != null) shieldEffect.SetActive(false);
    }
    #endregion

    #region Collision Handling
    /*
     * 장애물 충돌 감지 및 데미지 처리 수행
     */
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            // 데이터 매니저를 통한 데미지 적용
            if (DataManager.Instance != null) DataManager.Instance.TakeDamage(20);

            // 오디오 매니저를 통한 피격음 재생
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ShieldHit");

            // 충돌한 장애물 제거 수행
            Destroy(other.gameObject);
        }
    }
    #endregion
}