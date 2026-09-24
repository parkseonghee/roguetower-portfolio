using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VillageDoor : MonoBehaviour
{
    public GameObject childContent; // 확 나타날 오브젝트 (예: 버튼 아이콘)
    public SpriteRenderer childContent2Renderer;
    bool isAtDoor = false;
    // 1. 인스펙터에서 입력받을 씬 이름 변수 추가
    [Header("이동할 씬 이름")]
    [SerializeField] private string targetSceneName = "GameScene";


    private void Update()
    {
        if (Time.timeScale == 0f || Player.isAnyUIOpen) return;
        if (isAtDoor && Input.GetKeyDown(KeyCode.F))
        {
            SceneChanger sc = Object.FindFirstObjectByType<SceneChanger>();

            // Door인스펙터에서 어느씬으로 이동할지 직접 타이핑
            if (sc != null)
            {
                sc.EnterDoorFade();
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            childContent.SetActive(true);
            isAtDoor = true;
        }

    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            childContent.SetActive(false);
            isAtDoor = false;
        }
    }

}