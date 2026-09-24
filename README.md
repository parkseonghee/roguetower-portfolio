# 로그타워 (RogueTower) — 담당 시스템 포트폴리오

2D 탑다운 로그라이크 게임 **「로그타워」** 개발 중 제가 설계·구현한 시스템의 코드 모음입니다.
게임 전체 프로젝트가 아니라, **제가 담당한 파트의 스크립트만 발췌**해 정리한 저장소입니다.

---

## 1. 프로젝트 개요

| 항목 | 내용 |
|---|---|
| 장르 | 2D 탑다운 로그라이크 / 던전 크롤러 |
| 엔진 | Unity (C#) |
| 주요 패키지 | TextMesh Pro, Unity UI (EventSystem), NavMeshPlus(2D NavMesh), Cinemachine |
| 게임 구조 | 마을(Village) → 던전 1~9층(랜덤 맵) → 10층 보스(미노타우로스) |
| 핵심 루프 | 맵 입장 → 몬스터 전멸 → 보상(상자 / 상점 / 영구 포인트) → 다음 층 이동 |

플레이어는 층마다 무작위로 생성되는 방에서 몬스터를 전멸시키고, 그 보상으로 얻은 아이템과
골드로 캐릭터를 강화하며 탑을 올라갑니다. 층을 넘어가도 인벤토리와 스탯은 유지되고,
런이 끝나도 **영구 성장 포인트**는 `PlayerPrefs`에 저장되어 다음 런에 반영됩니다.

---

## 2. 담당 범위

제가 맡은 영역은 **"아이템 / 인벤토리 / 상점·상자" 재화 획득 루프**와
**"맵 진행 / 웨이브 클리어 / 씬 전환" 게임 진행 루프**입니다.

- 아이템 데이터 테이블 설계 및 런타임 로딩 (CSV → Dictionary)
- 티어 기반 아이템 확률 추첨 로직
- 인벤토리·장비 시스템 (드래그&드롭, 슬롯 타입 검증, 스탯 합산)
- 씬 전환 간 인벤토리 데이터 이관
- 아이템 상세 정보 툴팁 UI
- 상점(구매) / 상자(선택 보상) 상호작용
- 층 진행 및 맵 생성, 런타임 NavMesh 베이크, 몬스터 스폰
- 문(Door) 개방 조건 판정, 페이드 기반 씬 전환
- 영구 성장 포인트 저장/로드

> 플레이어 컨트롤(`Player`), 공격(`Weapon`), 몬스터 AI, 스탯 UI(`Stat`) 등은 팀원이 담당한 영역으로
> 이 저장소에는 포함되지 않습니다. 코드 내에서 해당 클래스는 참조로만 등장합니다.

---

## 3. 폴더 구조

```
roguetower-portfolio/
├── ItemScripts/                  # 아이템 · 인벤토리 코어
│   ├── ItemDatabase.cs           # CSV 파싱 → 아이템 DB(Dictionary), 티어 확률 추첨
│   ├── InventoryManager.cs       # 인벤토리/장비 총괄, 스탯 합산, 씬 간 데이터 이관
│   └── InventorySlot.cs          # 슬롯 단위 드래그&드롭 · 클릭 입력 처리
│
├── ManagerScript/                # 게임 진행 매니저
│   ├── MapManager.cs             # 층 수 관리, 일반맵/보스맵 선택, BGM 전환
│   ├── WaveManager.cs            # 맵 생성, NavMesh 베이크, 전멸 감지, 보상, 문 개방
│   ├── SpawnManager.cs           # 스폰 포인트 기반 몬스터 생성
│   └── MonsterSpawnPoint.cs      # 맵 프리팹에 배치하는 스폰 마커
│
├── MapScripts/                   # 맵 · 문 · 씬 전환
│   ├── Door.cs                   # 문 콜라이더/스프라이트 개폐
│   ├── DoorTrigger.cs            # F키 상호작용으로 다음 층 이동
│   ├── VillageDoor.cs            # 마을 → 던전 씬 이동
│   ├── SceneChange.cs            # 페이드 인/아웃 씬 전환 (SceneChanger)
│   ├── MapClearManager.cs        # 자식 몬스터 수 기반 클리어 판정 (보조)
│   ├── DemoDoorHandler.cs        # 데모 엔딩 트리거
│   └── PropsAltar.cs             # 제단 룬 발광 연출
│
├── Data/                         # 밸런스 데이터 테이블
│   ├── Charcter_data.json        # 플레이어 기본 스탯
│   └── MonsterStat.json          # 몬스터/보스 스탯
│
├── Store.cs                      # 상점 (골드 구매)
├── Chest.cs                      # 보상 상자 (3택 1)
├── ItemInfoManager.cs            # 아이템 상세 정보 패널(툴팁) 출력
└── ItemSlotHover.cs              # 마우스 호버 감지 → 툴팁 요청
```

---

## 4. 시스템 상세

### 4-1. 아이템 데이터베이스 — `ItemScripts/ItemDatabase.cs`

아이템 밸런싱을 코드가 아닌 **데이터로 분리**하기 위해, MySQL에서 관리하는 아이템 테이블을
CSV로 내보내 Unity `TextAsset`으로 읽어 들이는 구조를 택했습니다.

- CSV 14개 컬럼(`ItemID / MainType / SubType / ItemName / Tier / Description / ATK / ATKSPD / HP_Flat / HP_Pct / DEF / MovSPD / EFF / Ability`)을 `ItemData` 클래스에 1:1 매핑
- 파싱 결과를 `Dictionary<string, ItemData>`에 담아 **ID 기반 O(1) 조회**
- 싱글톤 + `DontDestroyOnLoad`로 **씬이 바뀌어도 단 한 번만 로드**
- 기획 데이터가 잘못 들어왔을 때를 대비한 방어 처리
  - 파일 미연결 / 컬럼 수 부족(14칸 미만) / 빈 줄 → 해당 줄만 건너뛰고 경고 출력 (게임은 중단되지 않음)

```csharp
public ItemData GetItem(string id)
    => itemDB.TryGetValue(id, out ItemData data) ? data : null;
```

### 4-2. 티어 확률 추첨

상점과 상자가 **동일한 확률표**를 쓰도록 추첨 로직을 `ItemDatabase`로 모았습니다.

| 티어 | 등장 확률 | 이름 색상 | 상점 가격 |
|---|---|---|---|
| 1 (동) | 65% | `#CD7F32` | 100 ~ 150 G |
| 2 (은) | 25% | `#C0C0C0` | 250 ~ 350 G |
| 3 (금) | 8% | `#FFD700` | 500 ~ 650 G |
| 4 (전설) | 2% | `#96E6FF` | 800 G (고정) |

- `Random.Range(1, 101)` 누적 구간 판정 방식으로 **확률 총합 100%** 보장
- 추첨된 티어에 해당하는 아이템이 CSV에 없는 경우 전체 풀에서 대체 지급하는 안전장치 포함

### 4-3. 인벤토리 / 장비 — `ItemScripts/InventoryManager.cs`, `InventorySlot.cs`

`InventorySlot`은 **입력 감지와 시각 표현만** 담당하고, 실제 판정과 데이터 변경은 전부
`InventoryManager`가 처리하도록 역할을 분리했습니다.

- **드래그 & 드롭 스왑** — `IBeginDragHandler` ~ `IDropHandler` 구현, 마우스를 따라다니는 고스트 아이콘 연출
- **슬롯 타입 검증**(`IsValidEquip`) — 무기 슬롯엔 무기만, 방어구 슬롯엔 방어구만.
  A→B, B→A 양방향을 모두 검사해 **스왑 시 잘못된 장착이 생기지 않도록** 처리
- **우클릭 퀵 장착/해제** — 인벤토리에서 우클릭 시 알맞은 장비 슬롯으로, 장비창에서 우클릭 시 빈 인벤토리 칸으로
- **소비 아이템** — 우클릭 즉시 사용 후 슬롯 비우기
- **아이콘 로딩** — `Resources.Load<Sprite>("Icons/" + itemID)`. 아이템 ID를 그대로 파일명 규칙으로 사용해 별도 매핑 테이블이 필요 없음
- **버리기** — 인벤토리 패널 **바깥**에 드롭했는지를 `Transform.IsChildOf`로 판정 → 확인 팝업 후 삭제
- **스탯 합산**(`UpdateEquipmentStats`) — 장착 중인 무기/방어구/장신구의 스탯을 전부 누적해 `Player`에 반영.
  최대 체력 증감분(`hpDifference`)만큼 현재 체력을 함께 보정하고, 상한 초과 및 0 이하를 클램프
- **특수 효과 조회**(`HasEquippedEffect`) — 장착 아이템의 `effectID`를 검사해, 다른 시스템(예: 흡혈)이 질의할 수 있는 인터페이스 제공

### 4-4. 씬 전환 간 인벤토리 이관

층을 이동할 때마다 UI와 매니저가 파괴되어 **획득한 아이템이 전부 사라지는 문제**가 있었습니다.
`DontDestroyOnLoad`로 UI 전체를 유지하면 씬별 UI 레이아웃이 깨지고 슬롯 참조가 꼬였습니다.

→ **매니저와 UI는 씬과 함께 파괴하고, 아이템 ID만 `static` 저장소에 백업**하는 방식으로 해결했습니다.

```csharp
private void OnDestroy() => SaveDataToCloud();   // 씬 종료 직전 백업
private void Start()     => LoadDataFromCloud(); // 새 씬 UI에 복원 후 스탯 재계산
```

`static` 필드는 씬 로드에 영향받지 않으므로, 무거운 GameObject를 살려두지 않고 데이터만 안전하게
다음 씬으로 넘길 수 있습니다. 복원 직후 `UpdateEquipmentStats()`를 호출해 플레이어 스탯까지 그대로 이어지게 했습니다.

### 4-5. 아이템 정보 툴팁 — `ItemInfoManager.cs`, `ItemSlotHover.cs`

- 인벤토리·상점·상자 등 **서로 다른 슬롯 프리팹이 하나의 정보 패널을 공유**하도록 `ItemInfoManager`를 싱글톤으로 구성
- 툴팁 표시 위치는 슬롯 프리팹마다 `myTooltipOffset`으로 인스펙터에서 조정 (화면 밖으로 잘리는 문제 대응)
- TMP 리치 텍스트로 스탯을 색상 구분 출력하고, 양수에만 `+`를 자동으로 붙임
- 티어에 따라 아이템 이름 색상을 변경, 고유 능력은 `[고유 능력]` 블록으로 분리 표기
- **호버 중에 창이 강제로 닫히면** 툴팁이 화면에 남는 버그가 있어, `OnDisable`에서 호버 상태를 확인해 함께 닫도록 처리

### 4-6. 상점 — `Store.cs`

- 근접 시 `F`키로 개폐, `Vector2.Lerp`로 버튼이 올라오는 연출 (`Time.unscaledDeltaTime` 사용 — `timeScale = 0`에서도 동작)
- 입장 시 1회만 품목을 추첨하고 `hasRolledItems`로 고정 → **닫았다 열어 리롤하는 어뷰징 차단**
- 같은 품목이 중복 진열되지 않도록 추첨된 ID를 후보 리스트에서 제거
- 티어별 배경 스프라이트와 가격 텍스트를 자동 세팅
- **중복 구매 버그 수정** — 버튼 연타 시 같은 아이템이 여러 번 결제되는 문제를,
  결제 성공 즉시 `rolledItems[i].itemID`를 비우고 버튼을 비활성화하는 2중 방어로 해결
- **골드 소실 방지** — 인벤토리가 가득 차 `AddItem`이 실패하면 `AddScore(itemPrice)`로 **전액 환불**하고
  "인벤토리 창을 비우세요!" 안내를 2초간 표시

### 4-7. 보상 상자 — `Chest.cs`

- 3개 후보 중 **1개만 선택**하는 로그라이크식 보상
- 확률 추첨은 `ItemDatabase`에 위임하고, 중복 방지를 `do-while` + `failSafe` 카운터(50회)로 처리해
  **아이템 풀이 작을 때 무한 루프에 빠지지 않도록** 방어
- 선택 성공 시에만 상자 콜라이더/스프라이트를 비활성화 (인벤토리 부족으로 실패하면 상자는 그대로 남음)

### 4-8. 층 진행 — `ManagerScript/MapManager.cs`

- `currentFloor`를 증가시키며 1~9층은 `mapPrefabs`에서 랜덤 선택, 10층은 `bossMapPrefab` 고정
- BGM은 `ChangeBGM()`에서 **같은 클립이면 교체하지 않고 유지** → 층을 넘어가도 음악이 끊기지 않고,
  10층 진입 순간에만 보스 BGM으로 전환

### 4-9. 웨이브 · 맵 생성 — `ManagerScript/WaveManager.cs`

게임 진행의 중심이 되는 스크립트입니다.

1. **`SpawnMap()`** — 이전 맵 파괴 → 새 맵 `Instantiate` → 문/상자/상점 참조 캐싱 → 문 전부 숨김 → 플레이어를 `PlayerSpawn`으로 이동
2. **`BakeNavMeshRoutine()`** — 런타임 NavMesh 베이크. 맵을 생성한 프레임에 바로 베이크하면
   Tilemap 콜라이더가 아직 갱신되지 않아 길이 잘못 뚫리는 문제가 있어, `WaitForEndOfFrame()`으로 한 프레임 양보한 뒤
   `navMeshSurface.BuildNavMesh()`를 호출하고, **베이크가 끝난 다음에야 몬스터를 스폰**하도록 순서를 고정
3. **`Update()`** — `Enemy` 태그 오브젝트가 0개가 되면 전멸로 판정
4. **`DelayedRewardAndOpenDoors()`** — 1초 연출 대기 후 보상 처리
   - 50% 확률로 상자 등장, 50% 확률로 상점 등장 (독립 판정이라 둘 다 / 하나만 / 없음 모두 가능)
   - 영구 포인트 +10 획득 및 UI 갱신
   - 문 개방
   - `isRewardWindowOpening` 플래그로 **보상창 중복 실행 차단**
5. **`UpdatePointUI()`** — 영구 포인트 100 도달 시 **스탯 포인트 1로 환산**하고 잔여 포인트를 이월.
   `PlayerPrefs`에 즉시 저장해 게임을 종료해도 성장이 유지됨

### 4-10. 문 · 씬 전환 — `MapScripts/Door.cs`, `DoorTrigger.cs`, `SceneChange.cs`

- `Door`는 콜라이더와 스프라이트를 함께 제어해, 미클리어 상태에서는 **보이지도 통과되지도 않게** 처리
- `DoorTrigger`는 `F`키 상호작용 방식. `entered` 플래그와 `Invoke(nameof(ResetTrigger), 2f)` 쿨타임으로 **연속 입력에 의한 중복 이동 방지**
- `SceneChanger`는 페이드 인/아웃을 코루틴으로 처리. 페이드 중에는 `timeScale = 0`이므로
  `Time.unscaledDeltaTime` / `WaitForSecondsRealtime`을 사용해 **시간이 멈춘 상태에서도 연출이 진행**되도록 구현
- `EnterDoorRoutine()`은 `암전 → 맵 이동 → 밝아짐` 순서를 보장해, 맵이 생성되는 과정이 플레이어에게 보이지 않게 함

### 4-11. 전역 UI 잠금

인벤토리·상점·상자가 동시에 열려 입력과 `timeScale`이 충돌하는 문제가 있었습니다.
`Player.isAnyUIOpen` 전역 플래그를 도입해, 각 UI가 열릴 때 잠금을 걸고 닫을 때 해제하도록 통일했습니다.
문 상호작용(`DoorTrigger`, `VillageDoor`)도 이 플래그를 확인하므로, **UI가 열린 상태에서는 맵 이동이 발생하지 않습니다.**

---

## 5. 데이터 테이블

밸런스 수치는 코드에서 분리해 JSON / CSV로 관리했습니다.

**`Data/Charcter_data.json`** — 플레이어 기본 스탯

| Player_ID | HP | ATK | DEF | MoveSpeed | Ranged | ATKSpeed | DashSpeed | DashCool |
|---|---|---|---|---|---|---|---|---|
| Normal_Player | 60 | 500 | 0 | 20 | 10 | 100 | 40 | 1 |

**`Data/MonsterStat.json`** — 몬스터 스탯 (전투 타입별 역할 분리)

| MonsterID | 이름 | CombatType | HP | ATK | AttackSpeed | MoveSpeed |
|---|---|---|---|---|---|---|
| `boss_mino_01` | 미노타우로스 | Boss | 1500 | 15 | 1.0 | 6 |
| `mob_fast_01` | 화이트 덕 | Fast | 30 | 7 | 1.3 | 8 |
| `mob_melee_01` | 블루 덕 | Melee | 60 | 9 | 1.0 | 6 |
| `mob_range_01` | 불 박쥐 | Ranged | 35 | 8 | 1.0 | 5 |
| `mob_tank_01` | 거북알도마뱀 | Tank | 80 | 12 | 1.0 | 4 |

---

## 6. 조작

| 키 | 동작 |
|---|---|
| `F` | 상호작용 (문 이동 / 상점·상자 열기 및 닫기) |
| `V` | 인벤토리 열기 / 닫기 |
| 좌클릭 드래그 | 아이템 이동 및 슬롯 교체 (패널 밖으로 드롭 → 버리기) |
| 우클릭 | 장착 / 해제 / 소비 아이템 사용 |
| 마우스 호버 | 아이템 상세 정보 표시 |

---

## 7. 트러블슈팅 요약

| 문제 | 원인 | 해결 |
|---|---|---|
| 층 이동 시 아이템 전부 소실 | 씬 전환으로 인벤토리 매니저·UI가 파괴됨 | `static` 저장소에 ID만 백업 → 새 씬에서 복원 (4-4) |
| 상점 버튼 연타로 중복 구매 | 결제 처리 후에도 같은 인덱스가 계속 유효 | 결제 성공 즉시 ID 비우기 + 버튼 비활성화 |
| 인벤토리가 가득 찬 상태로 구매 시 골드만 소진 | 결제 후 `AddItem` 실패를 처리하지 않음 | 실패 시 전액 환불 + 안내 문구 출력 |
| 상점·상자를 닫았다 열어 리롤 | 열 때마다 추첨을 실행 | `hasRolledItems` 플래그로 1회 고정 |
| 몬스터가 벽을 통과하거나 길을 못 찾음 | 맵 생성과 같은 프레임에 NavMesh를 베이크 | `WaitForEndOfFrame` 후 베이크, 완료 후 스폰 |
| 빈 슬롯을 드래그해도 고스트 아이콘이 생성 | 빈 슬롯도 드래그 이벤트를 수신 | `eventData.pointerDrag = null`로 이벤트 차단 |
| `timeScale = 0`에서 UI 연출·페이드가 정지 | `deltaTime`이 0 | `unscaledDeltaTime` / `WaitForSecondsRealtime` 사용 |
| UI 중첩으로 입력·시간 흐름 충돌 | UI마다 개별적으로 `timeScale`을 제어 | `Player.isAnyUIOpen` 전역 플래그로 통일 |
| 툴팁이 화면에 남음 | 호버 중 부모 UI가 강제 비활성화됨 | `OnDisable`에서 호버 상태 확인 후 패널 닫기 |
| 아이템 풀이 작을 때 중복 방지 루프가 멈추지 않음 | 조건 충족까지 무조건 재추첨 | `failSafe` 카운터(50회)로 탈출 보장 |

---

## 8. 설계 시 중점을 둔 부분

- **데이터와 로직의 분리** — 아이템·몬스터·플레이어 수치를 CSV/JSON으로 빼내, 코드 수정 없이 밸런싱이 가능한 구조
- **책임 분리** — 슬롯은 입력·표현만, 매니저는 판정·데이터 변경만 담당 (`InventorySlot` ↔ `InventoryManager`)
- **확률표 단일화** — 상점과 상자가 같은 추첨 함수를 사용해 밸런스 불일치 방지
- **방어적 프로그래밍** — `null` 체크, 데이터 결손 시 경고 로그, 무한 루프 탈출 카운터 등으로 잘못된 데이터가 들어와도 게임이 멈추지 않게 처리
- **프리팹 환경 대응** — 씬에 없을 수 있는 참조는 인스펙터 고정 대신 충돌·호출 시점에 동적으로 탐색

---

## 참고

- 이 저장소는 팀 프로젝트 「로그타워」에서 **제가 담당한 스크립트만 발췌**한 것으로, 단독 빌드는 불가능합니다.
- `.meta` 파일은 Unity 프로젝트에서의 원본 참조 관계를 보존하기 위해 함께 포함했습니다.
