using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;


public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance;

    private Door[] doors;
    private GameObject currentMap;
    private bool doorsOpened = false;
    private bool isSpawnFinished = false;
    public Vector3 pos;
    public NavMeshSurface navMeshSurface;
    public TextMeshProUGUI permanentPoint; // 영구 점수 표시 UI (optional)
    public TextMeshProUGUI statPoint; // 스탯 업그레이드 UI (optional)
    private Chest currentChest; // 보상창 관리를 위한 변수 추가
    private Store currentStore; // 상점 관리를 위한 변수 추가
    public int Pointed = 0;
    public int stat = 0;

    private bool isRewardWindowOpening = false; // 보상창 중복 실행 방지용 변수 추가

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    private void Start()
    {
        // 저장된 영구 점수 불러오기
        Pointed = PlayerPrefs.GetInt("SavedPermanentPoints", 0);
        stat =PlayerPrefs.GetInt("SavedStatPoints", 0);
        if (permanentPoint != null)
        {

            UpdatePointUI();
        }
    }

    private void Update()
    {
        if (currentMap == null) return;
        if (!isSpawnFinished) return;
        if (doorsOpened || isRewardWindowOpening) return; // 이미 문이 열렸거나 보상 대기 중이면 리턴

        // Enemy 태그 가진 오브젝트가 하나도 없으면
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
        {
            Pointed +=10;
            // 지연 후 보상창 및 문 열기를 처리하는 코루틴 시작
            StartCoroutine(DelayedRewardAndOpenDoors(1f)); // 1.5초 대기 (원하는 초로 수정 가능)
        }
    }

    public void SpawnMap(GameObject mapPrefab, Vector3 spawnPos, Transform playerSpawn = null)
    {

        if (currentMap != null) Destroy(currentMap);

        currentMap = Instantiate(mapPrefab, spawnPos, Quaternion.identity);

        StartCoroutine(BakeNavMeshRoutine());

        // 문 초기화
        doors = currentMap.GetComponentsInChildren<Door>();
        currentChest = currentMap.GetComponentInChildren<Chest>();
        currentStore = currentMap.GetComponentInChildren<Store>();

        if (currentChest != null)
        {
            currentChest.CloseChest();
        }

        doorsOpened = false;

        isSpawnFinished = false;

        foreach (Door d in doors)
        {
            d.CloseHidden();
        }

        Transform actualSpawnPoint = currentMap.transform.Find("PlayerSpawn");
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // 1. 플레이어의 대쉬 상태를 먼저 강제로 멈춥니다.
            Player playerScript = player.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.ResetDashState();
            }
            if (actualSpawnPoint != null)
            {
                player.transform.position = actualSpawnPoint.position;
            }
            else
            {
                player.transform.position = spawnPos;
            }
        }
        SpawnFinished(); 

    }
    public void CheckAndOpenDoors()
    {
        if (doorsOpened) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); //몬스터 태그찾아서
        if (enemies.Length == 0)//몬스터 배열이 0이면
        {
            OpenDoors(); // 즉시 문 열림

        }

        if (doorsOpened) return;

    }
    /// MapClearManager 테스팅중 맵에 직접 설치하니 자식개체로 탐지해서 0되면 사라지게하는중



    public void OpenDoors()
    {
        
        if (doorsOpened) return;
        doorsOpened = true;

        foreach (Door d in doors)
            d.Open();

        Debug.Log("문 열림!!!!!!!!!!!!!!!!!!!!!");
    }
    public IEnumerator CheckEnemy()
    {
        yield return new WaitForSeconds(0.1f);

        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            OpenDoors();
        }
    }
    public void PlayerEnterDoor()
    {
        MapManager.Instance.GoToNextMap();
    }

    IEnumerator BakeNavMeshRoutine()
    {

        // collider / tilemap 안정화
        yield return new WaitForEndOfFrame();
        
        if(navMeshSurface == null)
        {
            Debug.LogError("NavMeshSurface 없음");
        }
        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }

        //NavMesh 완성 후 몬스터 생성
        SpawnManager.Instance.SpawnMonsters(currentMap);
    }

    private IEnumerator DelayedRewardAndOpenDoors(float delay)
    {
        isRewardWindowOpening = true; // 중복 실행 방지

        // 지정된 시간만큼 기다림
        yield return new WaitForSeconds(delay);


        //0.1f 당 10%
        if (Random.value <= 0.5f) 
        {
            if (currentChest != null)
            {
                // 위치 이동 코드 삭제: 맵 프리팹 안의 원래 위치 그대로 둡니다.
                // 3. 상자 활성화!
                currentChest.OpenChest();
                Debug.Log("50% 확률로 맵 안의 상자가 나타났습니다!");
            }
        }

        if (Random.value <= 0.5f)
        {
            Debug.Log("[시스템] 50% 확률 당첨! 상점 켜기를 시도합니다.");

            if (currentStore != null)
            {
                currentStore.OpenStore();
                Debug.Log("[시스템] 맵 안의 상점이 성공적으로 나타났습니다!");
            }

        }


        UpdatePointUI();


        // 보상창 띄우기
        if (UIMoveing.uImaanger != null)
        {
            UIMoveing.uImaanger.Rewardtime();
        }



        // 문 열기
        OpenDoors();

        isRewardWindowOpening = false;
    }

    public void SpawnFinished() // 스폰완료함수
    {
        isSpawnFinished = true;
    }

    public void UpdatePointUI()
    {
        if (permanentPoint != null)
        {
            permanentPoint.text = "경험치 : " + Pointed.ToString() + "/" + "100";
            statPoint.text = "레벨 젬: " + stat.ToString();

            PlayerPrefs.SetInt("SavedPermanentPoints", Pointed);
            PlayerPrefs.SetInt("SavedStatPoints", stat);
            PlayerPrefs.Save();

            if (Pointed >= 100)
            {
                stat += 1; // 스탯 포인트 증가
                Pointed -= 100; // 영구 점수 초기화
                // UI 업데이트
                permanentPoint.text = "경험치 : " + Pointed.ToString() + "/" + "100";
                statPoint.text = "레벨 젬: " + stat.ToString();
                // 데이터 저장
                PlayerPrefs.SetInt("SavedPermanentPoints", Pointed);
                PlayerPrefs.SetInt("SavedStatPoints", stat);
                PlayerPrefs.Save();
            }
        }
    }

}


