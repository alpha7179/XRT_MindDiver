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
                FindAnyObjectByType<GamePhaseManager>()?.OnEnemyKilled();

                AudioManager.Instance.PlaySFX(SFXType.Collect_Energy);

                // 파티클 효과 생성 (생략 가능)
                Destroy(gameObject);
                break;

            case TargetType.SmallRock:
                DataManager.Instance.AddScore(50);
                FindAnyObjectByType<GamePhaseManager>()?.OnEnemyKilled();
                AudioManager.Instance.PlaySFX(SFXType.Die_Enemy);
                Destroy(gameObject);
                break;

            case TargetType.LargeRock:
                // 거대 암석은 포수가 파괴 불가 (혹은 여러 번 터치해야 함)
                // 여기서는 파괴 불가로 설정 (피드백만 재생)
                AudioManager.Instance.PlaySFX(SFXType.Damage_Shield); // 팅겨내는 소리
                break;

            case TargetType.BossWeakpoint:
                // 보스 데미지 로직 (추후 구현)
                break;
        }
    }
}