using UnityEngine;

public class EnemyModel
{
    //적 데이터 참조
    [SerializeField] private EnemyData data;

    //실시간으로 변하는 데이터만 변수로 선언
    [Header("실시간 상태")]
    public int currentHp;
    public bool isDead = false;

    //EnemyData의 기본 능력치로 초기화
    public float moveSpeed => data.moveSpeed;
    public float detectRange => data.detectRange;
    public float stopRange => data.stopRange;
    public float hitRecoveryTime => data.hitRecoveryTime;

    void Awake()
    {
        //최대 체력으로 현재 체력 초기화
        if(data!=null)
        {
            currentHp = data.maxHp;
        }
    }

    //대미지 계산
    public void OnTakeDamage(int amount)
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
