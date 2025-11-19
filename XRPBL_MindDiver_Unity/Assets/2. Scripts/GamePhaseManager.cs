using UnityEngine;
using System.Collections;

public class GamePhaseManager : MonoBehaviour
{
    public enum Phase { Phase1, Phase2, Phase3, Complete }
    public Phase currentPhase;

    [Header("Phase Settings")]
    public float phase1Duration = 80f;
    public float phase2Duration = 110f;

    [Header("Phase 1 Conditions")]


    [Header("Phase 2 Conditions")]
    public int phase2KillGoal = 20; // 예: 20마리 처치 시 조기 종료 가능 여부 등

    private float _timer;
    private int _phaseKillCount;

    private void Start()
    {
        // 게임 시작 시 초기화
        DataManager.Instance.InitializeGameData();
        StartPhase(Phase.Phase1);
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // 페이즈별 시간 체크
        if (currentPhase == Phase.Phase1)
        {
            if (_timer >= phase1Duration)
            {
                StartPhase(Phase.Phase2);
            }
        }
        else if (currentPhase == Phase.Phase2)
        {
            if (_timer >= phase2Duration)
            {
                StartPhase(Phase.Phase3);
            }
        }
    }

    public void StartPhase(Phase phase)
    {
        currentPhase = phase;
        _timer = 0f;
        _phaseKillCount = 0;

        GameManager.Instance.Log($"[GamePhaseManager] Start Phase: {phase}");

        // 페이즈 전환 시 BGM 변경 및 스포너 설정
        switch (phase)
        {
            case Phase.Phase1:
                // EnemyManager.StartSpawn(Type.Worm);
                // AudioManager.PlayBGM(bgm_Phase1);
                break;
            case Phase.Phase2:
                // EnemyManager.StartSpawn(Type.Rock);
                // AudioManager.PlayBGM(bgm_Phase2);
                break;
            case Phase.Phase3:
                // EnemyManager.SpawnBoss();
                // AudioManager.PlayBGM(bgm_Boss);
                break;
            case Phase.Complete:
                GameManager.Instance.ChangeState(GameManager.GameState.OutroVideo);
                break;
        }
    }

    // 적 처치 시 호출 (Target.cs에서 호출)
    public void OnEnemyKilled()
    {
        _phaseKillCount++;
        DataManager.Instance.IncrementKillCount();

        // Phase 2 조기 종료 조건 (예시)
        if (currentPhase == Phase.Phase2 && _phaseKillCount >= phase2KillGoal)
        {
            GameManager.Instance.Log("[GamePhaseManager] Phase 2 Kill Goal Reached!");
            StartPhase(Phase.Phase3);
        }
    }

    // 보스 처치 시 호출
    public void OnBossDefeated()
    {
        DataManager.Instance.StopTimer();
        StartPhase(Phase.Complete);
    }
}