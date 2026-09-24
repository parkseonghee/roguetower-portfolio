using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Store : MonoBehaviour
{
    private struct StoreItem
    {
        public string itemID;
        public int price;
    }

    [Header("상점 버튼 설정")]
    public RectTransform[] buttons; // 3개의 버튼 전체 (움직임용)
    public RectTransform[] buttons2; // 3개의 버튼 (클릭/비활성화용)
    public float upDistance = 100f;
    public float speed = 10f;
    private Vector2[] startPos;
    private bool isUp = false;
    private bool isPlayerNearStore = false;

    private StoreItem[] rolledItems;

    // 🌟 프리팹 대응: 인스펙터에 넣지 않고, 충돌할 때 자동으로 찾습니다.
    private Weapon attackScript;

    private bool hasRolledItems = false;

    [Header("상점 충돌체 및 시각적 요소")]
    public Collider2D StoreCollider;
    public SpriteRenderer spriteRenderer;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("아이템 아이콘 및 UI 설정")]
    public Image[] rewardIconImages;

    public Image[] rewardBackgroundImages;
    public Sprite[] tierBackgroundSprites;
    public TextMeshProUGUI[] priceTexts;

    void Start()
    {
        startPos = new Vector2[buttons.Length];
        rolledItems = new StoreItem[buttons2.Length];

        // 🌟 프리팹 환경이므로 Start()에서는 플레이어를 찾지 않습니다. (에러 방지)

        for (int i = 0; i < buttons.Length; i++)
            startPos[i] = buttons[i].anchoredPosition;

        if (StoreCollider != null)
            StoreCollider.enabled = false;
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    void Update()
    {
        // 🌟 상점이 닫혀있고, 시간이 멈춰있다면 무시 (다른 UI가 켜져서 멈춘 상태)
        if (Time.timeScale == 0f && !isUp) return;

        if (isPlayerNearStore && Input.GetKeyDown(KeyCode.F))
        {
            if (!isUp)
            {
                // 🌟 [상점 열기] 다른 UI(인벤토리, 상자 등)가 열려있으면 상점을 열지 못하게 방어!
                if (Player.isAnyUIOpen) return;

                isUp = true;
                Player.isAnyUIOpen = true; // 이제 다른 UI 못 열게 잠금

                Time.timeScale = 0f;

                // 한 번도 아이템을 뽑지 않았을 때만 뽑기 실행
                if (!hasRolledItems)
                {
                    RollRandomItems();
                    hasRolledItems = true;
                }

                // 상점이 열리면 무기 끄기
                if (attackScript != null) attackScript.enabled = false;
            }
            else
            {
                // 🌟 [상점 닫기] 열려있는 상태에서 F키를 다시 누르면 안전하게 닫기 함수 호출
                CloseStore();
            }
        }

        // 상점창 부드럽게 올라오는 로직
        for (int i = 0; i < buttons.Length; i++)
        {
            Vector2 targetPos = isUp ? startPos[i] + new Vector2(0, upDistance) : startPos[i];
            buttons[i].anchoredPosition = Vector2.Lerp(buttons[i].anchoredPosition, targetPos, Time.unscaledDeltaTime * speed);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearStore = true;

            // 🌟 [삭제 완료] 무기 스크립트를 찾아서 끄는 로직은 이제 필요 없습니다!
            // 상점 UI가 열리면 Weapon.cs가 알아서 멈춥니다.
        }
    }
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.CompareTag("Player"))
    //    {
    //        isPlayerNearStore = true;

    //        // 🌟 확실한 방법: Player 스크립트를 먼저 찾고, 그 안의 attackScript를 가져옵니다!
    //        if (attackScript == null)
    //        {
    //            // 1. 충돌한 오브젝트에서 Player 스크립트를 찾습니다.
    //            Player playerComponent = collision.GetComponent<Player>();

    //            // (만약 충돌체가 자식 오브젝트에 있어서 못 찾았다면 부모에서 찾습니다)
    //            if (playerComponent == null)
    //            {
    //                playerComponent = collision.GetComponentInParent<Player>();
    //            }

    //            // 2. Player 스크립트를 성공적으로 찾았다면?
    //            if (playerComponent != null)
    //            {
    //                // Player 스크립트 안에 이미 연결된 attackScript를 그대로 가져옵니다.
    //                attackScript = playerComponent.attackScript;
    //                Debug.Log("✅ 상점: Player를 통해 무기 스크립트 완벽 연결!");
    //            }
    //            else
    //            {
    //                Debug.LogWarning("❌ 상점: Player 스크립트를 찾지 못했습니다.");
    //            }
    //        }
    //    }
    //}

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) isPlayerNearStore = false;
    }

    public void OpenStore()
    {
        if (StoreCollider != null) StoreCollider.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    public void CloseStore()
    {
        isUp = false;
        Time.timeScale = 1f;

        // 🌟 상점이 닫히면 다른 UI를 열 수 있게 잠금 해제!
        Player.isAnyUIOpen = false;

        // 상점 닫기 함수 호출 시 무기 다시 켜기
        if (attackScript != null) attackScript.enabled = true;
    }

    private void RollRandomItems()
    {
        // 1. 전체 아이템 ID 리스트를 가져옵니다.
        List<string> availableIDs = ItemDatabase.Instance.GetAllItemIDs();

        // 아예 데이터베이스가 비어있다면 에러 로그만 띄우고 종료합니다.
        if (availableIDs == null || availableIDs.Count == 0)
        {
            Debug.LogError("데이터베이스에 불러올 아이템이 하나도 없습니다!");
            return;
        }

        // 2. 상점 슬롯(4개)만큼 반복문을 돕니다.
        for (int i = 0; i < buttons2.Length; i++)
        {
            // 🌟 핵심 해결책: 남은 아이템이 부족하면 빈 슬롯을 끄고 건너뜁니다!
            if (availableIDs.Count == 0)
            {
                if (buttons2[i] != null) buttons2[i].gameObject.SetActive(false);
                continue;
            }

            // 슬롯에 아이템이 들어갈 수 있다면 버튼을 다시 켜줍니다. (재사용 시 필요)
            if (buttons2[i] != null) buttons2[i].gameObject.SetActive(true);

            int rolledTier = GetRandomTier();

            List<string> filteredIDs = new List<string>();
            foreach (string id in availableIDs)
            {
                ItemData data = ItemDatabase.Instance.GetItem(id);
                if (data != null && data.tier == rolledTier)
                {
                    filteredIDs.Add(id);
                }
            }

            if (filteredIDs.Count == 0)
            {
                filteredIDs = availableIDs;
            }

            int randomIndex = Random.Range(0, filteredIDs.Count);
            string selectedID = filteredIDs[randomIndex];
            availableIDs.Remove(selectedID);

            ItemData itemData = ItemDatabase.Instance.GetItem(selectedID);
            int calculatedPrice = itemData != null ? CalculatePriceByTier(itemData.tier) : 0;

            rolledItems[i] = new StoreItem { itemID = selectedID, price = calculatedPrice };

            if (rewardIconImages.Length > i && rewardIconImages[i] != null)
            {
                Sprite loadedSprite = Resources.Load<Sprite>($"Icons/{selectedID}");
                rewardIconImages[i].sprite = loadedSprite;
                rewardIconImages[i].color = new Color(1, 1, 1, 1f);
            }

            if (rewardBackgroundImages.Length > i && rewardBackgroundImages[i] != null && itemData != null)
            {
                int tierIndex = itemData.tier - 1;
                if (tierIndex >= 0 && tierIndex < tierBackgroundSprites.Length)
                {
                    rewardBackgroundImages[i].sprite = tierBackgroundSprites[tierIndex];
                }
            }

            if (priceTexts != null && priceTexts.Length > i && priceTexts[i] != null)
            {
                priceTexts[i].text = calculatedPrice.ToString() + " G";
            }

            ItemSlotHover hoverScript = buttons2[i].GetComponent<ItemSlotHover>();
            if (hoverScript != null)
            {
                hoverScript.SetItemID(selectedID);
            }
        }
    }

    private int GetRandomTier()
    {
        // 1부터 100까지 무작위 숫자 뽑기
        int roll = Random.Range(1, 101);

        // 상점 원본 확률 적용 (총합 100%)
        if (roll <= 65) return 1;      // 65% 확률 (1 ~ 65)
        if (roll <= 90) return 2;      // 25% 확률 (66 ~ 90)
        if (roll <= 98) return 3;      // 8% 확률 (91 ~ 98)

        return 4;                      // 2% 확률 (99 ~ 100)
    }

    private int CalculatePriceByTier(int tier)
    {
        switch (tier)
        {
            case 1: return Random.Range(100, 151);
            case 2: return Random.Range(250, 351);
            case 3: return Random.Range(500, 651);
            case 4: return 800;
            default: return 100;
        }
    }

    public void WeaPonChoice(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= rolledItems.Length) return;

        string chosenItemID = rolledItems[buttonIndex].itemID;
        int itemPrice = rolledItems[buttonIndex].price;

        // 🌟 중복 구매 1차 방어: 이미 구매 완료되어 비어있는 슬롯이면 즉시 종료
        if (string.IsNullOrEmpty(chosenItemID)) return;

        ItemData data = ItemDatabase.Instance.GetItem(chosenItemID);
        if (data == null) return;

        // 🌟 이 부분도 프리팹 환경에서 동적으로 찾도록 안전하게 되어 있습니다!
        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        if (player == null) return;

        if (player.SpendScore(itemPrice))
        {
            bool isAdded = InventoryManager.Instance.AddItem(chosenItemID);

            if (isAdded)
            {
                Debug.Log($"[구매 완료] {data.itemName}을(를) {itemPrice}G에 획득했습니다!");

                // 🌟 [핵심 버그 픽스] 구매 성공 즉시 배열에서 아이템 ID를 제거하여 연타 무효화
                rolledItems[buttonIndex].itemID = "";

                if (buttons2.Length > buttonIndex && buttons2[buttonIndex] != null)
                {
                    buttons2[buttonIndex].gameObject.SetActive(false);
                }

                if (tooltipPanel != null) tooltipPanel.SetActive(true);
                if (tooltipText != null)
                {
                    tooltipText.text = "구매 성공!";
                    StartCoroutine(HideTooltipAfterDelay(2f));
                }
            }
            else
            {
                // 인벤토리가 꽉 차서 실패한 경우 (돈 환불)
                player.AddScore(itemPrice);

                if (tooltipPanel != null) tooltipPanel.SetActive(true);
                if (tooltipText != null)
                {
                    tooltipText.text = "인벤토리 창을 비우세요!";
                    StartCoroutine(HideTooltipAfterDelay(2f));
                }
            }
        }
        else
        {
            Debug.Log($"구매 실패: 점수가 부족하여 아이템을 살 수 없습니다.");
        }
    }

    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }
}