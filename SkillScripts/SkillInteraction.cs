using System.Collections;
using UnityEngine;

public class SkillInteraction : MonoBehaviour
{
    [Header("UI 설정")]
    public GameObject skillUIPanel;

    [Header("연결할 오브젝트 및 렌더러")]
    public GameObject childContent;
    public GameObject childContent2;
    public SpriteRenderer childContent2Renderer;

    [Header("파티클 설정")]
    public ParticleSystem effectParticleSystem;
    public float targetEmissionRate = 10f;

    [Header("연출 시간 설정")]
    public float fadeDuration = 0.5f;

    [Header("오디오")]
    public AudioSource audioSource;
    public AudioClip audioClip;

    private Coroutine currentEffectCoroutine;
    private ParticleSystem.EmissionModule emissionModule;
    private bool isPlayerInRange = false;

    void Start()
    {
        if (childContent2Renderer != null)
        {
            Color color = childContent2Renderer.color;
            color.a = 0f;
            childContent2Renderer.color = color;
        }

        if (effectParticleSystem != null)
        {
            emissionModule = effectParticleSystem.emission;
            emissionModule.rateOverTime = 0f;
            effectParticleSystem.Stop();
        }

        if (skillUIPanel != null) skillUIPanel.SetActive(false);
        if (childContent != null) childContent.SetActive(false);
        if (childContent2 != null) childContent2.SetActive(false);
    }

    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.F))
            ToggleSkillUI();
    }

    private void ToggleSkillUI()
    {
        if (skillUIPanel == null) return;

        bool isActive = !skillUIPanel.activeSelf;
        skillUIPanel.SetActive(isActive);
        Time.timeScale = isActive ? 0f : 1f;
        Player.isAnyUIOpen = isActive;

        if (childContent != null)
            childContent.SetActive(!isActive);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (childContent != null) childContent.SetActive(true);
            if (childContent2 != null) childContent2.SetActive(true);

            if (audioSource != null && audioClip != null)
                audioSource.PlayOneShot(audioClip);

            if (currentEffectCoroutine != null) StopCoroutine(currentEffectCoroutine);
            currentEffectCoroutine = StartCoroutine(ActivateEffectsOverTime(true));
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (childContent != null) childContent.SetActive(false);
            if (childContent2 != null) childContent2.SetActive(false);

            if (skillUIPanel != null && skillUIPanel.activeSelf)
            {
                skillUIPanel.SetActive(false);
                Time.timeScale = 1f;
                Player.isAnyUIOpen = false;
            }

            if (currentEffectCoroutine != null) StopCoroutine(currentEffectCoroutine);
            InitializeEffects();
        }
    }

    private IEnumerator ActivateEffectsOverTime(bool isAppearing)
    {
        float elapsedTime = 0f;
        float startAlpha = childContent2Renderer != null ? childContent2Renderer.color.a : (isAppearing ? 0f : 1f);
        float targetAlpha = isAppearing ? 1f : 0f;
        float startEmission = isAppearing ? 0f : targetEmissionRate;
        float targetEmission = isAppearing ? targetEmissionRate : 0f;

        if (isAppearing && effectParticleSystem != null) effectParticleSystem.Play();

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsedTime / fadeDuration);

            if (childContent2Renderer != null)
            {
                Color c = childContent2Renderer.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, normalizedTime);
                childContent2Renderer.color = c;
            }

            if (effectParticleSystem != null)
                emissionModule.rateOverTime = Mathf.Lerp(startEmission, targetEmission, normalizedTime);

            yield return null;
        }

        if (!isAppearing && effectParticleSystem != null) effectParticleSystem.Stop();
    }

    private void InitializeEffects()
    {
        if (childContent2Renderer != null)
        {
            Color c = childContent2Renderer.color;
            c.a = 0f;
            childContent2Renderer.color = c;
        }
        if (effectParticleSystem != null)
        {
            emissionModule.rateOverTime = 0f;
            effectParticleSystem.Stop();
        }
    }
}
