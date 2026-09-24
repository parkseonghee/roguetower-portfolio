using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum SlotType { Inventory, Weapon, Armor, Accessory }

public class InventorySlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("슬롯 설정")]
    public SlotType mySlotType;
    public string currentItemID = ""; // 🌟 0 대신 빈 문자열("")로 초기화합니다.

    [Header("UI 연결")]
    public Image itemIcon; // (Raycast Target 무조건 OFF)

    // 🌟 빈칸 검사 로직 변경: 0인지 검사하는 대신, 글자가 비어있는지 검사합니다.
    public bool IsEmpty() { return string.IsNullOrEmpty(currentItemID); }

    private void Start()
    {
        SetItem(currentItemID);
    }

    // 매니저가 스왑을 허락하면, ID 글자가 바뀌고 알아서 이미지를 새로 그립니다.
    public void SetItem(string id)
    {
        currentItemID = id;

        // 🌟 [핵심 추가] 현재 슬롯에 붙어있는 ItemSlotHover 스크립트를 찾아 ID를 넘겨줍니다!
        // 이렇게 하면 상점/상자처럼 마우스를 올렸을 때 자동으로 아이템 정보를 인식합니다.
        ItemSlotHover hoverScript = GetComponent<ItemSlotHover>();
        if (hoverScript != null)
        {
            hoverScript.SetItemID(id);
        }

        // 1. ID가 비어있으면 즉시 이미지를 끄고 나감
        if (string.IsNullOrEmpty(id))
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
            return; // 여기서 종료
        }

        // 2. 리소스 로드 시도
        Sprite loadedSprite = Resources.Load<Sprite>($"Icons/{id}");

        if (loadedSprite != null)
        {
            itemIcon.sprite = loadedSprite;
            itemIcon.enabled = true;
            itemIcon.color = Color.white; // 투명도나 색상이 변했을 수 있으니 초기화
        }
        else
        {
            // 3. ID는 있는데 이미지를 못 찾았다면? (이게 하얀 칸의 원인!)
            Debug.LogWarning($"{id} 에 해당하는 이미지를 찾을 수 없습니다! 경로를 확인하세요.");
            itemIcon.sprite = null;
            itemIcon.enabled = false; // 이미지가 없으면 차라리 안 보이게 함
        }
    }

    // 🌟 좌클릭, 우클릭 판독기
    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsEmpty()) return;

        // 클릭 버튼에 따라 매니저의 기능(뼈대)을 정확히 호출합니다.
        if (eventData.button == PointerEventData.InputButton.Left)
            InventoryManager.Instance.OnSlotLeftClicked(this);
        else if (eventData.button == PointerEventData.InputButton.Right)
            InventoryManager.Instance.HandleRightClick(this);
    }

    // =====================================
    // 고스트 드래그 (시각 효과 전용) - 원본 유지
    // =====================================
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. [핵심] 슬롯이 비어있다면 아예 드래그 연출 자체를 시작하지 않음
        if (IsEmpty())
        {
            // 빈 슬롯은 드래그 이벤트가 무시되도록 설정
            eventData.pointerDrag = null;
            return;
        }

        // 2. 아이템이 있을 때만 소리 재생
        InventoryManager.Instance.PlaySound(InventoryManager.Instance.grabSound);

        // 3. 아이템이 있을 때만 투명도 조절 및 고스트 아이콘 활성화
        itemIcon.color = new Color(1, 1, 1, 0.5f);

        Image ghost = InventoryManager.Instance.ghostIcon;
        ghost.sprite = itemIcon.sprite;
        ghost.gameObject.SetActive(true);
        ghost.transform.position = Input.mousePosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // [추가] 빈 슬롯이면 드래그 로직을 완전히 무시
        if (IsEmpty()) return;

        // 인스턴스나 고스트 아이콘이 null인지 한 번 더 체크 (방어적 프로그래밍)
        if (InventoryManager.Instance != null && InventoryManager.Instance.ghostIcon != null)
        {
            InventoryManager.Instance.ghostIcon.transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsEmpty()) return;

        Debug.Log("드래그 종료됨"); // 로그 1
        if (InventoryManager.Instance != null && InventoryManager.Instance.ghostIcon != null)
        {
            InventoryManager.Instance.ghostIcon.gameObject.SetActive(false);
            Debug.Log("고스트 아이콘 비활성화 시도함"); // 로그 2
        }

        itemIcon.color = new Color(1, 1, 1, 1f);

        // 1. 마우스를 뗐을 때 그 위치에 어떤 UI가 있는지 확인
        GameObject droppedOn = eventData.pointerEnter;
        bool isOutsideInventory = false;

        // 2. 마우스 아래에 아무 UI도 없다면? (순수하게 게임 필드/잔디밭 위라면)
        if (droppedOn == null)
        {
            isOutsideInventory = true;
        }
        else
        {
            if (InventoryManager.Instance.inventoryPanel != null)
            {   // 3. 마우스 아래에 UI가 있긴 한데, 그게 '인벤토리 패널(갈색 창)' 소속이 아니라면?
                Transform inventoryTransform = InventoryManager.Instance.inventoryPanel.transform;

                // 닿은 UI가 인벤토리 패널 본인이 아니거나, 인벤토리 패널의 자식(슬롯 등)도 아니라면 밖으로 간주!
                if (!droppedOn.transform.IsChildOf(inventoryTransform) && droppedOn.transform != inventoryTransform)
                {
                    isOutsideInventory = true;
                }
            }
        }

        // 4. 최종적으로 인벤토리 밖으로 판정되었다면 버리기 팝업 띄우기
        if (isOutsideInventory)
        {
            InventoryManager.Instance.RequestDropItem(this);
        }
    }

    // 🌟 떨어뜨리는 순간! 
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            InventorySlot fromSlot = eventData.pointerDrag.GetComponent<InventorySlot>();

            if (fromSlot != null && fromSlot != this && !fromSlot.IsEmpty())
            {
                // 드롭을 받는 순간 일단 아이콘부터 끕니다.
                if (InventoryManager.Instance.ghostIcon != null)
                    InventoryManager.Instance.ghostIcon.gameObject.SetActive(false);

                InventoryManager.Instance.HandleDragAndDrop(fromSlot, this);
            }
        }
    }
}