using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 스킬 구매/장착/진화 해금을 관리하는 싱글톤.
/// - 구매/장착 상태: PlayerPrefs (영속)
/// - 진화 해금: 메모리 (세션 동안만 유지, DDOL로 씬 전환은 살아남음)
/// </summary>
public class SkillManager : MonoBehaviour
{
    public static SkillManager Instance { get; private set; }

    private const string PurchasedKeyPrefix = "SkillPurchased_";
    private const string EquippedKey = "EquippedSkillId";

    public event Action OnStateChanged;

    private readonly HashSet<string> purchased = new HashSet<string>();
    private string equippedSkillId = "";

    // skillId → 현재까지 해금된 가장 높은 레벨 번호 (0 = 아무 레벨도 해금 안됨)
    private readonly Dictionary<string, int> unlockedLevels = new Dictionary<string, int>();

    // skillId → 쿨타임이 끝나는 Time.time 값
    private readonly Dictionary<string, float> cooldownEndTimes = new Dictionary<string, float>();

    // 저격 모드 상태
    private bool sniperModeActive = false;
    private SkillData sniperSkill;
    private SkillLevel sniperLevel;

    // 오라(토글) 상태
    private GameObject activeAuraInstance;
    private string activeAuraSkillId = "";

    public string EquippedSkillId => equippedSkillId;

    [Header("등록된 모든 스킬 (게임에 등장하는 스킬은 여기에 추가)")]
    public SkillData[] allSkills;

    [Header("효과음 재생기 (비우면 자동 생성, DDOL 보장)")]
    public AudioSource sfxSource;

    [Header("저격 적중 판정")]
    [Tooltip("저격 클릭 시 마우스 지점 주변 이 반지름(월드 단위) 안의 적/보스를 명중 처리. 0이면 정확히 한 점만 검사")]
    [SerializeField] private float sniperHitRadius = 1.0f;

