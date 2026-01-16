using UnityEngine;

public class EnemyModel : MonoBehaviour
{
    //적 데이터 참조
    [SerializeField] private EnemyData data;

    //실시간으로 변하는 데이터만 변수로 선언
    [Header("실시간 상태")]
    public int currentHp;    
    public bool isDead = false;
    public bool isInvincible = false;

    //어떤 풀에서 나왔는지 기억하는 변수
    [HideInInspector] public string poolKey;

    //공격 타입 설정 (기본 / 돌진형)
    public enum AttackType { Normal, Dash, Jump }
    public AttackType attackType = AttackType.Normal;

    //돌진 공격 관련 스탯
    public float dashSpeed = 12f;               //돌진 속도
    public float dashDuration = 0.5f;           //돌진 지속 시간
    public float prepareTime = 0.7f;            //돌진 전 준비(기 모으기) 시간

    public float minAreaY = -4.0f;              //화면 아래쪽 한계선
    public float maxAreaY = -2.2f;              //화면 위쪽(벽) 한계선

    //EnemyData의 기본 능력치로 초기화
    public string enemyName => data != null ? data.enemyName : "Enemy";
    public int damage => data.damage;
    public int dashDamage => data.dashDamage;
    public int maxHp => data.maxHp;
    public float moveSpeed => data.moveSpeed;
    public float detectRange => data.detectRange;
    public float stopRange => data.stopRange;
    public float hitRecoveryTime => data.hitRecoveryTime;

    void Awake()
    {
        Initialize();
    }

    //다시 사용될 때 마다 초기화
    public void Initialize()
    {
        if (data != null)
        {
            currentHp = data.maxHp;
            //attackType = EnemyModel.AttackType.Normal; //기본값 리셋 필요 시
        }
        isDead = false;
        isInvincible = false;
    }

    //대미지 계산
    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0)
        {
            //HP가 0 이하가 되면 사망 처리
            currentHp = 0;
            isDead = true;
        }
    }
}
