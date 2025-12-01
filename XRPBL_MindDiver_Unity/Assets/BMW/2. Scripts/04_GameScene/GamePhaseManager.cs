using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// 게임의 진행 단계(페이즈)와 각 단계별 목표 및 시간 제한을 관리하는 클래스
/// </summary>
public class GamePhaseManager : MonoBehaviour
{
    #region Enums
    public enum Phase { Phase1, Phase2, Phase3, Complete, Null }
    #endregion

    #region Inspector Fields
    // 현재 진행 중인 페이즈 상태
    public Phase currentPhase;

    [Header("Phase Settings")]
    // 1페이즈 제한 시간
    public float phase1Duration = 80f;
    // 2페이즈 제한 시간
    public float phase2Duration = 110f;

    [Header("Phase 2 Conditions")]
    // 2페이즈 목표 처치 수
    public int phase2KillGoal = 20;

    [Header("Target Zone")]
    [Tooltip("Phase 1에서 도달해야 할 목표 지점 오브젝트 (활성화/비활성화 제어용)")]
    [SerializeField] private GameObject phase1TargetZone;

    [Header("Debug Settings")]
    // 디버그 로그 출력 여부
    [SerializeField] private bool isDebugMode = true;
    #endregion

    #region Private Fields
    // 현재 페이즈 내 처치 수 누적
    private int _phaseKillCount;
    // 목표 지점 도달 여부 확인
    private bool isZoneReached = false;
    #endregion

    #region Unity Lifecycle
    /*
     * 게임 데이터 초기화 및 페이즈 시나리오 코루틴 시작
     */
    private void Start()
    {
        // 게임 데이터 초기화 수행
        if (DataManager.Instance != null)
            DataManager.Instance.InitializeGameData();

        // 메인 게임 흐름 코루틴 실행
        StartCoroutine(GameFlowRoutine());
    }
    #endregion

    #region Coroutines
    /*
     * 전체 게임 페이즈의 순차적 흐름을 제어하는 메인 코루틴
     */
    private IEnumerator GameFlowRoutine()
    {
        // -------------------------------------------------------------------------
        // Phase 1: 존 도달 미션
        // -------------------------------------------------------------------------
        StartPhase(Phase.Phase1);

        // 목표 존 오브젝트 활성화
        if (phase1TargetZone != null) phase1TargetZone.SetActive(true);
        isZoneReached = false;

        // 존 도달 성공 또는 제한 시간 종료 대기
        yield return StartCoroutine(WaitForConditionOrTime(() => isZoneReached, phase1Duration));

        // 목표 존 오브젝트 비활성화
        if (phase1TargetZone != null) phase1TargetZone.SetActive(false);

        // -------------------------------------------------------------------------
        // Phase 2: 적 처치 또는 버티기 미션
        // -------------------------------------------------------------------------
        StartPhase(Phase.Phase2);

        // 목표 킬 달성 또는 제한 시간 종료 대기
        yield return StartCoroutine(WaitForConditionOrTime(() => _phaseKillCount >= phase2KillGoal, phase2Duration));

        // -------------------------------------------------------------------------
        // Phase 3: 보스전
        // -------------------------------------------------------------------------
        StartPhase(Phase.Phase3);

        // 보스 처치 대기 (OnBossDefeated 이벤트에 의해 페이즈 전환)
    }

    /*
     * 특정 조건 만족 또는 제한 시간 경과를 기다리는 유틸리티 코루틴
     */
    private IEnumerator WaitForConditionOrTime(Func<bool> condition, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            // 성공 조건 달성 확인
            if (condition != null && condition.Invoke())
            {
                Log("[GamePhaseManager] Condition Met! Proceeding to next phase.");
                yield break; // 조건 만족 시 즉시 종료 및 다음 단계 진행
            }

            timer += Time.deltaTime;
            yield return null;
        }

        Log("[GamePhaseManager] Time's up! Proceeding to next phase.");
    }
    #endregion

    #region Public Methods
    /*
     * 외부 트리거에 의한 목표 지점 도달 상태 설정
     */
    public void SetZoneReached(bool reached)
    {
        isZoneReached = reached;
        Log($"[GamePhaseManager] Zone Reached: {reached}");
    }

    /*
     * 특정 페이즈를 시작하고 관련 설정(BGM, 카운트 초기화 등)을 적용하는 함수
     */
    public void StartPhase(Phase phase)
    {
        currentPhase = phase;
        _phaseKillCount = 0;

        Log($"[GamePhaseManager] Start Phase: {phase}");

        switch (phase)
        {
            case Phase.Phase1:
                // 페이즈 1 BGM 재생 및 설정
                if (AudioManager.Instance) AudioManager.Instance.PlayBGM(GameManager.Instance.currentState, Phase.Phase1);
                break;

            case Phase.Phase2:
                // 페이즈 2 BGM 재생 및 설정
                if (AudioManager.Instance) AudioManager.Instance.PlayBGM(GameManager.Instance.currentState, Phase.Phase2);
                break;

            case Phase.Phase3:
                // 페이즈 3 BGM 재생 및 설정
                if (AudioManager.Instance) AudioManager.Instance.PlayBGM(GameManager.Instance.currentState, Phase.Phase3);
                break;

            case Phase.Complete:
                // 게임 클리어 처리 및 아웃트로 영상 전환
                if (GameManager.Instance) GameManager.Instance.ChangeState(GameManager.GameState.OutroVideo);
                break;
        }
    }

    /*
     * 적 처치 시 호출되어 페이즈 킬 카운트 및 전체 통계 갱신
     */
    public void OnEnemyKilled()
    {
        _phaseKillCount++;
        if (DataManager.Instance) DataManager.Instance.IncrementKillCount();
    }

    /*
     * 보스 처치 시 호출되어 게임 완료 상태로 강제 전환
     */
    public void OnBossDefeated()
    {
        if (DataManager.Instance) DataManager.Instance.StopTimer();

        // 진행 중인 대기 코루틴 중단 및 완료 페이즈 즉시 시작
        StopAllCoroutines();
        StartPhase(Phase.Complete);
    }

    /*
     * 디버그 모드 시 로그 출력 수행
     */
    public void Log(string message)
    {
        if (isDebugMode) Debug.Log(message);
    }
    #endregion
}