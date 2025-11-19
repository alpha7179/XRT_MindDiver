using UnityEngine;

public class PlayerShipController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 20f;  // 전진 속도
    public float steeringSpeed = 15f; // 좌우/상하 이동 속도
    public float leanAngle = 30f;     // 회전 시 기울기
    public Vector2 moveLimits = new Vector2(10f, 5f); // 이동 제한 범위 (X, Y)

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
        // 1. 입력 처리 (WASD)
        float h = Input.GetAxis("Horizontal"); // A, D
        float v = Input.GetAxis("Vertical");   // W, S

        _input = new Vector2(h, v);

        // 2. 방어막 테스트 (Spacebar)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ActivateShield();
        }
    }

    private void FixedUpdate()
    {
        // 3. 물리 이동 (전진 + 조향)
        // 계속 앞으로 전진
        Vector3 forwardMove = Vector3.forward * forwardSpeed * Time.fixedDeltaTime;

        // WASD로 상하좌우 이동
        Vector3 steeringMove = new Vector3(_input.x, _input.y, 0) * steeringSpeed * Time.fixedDeltaTime;

        Vector3 nextPosition = _rb.position + forwardMove + steeringMove;

        // 4. 이동 제한 (터널 밖으로 못 나가게)
        // (참고: Z축(전진)은 계속 증가하므로 X, Y만 제한)
        // 실제 게임에서는 플레이어는 가만히 있고 맵이 움직이는 방식을 쓸 수도 있지만, 
        // 여기서는 플레이어가 전진하는 방식으로 구현함.
        // nextPosition.x = Mathf.Clamp(nextPosition.x, -moveLimits.x, moveLimits.x);
        // nextPosition.y = Mathf.Clamp(nextPosition.y, -moveLimits.y, moveLimits.y);

        _rb.MovePosition(nextPosition);

        // 5. 회전 연출 (이동 방향으로 기울기)
        Quaternion targetRotation = Quaternion.Euler(-_input.y * (leanAngle / 2), 0, -_input.x * leanAngle);
        _rb.rotation = Quaternion.Lerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * 5f);
    }

    private void ActivateShield()
    {
        if (shieldEffect != null)
        {
            shieldEffect.SetActive(true);
            Invoke("DeactivateShield", 3f); // 3초 뒤 해제
        }
    }

    private void DeactivateShield()
    {
        if (shieldEffect != null) shieldEffect.SetActive(false);
    }

    // 충돌 처리
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle")) // 태그 설정 필요!
        {
            DataManager.Instance.TakeDamage(20);
            AudioManager.Instance.PlaySFX("ShieldHit");
            Destroy(other.gameObject); // 부딪힌 장애물 파괴

            // TODO: 화면 붉어짐 효과 등 추가
        }
    }
}