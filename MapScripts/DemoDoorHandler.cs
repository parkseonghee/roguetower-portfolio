using UnityEngine;

public class DemoEndingTrigger : MonoBehaviour
{
    private GameObject targetPanel; // 캔버스에 있는 그 패널
    private bool isPlayerInZone = false;

    private void Start()
    {
        // 1. HpBarStats 캔버스를 찾습니다.
        GameObject canvas = GameObject.Find("HpBarStats");

        if (canvas != null)
        {
            // 2. 그 캔버스 아래에 미리 넣어둔 "DemoEndingPanel"을 찾습니다.
            Transform t = canvas.transform.Find("DemoEndingPanel");
            if (t != null) targetPanel = t.gameObject;
        }
    }

    private void Update()
    {
        // 플레이어가 영역 안에서 F키를 누르면
        if (isPlayerInZone && Input.GetKeyDown(KeyCode.F))
        {
            // 캔버스에 있는 패널을 켠다!
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);

                // 시간 멈춤 및 마우스 커서 활성화
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerInZone = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerInZone = false;
    }
}