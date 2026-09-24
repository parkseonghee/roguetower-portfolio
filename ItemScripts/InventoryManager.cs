using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // List 사용을 위해 추가

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("UI 패널 & 드래그 연출")]
    public GameObject inventoryPanel;
    public Image ghostIcon;

    [Header("슬롯 등록 (현재 맵의 슬롯들)")]
    public InventorySlot weaponSlot;
    public InventorySlot ArmorSlot;
    public InventorySlot[] accessorySlots;
    public InventorySlot[] inventorySlots;

    [Header("외부 스크립트 연결")]
    public Weapon attackScript;
    public Player playerScript;
    public Transform playerWeaponTransform;

    [Header("버리기 확인 팝업 UI")]
    public GameObject dropConfirmPanel;
    public Button btnYes;
    public Button btnNo;

    [Header("사운드 설정")]
    public AudioSource inventoryAudioSource;
    public AudioClip grabSound;
    public AudioClip equipSound;

    private InventorySlot slotPendingDrop;
    private bool isInventoryOpen = false;

    // =========================================================
    // ☁️ [핵심] 클라우드 저장소 (static 변수는 씬이 바뀌어도 파괴되지 않음!)
    // =========================================================
    public static bool hasSavedData = false; // 저장된 데이터가 있는지 확인
    public static string cloudWeapon = "";
    public static string cloudArmor = "";
    public static List<string> cloudAccessories = new List<string>();
    public static List<string> cloudInventory = new List<string>();

    private void Awake()
    {
        Instance = this;
        // 🌟 DontDestroyOnLoad 삭제! 이제 이 매니저와 UI는 씬이 끝나면 쿨하게 파괴됩니다.
    }

    private void Start()
    {
        if (btnYes != null)
        {
            btnYes.onClick.RemoveAllListeners();
            btnYes.onClick.AddListener(ConfirmDropItem);
        }
        if (btnNo != null)
        {
            btnNo.onClick.RemoveAllListeners();
            btnNo.onClick.AddListener(CancelDropItem);
        }

        // 🌟 게임 시작 (또는 씬 로드) 시, 클라우드에 저장된 아이템이 있다면 새 UI에 불러옵니다.
        LoadDataFromCloud();

        ToggleInventory(false); // 인벤토리 닫고 시작
    }

    // 🌟 이 씬이 끝나고 매니저가 파괴되기 직전에 자동으로 실행되는 함수
    private void OnDestroy()
    {
        // 파괴되기 전에 내가 가진 모든 아이템을 클라우드에 백업해 둡니다!
        SaveDataToCloud();
    }

    // =================================------------------------------------
    // ☁️ 클라우드 저장 / 불러오기 로직
    // =================================------------------------------------
    private void SaveDataToCloud()
    {
        hasSavedData = true;
        cloudWeapon = weaponSlot != null ? weaponSlot.currentItemID : "";
        cloudArmor = ArmorSlot != null ? ArmorSlot.currentItemID : "";

        cloudAccessories.Clear();
        foreach (var slot in accessorySlots)
        {
            if (slot != null) cloudAccessories.Add(slot.currentItemID);
        }

        cloudInventory.Clear();
        foreach (var slot in inventorySlots)
        {
            if (slot != null) cloudInventory.Add(slot.currentItemID);
        }

        Debug.Log("☁️ [클라우드 백업 완료] 다음 맵으로 데이터를 안전하게 전송합니다.");
    }

    private void LoadDataFromCloud()
    {
        if (!hasSavedData) return; // 처음 게임을 시작해서 저장된 게 없다면 무시

        if (weaponSlot != null) weaponSlot.SetItem(cloudWeapon);
        if (ArmorSlot != null) ArmorSlot.SetItem(cloudArmor);

        for (int i = 0; i < accessorySlots.Length && i < cloudAccessories.Count; i++)
        {
            if (accessorySlots[i] != null) accessorySlots[i].SetItem(cloudAccessories[i]);
        }

        for (int i = 0; i < inventorySlots.Length && i < cloudInventory.Count; i++)
        {
            if (inventorySlots[i] != null) inventorySlots[i].SetItem(cloudInventory[i]);
        }

        // 아이템을 다 꽂아 넣었으니 스탯도 다시 계산해서 플레이어에게 적용!
        UpdateEquipmentStats();
        Debug.Log("☁️ [클라우드 다운로드 완료] 이전 맵의 아이템을 성공적으로 불러왔습니다.");
    }

    // =================================------------------------------------
    // 아래 V키 작동, 클릭, 스왑, 스탯 적용 로직은 기존과 100% 동일합니다.
    // =================================------------------------------------
    private void Update()
    {
        if (Stat.instance != null && Stat.instance.PausePanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.V))
        {
            if (!isInventoryOpen && Player.isAnyUIOpen) return;
            ToggleInventory(!isInventoryOpen);
        }
    }

    private void ToggleInventory(bool open)
    {
        if (inventoryPanel == null) return;

        isInventoryOpen = open;
        inventoryPanel.SetActive(open);

        Time.timeScale = open ? 0f : 1f;
        Player.isAnyUIOpen = open;

        if (attackScript != null)
        {
            attackScript.enabled = !open;
        }

        if (dropConfirmPanel != null) dropConfirmPanel.SetActive(false);
        slotPendingDrop = null;
    }

    public void PlaySound(AudioClip clip)
    {
        if (inventoryAudioSource != null && clip != null)
            inventoryAudioSource.PlayOneShot(clip);
    }

    public void OnSlotLeftClicked(InventorySlot clickedSlot)
    {
        if (string.IsNullOrEmpty(clickedSlot.currentItemID)) return;

        ItemData data = ItemDatabase.Instance.GetItem(clickedSlot.currentItemID);
        if (data != null)
        {
            Debug.Log($"[정보] 이름: {data.itemName} / 티어: {data.tier}");
        }
    }

    public bool HandleDragAndDrop(InventorySlot fromSlot, InventorySlot toSlot)
    {
        if (fromSlot == toSlot || string.IsNullOrEmpty(fromSlot.currentItemID)) return false;

        ItemData dragItem = ItemDatabase.Instance.GetItem(fromSlot.currentItemID);
        ItemData targetItem = !string.IsNullOrEmpty(toSlot.currentItemID) ? ItemDatabase.Instance.GetItem(toSlot.currentItemID) : null;

        if (IsValidEquip(toSlot, dragItem) && IsValidEquip(fromSlot, targetItem))
        {
            string tempID = fromSlot.currentItemID;
            fromSlot.SetItem(toSlot.currentItemID);
            toSlot.SetItem(tempID);

            UpdateEquipmentStats();

            if (toSlot.mySlotType != SlotType.Inventory || fromSlot.mySlotType != SlotType.Inventory)
            {
                PlaySound(equipSound);
            }

            return true;
        }
        if (ghostIcon != null) ghostIcon.gameObject.SetActive(false);
        return false;
    }

    private bool IsValidEquip(InventorySlot slot, ItemData data)
    {
        if (data == null) return true;

        if (slot.mySlotType != SlotType.Inventory && data.mainType == ItemMainType.Consumable) return false;
        if (slot.mySlotType == SlotType.Weapon && data.mainType != ItemMainType.Weapon) return false;
        if (slot.mySlotType == SlotType.Armor && data.mainType != ItemMainType.Armor) return false;
        if (slot.mySlotType == SlotType.Accessory && data.mainType != ItemMainType.Accessory) return false;

        return true;
    }

    public void HandleRightClick(InventorySlot clickedSlot)
    {
        if (string.IsNullOrEmpty(clickedSlot.currentItemID)) return;

        ItemData item = ItemDatabase.Instance.GetItem(clickedSlot.currentItemID);
        if (item == null) return;

        if (item.mainType == ItemMainType.Consumable)
        {
            UseConsumable(clickedSlot, item);
            return;
        }

        if (clickedSlot.mySlotType == SlotType.Inventory) EquipItem(clickedSlot, item);
        else UnequipItem(clickedSlot);
    }

    private void UseConsumable(InventorySlot slot, ItemData item)
    {
        if (playerScript != null && item.hpFlat > 0)
        {
            playerScript.ChangeHp(item.hpFlat);
        }
        slot.SetItem("");
        UpdateEquipmentStats();
    }

    private void EquipItem(InventorySlot inventorySlot, ItemData item)
    {
        if (item.mainType == ItemMainType.Weapon)
            HandleDragAndDrop(inventorySlot, weaponSlot);
        else if (item.mainType == ItemMainType.Armor)
        {
            if (ArmorSlot != null) HandleDragAndDrop(inventorySlot, ArmorSlot);
        }
        else if (item.mainType == ItemMainType.Accessory)
        {
            foreach (InventorySlot accSlot in accessorySlots)
            {
                if (string.IsNullOrEmpty(accSlot.currentItemID)) { HandleDragAndDrop(inventorySlot, accSlot); return; }
            }
            if (accessorySlots.Length > 0) HandleDragAndDrop(inventorySlot, accessorySlots[0]);
        }
    }

    private void UnequipItem(InventorySlot equipSlot)
    {
        foreach (InventorySlot invSlot in inventorySlots)
        {
            if (string.IsNullOrEmpty(invSlot.currentItemID)) { HandleDragAndDrop(equipSlot, invSlot); return; }
        }
    }

    public void UpdateEquipmentStats()
    {
        float totalAtk = 0f, totalAtkSpd = 0f, totalHpFlat = 0f, totalMovSpd = 0f, totalDef = 0f;

        void AccumulateStats(string itemID)
        {
            if (string.IsNullOrEmpty(itemID)) return;
            ItemData data = ItemDatabase.Instance.GetItem(itemID);
            if (data != null)
            {
                totalAtk += data.atk;
                totalAtkSpd += data.atkSpd;
                totalHpFlat += data.hpFlat;
                totalMovSpd += data.movSpd;
                totalDef += data.def;
            }
        }

        AccumulateStats(weaponSlot.currentItemID);
        if (ArmorSlot != null) AccumulateStats(ArmorSlot.currentItemID);
        foreach (InventorySlot accSlot in accessorySlots) AccumulateStats(accSlot.currentItemID);

        if (!string.IsNullOrEmpty(weaponSlot.currentItemID))
        {
            ItemData currentWeapon = ItemDatabase.Instance.GetItem(weaponSlot.currentItemID);
            if (playerWeaponTransform != null && currentWeapon != null)
            {
                if (currentWeapon.effectID == "GiantWeapon") playerWeaponTransform.localScale = new Vector3(3f, 3f, 3f);
                else playerWeaponTransform.localScale = new Vector3(2f, 2f, 2f);
            }
        }
        else if (playerWeaponTransform != null) playerWeaponTransform.localScale = new Vector3(2f, 2f, 2f);

        // 🌟 [수정] attackScript ➔ playerScript (또는 player)
        if (playerScript != null)
        {
            // 🌟 [수정] WeaponBonusDamage ➔ WeaponBonusAtk
            playerScript.WeaponBonusAtk = totalAtk;

            // 🌟 [수정] WeaponbonusAttackSpeed ➔ WeaponBonusAtkSpeed
            playerScript.WeaponBonusAtkSpeed = totalAtkSpd;
        }

        if (playerScript != null)
        {
            float hpDifference = totalHpFlat - playerScript.WeaponbonusMaxHp;
            playerScript.WeaponbonusMaxHp = totalHpFlat;
            playerScript.currentHp += hpDifference;
            playerScript.WeaponbonusMoveSpeed = totalMovSpd;

            float maxHpLimit = playerScript.baseMaxHp + playerScript.bonusMaxHp + playerScript.WeaponbonusMaxHp;
            if (playerScript.currentHp > maxHpLimit) playerScript.currentHp = maxHpLimit;
            if (playerScript.currentHp <= 0) playerScript.currentHp = 1;
        }
    }

    public bool HasItem(string targetItemID)
    {
        if (weaponSlot != null && weaponSlot.currentItemID == targetItemID) return true;
        if (ArmorSlot != null && ArmorSlot.currentItemID == targetItemID) return true;
        foreach (InventorySlot slot in accessorySlots) if (slot != null && slot.currentItemID == targetItemID) return true;
        foreach (InventorySlot slot in inventorySlots) if (slot != null && slot.currentItemID == targetItemID) return true;
        return false;
    }

    public bool AddItem(string itemID)
    {
        if (ItemDatabase.Instance == null) return false;
        ItemData data = ItemDatabase.Instance.GetItem(itemID);
        if (data == null) return false;
        if (inventorySlots == null || inventorySlots.Length == 0) return false;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;
            if (inventorySlots[i].IsEmpty())
            {
                inventorySlots[i].SetItem(itemID);
                return true;
            }
        }
        return false;
    }

    public void RequestDropItem(InventorySlot slot)
    {
        if (string.IsNullOrEmpty(slot.currentItemID)) return;
        slotPendingDrop = slot;
        dropConfirmPanel.SetActive(true);
    }

    private void ConfirmDropItem()
    {
        if (slotPendingDrop != null && !string.IsNullOrEmpty(slotPendingDrop.currentItemID))
        {
            slotPendingDrop.SetItem("");
            UpdateEquipmentStats();
        }
        CancelDropItem();
    }

    private void CancelDropItem()
    {
        slotPendingDrop = null;
        dropConfirmPanel.SetActive(false);
    }

    public void DropItemFromSlot(InventorySlot slot)
    {
        if (string.IsNullOrEmpty(slot.currentItemID)) return;
        slot.SetItem("");
        UpdateEquipmentStats();
    }

    public bool HasEquippedEffect(string targetEffectID)
    {
        if (ItemDatabase.Instance == null) return false;

        // 검사할 장착 슬롯들을 임시 배열로 묶습니다.
        InventorySlot[] equipSlots = new InventorySlot[2 + accessorySlots.Length];
        equipSlots[0] = weaponSlot;
        equipSlots[1] = ArmorSlot;
        for (int i = 0; i < accessorySlots.Length; i++)
        {
            equipSlots[2 + i] = accessorySlots[i];
        }

        // 장착된 아이템들의 데이터를 하나씩 까서 effectID를 확인합니다.
        foreach (InventorySlot slot in equipSlots)
        {
            if (slot != null && !string.IsNullOrEmpty(slot.currentItemID))
            {
                ItemData data = ItemDatabase.Instance.GetItem(slot.currentItemID);

                // 🌟 장착된 아이템의 effectID가 내가 찾는 효과("Vampire" 등)와 똑같다면 true 반환!
                if (data != null && data.effectID == targetEffectID)
                {
                    return true;
                }
            }
        }

        return false;
    }
}