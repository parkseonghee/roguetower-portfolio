using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스킬 정보를 보여주는 고정 위치 툴팁. 캔버스 자식으로 두고 인스펙터에서 위치를 잡으면 됨.
/// E 키로 열기 / ESC 키로 닫기. 해금된 레벨은 흰색, 해금 안 된 레벨은 회색으로 표시.
/// </summary>
public class SkillTooltip : MonoBehaviour
{
    public static SkillTooltip Instance { get; private set; }

    [Header("UI 참조")]
    public RectTransform panelRect;
    public TextMeshProUGUI textLabel;

    [Header("닫기 키 (열기 키는 SkillManager가 처리)")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("색상")]
    public Color unlockedColor = Color.white;
    public Color lockedColor = new Color(0.55f, 0.55f, 0.55f);

    private SkillData currentSkill;
    private bool subscribed = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            return;
        }
        Instance = this;

        if (panelRect == null) panelRect = GetComponent<RectTransform>();

        // 툴팁 자체가 마우스 이벤트를 가로채면 호버 깜빡임 발생 → 모든 Graphic 레이캐스트 끔
        foreach (var g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// ESC 처리는 LateUpdate에서. Stat.cs의 일시정지 토글이 Update에서 ESC를 먼저 보는데,
    /// Show 시점에 Player.isAnyUIOpen=true로 잠궈두면 Stat이 일찍 return하므로
    /// LateUpdate에서 안전하게 Hide() 호출 가능.
    /// </summary>
    private void LateUpdate()
    {
        if (!gameObject.activeSelf) return;
        if (Input.GetKeyDown(closeKey))
            Hide();
    }

    public void Show(SkillData skill)
    {
        if (skill == null || textLabel == null) return;
        currentSkill = skill;
        textLabel.text = BuildText(skill);
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        Player.isAnyUIOpen = true;
        TrySubscribe();
    }

    public void Hide()
    {
        TryUnsubscribe();
        currentSkill = null;
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        Player.isAnyUIOpen = false;
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
        if (currentSkill != null && textLabel != null && gameObject.activeSelf)
            textLabel.text = BuildText(currentSkill);
    }

    private string BuildText(SkillData skill)
    {
        var sb = new StringBuilder();
        string unlockedHex = ColorUtility.ToHtmlStringRGB(unlockedColor);
        string lockedHex = ColorUtility.ToHtmlStringRGB(lockedColor);

        sb.Append("<b>").Append(string.IsNullOrEmpty(skill.skillName) ? skill.skillId : skill.skillName).Append("</b>");

        if (!string.IsNullOrEmpty(skill.description))
            sb.Append('\n').Append(skill.description);

        if (skill.levels != null && skill.levels.Length > 0)
        {
            int currentLv = SkillManager.Instance != null ? SkillManager.Instance.GetHighestUnlockedLevel(skill.skillId) : 0;

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
                sb.Append("</color>");
                sb.Append("\n  ");
            }
        }

        return sb.ToString();
    }
}
