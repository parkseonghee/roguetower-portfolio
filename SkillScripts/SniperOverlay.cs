using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 저격 모드 활성화 시 화면을 어둡게 깔고 마우스 위치에 스코프 커서를 표시.
/// DimPanel의 Image에 UI/DimWithHole 셰이더 머티리얼을 붙이면 마우스 주변이 원형으로 뚫림.
/// 입력 처리(좌클릭/우클릭/ESC)는 SkillManager가 담당.
/// </summary>
public class SniperOverlay : MonoBehaviour
{
    public static SniperOverlay Instance { get; private set; }

    [Header("UI 참조")]
    [Tooltip("화면 전체를 덮을 어두운 반투명 패널 (RectTransform)")]
    public RectTransform dimPanel;

    [Tooltip("마우스 위치를 따라다닐 스코프 커서 (RectTransform)")]
    public RectTransform scopeCursor;

    [Header("스코프 구멍 효과 (UI/DimWithHole 셰이더 필요)")]
    [Tooltip("DimPanel의 Image. 머티리얼이 UI/DimWithHole 셰이더면 마우스 따라 구멍이 뚫림. 비워두면 단순 어둠 효과만 적용")]
    public Image dimImage;

    [Tooltip("구멍 반지름 (화면 높이의 비율, 0.08 = 화면 높이의 8%)")]
    [Range(0.01f, 0.5f)] public float holeRadius = 0.08f;

    [Tooltip("구멍 경계 부드러운 정도")]
    [Range(0.0f, 0.1f)] public float softEdge = 0.02f;

    private static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenterUV");
    private static readonly int HoleRadiusID = Shader.PropertyToID("_HoleRadius");
    private static readonly int SoftEdgeID = Shader.PropertyToID("_SoftEdge");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            gameObject.SetActive(false);
            return;
        }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        UpdateScopePosition();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateScopePosition();
        UpdateHoleShader();
    }

    private void UpdateScopePosition()
    {
        if (scopeCursor != null)
            scopeCursor.position = Input.mousePosition;
    }

    private void UpdateHoleShader()
    {
        if (dimImage == null || dimImage.material == null) return;

        Vector2 mouseUV = new Vector2(
            Input.mousePosition.x / Screen.width,
            Input.mousePosition.y / Screen.height
        );

        dimImage.material.SetVector(HoleCenterID, mouseUV);
        dimImage.material.SetFloat(HoleRadiusID, holeRadius);
        dimImage.material.SetFloat(SoftEdgeID, softEdge);
    }
}
