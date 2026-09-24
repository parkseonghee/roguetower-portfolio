using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// 스킬 아이콘 클릭 시 열리는 상세 패널. 아이콘, 설명 텍스트, 구매/장착 버튼을 한 번에 보여준다.
/// 여러 스킬에서 재사용되며 호출자 측에서 Show(SkillData) 로 대상 스킬을 지정한다.
/// </summary>
public class SkillDetailPanel : MonoBehaviour
{
    public static SkillDetailPanel Instance { get; private set; }

    [Header("UI 참조")]
    public GameObject panelRoot;            // 켜고 끌 루트 (보통 자기 자신 또는 자식 패널)
    public Image iconImage;                 // 스킬 아이콘 표시
    public TextMeshProUGUI descriptionText; // 스킬 이름 + 설명 + 레벨 정보
    public SkillShopButton shopButton;      // 구매/장착 버튼

    [Header("색상 (해금/잠금 텍스트 색)")]
    public Color unlockedColor = Color.white;
    public Color lockedColor = new Color(0.55f, 0.55f, 0.55f);

    [Header("열릴 때 게임 일시정지")]
    public bool pauseOnOpen = true;

    private SkillData currentSkill;
    private Sprite currentIconOverride;
    private bool subscribed = false;

    // 패널 열 때의 이전 일시정지/UI 상태. 닫을 때 복원하기 위함 (상위 UI가 이미 멈춰뒀을 수 있음)
    private float prevTimeScale = 1f;
    private bool prevUIOpen = false;
    private bool pauseStateSaved = false;

    /// <summary>패널이 어떤 스킬을 보여주는 상태로 바뀔 때 호출. 닫히면 null이 들어옴.</summary>
    public event System.Action<SkillData> OnPanelStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            return;
        }
        Instance = this;

        if (panelRoot == null) panelRoot = gameObject;
        panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        TryUnsubscribe();
    }

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public SkillData CurrentSkill => currentSkill;

    /// <summary>해당 스킬로 패널을 연다. 이미 같은 스킬이 열려있으면 닫는다 (토글).</summary>
    public void Toggle(SkillData skill, Sprite iconOverride = null)
    {
        if (skill == null) return;
        if (IsOpen && currentSkill == skill)
        {
            Hide();
            return;
        }
        Show(skill, iconOverride);
    }

    public void Show(SkillData skill, Sprite iconOverride = null)
    {
        if (skill == null || panelRoot == null) return;

        currentSkill = skill;
        currentIconOverride = iconOverride;
        ApplyVisuals();

        panelRoot.SetActive(true);

        if (pauseOnOpen && !pauseStateSaved)
        {
            prevTimeScale = Time.timeScale;
            prevUIOpen = Player.isAnyUIOpen;
            pauseStateSaved = true;
            Time.timeScale = 0f;
            Player.isAnyUIOpen = true;
        }

        TrySubscribe();
        OnPanelStateChanged?.Invoke(currentSkill);
    }

    public void Hide()
    {
        TryUnsubscribe();
        currentSkill = null;
        currentIconOverride = null;

        if (panelRoot != null) panelRoot.SetActive(false);

        if (pauseOnOpen && pauseStateSaved)
        {
            Time.timeScale = prevTimeScale;
            Player.isAnyUIOpen = prevUIOpen;
            pauseStateSaved = false;
        }

        OnPanelStateChanged?.Invoke(null);
    }

    private void ApplyVisuals()
    {
        if (currentSkill == null) return;

        // 아이콘: 현재 해금된 레벨의 아이콘이 있으면 그걸, 없으면 SkillData 기본 아이콘
        if (iconImage != null)
        {
            Sprite sprite = ResolveIconSprite(currentSkill);
            if (sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }
        }

        if (descriptionText != null)
            descriptionText.text = BuildText(currentSkill);

        if (shopButton != null)
        {
            shopButton.skill = currentSkill;
            shopButton.Refresh();
        }
    }

    private Sprite ResolveIconSprite(SkillData skill)
    {
        if (currentIconOverride != null) return currentIconOverride;
        if (skill == null) return null;
        if (SkillManager.Instance != null)
        {
            SkillLevel lv = SkillManager.Instance.GetCurrentLevelData(skill);
            if (lv != null && lv.icon != null) return lv.icon;
        }
        return skill.icon;
    }

    private void TrySubscribe()
    {
        if (subscribed || SkillManager.Instance == null) return;
        SkillManager.Instance.OnStateChanged += OnSkillStateChanged;
        subscribed = true;
    }

    private void TryUnsubscribe()
    {
        if (!subscribed) return;
        if (SkillManager.Instance != null)
            SkillManager.Instance.OnStateChanged -= OnSkillStateChanged;
        subscribed = false;
    }

    private void OnSkillStateChanged()
    {
        if (IsOpen && currentSkill != null) ApplyVisuals();
    }

    private string BuildText(SkillData skill)
    {
        var sb = new StringBuilder();
        string unlockedHex = ColorUtility.ToHtmlStringRGB(unlockedColor);
        string lockedHex = ColorUtility.ToHtmlStringRGB(lockedColor);

        sb.Append("<b>")
          .Append(string.IsNullOrEmpty(skill.skillName) ? skill.skillId : skill.skillName)
          .Append("</b>");

        if (!string.IsNullOrEmpty(skill.description))
            sb.Append('\n').Append(skill.description);

        if (skill.levels != null && skill.levels.Length > 0)
        {
            int currentLv = SkillManager.Instance != null
                ? SkillManager.Instance.GetHighestUnlockedLevel(skill.skillId)
                : 0;

            foreach (var lv in skill.levels)
            {
                if (lv == null) continue;
                bool unlocked = lv.level <= currentLv;
                string hex = unlocked ? unlockedHex : lockedHex;

                sb.Append("\n<color=#").Append(hex).Append('>');
                sb.Append("Lv").Append(lv.level);
                sb.Append(" (조건: 공격력 ").Append(lv.requiredAtk);
                sb.Append(" / 공속 ").Append(lv.requiredAtkSpeed).Append(')');
                if (!string.IsNullOrEmpty(lv.description))
                    sb.Append("\n  ").Append(lv.description);
                sb.Append("</color>").Append("\n  ");
            }
        }

        return sb.ToString();
    }
}
