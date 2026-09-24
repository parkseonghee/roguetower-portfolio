using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 스킬용 직선 총알. SkillManager가 Instantiate 후 Initialize로 데미지/속도/방향을 주입.
/// Enemy/Boss 태그에 닿으면 OnHitReceived로 피해를 주고 소멸, 벽(obstacleLayer)에 닿아도 소멸.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerSkillBullet : MonoBehaviour
{
    [Tooltip("벽 등 장애물 레이어. 이 레이어에 닿으면 총알 파괴")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("총알 자동 소멸 시간 (안전장치)")]
    [SerializeField] private float lifetime = 5f;

    private float damage;
    private float speed;
    private Vector2 direction;
    private bool piercing;
    private bool isInitialized = false;

    private Rigidbody2D rb;
    private readonly HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(float bulletDamage, float bulletSpeed, Vector2 bulletDirection, bool isPiercing)
    {
        damage = bulletDamage;
        speed = bulletSpeed;
        direction = bulletDirection.normalized;
        piercing = isPiercing;
        isInitialized = true;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 자식 콜라이더 대응: GetComponentInParent로 계층 전체 탐색
        MonsterBrain brain           = collision.GetComponentInParent<MonsterBrain>();
        MonsterController controller = collision.GetComponentInParent<MonsterController>();
        BossBrain_Minotaur boss      = collision.GetComponentInParent<BossBrain_Minotaur>();
        BossStatHandler bossShaman   = collision.GetComponentInParent<BossStatHandler>();

        if (brain != null || controller != null || boss != null || bossShaman != null)
        {
            // 루트 오브젝트 기준으로 중복 타격 방지
            GameObject root = collision.transform.root.gameObject;
            if (alreadyHit.Add(root))
            {
                if (brain != null)      brain.OnHitReceived(damage);
                if (controller != null) controller.OnHitReceived(damage);
                if (boss != null)       boss.OnHitReceived(damage);
                if (bossShaman != null) bossShaman.OnHitReceived(damage);
            }

            if (!piercing) Destroy(gameObject);
            return;
        }

        if (((1 << collision.gameObject.layer) & obstacleLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}
