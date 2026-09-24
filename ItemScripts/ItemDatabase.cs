using System;
using System.Collections.Generic;
using UnityEngine;

// 1. 카테고리 정의 (CSV의 MainType 컬럼 내용과 정확히 일치해야 함)
public enum ItemMainType { Weapon, Armor, Accessory, Consumable }

// 2. CSV 구조와 100% 일치하는 아이템 데이터 그릇
[System.Serializable]
public class ItemData
{
    public string itemID;        // ItemID (ex: wpn_greatsword_1)
    public ItemMainType mainType; // MainType (Weapon, Armor 등)
    public string subType;       // SubType (Greatsword 등)
    public string itemName;      // ItemName
    public int tier;             // Tier (1, 2 등 정수)
    public string description;   // Description

    // 실제 스탯 수치들 (CSV 컬럼과 동일)
    public float atk;            // ATK
    public float atkSpd;         // ATKSPD
    public float hpFlat;         // HP_Flat (고정 체력 증가)
    public float hpPct;          // HP_Pct (퍼센트 체력 증가)
    public float def;            // DEF
    public float movSpd;         // MovSPD

    public string effectID;      // EFF (특수효과 식별자, ex: "NOT EFF")
    public string ability;
}

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Tooltip("MySQL에서 뽑아낸 ItemData.csv 파일을 여기에 넣으세요")]
    public TextAsset csvFile;

    private Dictionary<string, ItemData> itemDB = new Dictionary<string, ItemData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 🌟 씬이 넘어가도 파괴되지 않게 보호합니다!
            LoadCSV();                     // 최초 1회만 불러옵니다.
        }
        else
        {
            Destroy(gameObject); // 중복 생성 방지
        }
    }

   private void LoadCSV()
    {
        // 🌟 1. 파일이 안 들어가 있으면 빨간 에러 띄우기
        if (csvFile == null) 
        {
            Debug.LogError("❌ [ItemDatabase] CSV 파일이 연결되지 않았습니다! 인스펙터 창에서 Csv File 칸에 파일을 넣어주세요.");
            return;
        }

        string[] lines = csvFile.text.Replace("\r", "").Split('\n');
        
        // 🌟 2. 파일은 찾았는데 몇 줄인지 확인하기
        Debug.Log($"✅ [ItemDatabase] 파일을 찾았습니다! 전체 줄 수: {lines.Length}줄");

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] row = lines[i].Split(',');

            // 🌟 3. 데이터가 14칸으로 안 쪼개져서 스킵되는 경우 경고 띄우기
            if (row.Length < 14) 
            {
                Debug.LogWarning($"⚠️ [ItemDatabase] {i}번째 줄 파싱 실패! (현재 {row.Length}칸 / 필요 14칸) 내용: {lines[i]}");
                continue;
            }
            if (string.IsNullOrWhiteSpace(row[0]) || string.IsNullOrWhiteSpace(row[1])) continue;

            ItemData newItem = new ItemData
            {
                itemID = row[0],
                mainType = (ItemMainType)Enum.Parse(typeof(ItemMainType), row[1].Trim().Trim('"'), true),
                subType = row[2],
                itemName = row[3].Trim('"'), 
                tier = int.Parse(row[4]),
                description = row[5],

                atk = float.Parse(row[6]),
                atkSpd = float.Parse(row[7]),
                hpFlat = float.Parse(row[8]),
                hpPct = float.Parse(row[9]),
                def = float.Parse(row[10]),
                movSpd = float.Parse(row[11]),

                effectID = row[12].Trim('"'),
                ability = row[13].Trim('"')
            };

            if (!itemDB.ContainsKey(newItem.itemID))
            {
                itemDB.Add(newItem.itemID, newItem);
            }
        }
        Debug.Log($"[아이템 로드 완료] 총 {itemDB.Count}개의 아이템을 성공적으로 딕셔너리에 담았습니다.");
    }

    public ItemData GetItem(string id)
    {
        if (itemDB.TryGetValue(id, out ItemData data))
            return data;

        return null; // 없는 아이템일 경우 null 반환
    }
    // 🌟 [추가된 함수] 데이터베이스에 있는 모든 아이템 ID를 리스트로 뽑아줍니다.
    public List<string> GetAllItemIDs()
    {
        // itemDB(딕셔너리)가 가지고 있는 모든 Key(아이템 ID)를 리스트로 만들어 반환합니다.
        return new List<string>(itemDB.Keys);
    }


    public string GetRandomItemIDByTierProbability()
    {
        // 1. 1부터 100까지의 무작위 숫자(퍼센트)를 뽑습니다.
        int rand = UnityEngine.Random.Range(1, 101);
        int selectedTier = 1;

        // 2. 상점과 동일한 원본 확률 적용 (총합 100%)
        if (rand <= 65)
        {
            selectedTier = 1; // 65% 확률 (1 ~ 65)
        }
        else if (rand <= 90)
        {
            selectedTier = 2; // 25% 확률 (66 ~ 90)
        }
        else if (rand <= 98)
        {
            selectedTier = 3; // 8% 확률 (91 ~ 98)
        }
        else
        {
            selectedTier = 4; // 2% 확률 (99 ~ 100)
        }

        // 3. 딕셔너리에서 해당 티어(selectedTier)를 가진 아이템만 리스트로 추려냅니다.
        List<string> tierItems = new List<string>();
        foreach (var item in itemDB.Values)
        {
            if (item.tier == selectedTier)
            {
                tierItems.Add(item.itemID);
            }
        }

        // [안전 장치] 만약 CSV에 해당 티어 아이템이 하나도 없을 경우
        if (tierItems.Count == 0)
        {
            Debug.LogWarning($"⚠️ [ItemDatabase] {selectedTier}티어 아이템이 없어서 아무거나 줍니다!");
            List<string> allIDs = new List<string>(itemDB.Keys);
            return allIDs[UnityEngine.Random.Range(0, allIDs.Count)];
        }

        // 4. 추려낸 해당 티어의 아이템 리스트 중에서 무작위로 1개를 골라서 아이템 ID를 반환합니다.
        int randomIndex = UnityEngine.Random.Range(0, tierItems.Count);
        return tierItems[randomIndex];
    }

}