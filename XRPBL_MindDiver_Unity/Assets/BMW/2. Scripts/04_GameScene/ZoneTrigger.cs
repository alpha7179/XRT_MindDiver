using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isGoal = true;
    [SerializeField] private bool isDanger = false;
    [SerializeField] private bool isDebug = true;
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || (other.transform.root != null && other.transform.root.CompareTag(playerTag)))
        {
            if (isDebug) Debug.Log($"[ZoneTrigger] Player entered trigger: {gameObject.name}");

            var phaseManager = FindAnyObjectByType<GamePhaseManager>();
            if (phaseManager != null && isGoal)
            {
                // 페이즈 매니저에게 도달했다고 알림
                phaseManager.SetZoneReached(true);
            }
        }
    }
}