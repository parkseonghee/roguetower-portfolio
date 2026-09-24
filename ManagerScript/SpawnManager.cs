using System.Threading;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    public GameObject[] monsterPrefabs;   // Inspector에 표시됨

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SpawnMonsters(GameObject map)
    {
        MonsterSpawnPoint[] spawnPoints = map.GetComponentsInChildren<MonsterSpawnPoint>();
        Debug.Log("SpawnMonsters 호출됨");

        foreach (var sp in spawnPoints)
        {
            
            GameObject monster = monsterPrefabs[Random.Range(0, monsterPrefabs.Length)];

            GameObject spawned = Instantiate(
                monster,
                sp.transform.position,
                Quaternion.identity,
                map.transform
            );

            // 반드시 Enemy 태그 적용
            spawned.tag = "Enemy";
            WaveManager.Instance.SpawnFinished();
        }
    }

    public GameObject playerPrefab;
    GameObject currentPlayer;

    public void PlayerSpawn(Vector3 pos)
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }

        currentPlayer = Instantiate(playerPrefab, pos, Quaternion.identity);
    }
}