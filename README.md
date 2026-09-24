<div align="center">

# 🗼 로그타워 <sub>RogueTower</sub>

**2D 탑다운 로그라이크 — 담당 시스템 포트폴리오**

[![Unity](https://img.shields.io/badge/Unity-2D-000000?style=flat-square&logo=unity&logoColor=white)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=c-sharp&logoColor=white)](https://learn.microsoft.com/dotnet/csharp/)
[![Genre](https://img.shields.io/badge/Roguelike-8E44AD?style=flat-square)](#-프로젝트-요약)
[![Team](https://img.shields.io/badge/팀_프로젝트-담당_파트_발췌-F39C12?style=flat-square)](#-담당-범위)

### ▶️ 게임 플레이 영상

[![RogueTower 게임 플레이 영상](https://img.youtube.com/vi/-2gCSaoDLzc/hqdefault.jpg)](https://youtu.be/-2gCSaoDLzc)

<sub>🎬 썸네일 클릭 → YouTube로 이동</sub>

</div>

> 팀 프로젝트 「로그타워」에서 **제가 담당한 스크립트만 발췌**한 저장소입니다. 단독 빌드는 불가능합니다.

---

## 🎮 프로젝트 요약

|  | 내용 |
|---|---|
| 🕹️ **장르 / 엔진** | 2D 탑다운 로그라이크 · Unity (C#) |
| 📦 **주요 패키지** | TextMesh Pro · Unity UI(EventSystem) · NavMeshPlus(2D NavMesh) · Cinemachine |
| 🗺️ **구조** | 마을 → 던전 1~9층(랜덤 맵) → 10층 보스(미노타우로스) |
| 🔁 **핵심 루프** | 맵 입장 → 전멸 → 보상(🎁 상자 50% / 🏪 상점 50% / ⭐ 경험치 +10) → 다음 층 |
| 💾 **영속 데이터** | 인벤토리·스킬 장착·경험치·레벨 젬 (`PlayerPrefs` + `static`) |

**두 갈래 성장** — 런 중에는 아이템으로, 런을 넘어서는 **레벨 젬**으로 스킬을 구매해 강해집니다.

---

## 🙋 담당 범위

| 💰 재화 · 성장 루프 | 🗺️ 게임 진행 루프 |
|---|---|
| 아이템 데이터 테이블 (CSV → Dictionary) | 층 진행 · 맵 생성 |
| 티어 확률 추첨 (상점 / 상자 공용) | 런타임 NavMesh 베이크 |
| 인벤토리 · 장비 (드래그&드롭, 스탯 합산) | 스폰 포인트 기반 몬스터 생성 |
| 씬 전환 간 인벤토리 이관 | 문 개방 조건 판정 |
| 상점 · 상자 상호작용 | 페이드 씬 전환 |
| **스킬 구매 · 장착 · 자동 진화 · 발동** | 경험치 / 레벨 젬 영구 저장 |
| 아이템 · 스킬 툴팁 UI | 전역 UI 잠금 |

> [!NOTE]
> 플레이어 컨트롤(`Player`), 근접 공격(`Weapon`), 몬스터 AI(`MonsterBrain` 등)는 **팀원 담당**으로 포함되지 않았습니다. 코드에서 참조로만 등장합니다.

---

## 📂 폴더 구조

```
🎒 ItemScripts/     ItemDatabase · InventoryManager · InventorySlot
✨ SkillScripts/    SkillManager · SkillData · PlayerSkillBullet · PlayerSkillAura
                    SniperOverlay · SkillShopButton · SkillEquipSlot · SkillTooltip
                    SkillDetailPanel · SkillDetailToggle · SkillInteraction · SkillEquipPersist
🗺️ ManagerScript/   MapManager · WaveManager · SpawnManager · MonsterSpawnPoint
🚪 MapScripts/      Door · DoorTrigger · VillageDoor · SceneChange · MapClearManager
                    DemoDoorHandler · PropsAltar
📊 Data/            Charcter_data.json · MonsterStat.json
📄 ItemData 1.csv   아이템 30종 원본 테이블 (MySQL → CSV)
🏪 Store.cs  🎁 Chest.cs  💬 ItemInfoManager.cs  🖱️ ItemSlotHover.cs
```

---

## ⚙️ 시스템 요약

| 시스템 | 핵심 구현 |
|---|---|
| 📋 **아이템 DB** | MySQL → CSV → `TextAsset` → `Dictionary`로 **ID 기반 O(1) 조회**. 싱글톤+DDOL로 1회만 로드, 컬럼 결손 줄은 건너뛰고 경고 |
| 🎲 **티어 추첨** | 누적 구간 판정(총합 100%). 추첨 로직을 `ItemDatabase`에 모아 **상점·상자가 같은 확률표** 사용 |
| 🎒 **인벤토리 / 장비** | 슬롯=입력·표현 / 매니저=판정·데이터로 분리. 드래그 스왑 시 **양방향 슬롯 타입 검증**, 우클릭 퀵 장착, 장착 스탯 합산 후 최대 체력 증감분 보정 |
| ☁️ **씬 간 이관** | UI는 씬과 함께 파괴하고 **아이템 ID만 `static`에 백업** — `OnDestroy` 저장 / `Start` 복원 후 스탯 재계산 |
| 🏪 **상점** | 입장 시 1회만 추첨해 리롤 차단, 연타 중복 구매 2중 방어, 인벤 만석이면 **골드 전액 환불** |
| 🎁 **상자** | 3택 1. 중복 재추첨에 `failSafe` 카운터를 둬 풀이 작을 때 무한 루프 방지 |
| ✨ **스킬** | `ScriptableObject` 기반. **스탯 도달 시 자동 진화**, 발동 타입 3종(총알 / 저격 / 오라) → [상세](#-스킬-시스템) |
| 🌊 **웨이브** | 맵 생성 → NavMesh 베이크 → 스폰 순서를 코루틴으로 고정. `Enemy` 태그 0개로 전멸 판정 후 보상·문 개방 |
| 🏗️ **층 · BGM** | 1~9층 랜덤 / 10층 보스맵 고정. BGM은 같은 클립이면 교체하지 않아 **층 이동 시 끊기지 않음** |
| 🚪 **문 · 씬 전환** | 미클리어 문은 콜라이더+스프라이트를 함께 꺼 **보이지도 통과되지도 않게**. 페이드는 `unscaledDeltaTime`으로 `timeScale=0`에서도 동작 |
| 🔐 **전역 UI 잠금** | `Player.isAnyUIOpen` 하나로 인벤·상점·상자·스킬·문 상호작용의 중복 오픈을 통제 |

---

## 📄 아이템 데이터 — `ItemData 1.csv`

MySQL에서 관리한 테이블을 CSV로 내보내 로드합니다. **14개 컬럼 · 아이템 30종.**

```
ItemID, MainType, SubType, ItemName, Tier, Description,
ATK, ATKSPD, HP_Flat, HP_Pct, DEF, MovSPD, EFF, Ability
```

| 분류 | 구성 |
|---|---|
| **타입** | ⚔️ Weapon 6 · 🛡️ Armor 7 · 💍 Accessory 17 |
| **티어** | 1티어 15 · 2티어 9 · 3티어 3 · 4티어 3 |
| **특수효과(EFF)** | `GiantWeapon`(무기 크기 증가) · `Vampire`(흡혈) · `NOT EFF` 28 |

| 티어 | 등급 | 확률 | 색상 | 가격 |
|:--:|---|:--:|---|---|
| 1 | 🥉 동 | `65%` | ![](https://img.shields.io/badge/CD7F32-CD7F32?style=flat-square) | 100~150 G |
| 2 | 🥈 은 | `25%` | ![](https://img.shields.io/badge/C0C0C0-C0C0C0?style=flat-square) | 250~350 G |
| 3 | 🥇 금 | `8%` | ![](https://img.shields.io/badge/FFD700-FFD700?style=flat-square) | 500~650 G |
| 4 | 💎 전설 | `2%` | ![](https://img.shields.io/badge/96E6FF-96E6FF?style=flat-square) | 800 G |

**트레이드오프 설계** — 예: `대검`(4티어)은 공격력 +5 / 공격속도 −0.15 / 이동속도 −1 처럼 **장점과 대가를 함께** 부여해 단순 상위호환을 피했습니다.

<details>
<summary>📊 <b>플레이어 · 몬스터 밸런스 (JSON)</b></summary>

| Player_ID | ❤️ HP | ⚔️ ATK | 👟 이속 | ⚡ 공속 | 💨 대시 | ⏱️ 대시 쿨 |
|---|:--:|:--:|:--:|:--:|:--:|:--:|
| `Normal_Player` | 60 | 500 | 20 | 100 | 40 | 1 |

| MonsterID | 이름 | 타입 | ❤️ HP | ⚔️ ATK | ⚡ 공속 | 👟 이속 |
|---|---|:--:|:--:|:--:|:--:|:--:|
| `boss_mino_01` | 👹 미노타우로스 | **Boss** | 1500 | 15 | 1.0 | 6 |
| `mob_fast_01` | 🦢 화이트 덕 | Fast | 30 | 7 | 1.3 | 8 |
| `mob_melee_01` | 🦆 블루 덕 | Melee | 60 | 9 | 1.0 | 6 |
| `mob_range_01` | 🦇 불 박쥐 | Ranged | 35 | 8 | 1.0 | 5 |
| `mob_tank_01` | 🐢 거북알도마뱀 | Tank | 80 | 12 | 1.0 | 4 |

</details>

---

## ✨ 스킬 시스템 — `SkillScripts/`

레벨 젬으로 스킬을 구매하고, **플레이어 스탯이 조건에 도달하면 스킬이 자동으로 진화**합니다.
레벨업 버튼이 없어 플레이어는 "장비를 강화하다 보면 스킬이 강해지는" 경험을 하게 됩니다.

```
🏪 구매 (레벨 젬 차감) ──▶ 🎯 장착 (1개만) ──▶ 📈 스탯 도달 시 Lv 자동 해금 ──▶ ⌨️ Q키 발동
```

### 데이터 구조 — `SkillData` (ScriptableObject)

`SkillData`(식별자·이름·아이콘·비용) 안에 **`SkillLevel[]` 배열**을 둬, 레벨마다 해금 조건과 성능·연출을 따로 지정합니다. 코드 수정 없이 인스펙터에서 스킬을 추가·조정할 수 있습니다.

| 구분 | 필드 |
|---|---|
| 🔓 **해금 조건** | `requiredAtk` · `requiredAtkSpeed` |
| ⚔️ **공통 성능** | `damageMultiplier`(공격력 배율) · `cooldownSeconds` · `bulletSpeed` · `piercing` |
| 🎨 **레벨별 연출** | `icon` · `description` · `castSound` · `attackSound` · `auraColor` |

### 발동 타입 3종

| 타입 | 동작 |
|---|---|
| 🔫 **총알** `PlayerSkillBullet` | 마우스 방향으로 직선 발사. 관통 시 `HashSet`으로 **루트 오브젝트 기준 중복 타격 방지**, 벽 레이어·수명으로 소멸 |
| 🎯 **저격** `SniperOverlay` | Q로 `timeScale=0` 진입 → 셰이더로 마우스 주변만 뚫린 암전 스코프 → 클릭 시 `OverlapCircleAll` 명중 판정. ESC·우클릭 취소. **취소해도 쿨타임 소모** |
| 🌀 **오라** `PlayerSkillAura` | Q 토글로 플레이어 자식에 원형 트리거 생성. 범위 내 적에게 **슬로우 + 주기적 도트**. 반지름 0이면 스프라이트 경계에 자동 매칭해 **시각 = 실제 범위 일치** |

### 상태 관리 — `SkillManager` (싱글톤 + DDOL)

| 상태 | 저장 위치 | 수명 |
|---|---|---|
| 구매 · 장착 | `PlayerPrefs` | **영구** |
| 진화 해금 · 쿨타임 · 오라 | 메모리 | **런 단위** (`VillageScene` 진입 시 `ResetRunState`) |

- `OnStateChanged` 이벤트로 UI(장착 슬롯 · 툴팁 · 상점 버튼)가 **각자 구독해 갱신** — 매니저가 UI를 직접 알지 않음
- 오라를 매 프레임 `OverlapCircle`로 훑지 않고 **`OnTriggerEnter/Exit`로 내부 적만 추적**해 부하를 줄임
- 도트 처리 중 적이 죽어 `Dictionary`가 수정되는 것을 막기 위해 **키 스냅샷 후 순회**
- 오라가 꺼지거나 씬이 바뀔 때 `OnDisable`에서 **남은 슬로우를 전부 해제**

---

## 🛠️ 트러블슈팅

| 문제 | 해결 |
|---|---|
| ☁️ 층 이동 시 아이템 전부 소실 | UI는 파괴하고 **ID만 `static`에 백업** 후 새 씬에서 복원 |
| 🖱️ 상점 연타로 중복 구매 | 결제 즉시 ID 비우기 + 버튼 비활성화 (2중 방어) |
| 💰 만석 구매 시 골드만 소진 | `AddItem` 실패 시 **전액 환불** + 안내 |
| 🧭 몬스터가 벽 통과 / 길 못 찾음 | 같은 프레임 베이크가 원인 → `WaitForEndOfFrame` 후 베이크, **완료 뒤 스폰** |
| ⏸️ `timeScale=0`에서 연출 정지 | `unscaledDeltaTime` / `WaitForSecondsRealtime` |
| 🔐 UI 중첩으로 입력·시간 충돌 | `Player.isAnyUIOpen` 전역 플래그로 통일 |
| 👻 빈 슬롯도 고스트 아이콘 생성 | `eventData.pointerDrag = null`로 이벤트 차단 |
| ♾️ 중복 방지 루프가 안 멈춤 | `failSafe` 카운터(50회)로 탈출 보장 |
| 🔒 뒤늦게 산 스킬이 Lv1도 안 열림 | 미구매 시 해금 검사를 건너뛴 탓 → 구매 시점에 **도달 가능한 레벨까지 일괄 해금** |
| 🎯 저격 모드 중 씬 전환하면 시간 정지 잔류 | `ResetRunState`에서 `timeScale`·커서·UI 플래그를 **강제 복구** |
| 🌀 오라 안에서 적이 죽으면 슬로우가 남음 | `OnDisable`·`OnTriggerExit`에서 슬로우 해제, 파괴된 콜라이더 `null` 방어 |

---

## 🕹️ 조작

| 키 | 동작 |
|:--:|---|
| `F` | 상호작용 — 문 이동 / 상점 · 상자 · 스킬 좌대 |
| `V` | 인벤토리 |
| `Q` | 장착 스킬 발동 (저격은 진입 → 클릭 발사) |
| `E` / `ESC` | 스킬 툴팁 열기 / 닫기 · 저격 취소 |
| 🖱️ 드래그 · 우클릭 · 호버 | 슬롯 교체(밖으로 드롭 → 버리기) · 장착/해제/사용 · 상세 정보 |

---

## 💡 설계 시 중점

- **데이터와 로직의 분리** — 아이템·몬스터는 CSV/JSON, 스킬은 `ScriptableObject`로 빼내 코드 수정 없이 밸런싱
- **책임 분리** — 슬롯은 입력·표현만, 매니저는 판정·데이터만. UI는 이벤트를 구독해 스스로 갱신
- **단일 진실 공급원** — 확률표는 `ItemDatabase` 하나, UI 잠금은 플래그 하나
- **방어적 프로그래밍** — 데이터 결손 경고, 무한 루프 탈출 카운터, `null`·파괴된 참조 방어
- **상태 복구 보장** — `timeScale`·커서·슬로우처럼 전역에 영향을 주는 값은 반드시 되돌리는 경로를 둠
