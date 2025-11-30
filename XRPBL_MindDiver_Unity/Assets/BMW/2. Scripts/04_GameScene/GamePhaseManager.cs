using UnityEngine;
using System.Collections;
using System; // Func 사용을 위해 추가

public class GamePhaseManager : MonoBehaviour
{
    public enum Phase { Phase1, Phase2, Phase3, Complete, Null }
    public Phase currentPhase;

    [Header("Phase Settings")]
    public float phase1Duration = 80f;
    public float phase2Duration = 110f;

    [Header("Phase 2 Conditions")]
    public int phase2KillGoal = 20;

    [Header("Target Zone")]
    [Tooltip("Phase 1에서 도달해야 할 목표 지점 오브젝트 (활성화/비활성화 제어용)")]
    [SerializeField] private GameObject phase1TargetZone;

    [Header("디버그 로그")]
    [SerializeField] private bool isDebugMode = true;

    // 내부 상태 변수
    private int _phaseKillCount;
    private bool isZoneReached = false; // 존 도달 여부 체크 변수

    // UI 매니저 등 외부 참조가 필요하다면 추가 (ShowTimedMission에서 사용된다면)
    // [SerializeField] private IngameUIManager uiManager; 

    private void Start()
    {
        // 게임 시작 시 초기화
        if (DataManager.Instance != null)
            DataManager.Instance.InitializeGameData();

        // 코루틴 기반의 시나리오 시작
        StartCoroutine(GameFlowRoutine());
    }

    // ZoneTrigger 등 외부에서 호출하여 도달 상태를 true로 설정하는 메서드
    public void SetZoneReached(bool reached)
    {
        isZoneReached = reached;
        Log($"[GamePhaseManager] Zone Reached: {reached}");
    }

    /// <summary>
    /// 전체 게임 페이즈 흐름을 제어하는 메인 코루틴
    /// </summary>
    private IEnumerator GameFlowRoutine()
    {
        // -------------------------------------------------------------------------
        // Phase 1 시작 (존 도달 미션)
        // -------------------------------------------------------------------------
        StartPhase(Phase.Phase1);

        // 목표 존 활성화 (필요한 경우)
        if (phase1TargetZone != null) phase1TargetZone.SetActive(true);
        isZoneReached = false; // 상태 초기화

        // Phase 1은 "시간이 다 되거나" OR "존에 도달하면" 종료되는 조건이라고 가정
        // 만약 '존 도달'이 필수라면 시간 제한 없이 wait하거나, 시간 내에 도달 못하면 실패 처리 등을 추가 가능
        // 여기서는 GameStepManager처럼 '조건(isZoneReached)을 만족할 때까지 대기'하는 방식으로 구현

        // 조건이 만족될 때까지 대기 (제한 시간 phase1Duration 적용)
        yield return StartCoroutine(WaitForConditionOrTime(() => isZoneReached, phase1Duration));

        // 목표 존 비활성화
        if (phase1TargetZone != null) phase1TargetZone.SetActive(false);

        // -------------------------------------------------------------------------
        // Phase 2 시작 (킬 카운트 or 시간 경과)
        // -------------------------------------------------------------------------
        StartPhase(Phase.Phase2);

        // Phase 2 조건: 킬 목표 달성 또는 시간 종료
        yield return StartCoroutine(WaitForConditionOrTime(() => _phaseKillCount >= phase2KillGoal, phase2Duration));

        // -------------------------------------------------------------------------
        // Phase 3 시작 (보스전)
        // -------------------------------------------------------------------------
        StartPhase(Phase.Phase3);

        // 보스전은 보통 보스가 죽을 때까지 무한 대기 (OnBossDefeated에서 다음 단계 호출)
        // 여기서는 코루틴 흐름을 잠시 멈추고 이벤트 대기 상태로 둠
        // OnBossDefeated가 호출되면 StartPhase(Complete)가 실행됨
    }

    /// <summary>
    /// GameStepManager의 ShowTimedMission과 유사한 기능을 하는 대기 코루틴
    /// 조건이 true가 되거나 시간이 다 될 때까지 대기함
    /// </summary>
    private IEnumerator WaitForConditionOrTime(Func<bool> condition, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            // 성공 조건 체크
            if (condition != null && condition.Invoke())
            {
                Log("[GamePhaseManager] Condition Met! Proceeding to next phase.");
                yield break; // 코루틴 종료 -> 다음 페이즈로 이동
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Log("[GamePhaseManager] Time's up! Proceeding to next phase.");
    }

    public void StartPhase(Phase phase)
    {
        currentPhase = phase;
        _phaseKillCount = 0;

        Log($"[GamePhaseManager] Start Phase: {phase}");

        switch (phase)
        {
            case Phase.Phase1:
                // EnemyManager.StartSpawn(Type.Worm);
                if (AudioManager.Instance) AudioManager.Instance.PlayBGM(GameManager.Instance.currentState, Phase.Phase1);
                break;
            case Phase.Phase2:
                // EnemyManager.StartSpawn(Type.Rock);
                if (AudioManager.Instance) AudioManager.Instance.PlayBGM(GameManager.Instance.currentState, Phase.Phase2);
                break;
            case Phase.Phase3:
                // EnemyManager.SpawnBoss();
                if (AudioManager.Instance) AudioManager.Instance.PlayBGM(GameManager.Instance.currentState, Phase.Phase3);
                break;
            case Phase.Complete:
                if (GameManager.Instance) GameManager.Instance.ChangeState(GameManager.GameState.OutroVideo);
                break;
        }
    }

    // 적 처치 시 호출
    public void OnEnemyKilled()
    {
        _phaseKillCount++;
        if (DataManager.Instance) DataManager.Instance.IncrementKillCount();
    }

    // 보스 처치 시 호출
    public void OnBossDefeated()
    {
        if (DataManager.Instance) DataManager.Instance.StopTimer();

        // 보스 처치 시 바로 완료 단계로 이동 (코루틴 흐름과 별개로 강제 전환)
        StopAllCoroutines(); // 진행 중인 대기 코루틴 중단
        StartPhase(Phase.Complete);
    }

    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
}