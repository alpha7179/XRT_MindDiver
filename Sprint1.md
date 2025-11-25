🚀 마인드 다이버 - Sprint 1: 핵심 게임 루프 구현 가이드

목표: GameStageScene에서 파일럿은 함선을 운전하여 장애물을 피하고, 포수는 마우스 클릭으로 적을 파괴하며, 페이즈가 자동으로 전환되는 '플레이 가능한 프로토타입'을 완성합니다.

1. 씬 구성 (GameStageScene)

GameStageScene을 열고 다음과 같이 오브젝트를 구성합니다.

1.1. 환경 설정

Tunnel (가칭): 플레이어가 앞으로 나아가는 느낌을 줄 긴 터널이나 바닥을 만듭니다. (간단히 큐브를 길게 늘려서 배치)

Lighting: Directional Light 하나를 배치하여 그림자가 보이게 합니다.

1.2. 플레이어 (파일럿)

Player_Ship (Empty Object): PlayerShipController.cs 컴포넌트 추가. Rigidbody 컴포넌트 추가 (Use Gravity: False).

Camera_Rig (Child): 메인 카메라 및 3면 카메라(DisplayManager의 카메라들)를 이 아래로 옮기거나, 카메라가 이 오브젝트를 따라다니게 설정해야 합니다. (가장 쉬운 방법: DisplayManager의 카메라들을 Player_Ship의 자식으로 넣으세요.)

Ship_Model (Child): 함선 모양을 대신할 큐브(Cube)나 캡슐(Capsule).

Shield_Effect (Child): 방어막을 표현할 구체(Sphere). 평소엔 비활성화(SetActive(false)).

1.3. 매니저 배치

@GamePhaseManager: GamePhaseManager.cs 추가.

@EnemyManager: EnemyManager.cs 추가.

SpawnPoints (Children): 자식으로 빈 오브젝트들을 만들어서 화면 밖 전방(Front), 좌측(Left), 우측(Right) 등에 배치합니다. 이 위치에서 적들이 생성됩니다.

1.4. UI 구성

Game_UI_Canvas: UIManager_GameStage.cs 추가. 점수 텍스트, 실드 슬라이더 등을 배치하고 스크립트에 연결합니다.

2. 스크립트 구현 (Copy & Paste)

Sprint 1의 핵심이 될 3가지 스크립트의 구체적인 코드입니다.

2.1. PlayerShipController.cs (파일럿 조작)

WASD로 함선을 이동시키고, 장애물 충돌을 감지합니다.

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


2.2. EnemyManager.cs (스폰 시스템)

플레이어의 진행 방향 앞쪽에서 적을 주기적으로 생성합니다.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject spamMitePrefab;   // 악플 벌레
    public GameObject smallRockPrefab;  // 작은 암석 (포수용)
    public GameObject largeRockPrefab;  // 거대 암석 (파일럿 회피용)
    public GameObject bossPrefab;

    [Header("Spawn Settings")]
    public Transform playerTransform;   // 플레이어 위치 참조 (플레이어 앞쪽에 스폰하기 위해)
    public float spawnDistance = 50f;   // 플레이어 전방 50m에서 스폰
    public Vector2 spawnAreaSize = new Vector2(15f, 8f); // 스폰 범위

    private bool _isSpawning = false;

    // 스폰 시작 함수
    public void StartSpawning(string type)
    {
        _isSpawning = true;
        
        // 기존 코루틴이 있다면 중지하고 새로 시작
        StopAllCoroutines(); 

        if (type == "Phase1") StartCoroutine(Phase1Routine());
        else if (type == "Phase2") StartCoroutine(Phase2Routine());
    }

    public void StopSpawning()
    {
        _isSpawning = false;
        StopAllCoroutines();
    }

    public void SpawnBoss()
    {
        Vector3 spawnPos = playerTransform.position + Vector3.forward * (spawnDistance + 20f);
        Instantiate(bossPrefab, spawnPos, Quaternion.identity);
    }

    // Phase 1: 악플 벌레만 스폰
    IEnumerator Phase1Routine()
    {
        while (_isSpawning)
        {
            SpawnObject(spamMitePrefab);
            yield return new WaitForSeconds(1.5f); // 1.5초 간격
        }
    }

    // Phase 2: 암석들 스폰
    IEnumerator Phase2Routine()
    {
        while (_isSpawning)
        {
            // 랜덤하게 작은 돌 or 큰 돌
            if (Random.value > 0.5f) SpawnObject(smallRockPrefab);
            else SpawnObject(largeRockPrefab);

            yield return new WaitForSeconds(1.0f); // 1초 간격 (더 빠름)
        }
    }

    void SpawnObject(GameObject prefab)
    {
        if (playerTransform == null) return;

        // 플레이어 기준 전방 + 랜덤 X, Y 위치 계산
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnAreaSize.x, spawnAreaSize.x),
            Random.Range(-spawnAreaSize.y, spawnAreaSize.y),
            spawnDistance
        );

        Vector3 spawnPos = playerTransform.position + randomOffset;
        
        // 생성 및 플레이어 바라보게 회전 (선택 사항)
        Instantiate(prefab, spawnPos, Quaternion.identity);
    }
}


