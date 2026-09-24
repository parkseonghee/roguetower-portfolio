using UnityEngine;
using TMPro;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("Map Settings")]
    public GameObject[] mapPrefabs;      // 일반방
    public GameObject bossMapPrefab;     // 보스방
    public TextMeshProUGUI floorText;    // 층수 표시 UI (optional)

    public int currentFloor = 0;         // 현재 층수 관리 
    public int maxFloorBeforeBoss = 9;   // 보스 전까지의 일반 층수

    [Header("BGM Settings")]
    public AudioSource bgmSource;        // 배경음악을 재생할 오디오 소스
    public AudioClip normalBGM;          // 1~9층 기본 배경음악
    public AudioClip bossBGM;            // 10층 보스방 전용 배경음악

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GoToNextMap(); // 게임 시작 시 랜덤 맵 스폰
    }

    public void GoToNextMap()
    {
        currentFloor++; // 층수 증가

        Debug.Log($"현재 {currentFloor}층 진입");

        if (floorText != null)
            floorText.text = $"{currentFloor} 층"; // UI 업데이트

        GameObject mapToSpawn = null;

        // 1. 층수에 따른 맵 결정 및 BGM 세팅
        if (currentFloor == 10)
        {
            // 10층이면 보스방 확정
            if (bossMapPrefab != null)
            {
                mapToSpawn = bossMapPrefab;
                Debug.Log("보스방을 생성합니다!");

                // 보스전 BGM으로 변경!
                ChangeBGM(bossBGM);
            }
            else
            {
                Debug.LogError("Boss Map Prefab이 할당되지 않았습니다!");
                return;
            }
        }
        else if (currentFloor > 10)
        {
            // 10층 이후 로직
            Debug.Log("축하합니다! 모든 층을 정복했습니다.");
            return;
        }
        else
        {
            // 1~9층은 랜덤 맵
            if (mapPrefabs == null || mapPrefabs.Length == 0) return;
            int idx = Random.Range(0, mapPrefabs.Length);
            mapToSpawn = mapPrefabs[idx];

            // 1~9층 기본 브금 재생 (ChangeBGM이 알아서 안 끊기게 해줌)
            ChangeBGM(normalBGM);
        }

        // 2. WaveManager를 통해 맵 생성
        WaveManager.Instance.SpawnMap(mapToSpawn, Vector3.zero);
    }

    // BGM을 끊기지 않게 교체해 주는 핵심 함수
    private void ChangeBGM(AudioClip newClip)
    {
        if (bgmSource == null || newClip == null) return;

        // 현재 재생 중인 음악과 다음에 틀 음악이 같다면 무시 (1~9층 이동 시 계속 이어짐)
        if (bgmSource.clip == newClip && bgmSource.isPlaying)
        {
            return;
        }

        // 음악이 다르다면 새로운 음악으로 교체 후 처음부터 재생 (10층 진입 시 작동)
        bgmSource.clip = newClip;
        bgmSource.Play();
    }
}