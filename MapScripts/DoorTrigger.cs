using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    private Door door;
    public GameObject childContent;
    private bool entered = false;
    private bool isPlayerInZone = false; // 플레이어가 문 영역 안에 있는지 확인하는 변수

    private void Awake() => door = GetComponent<Door>();

    // 매 프레임마다 플레이어의 키 입력을 확인합니다.
    private void Update()
    {
        // 1. 플레이어가 영역 안에 있고
        // 2. 문이 열려있으며
        // 3. 아직 들어가지 않은 상태에서
        // 4. F키를 눌렀다면

        if (Time.timeScale == 0f || Player.isAnyUIOpen) return;

        if (isPlayerInZone && door.isOpen && !entered && Input.GetKeyDown(KeyCode.F))
        {
            InteractWithDoor();
        }
    }

    // 플레이어가 문 영역에 들어왔을 때
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            childContent.SetActive(true);
            isPlayerInZone = true;
        }
    }

    // 플레이어가 문 영역에서 나갔을 때
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            childContent.SetActive(false);
            isPlayerInZone = false;
        }
    }

    // 실제 문으로 들어가는 처리 로직 (기존 코드 분리)
    private void InteractWithDoor()
    {
        entered = true;

        SceneChanger sc = Object.FindFirstObjectByType<SceneChanger>();
        if (sc != null)
        {
            // 새로 만든 함수 호출!
            sc.EnterDoorFade();
        }
        else
        {
            // 혹시 SceneChanger가 없으면 그냥 이동이라도 시킴
            WaveManager.Instance.PlayerEnterDoor();
        }

        // 맵이 바뀌고 나서 다시 트리거를 쓸 수 있게 약간의 쿨타임 후 리셋
        Invoke(nameof(ResetTrigger), 2f);
    }

    private void ResetTrigger() => entered = false;
}