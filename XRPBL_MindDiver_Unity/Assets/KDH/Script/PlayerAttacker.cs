using Energy;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttacker : MonoBehaviour
{
    // 카메라들을 저장할 리스트
    private List<Camera> activeCameras = new List<Camera>();

    private void Start()
    {
        // 씬에 있는 모든 활성화된 카메라 컴포넌트를 가져옵니다.
        activeCameras.AddRange(FindObjectsByType<Camera>(FindObjectsSortMode.None));

        if (activeCameras.Count == 0)
        {
            Debug.LogError("씬에서 활성화된 Camera 컴포넌트를 찾을 수 없습니다.");
        }
    }

    void Update()
    {
        CheckGameOver();

        // 마우스 왼쪽 버튼(0번)이 눌렸는지 확인합니다.
        if (Input.GetMouseButtonDown(0))
        {
            // [수정] 총알이 있는지 먼저 확인하고 발사 로직 수행
            if (DataManager.Instance != null && DataManager.Instance.GetBullet() > 0)
            {
                // 1. 발사 사운드 재생
                PlaySound(SFXType.Attack_Player);

                // 2. 총알 차감
                //DataManager.Instance.SetBullet(DataManager.Instance.GetBullet() - 1);

                // 3. 레이캐스트 판정
                HandleClick();
            }
        }
    }

    private void HandleClick()
    {
        // 모든 카메라를 순회하며 Raycast 검사
        foreach (Camera cam in activeCameras)
        {
            if (cam == null || !cam.isActiveAndEnabled) continue;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                EnemyHealth enemy = hit.transform.GetComponent<EnemyHealth>();
                EnergyClass energy = hit.transform.GetComponent<EnergyClass>();

                // 적(Enemy) 적중 시
                if (enemy != null)
                {
                    enemy.TakeDamage(enemy.damagePerClick);

                    // [추가] 적 타격 사운드
                    PlaySound(SFXType.Damage_Enemy);

                    return;
                }

                // 에너지(Energy) 적중 시
                if (energy != null)
                {
                    energy.TakeDamage(energy.damagePerClick);

                    // [추가] 에너지 타격(수집) 사운드
                    PlaySound(SFXType.Collect_Energy);

                    return;
                }
            }
        }
    }

    private void CheckGameOver()
    {
        if (DataManager.Instance != null && IngameUIManager.Instance != null)
        {
            // 총알이 0 이하이고, 이미 실패 창이 떠있지 않다면 실패 처리
            if (DataManager.Instance.GetBullet() <= 0 && !IngameUIManager.Instance.GetDisplayPanel())
            {
                IngameUIManager.Instance.OnClickFailButton();
            }
        }
    }

    // [추가] 사운드 재생 헬퍼 메서드
    private void PlaySound(SFXType type)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(type);
        }
    }
}