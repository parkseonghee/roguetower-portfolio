using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 이미지(BulletImage 등)에 부착. 이미지를 클릭하면 SkillDetailPanel을 토글한다.
/// - 같은 이미지를 다시 누르면 닫힘 / 다른 이미지를 누르면 그 스킬로 갈아끼움
/// - customIcon이 있으면 그 스프라이트를 패널에 표시
/// - 패널이 이 스킬을 보여주는 동안 Outline(노란 글로우)이 켜져 '읽는 중' 상태 표시
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SkillDetailToggle : MonoBehaviour, IPointerClickHandler
{
    [Header("이 이미지가 대표하는 스킬")]
    public SkillData skill;

    [Header("패널 참조 (비우면 SkillDetailPanel.Instance 사용)")]
    public SkillDetailPanel panel;

    [Header("패널에 표시할 아이콘 (비우면 SkillData 기본 아이콘 사용)")]
    public Sprite customIcon;

    [Header("선택 상태 글로우 (없으면 자동 추가)")]
    public Outline glowOutline;
    public Color glowColor = new Color(1f, 0.85f, 0.1f, 1f);
    public Vector2 glowDistance = new Vector2(6f, -6f);
    [Tooltip("알파 펄스 속도. 0이면 정적인 외곽선.")]
    public float glowPulseSpeed = 3f;
    [Range(0f, 1f)] public float glowMinAlpha = 0.5f;

    private bool subscribed = false;
    private SkillDetailPanel boundPanel;
    private bool isSelected = false;

    private void Awake()
    {
        var graphic = GetComponent<Graphic>();
        if (graphic != null) graphic.raycastTarget = true;

        if (glowOutline == null) glowOutline = GetComponent<Outline>();
        if (glowOutline == null) glowOutline = gameObject.AddComponent<Outline>();
        glowOutline.effectColor = glowColor;
        glowOutline.effectDistance = glowDistance;
        glowOutline.enabled = false;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        TryUnsubscribe();
        SetSelected(false);
    }

    private void Update()
    {
        if (!isSelected || glowOutline == null || glowPulseSpeed <= 0f) return;
        float t = (Mathf.Sin(Time.unscaledTime * glowPulseSpeed) + 1f) * 0.5f;
        float a = Mathf.Lerp(glowMinAlpha, 1f, t);
        var c = glowColor; c.a = a;
        glowOutline.effectColor = c;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (skill == null)
        {
            Debug.LogWarning($"[SkillDetailToggle] '{name}' 에 SkillData가 비어있음");
            return;
        }

        SkillDetailPanel target = panel != null ? panel : SkillDetailPanel.Instance;
        if (target == null)
        {
            Debug.LogWarning("[SkillDetailToggle] SkillDetailPanel을 찾을 수 없음 (씬에 배치돼있는지 확인)");
            return;
        }

        BindPanel(target);
        target.Toggle(skill, customIcon);
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        SkillDetailPanel target = panel != null ? panel : SkillDetailPanel.Instance;
        if (target == null) return;
        BindPanel(target);
    }

    private void TryUnsubscribe()
    {
        if (!subscribed || boundPanel == null) { subscribed = false; return; }
        boundPanel.OnPanelStateChanged -= OnPanelStateChanged;
        subscribed = false;
        boundPanel = null;
    }

    private void BindPanel(SkillDetailPanel target)
    {
        if (boundPanel == target && subscribed) return;
        TryUnsubscribe();
        boundPanel = target;
        boundPanel.OnPanelStateChanged += OnPanelStateChanged;
        subscribed = true;
        OnPanelStateChanged(boundPanel.CurrentSkill);
    }

    private void OnPanelStateChanged(SkillData shownSkill)
    {
        SetSelected(shownSkill != null && shownSkill == skill);
    }

    private void SetSelected(bool on)
    {
        isSelected = on;
        if (glowOutline == null) return;
        if (on)
        {
            glowOutline.effectColor = glowColor;
            glowOutline.effectDistance = glowDistance;
            glowOutline.enabled = true;
        }
        else
        {
            glowOutline.enabled = false;
        }
    }
}
