using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemInfoManager : MonoBehaviour
{
    public static ItemInfoManager Instance;

    [Header("상세 정보 패널 설정")]
    public GameObject infoPanel;

    [Header("상세 정보 UI 연결")]
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemStatsText;
    public TextMeshProUGUI itemAbilityText; // 고유 능력 전용 텍스트
    public Image itemIconImage;

    [Header("티어별 이름 색상 설정")]
    public Color tier1Color = new Color32(205, 127, 50, 255);
    public Color tier2Color = new Color32(192, 192, 192, 255);
    public Color tier3Color = new Color32(255, 215, 0, 255);
    public Color tier4Color = new Color32(150, 230, 255, 255);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        infoPanel.SetActive(false);
    }

    public void OpenPanel(string itemID, Transform slotTransform, Vector3 customOffset)
    {
        ItemData data = ItemDatabase.Instance.GetItem(itemID);
        if (data == null) return;

        // 1. 아이템 이름 및 티어 색상
        itemNameText.text = data.itemName;
        switch (data.tier)
        {
            case 1: itemNameText.color = tier1Color; break;
            case 2: itemNameText.color = tier2Color; break;
            case 3: itemNameText.color = tier3Color; break;
            case 4: itemNameText.color = tier4Color; break;
            default: itemNameText.color = Color.white; break;
        }

        // 2. 아이콘 설정
        if (itemIconImage != null)
        {
            Sprite loadedSprite = Resources.Load<Sprite>($"Icons/{itemID}");
            if (loadedSprite != null)
            {
                itemIconImage.sprite = loadedSprite;
                itemIconImage.enabled = true;
            }
            else itemIconImage.enabled = false;
        }

        // 3. 스탯 텍스트 설정 (색상 및 +,- 기호 자동 적용)
        string stats = "";

        // 🌟 수정완료: 수치가 양수(>0)일 때만 "+"를 붙이고, 음수면 숫자 자체의 "-"를 그대로 씁니다.
        if (data.atk != 0) stats += $"공격력: <color=#FF6347>{(data.atk > 0 ? "+" : "")}{data.atk}</color>\n";
        if (data.atkSpd != 0) stats += $"공격속도: <color=#FFD700>{(data.atkSpd > 0 ? "+" : "")}{data.atkSpd}</color>\n";
        if (data.hpFlat != 0) stats += $"체력: <color=#FF4500>{(data.hpFlat > 0 ? "+" : "")}{data.hpFlat}</color>\n";
        if (data.hpPct != 0) stats += $"체력(%): <color=#FF4500>{(data.hpPct > 0 ? "+" : "")}{data.hpPct * 100}%</color>\n";
        if (data.def != 0) stats += $"방어력: <color=#1E90FF>{(data.def > 0 ? "+" : "")}{data.def}</color>\n";
        if (data.movSpd != 0) stats += $"이동속도: <color=#00FFFF>{(data.movSpd > 0 ? "+" : "")}{data.movSpd}</color>\n";

        itemStatsText.text = stats;

        // 4. 고유 능력(Ability) 전용 텍스트 설정 (항상 켜두기)
        if (itemAbilityText != null)
        {
            itemAbilityText.gameObject.SetActive(true);

            // CSV에 글자가 비어있을 경우 에러 방지용으로 기본 문구를 넣어줍니다.
            string abilityDesc = string.IsNullOrEmpty(data.ability) ? "해당 아이템의 능력은 아직 없습니다." : data.ability;

            itemAbilityText.text = $"<color=#F0E68C><b>[고유 능력]</b></color>\n<size=85%>{abilityDesc}</size>";
        }

        // 5. 위치 잡기 및 활성화
        infoPanel.SetActive(true);
        infoPanel.transform.position = slotTransform.position + customOffset;

        // 레이아웃 강제 새로고침
        Canvas.ForceUpdateCanvases();
    }

    public void ClosePanel()
    {
        infoPanel.SetActive(false);
    }
}