2.3. Target.cs (포수 상호작용)

적이나 아이템 프리팹에 붙여서 클릭(터치)되었을 때의 동작을 정의합니다.

using UnityEngine;

public class Target : MonoBehaviour
{
    public enum TargetType { SpamMite, SmallRock, LargeRock, BossWeakpoint }
    public TargetType type;

    // InteractionManager의 SendMessage("OnHit")에 의해 호출됨
    public void OnHit()
    {
        // 타입별 동작
        switch (type)
        {
            case TargetType.SpamMite:
                DataManager.Instance.AddScore(100);
                DataManager.Instance.IncrementKillCount();
                
                // 현재 페이즈 매니저에 알림 (킬 카운트 증가용)
                FindObjectOfType<GamePhaseManager>()?.OnEnemyKilled();
                
                AudioManager.Instance.PlaySFX("Splat");
                
                // 파티클 효과 생성 (생략 가능)
                Destroy(gameObject);
                break;

            case TargetType.SmallRock:
                DataManager.Instance.AddScore(50);
                FindObjectOfType<GamePhaseManager>()?.OnEnemyKilled();
                AudioManager.Instance.PlaySFX("Explode");
                Destroy(gameObject);
                break;

            case TargetType.LargeRock:
                // 거대 암석은 포수가 파괴 불가 (혹은 여러 번 터치해야 함)
                // 여기서는 파괴 불가로 설정 (피드백만 재생)
                AudioManager.Instance.PlaySFX("ShieldHit"); // 팅겨내는 소리
                break;

            case TargetType.BossWeakpoint:
                // 보스 데미지 로직 (추후 구현)
                break;
        }
    }
}


3. 프리팹(Prefab) 제작 및 테스트 순서

적 프리팹 만들기:

SpamMite: 붉은색 큐브. Target.cs (Type: SpamMite), BoxCollider 추가. 태그는 필요 없음.

SmallRock: 회색 작은 구체. Target.cs (Type: SmallRock), SphereCollider 추가. 태그 없음.

LargeRock: 회색 큰 구체. Target.cs (Type: LargeRock), SphereCollider (IsTrigger 체크) 추가. Tag를 Obstacle로 설정! (중요: 그래야 PlayerShipController가 충돌 감지함).

매니저 연결:

GamePhaseManager 스크립트에서 EnemyManager 변수에 씬에 있는 EnemyManager 오브젝트를 연결합니다.

EnemyManager 스크립트에서 playerTransform에 Player_Ship을 연결하고, 위에서 만든 프리팹들을 할당합니다.

실행 및 테스트:

이동: Play를 누르고 WASD로 함선이 움직이는지 확인합니다.

스폰: 게임이 시작되면(Phase 1) 붉은색 큐브들이 전방에서 생성되는지 확인합니다.

공격: 마우스로 붉은색 큐브를 클릭하면 사라지고 점수가 오르는지 확인합니다. (InteractionManager가 켜져 있어야 함)

페이즈 전환: 시간이 지나거나 적을 일정 수 잡으면 돌멩이가 날아오는 Phase 2로 바뀌는지 확인합니다.

충돌: 거대한 돌에 부딪히면 콘솔에 "Damage Taken" 로그가 뜨는지 확인합니다.

이 과정을 완료하면 Sprint 1 성공입니다! 바로 시작해보세요.