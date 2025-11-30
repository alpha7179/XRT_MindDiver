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
