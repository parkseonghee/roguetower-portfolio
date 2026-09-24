using UnityEngine;

[System.Serializable]
public class SkillLevel
{
    [Tooltip("표시용 레벨 번호 (1부터 시작)")]
    public int level = 1;

    [Header("해금 조건")]
    public float requiredAtk;
    public float requiredAtkSpeed;

    [Header("이 레벨에서 슬롯에 표시할 아이콘 (비우면 SkillData.icon 사용)")]
    public Sprite icon;

    [Header("이 레벨 설명 (툴팁에 표시)")]
    [TextArea(2, 4)] public string description;

    [Header("Q키 발동 시 출력할 디버그 메시지 (테스트용)")]
    [TextArea(1, 3)] public string debugMessage = "스킬 발동";

    [Header("발사 설정 (총알형 스킬)")]
    [Tooltip("이 레벨에서 발사할 총알 프리팹. 비워두면 디버그 로그만 찍힘")]
    public GameObject bulletPrefab;

    [Tooltip("총알 속도 (단위/초)")]
    public float bulletSpeed = 8f;

    [Tooltip("공격력에 곱해질 피해량 배율. 1.5 = 공격력 × 1.5")]
    public float damageMultiplier = 1.5f;

    [Tooltip("쿨타임 (초)")]
    public float cooldownSeconds = 10f;

    [Tooltip("총알이 적을 관통할지 여부 (true면 같은 적은 한 번만 맞고 벽/시간만료로 소멸)")]
    public bool piercing = false;

    [Header("저격 모드 (Lv2 같은 단발 강타격용)")]
    [Tooltip("true면 Q 누를 때 즉시 발사 대신 저격 모드 진입 (시간정지 + 스코프 + 클릭 발사)")]
    public bool isSniperSkill = false;

    [Header("오라 모드 (광역 지속 효과 스킬, 토글)")]
    [Tooltip("true면 Q 누를 때 캐릭터 주변에 오라 생성/제거 (토글). bulletPrefab 대신 auraPrefab 사용")]
    public bool isAuraSkill = false;

    [Tooltip("오라 프리팹 (CircleCollider2D 트리거 + PlayerSkillAura 컴포넌트 필요)")]
    public GameObject auraPrefab;

    [Tooltip("오라 반지름 (로컬 단위, scale에 곱해짐). 0 이하면 프리팹의 SpriteRenderer 시각 가장자리에 자동 매칭 (시각=영향범위 일치 — 권장)")]
    public float auraRadius = 0f;

    [Tooltip("적이 오라 안에 있을 때 이동속도 배율 (1 = 슬로우 없음, 0.5 = 절반속도, 0.05 = 사실상 정지). Lv1 슬로우 강도")]
    [Range(0.05f, 1f)] public float auraSlowMultiplier = 0.5f;

    [Tooltip("1틱당 공격력 배율로 들어가는 도트 데미지. 0이면 도트 없음(슬로우만). 예: 0.2 = 공격력 × 0.2 / 1틱")]
    public float auraDotMultiplier = 0f;

    [Tooltip("도트 적용 주기(초). 0.5 = 0.5초마다 1번 데미지")]
    public float auraDotInterval = 0.5f;

    [Tooltip("오라 SpriteRenderer에 입힐 틴트 색. Lv1=흰색, Lv2=빨강 같은 식으로 레벨별 시각 차별화")]
    public Color auraColor = Color.white;

    [Header("사운드")]
    [Tooltip("스킬이 나갈 때(시전) 재생. Lv1=총알 발사 순간, Lv2(저격)=Q로 저격 모드 진입 순간")]
    public AudioClip castSound;

    [Tooltip("실제 공격이 적중/발사되는 순간 재생. 저격 스킬에서 클릭으로 발사할 때 사용")]
    public AudioClip attackSound;

    [Range(0f, 1f)] public float castSoundVolume = 1f;
    [Range(0f, 1f)] public float attackSoundVolume = 1f;
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("식별자 (PlayerPrefs 키로 사용. 유니크해야 함)")]
    public string skillId;

    [Header("표시 정보")]
    public string skillName;
    public Sprite icon;
    [TextArea(2, 4)] public string description;

    [Header("비용 (레벨 젬)")]
    public int cost = 20;

    [Header("진화 레벨 (Lv1, Lv2, Lv3... 순서대로 채워넣기)")]
    public SkillLevel[] levels;
}
