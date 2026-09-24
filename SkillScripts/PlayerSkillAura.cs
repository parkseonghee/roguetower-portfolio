using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 주변에 원형 범위를 만드는 광역 스킬 오라.
/// SkillManager가 Instantiate(플레이어의 자식으로) → Initialize로 반지름/슬로우/도트 주입.
/// 트리거 콜라이더 기반: OverlapCircle을 매프레임 돌리지 않고, OnTriggerEnter/Exit2D로만 내부 적을 추적.
/// 도트 데미지는 dotInterval 주기로 1번씩만 ApplyDotTick 호출.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerSkillAura : MonoBehaviour
{
    [Tooltip("자동 소멸 시간(초). 0 이하면 무한 — SkillManager 토글로 끄기 전까지 유지")]
    [SerializeField] private float fallbackLifetime = 0f;

    // 인스펙터 외부에서 주입되는 런타임 파라미터
    private float slowMultiplier = 1f;
    private float dotDamagePerTick = 0f;
    private float dotInterval = 0.5f;

    private float dotAccumulator = 0f;
    private bool isInitialized = false;

    private CircleCollider2D circle;

    // 콜라이더 → 캐싱된 적 컴포넌트 (한 번 GetComponent하고 재사용)
    private class EnemyEntry
    {
        public MonsterStatHandler stat;
        public MonsterBrain brain;
        public MonsterController controller; // DashMonster
        public BossBrain_Minotaur boss;
        public BossStatHandler bossShaman;   // Boss_Shaman
    }
    private readonly Dictionary<Collider2D, EnemyEntry> inside = new Dictionary<Collider2D, EnemyEntry>();

    // ApplyDotTick에서 foreach 중 Dictionary 수정 가능성 대비 키 스냅샷용 — GC 부담 줄이려고 멤버 캐시
    private readonly List<Collider2D> tickKeyBuffer = new List<Collider2D>();

    private void Awake()
    {
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;
    }

    /// <summary>SkillManager가 호출. 반지름/슬로우/도트/색을 주입.
    /// radius가 0 이하이면 같은 GameObject의 SpriteRenderer 가장자리에 자동 매칭 (시각 = 영향범위).</summary>
    public void Initialize(float radius, float slowMul, float dotDmgPerTick, float dotIntervalSeconds, Color tintColor)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (radius > 0f)
        {
            circle.radius = radius;
        }
        else
        {
            // sprite.bounds는 asset 로컬(월드 단위, scale 미적용). CircleCollider2D.radius도 로컬.
            // 둘 다 같은 좌표계라 1:1 매칭이면 scale에 비례해 자동으로 일치됨.
            if (sr != null && sr.sprite != null)
            {
                Vector3 ext = sr.sprite.bounds.extents;
                circle.radius = Mathf.Min(ext.x, ext.y);
            }
            else
            {
                circle.radius = 0.5f;
            }
        }

        if (sr != null) sr.color = tintColor;

        slowMultiplier = Mathf.Clamp(slowMul, 0.05f, 1f);
        dotDamagePerTick = Mathf.Max(0f, dotDmgPerTick);
        dotInterval = Mathf.Max(0.05f, dotIntervalSeconds);
        dotAccumulator = 0f;
        isInitialized = true;

        if (fallbackLifetime > 0f) Destroy(gameObject, fallbackLifetime);
    }

    private void Update()
    {
        if (!isInitialized) return;
        if (dotDamagePerTick <= 0f) return; // Lv1 (슬로우만)일 땐 도트 루프 자체를 스킵

        dotAccumulator += Time.deltaTime;
        if (dotAccumulator < dotInterval) return;
        dotAccumulator -= dotInterval;

        ApplyDotTick();
    }

    private void ApplyDotTick()
    {
        // foreach 중에 OnHit → Die → Exit이 일어나면 Dict이 수정될 수 있으므로 키 스냅샷
        if (inside.Count == 0) return;

        tickKeyBuffer.Clear();
        foreach (var k in inside.Keys) tickKeyBuffer.Add(k);

        for (int i = 0; i < tickKeyBuffer.Count; i++)
        {
            var col = tickKeyBuffer[i];
            if (col == null) continue; // 콜라이더가 파괴됨 (Exit이 안 불릴 수도 있어서 방어)
            if (!inside.TryGetValue(col, out var e)) continue;
            if (e.brain != null)       e.brain.OnHitReceived(dotDamagePerTick);
            if (e.controller != null)  e.controller.OnHitReceived(dotDamagePerTick);
            if (e.boss != null)        e.boss.OnHitReceived(dotDamagePerTick);
            if (e.bossShaman != null)  e.bossShaman.OnHitReceived(dotDamagePerTick);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isInitialized || other == null) return;
        if (inside.ContainsKey(other)) return;

        // 자식 콜라이더 대응: GetComponentInParent로 계층 전체 탐색
        MonsterBrain brain           = other.GetComponentInParent<MonsterBrain>();
        MonsterController controller = other.GetComponentInParent<MonsterController>();
        BossBrain_Minotaur boss      = other.GetComponentInParent<BossBrain_Minotaur>();
        BossStatHandler bossShaman   = other.GetComponentInParent<BossStatHandler>();

        if (brain == null && controller == null && boss == null && bossShaman == null) return;

        var entry = new EnemyEntry
        {
            stat       = other.GetComponentInParent<MonsterStatHandler>(),
            brain      = brain,
            controller = controller,
            boss       = boss,
            bossShaman = bossShaman,
        };
        inside[other] = entry;

        if (entry.stat != null && slowMultiplier < 1f)
            entry.stat.SetSlowMultiplier(slowMultiplier);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        if (!inside.TryGetValue(other, out var entry)) return;

        if (entry.stat != null) entry.stat.ClearSlow();
        inside.Remove(other);
    }

    private void OnDisable()
    {
        // 오라가 비활성/파괴될 때 안에 갇힌 적들 슬로우 해제 (몬스터가 죽었어도 안전)
        foreach (var kv in inside)
        {
            if (kv.Value != null && kv.Value.stat != null)
                kv.Value.stat.ClearSlow();
        }
        inside.Clear();
    }
}
