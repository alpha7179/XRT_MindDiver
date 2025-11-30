using UnityEngine;

public class PlayerShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 20f;  // 앞으로 자동 전진하는 속도
    public float strafeSpeed = 15f;   // 좌우(A/D), 상하(W/S) 이동 속도

    [Header("Visual Settings")]
    public float leanAngle = 20f;     // 이동 시 살짝 기울어지는 연출 (0으로 하면 아예 안 기울어짐)
    public Vector2 moveLimits = new Vector2(10f, 5f); // 이동 제한 범위 (X:좌우, Y:상하)

    [Header("References")]
    public GameObject shieldEffect;

    private Rigidbody _rb;
    private Vector2 _input;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // 1. 입력 처리
        // Horizontal(A/D): 좌우 이동을 위해 사용
        // Vertical(W/S): 상하 이동을 위해 사용
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        _input = new Vector2(h, v);

        // 방어막 테스트
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateShield();
        }
    }

    private void FixedUpdate()
    {
        // =========================================================
        // [핵심 수정] A/D를 좌우 '이동'으로 처리하는 로직
        // =========================================================

        // 1. 이동 벡터 계산
        // Z축: 계속 앞으로 전진 (자동)
        float moveZ = forwardSpeed * Time.fixedDeltaTime;

        // X축(좌우): A/D 입력값 * 속도 (이게 좌우 '이동'을 만듭니다)
        float moveX = _input.x * strafeSpeed * Time.fixedDeltaTime;

        // Y축(상하): W/S 입력값 * 속도
        float moveY = _input.y * strafeSpeed * Time.fixedDeltaTime;

        // 2. 현재 위치에 이동량을 더해서 '다음 위치' 계산
        // (회전이 아니라 좌표 자체가 옆으로 이동합니다)
        Vector3 nextPosition = _rb.position + new Vector3(moveX, moveY, moveZ);

        // 3. 이동 범위 제한 (터널 밖으로 나가지 않게)
        // X축(좌우) 제한
        nextPosition.x = Mathf.Clamp(nextPosition.x, -moveLimits.x, moveLimits.x);
        // Y축(상하) 제한
        nextPosition.y = Mathf.Clamp(nextPosition.y, -moveLimits.y, moveLimits.y);

        // 4. 리지드바디 실제 이동 적용
        _rb.MovePosition(nextPosition);


        // =========================================================
        // [연출] 이동할 때 살짝 기울이기 (이동에는 영향 없고 눈에 보이는 것만)
        // =========================================================
        // 기체가 이동할 때 뻣뻣하게 움직이면 어색하므로 Z축(Roll)만 살짝 줍니다.
        // 만약 완전 평평하게 이동하고 싶다면 leanAngle을 0으로 설정하세요.
        Quaternion targetRotation = Quaternion.Euler(
            -_input.y * (leanAngle / 2),  // 위아래로 움직일 때 살짝 끄덕임 (Pitch)
            0,                            // 좌우 회전(Yaw)은 0으로 고정 -> 머리는 항상 앞을 봄
            -_input.x * leanAngle         // 좌우 이동 시 날개만 살짝 기우뚱 (Roll)
        );

        _rb.rotation = Quaternion.Lerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * 5f);
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            if (DataManager.Instance != null) DataManager.Instance.TakeDamage(20);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ShieldHit");

            Destroy(other.gameObject);
        }
    }
}