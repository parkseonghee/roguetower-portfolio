using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Chest : MonoBehaviour
{
    [Header("UI 설정")]
    public RectTransform[] buttons;
    public float upDistance = 100f;
    public float speed = 10f;

    [Header("아이콘 및 텍스트 연결")]
    [Tooltip("버튼 순서와 동일하게 아이템 이미지를 띄울 UI Image 3개를 연결하세요.")]
    public Image[] rewardIconImages;

    public TextMeshProUGUI[] rewardNameTexts;
    public TextMeshProUGUI[] rewardDescTexts;

    [Header("경고 메시지 UI")]
    public GameObject tooltipPanel2;
    public TextMeshProUGUI tooltipText2;

    [Header("사운드 설정")]
    public AudioSource audioSource;
    public AudioClip openSound;

    private Vector2[] startPos;
    private bool isUp = false;
    private bool isPlayerNearBox = false;
    public Collider2D ChestCollider;
    public SpriteRenderer spriteRenderer;

    private string[] rolledItemIDs;

    // 🌟 [핵심] 상자를 닫았다 열어도 아이템이 다시 바뀌지 않게 기억하는 스위치!
    private bool hasRolledItems = false;

    private Weapon playerAttackScript;

    void Start()
    {
        startPos = new Vector2[buttons.Length];
        rolledItemIDs = new string[buttons.Length];

        if (ChestCollider != null) ChestCollider.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        for (int i = 0; i < buttons.Length; i++)
        {
            startPos[i] = buttons[i].anchoredPosition;

            if (rewardNameTexts.Length > i && rewardNameTexts[i] != null)
                rewardNameTexts[i].text = "";

            if (rewardDescTexts.Length > i && rewardDescTexts[i] != null)
                rewardDescTexts[i].text = "";
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearBox = true;

            // 🌟 [삭제 완료] 무기 스크립트를 찾아서 연결하는 로직 제거
            // 상자 UI가 열리면 Player.isAnyUIOpen이 true가 되어 Weapon.cs가 알아서 멈춥니다.
        }
    }
    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.CompareTag("Player"))
    //    {
    //        isPlayerNearBox = true;

    //        // Player 스크립트를 통해 무기 스크립트 연결
    //        if (playerAttackScript == null)
    //        {
    //            Player playerComponent = collision.GetComponent<Player>();
    //            if (playerComponent == null)
    //            {
    //                playerComponent = collision.GetComponentInParent<Player>();
    //            }

    //            if (playerComponent != null)
    //            {
    //                playerAttackScript = playerComponent.attackScript;
    //            }
    //        }
    //    }
    //}

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerNearBox = false;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f && !isUp) return;

        if (isPlayerNearBox && Input.GetKeyDown(KeyCode.F))
        {
            if (!isUp)
            {
                // [상자 열기]
                if (Player.isAnyUIOpen) return; // 다른 UI가 열려있으면 무시

                isUp = true;
                Player.isAnyUIOpen = true;
                Time.timeScale = 0f;

                if (playerAttackScript != null) playerAttackScript.enabled = false;

                if (audioSource != null && openSound != null)
                {
                    audioSource.PlayOneShot(openSound);
                }

                // 🌟 한 번도 뽑지 않았을 때만 뽑기를 실행합니다.
                if (!hasRolledItems)
                {
                    RollRandomItems();
                    hasRolledItems = true; // 스위치를 켜서 다시는 안 뽑게 만듦
                }
            }
            else
            {
                // [상자 닫기]
                CloseChest();
            }
        }

        // UI 부드럽게 올라오는 로직
        for (int i = 0; i < buttons.Length; i++)
        {
            Vector2 targetPos = isUp ? startPos[i] + new Vector2(0, upDistance) : startPos[i];
            buttons[i].anchoredPosition = Vector2.Lerp(buttons[i].anchoredPosition, targetPos, Time.unscaledDeltaTime * speed);
        }
    }

    private void RollRandomItems()
    {
        // 상자 버튼 개수(보통 3개)만큼 뽑힌 아이템을 임시 저장할 리스트 (중복 방지용)
        List<string> pickedItems = new List<string>();

        for (int i = 0; i < buttons.Length; i++)
        {
            string randomID = "";
            int failSafe = 0; // 무한 루프 방지용

            // 🌟 [확률 적용] ItemDatabase에 확률대로 뽑아달라고 요청합니다. 
            do
            {
                randomID = ItemDatabase.Instance.GetRandomItemIDByTierProbability();
                failSafe++;

                // 만약 이미 뽑은 아이템이라면 다시 뽑습니다. (50번 넘게 실패하면 그냥 넘어감)
            } while (pickedItems.Contains(randomID) && failSafe < 50);

            pickedItems.Add(randomID);
            rolledItemIDs[i] = randomID;

            // -------------------- 이하 기존 UI 세팅 코드 유지 --------------------

            if (rewardIconImages.Length > i && rewardIconImages[i] != null)
            {
                Sprite loadedSprite = Resources.Load<Sprite>($"Icons/{rolledItemIDs[i]}");
                rewardIconImages[i].sprite = loadedSprite;
                rewardIconImages[i].color = new Color(1, 1, 1, 1f);
            }

            ItemData itemData = ItemDatabase.Instance.GetItem(rolledItemIDs[i]);
            if (itemData != null)
            {
                if (rewardNameTexts.Length > i && rewardNameTexts[i] != null)
                {
                    rewardNameTexts[i].text = itemData.itemName;
                }
            }

            if (buttons.Length > i && buttons[i] != null)
            {
                ItemSlotHover hoverScript = buttons[i].GetComponent<ItemSlotHover>();
                if (hoverScript != null)
                {
                    hoverScript.SetItemID(rolledItemIDs[i]);
                }
            }
        }
    }

    public void WeaPonChoice(int buttonIndex)
    {
        string chosenItemID = rolledItemIDs[buttonIndex];
        if (string.IsNullOrEmpty(chosenItemID)) return;

        bool isAdded = InventoryManager.Instance.AddItem(chosenItemID);

        if (isAdded)
        {
            CloseChest();

            // 아이템을 먹었을 때만 상자 외형 지우기
            if (ChestCollider != null) ChestCollider.enabled = false;
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }
        else
        {
            if (tooltipPanel2 != null) tooltipPanel2.SetActive(true);
            if (tooltipText2 != null)
            {
                tooltipText2.text = "인벤토리 창을 비우세요!";
                StartCoroutine(HideTooltipAfterDelay(2f));
            }
        }
    }

    public void Cansle()
    {
        CloseChest();
    }

    public void OpenChest()
    {
        if (ChestCollider != null) ChestCollider.enabled = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
    }

    public void CloseChest()
    {
        isUp = false;
        Time.timeScale = 1f;

        Player.isAnyUIOpen = false; // UI 잠금 해제

        if (playerAttackScript != null)
        {
            playerAttackScript.enabled = true;
        }
    }

    private IEnumerator HideTooltipAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (tooltipPanel2 != null) tooltipPanel2.SetActive(false);
    }
}