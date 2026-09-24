using UnityEngine;
using UnityEngine.EventSystems; // UI 이벤트 시스템 사용을 위해 필수

// IPointerEnterHandler, IPointerExitHandler 인터페이스를 상속받아야 마우스를 인식합니다.
public class ItemSlotHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("현재 슬롯의 아이템 ID")]
    public string currentItemID;

    [Header("정보창 띄울 위치 (현재 슬롯 기준)")]
    [Tooltip("인벤, 상점 등 슬롯 프리팹마다 인스펙터에서 따로 X, Y 값을 설정하세요!")]
    public Vector3 myTooltipOffset = new Vector3(200, 0, 0); // 🌟 나만의 띄우기 좌표 추가!

    private bool isHovering = false;

    // 1. 마우스가 아이템 슬롯 위로 올라왔을 때 (자동 실행)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(currentItemID)) return; // 빈 슬롯이면 무시

        isHovering = true;

        // 🌟 수정완료: 변수 이름들을 현재 스크립트에 있는 진짜 이름으로 맞춰줬습니다!
        if (ItemInfoManager.Instance != null)
        {
            ItemInfoManager.Instance.OpenPanel(currentItemID, transform, myTooltipOffset);
        }
    }

    // 2. 마우스가 아이템 슬롯에서 벗어났을 때 (자동 실행)
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        // 🌟 마우스가 밖으로 나가면 상세 정보 패널을 닫습니다.
        if (ItemInfoManager.Instance != null)
        {
            ItemInfoManager.Instance.ClosePanel();
        }
    }

    // 3. 상자나 상점에서 랜덤 아이템을 뽑았을 때, 이 함수를 불러서 ID를 세팅해줍니다.
    public void SetItemID(string id)
    {
        currentItemID = id;
    }

    // 4. 슬롯 UI 자체가 꺼질 때 (예: 상점 창을 닫아버릴 때)
    private void OnDisable()
    {
        // 마우스가 올라가 있는 상태에서 창이 강제로 꺼졌다면 툴팁도 안전하게 같이 끕니다.
        if (isHovering)
        {
            isHovering = false;
            if (ItemInfoManager.Instance != null)
            {
                ItemInfoManager.Instance.ClosePanel();
            }
        }
    }
}