    [Header("공용 효과음")]
    [Tooltip("스킬을 장착했을 때 재생")]
    public AudioClip equipSound;
    [Range(0f, 1f)] public float equipSoundVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            sfxSource.ignoreListenerPause = true;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>VillageScene 진입 시 진화 해금 상태와 런타임 상태(쿨타임/오라/저격모드)를 초기화.
    /// 구매/장착 정보는 PlayerPrefs라 유지됨.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "VillageScene") return;
        ResetRunState();
    }

    /// <summary>현재 게임 런(run)에 종속된 모든 상태를 초기화. VillageScene 자동 진입 + 재시작 버튼에서 호출.
    /// 구매/장착(PlayerPrefs)은 유지되고, 진화 해금/쿨타임/오라/저격모드만 리셋됨.</summary>
    public void ResetRunState()
    {
        unlockedLevels.Clear();
        cooldownEndTimes.Clear();

        // 씬 전환 시 오라 인스턴스는 같이 파괴되지만 참조가 남아있을 수 있어 정리
        if (activeAuraInstance != null) Destroy(activeAuraInstance);
        activeAuraInstance = null;
        activeAuraSkillId = "";

        // 저격 모드 중에 호출됐다면 timeScale 등 글로벌 상태도 복구
        if (sniperModeActive)
        {
            sniperModeActive = false;
            sniperSkill = null;
            sniperLevel = null;
            Time.timeScale = 1f;
            Player.isAnyUIOpen = false;
            Cursor.visible = true;
            if (SniperOverlay.Instance != null) SniperOverlay.Instance.Hide();
        }

        OnStateChanged?.Invoke();
    }

    /// <summary>일시정지 중에도 들리도록 PlayOneShot 사용. clip null이면 무시.</summary>
    private void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void Update()
    {
        TryUnlockLevels();

        // 저격 모드 중에는 다른 입력 무시하고 저격 입력만 처리
        if (sniperModeActive)
        {
            HandleSniperInput();
            return;
        }

        // 일시정지 / 다른 UI 열려있을 때는 Q 발동 차단
        if (Time.timeScale != 0f && !Player.isAnyUIOpen)
        {
            if (Input.GetKeyDown(KeyCode.Q))
                FireEquippedSkill();
        }
    }

    /// <summary>모든 스킬에 대해 현재 플레이어 스탯으로 해금 가능한 레벨을 갱신.</summary>
    private void TryUnlockLevels()
    {
        if (allSkills == null) return;
        Player p = Player.Instance;
        if (p == null) return;

        float atk = p.TotalAtk;
        float spd = p.TotalAtkSpeed;
        bool anyUnlocked = false;

        foreach (var s in allSkills)
        {
            if (s == null || string.IsNullOrEmpty(s.skillId) || s.levels == null) continue;
            if (!IsPurchased(s.skillId)) continue;

            int current = GetHighestUnlockedLevel(s.skillId);

            foreach (var lv in s.levels)
            {
                if (lv == null) continue;
                if (lv.level <= current) continue;
                if (atk >= lv.requiredAtk && spd >= lv.requiredAtkSpeed)
                {
                    unlockedLevels[s.skillId] = lv.level;
                    current = lv.level;
                    anyUnlocked = true;
                    Debug.Log($"[SkillManager] '{s.skillName}' Lv{lv.level} 해금 (atk {atk:F1}/{lv.requiredAtk}, spd {spd:F2}/{lv.requiredAtkSpeed})");
                }
            }
        }

        if (anyUnlocked)
            OnStateChanged?.Invoke();
    }

    private void FireEquippedSkill()
    {
        SkillData equipped = GetEquippedSkill();
        if (equipped == null)
        {
            Debug.Log("[SkillManager] 장착된 스킬이 없음");
            return;
        }

        int lvNum = GetHighestUnlockedLevel(equipped.skillId);
        if (lvNum <= 0)
        {
            Debug.Log($"[SkillManager] '{equipped.skillName}' 해금된 레벨이 없음 (조건 미달)");
            return;
        }

        SkillLevel lvData = FindLevelData(equipped, lvNum);
        if (lvData == null)
        {
            Debug.LogWarning($"[SkillManager] '{equipped.skillName}' Lv{lvNum} 데이터가 없음");
            return;
        }

        // 오라가 이미 켜진 상태에서 같은 스킬로 Q를 다시 누르면 쿨다운 무시하고 끄기
        if (lvData.isAuraSkill && activeAuraInstance != null && activeAuraSkillId == equipped.skillId)
        {
            ToggleAuraOff(equipped, lvData);
            return;
        }

        if (IsOnCooldown(equipped.skillId))
        {
            Debug.Log($"[SkillManager] '{equipped.skillName}' 쿨타임 {GetCooldownRemaining(equipped.skillId):F1}초 남음");
            return;
        }

        // 저격 모드 스킬이면 즉시 발사 대신 에임 모드 진입
        if (lvData.isSniperSkill)
        {
            EnterSniperMode(equipped, lvData);
            return;
        }

        // 오라 스킬이면 토글로 ON
        if (lvData.isAuraSkill)
        {
            ToggleAuraOn(equipped, lvData);
            return;
        }

        bool fired = TryFireBullet(equipped, lvData);
        if (!fired)
        {
            string msg = !string.IsNullOrEmpty(lvData.debugMessage)
                ? lvData.debugMessage
                : $"{equipped.skillName} Lv{lvNum} 발동";
            Debug.Log($"[SkillManager] {msg} (id={equipped.skillId}, lv={lvNum}) — 총알 프리팹 미설정");
            return;
        }

        cooldownEndTimes[equipped.skillId] = Time.time + lvData.cooldownSeconds;
        OnStateChanged?.Invoke();
    }

    /// <summary>총알 프리팹이 있으면 마우스 방향으로 발사하고 true 반환.</summary>
    private bool TryFireBullet(SkillData skill, SkillLevel lvData)
    {
        if (lvData.bulletPrefab == null) return false;

        Player p = Player.Instance;
        if (p == null) return false;

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[SkillManager] Camera.main을 찾을 수 없어 총알 방향 계산 불가");
            return false;
        }

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 origin = p.transform.position;
        Vector2 dir = ((Vector2)mouseWorld - origin);
        if (dir.sqrMagnitude < 0.0001f) dir = p.spriteRenderer != null && p.spriteRenderer.flipX ? Vector2.left : Vector2.right;
        dir.Normalize();

        GameObject obj = Instantiate(lvData.bulletPrefab, origin, Quaternion.identity);
        PlayerSkillBullet bullet = obj.GetComponent<PlayerSkillBullet>();
        if (bullet == null)
        {
            Debug.LogWarning("[SkillManager] 총알 프리팹에 PlayerSkillBullet 컴포넌트가 없음");
            Destroy(obj);
            return false;
        }

        float damage = p.TotalAtk * lvData.damageMultiplier;
        bullet.Initialize(damage, lvData.bulletSpeed, dir, lvData.piercing);

        PlaySfx(lvData.castSound, lvData.castSoundVolume);
        return true;
    }

    /// <summary>오라 ON: 플레이어의 자식으로 오라 스폰 후 Initialize. 쿨타임 소모.</summary>
    private void ToggleAuraOn(SkillData skill, SkillLevel lvData)
    {
        if (lvData.auraPrefab == null)
        {
            Debug.LogWarning($"[SkillManager] '{skill.skillName}' Lv{lvData.level} 오라 프리팹 미설정");
            return;
        }

        Player p = Player.Instance;
        if (p == null) return;

        // 다른 오라가 떠있으면 먼저 정리 (방어 코드)
        if (activeAuraInstance != null)
        {
            Destroy(activeAuraInstance);
            activeAuraInstance = null;
            activeAuraSkillId = "";
        }

        GameObject obj = Instantiate(lvData.auraPrefab, p.transform.position, Quaternion.identity, p.transform);
        PlayerSkillAura aura = obj.GetComponent<PlayerSkillAura>();
        if (aura == null)
        {
            Debug.LogWarning("[SkillManager] 오라 프리팹에 PlayerSkillAura 컴포넌트가 없음");
            Destroy(obj);
            return;
        }

        float dotDmg = p.TotalAtk * lvData.auraDotMultiplier;
        aura.Initialize(lvData.auraRadius, lvData.auraSlowMultiplier, dotDmg, lvData.auraDotInterval, lvData.auraColor);

        activeAuraInstance = obj;
        activeAuraSkillId = skill.skillId;

        PlaySfx(lvData.castSound, lvData.castSoundVolume);
        cooldownEndTimes[skill.skillId] = Time.time + lvData.cooldownSeconds;
        OnStateChanged?.Invoke();
    }

    /// <summary>오라 OFF: 인스턴스 파괴. 쿨타임은 ON일 때 이미 시작되었으므로 추가 적용 안 함.</summary>
    private void ToggleAuraOff(SkillData skill, SkillLevel lvData)
    {
        if (activeAuraInstance != null) Destroy(activeAuraInstance);
        activeAuraInstance = null;
        activeAuraSkillId = "";
        OnStateChanged?.Invoke();
    }

    private void EnterSniperMode(SkillData skill, SkillLevel lvData)
    {
        sniperModeActive = true;
        sniperSkill = skill;
        sniperLevel = lvData;

        Time.timeScale = 0f;
        Player.isAnyUIOpen = true;
        Cursor.visible = false;

        PlaySfx(lvData.castSound, lvData.castSoundVolume);

        if (SniperOverlay.Instance != null)
            SniperOverlay.Instance.Show();
        else
            Debug.LogWarning("[SkillManager] SniperOverlay.Instance가 씬에 없음 — UI 표시 생략");
    }

    private void HandleSniperInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ExitSniperMode(fired: false);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            FireSniperShot();
            ExitSniperMode(fired: true);
        }
    }

    private void FireSniperShot()
    {
        if (sniperSkill == null || sniperLevel == null) return;

        Camera cam = Camera.main;
        Player p = Player.Instance;
        if (cam == null || p == null) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        float damage = p.TotalAtk * sniperLevel.damageMultiplier;

        PlaySfx(sniperLevel.attackSound, sniperLevel.attackSoundVolume);

        Collider2D[] hits = sniperHitRadius > 0f
            ? Physics2D.OverlapCircleAll(mouseWorld, sniperHitRadius)
            : Physics2D.OverlapPointAll(mouseWorld);
        bool dealt = false;
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            MonsterBrain brain           = hit.GetComponentInParent<MonsterBrain>();
            MonsterController controller = hit.GetComponentInParent<MonsterController>();
            BossBrain_Minotaur boss      = hit.GetComponentInParent<BossBrain_Minotaur>();
            BossStatHandler bossShaman   = hit.GetComponentInParent<BossStatHandler>();

            if (brain == null && controller == null && boss == null && bossShaman == null) continue;

            if (brain != null)      { brain.OnHitReceived(damage);      dealt = true; }
            if (controller != null) { controller.OnHitReceived(damage); dealt = true; }
            if (boss != null)       { boss.OnHitReceived(damage);       dealt = true; }
            if (bossShaman != null) { bossShaman.OnHitReceived(damage); dealt = true; }
        }

        Debug.Log(dealt
            ? $"[SkillManager] 저격 명중! 데미지 {damage:F1}"
            : "[SkillManager] 저격 빗나감");
    }

    private void ExitSniperMode(bool fired)
    {
        // 발사/취소 둘 다 쿨타임 소모
        if (sniperSkill != null && sniperLevel != null)
        {
            cooldownEndTimes[sniperSkill.skillId] = Time.time + sniperLevel.cooldownSeconds;
            OnStateChanged?.Invoke();
        }

        sniperModeActive = false;
        sniperSkill = null;
        sniperLevel = null;

        Time.timeScale = 1f;
        Player.isAnyUIOpen = false;
        Cursor.visible = true;

        if (SniperOverlay.Instance != null)
            SniperOverlay.Instance.Hide();
    }

    public bool IsOnCooldown(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return false;
        return cooldownEndTimes.TryGetValue(skillId, out float end) && Time.time < end;
    }

    public float GetCooldownRemaining(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return 0f;
        if (!cooldownEndTimes.TryGetValue(skillId, out float end)) return 0f;
        return Mathf.Max(0f, end - Time.time);
    }

    private SkillLevel FindLevelData(SkillData skill, int levelNum)
    {
        if (skill?.levels == null) return null;
        foreach (var lv in skill.levels)
            if (lv != null && lv.level == levelNum) return lv;
        return null;
    }

    /// <summary>현재 해금된 가장 높은 레벨의 SkillLevel 데이터 반환. 해금된 레벨 없으면 null.</summary>
    public SkillLevel GetCurrentLevelData(SkillData skill)
    {
        if (skill == null) return null;
        int lv = GetHighestUnlockedLevel(skill.skillId);
        return lv > 0 ? FindLevelData(skill, lv) : null;
    }

    public int GetHighestUnlockedLevel(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return 0;
        return unlockedLevels.TryGetValue(skillId, out int v) ? v : 0;
    }

    private void Load()
    {
        purchased.Clear();
        if (allSkills != null)
        {
            foreach (var s in allSkills)
            {
                if (s == null || string.IsNullOrEmpty(s.skillId)) continue;
                if (PlayerPrefs.GetInt(PurchasedKeyPrefix + s.skillId, 0) == 1)
                    purchased.Add(s.skillId);
            }
        }
        equippedSkillId = PlayerPrefs.GetString(EquippedKey, "");
    }

    public bool IsPurchased(string skillId)
    {
        return !string.IsNullOrEmpty(skillId) && purchased.Contains(skillId);
    }

    public bool IsEquipped(string skillId)
    {
        return !string.IsNullOrEmpty(skillId) && equippedSkillId == skillId;
    }

    public SkillData GetEquippedSkill()
    {
        if (string.IsNullOrEmpty(equippedSkillId) || allSkills == null) return null;
        foreach (var s in allSkills)
            if (s != null && s.skillId == equippedSkillId) return s;
        return null;
    }

    public void Purchase(SkillData skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.skillId)) return;
        if (purchased.Contains(skill.skillId)) return;

        purchased.Add(skill.skillId);
        PlayerPrefs.SetInt(PurchasedKeyPrefix + skill.skillId, 1);
        PlayerPrefs.Save();

        CatchUpUnlockedLevels(skill);

        OnStateChanged?.Invoke();
    }

    /// <summary>구매 시점에 현재 스탯으로 도달 가능한 최고 레벨까지 무음으로 일괄 해금.
    /// 미구매 상태에서는 TryUnlockLevels가 스킵하므로, 뒤늦게 구매했을 때 Lv1조차 해금 안 되는 문제를 방지.</summary>
    private void CatchUpUnlockedLevels(SkillData skill)
    {
        if (skill?.levels == null) return;
        Player p = Player.Instance;
        if (p == null) return;

        float atk = p.TotalAtk;
        float spd = p.TotalAtkSpeed;
        int current = GetHighestUnlockedLevel(skill.skillId);

        foreach (var lv in skill.levels)
        {
            if (lv == null || lv.level <= current) continue;
            if (atk >= lv.requiredAtk && spd >= lv.requiredAtkSpeed)
            {
                unlockedLevels[skill.skillId] = lv.level;
                current = lv.level;
            }
        }
    }

    /// <summary>스킬 하나만 장착 가능. 이미 다른 스킬이 장착되어 있으면 자동 해제.</summary>
    public void Equip(SkillData skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.skillId)) return;
        if (!IsPurchased(skill.skillId)) return;

        equippedSkillId = skill.skillId;
        PlayerPrefs.SetString(EquippedKey, equippedSkillId);
        PlayerPrefs.Save();

        PlaySfx(equipSound, equipSoundVolume);

        OnStateChanged?.Invoke();
    }

    public void Unequip()
    {
        if (string.IsNullOrEmpty(equippedSkillId)) return;
        equippedSkillId = "";
        PlayerPrefs.SetString(EquippedKey, "");
        PlayerPrefs.Save();

        OnStateChanged?.Invoke();
    }
}
