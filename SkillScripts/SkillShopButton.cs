using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스킬 구매/장착 토글 버튼. 한 버튼이 두 상태를 가짐:
/// - 미구매 → "구매" 표시, 클릭 시 WaveManager.stat에서 cost 차감 + 구매 등록
/// - 구매 후 → "장착" 표시, 클릭 시 SkillManager에 장착 등록
/// 현재 장착 중이면 버튼 비활성화 (다른 스킬을 장착하면 자동 활성화).
/// </summary>
public class SkillShopButton : MonoBehaviour
{
    [Header("이 버튼이 다루는 스킬")]
    public SkillData skill;

    [Header("UI 요소")]
    public Button button;
    public TextMeshProUGUI label;        // 버튼 상태 텍스트 (구매 / 장착 / 장착 중)
    public TextMeshProUGUI costLabel;    // 가격 표시 (선택)
    public TextMeshProUGUI costText;     // 가격 안내 텍스트 (선택, 구매 시 사라짐 - Cost와 동일 동작)
    public Image iconImage;              // 스킬 아이콘 (선택)

    [Header("표시 텍스트")]
    public string buyText = "구매";
    public string equipText = "장착";
    public string unequipText = "장착 해제";

    [Header("외부 참조 (자동 검색되지만 명시 권장)")]
    public WaveManager waveManager;

    private bool subscribed = false;
    private string costTextOriginal;     // costText의 원래 안내 문구 (미구매 시 복원용)
    private bool costTextCaptured = false;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void OnEnable()
    {
        if (waveManager == null)
            waveManager = Object.FindFirstObjectByType<WaveManager>();

        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (subscribed && SkillManager.Instance != null)
            SkillManager.Instance.OnStateChanged -= Refresh;
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

    private void OnClick()
    {
        if (skill == null || SkillManager.Instance == null) return;

        if (!SkillManager.Instance.IsPurchased(skill.skillId))
        {
            TryBuy();
        }
        else if (SkillManager.Instance.IsEquipped(skill.skillId))
        {
            SkillManager.Instance.Unequip();
        }
        else
        {
            SkillManager.Instance.Equip(skill);
        }
    }

    private void TryBuy()
    {
        if (waveManager == null)
        {
            Debug.LogWarning("[SkillShopButton] WaveManager를 찾을 수 없음.");
            return;
        }
        if (waveManager.stat < skill.cost)
        {
            Debug.Log("[SkillShopButton] 레벨 젬 부족");
            return;
        }

        waveManager.stat -= skill.cost;
        waveManager.UpdatePointUI();
        SkillManager.Instance.Purchase(skill);
    }

    public void Refresh()
    {
        if (skill == null || button == null) return;

        if (iconImage != null && skill.icon != null)
            iconImage.sprite = skill.icon;

        bool purchased = SkillManager.Instance != null && SkillManager.Instance.IsPurchased(skill.skillId);
        bool equipped = SkillManager.Instance != null && SkillManager.Instance.IsEquipped(skill.skillId);

        if (label != null)
            label.text = equipped ? unequipText : (purchased ? equipText : buyText);

        if (costLabel != null)
            costLabel.text = purchased ? "" : skill.cost.ToString();

        // costtext도 Cost처럼 구매 시 글자가 사라지고, 미구매 시 원래 문구로 복원.
        // Refresh가 Awake보다 먼저 호출될 수 있어, 원본 문구가 살아있는 첫 호출에서 한 번만 캐싱.
        if (costText != null)
        {
            if (!costTextCaptured)
            {
                costTextOriginal = costText.text;
                costTextCaptured = true;
            }
            costText.text = purchased ? "" : costTextOriginal;
        }

        button.interactable = true;
    }
}
