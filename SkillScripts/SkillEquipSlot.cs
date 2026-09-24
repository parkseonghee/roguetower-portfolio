using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캔버스 오른쪽 가운데 끝에 배치되는 장착 슬롯. 장착된 스킬의 아이콘을 표시.
/// 레벨이 해금되면 해당 레벨의 아이콘으로 자동 교체 + "Lv2" 같은 텍스트 표시.
/// </summary>
[RequireComponent(typeof(Image))]
public class SkillEquipSlot : MonoBehaviour
{
    [Header("아이콘이 표시될 Image")]
    public Image iconImage;

    [Header("레벨 표시 텍스트 (선택). 비워두면 표시 안함")]
    public TextMeshProUGUI levelLabel;

    [Header("Lv 표시 포맷 ({0} = 레벨 숫자)")]
    public string levelFormat = "Lv{0}";

    [Header("쿨타임 표시")]
    [Tooltip("쿨타임 동안 아이콘을 덮을 반투명 Image. Image Type=Filled, Fill Method=Radial 360 권장")]
    public Image cooldownOverlay;

    [Tooltip("쿨타임 남은 초를 표시할 텍스트 (없으면 표시 생략)")]
    public TextMeshProUGUI cooldownLabel;

    private bool subscribed = false;

    private void Reset()
    {
        iconImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (iconImage == null) iconImage = GetComponent<Image>();
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && SkillManager.Instance != null)
        {
            SkillManager.Instance.OnStateChanged -= Refresh;
        }
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (SkillManager.Instance == null) return;
        SkillManager.Instance.OnStateChanged += Refresh;
        subscribed = true;
        Refresh();
    }

    public void Refresh()
    {
        if (iconImage == null) return;

        SkillData equipped = SkillManager.Instance != null ? SkillManager.Instance.GetEquippedSkill() : null;

        if (equipped == null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            if (levelLabel != null) levelLabel.text = "";
            SetCooldownVisuals(0f, 0f);
            return;
        }

        SkillLevel currentLv = SkillManager.Instance.GetCurrentLevelData(equipped);

        // 아이콘: 현재 레벨 아이콘이 설정돼 있으면 그것, 없으면 SkillData 기본 아이콘
        Sprite spriteToShow = (currentLv != null && currentLv.icon != null) ? currentLv.icon : equipped.icon;
        if (spriteToShow != null)
        {
            iconImage.sprite = spriteToShow;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        // Lv 텍스트: 해금된 레벨이 있을 때만 표시
        if (levelLabel != null)
            levelLabel.text = currentLv != null ? string.Format(levelFormat, currentLv.level) : "";
    }

    private void Update()
    {
        if (SkillManager.Instance == null) return;
        SkillData equipped = SkillManager.Instance.GetEquippedSkill();
        if (equipped == null) { SetCooldownVisuals(0f, 0f); return; }

        SkillLevel lv = SkillManager.Instance.GetCurrentLevelData(equipped);
        float total = lv != null ? lv.cooldownSeconds : 0f;
        float remaining = SkillManager.Instance.GetCooldownRemaining(equipped.skillId);
        SetCooldownVisuals(remaining, total);
    }

    private void SetCooldownVisuals(float remaining, float total)
    {
        bool active = remaining > 0f && total > 0f;

        if (cooldownOverlay != null)
        {
            if (cooldownOverlay.gameObject.activeSelf != active)
                cooldownOverlay.gameObject.SetActive(active);
            if (active) cooldownOverlay.fillAmount = remaining / total;
        }

        if (cooldownLabel != null)
        {
            if (cooldownLabel.gameObject.activeSelf != active)
                cooldownLabel.gameObject.SetActive(active);
            if (active)
                cooldownLabel.text = remaining >= 1f
                    ? Mathf.CeilToInt(remaining).ToString()
                    : remaining.ToString("F1");
        }
    }
}
