using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneChanger : MonoBehaviour
{
    public Image fadeImage; // 1단계에서 만든 이미지를 여기에 연결
    public float fadeSpeed = 2.0f; // 페이드 속도
    public static bool IsGameStarted = false; // 다른 스크립트에서 접근 가능하게 static

    // 씬이 시작되자마자 실행되는 부분
    private void Start()
    {
        Time.timeScale = 0f;
        // 씬에 들어왔으니 커튼을 걷어야 함 (검정 -> 투명)
        StartCoroutine(FadeIn());
    }

    // [버튼에 연결할 함수]
    public void GoToNextScene(string sceneName)
    {
        // 버튼을 누르면 커튼을 치고 씬 이동 (투명 -> 검정)
        StartCoroutine(FadeOut(sceneName));
    }

    public void EnterDoorFade()
    {
        StopAllCoroutines(); // 혹시 실행 중인 페이드가 있다면 중단
        StartCoroutine(EnterDoorRoutine());
    }
    // 즉시 화면을 검게 만들고 시간을 멈추는 함수
    public void CutToBlack()
    {
        Time.timeScale = 0f;
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 1f); // 즉시 100% 검정
    }
    public IEnumerator FadeIn()
    {
        Debug.Log("페이드 인 시작!"); // 콘솔 확인용
        fadeImage.gameObject.SetActive(true);

        float alpha = 1.0f;
        fadeImage.color = new Color(0, 0, 0, 1f);

        // 루프가 도는지 확인하기 위해 아주 확실한 구조로 변경
        while (alpha > 0)
        {
            // 1. 알파값 감소 (Time.unscaledDeltaTime이 0인지 확인 필요)
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime == 0) deltaTime = 0.01f; // 혹시 모르니 최소값 부여

            alpha -= deltaTime * fadeSpeed;

            // 2. 컬러 적용
            fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));

            // Debug.Log($"현재 알파값: {alpha}"); // 너무 많이 찍히면 주석 처리해

            // 3. 시간 정지 상태에서도 무조건 다음 프레임으로 넘어가게 함
            yield return new WaitForSecondsRealtime(0.02f);
        }

        Debug.Log("페이드 인 완료! 시간 재생합니다."); // 여기까지 와야 성공

        fadeImage.gameObject.SetActive(false);
        Time.timeScale = 1f;
    }
    public void OnRetryButton()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        StartCoroutine(FadeOut(currentScene));
    }

    public void OnVillageButton()
    {
        // 마을 씬의 실제 이름을 " " 안에 정확히 써줘!
        StartCoroutine(FadeOut("VillageScene"));
    }

    public void OnTitleButton()
    {
        // 스타트 씬의 실제 이름을 " " 안에 정확히 써줘!
        StartCoroutine(FadeOut("StartScene"));
    }
    public void OnTutorialButton()
    {
        StartCoroutine(FadeOut("TutorialScene"));
    }

    IEnumerator FadeOut(string sceneName)
    {
        fadeImage.gameObject.SetActive(true);
        // 시작할 때 확실하게 투명한 검정색으로 설정
        fadeImage.color = new Color(0, 0, 0, 0);
        if (fadeImage == null)
        {
            SceneManager.LoadScene(sceneName);
            yield break;
        }

        fadeImage.gameObject.SetActive(true);
        float alpha = 0f;
        float currentSpeed = (fadeSpeed <= 0) ? 1.0f : fadeSpeed;

        while (alpha < 1.0f)
        {
            alpha += Time.unscaledDeltaTime * currentSpeed;
            fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));
            yield return null;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator EnterDoorRoutine()
    {
        // 1. 즉시 암전 및 시간 정지
        Time.timeScale = 0f;
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 1f);
        Debug.Log("1. 화면 암전 및 시간 정지 완료");

        // 2. 물리적 이동이 처리될 아주 짧은 실시간 대기
        yield return new WaitForSecondsRealtime(0.1f);

        // 3. 실제 이동 로직 실행 (깜깜한 상태)
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.PlayerEnterDoor();
            Debug.Log("2. PlayerEnterDoor 이동 로직 실행됨");
        }

        // 4. 이동 후 맵 세팅을 위한 짧은 대기
        yield return new WaitForSecondsRealtime(0.1f);

        // 5. 다시 스르륵 밝아지기
        float alpha = 1.0f;
        while (alpha > 0)
        {
            // Time.timeScale이 0이어도 돌아가도록 unscaledDeltaTime 사용
            alpha -= Time.unscaledDeltaTime * fadeSpeed;
            fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));

            // 시간 정지 상태에서도 루프를 돌리기 위한 핵심 코드
            yield return new WaitForSecondsRealtime(0.01f);
        }

        // 6. 마무리 및 시간 재생
        fadeImage.gameObject.SetActive(false);
        Time.timeScale = 1f; // 페이드 인이 완료된 시점에서 게임 재개

        Debug.Log("3. 페이드 인 완료 및 게임 재개!");
    }
}