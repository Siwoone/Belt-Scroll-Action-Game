using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("이동속도 설정")]
    public float moveSpeed = 7f;                //이동속도
    public float waveSpeed = 9.5f;              //웨이브속도
    public float waveDuration = 0.3f;           //웨이브거리

    [Header("플레이어 능력치")]
    public int cuntinueCount = 2;               //남은 목숨
    public int maxHp = 300;                     //최대체력
    public int currentHp = 300;                 //현재체력
    public float comboLimitTime = 0.5f;         //콤보유지시간

    [Header("이동 범위 제한 (바닥 설정)")]
    public float minAreaY = -4.0f;              // 화면 아래쪽 한계선
    public float maxAreaY = -2.2f;              // 화면 위쪽(벽) 한계선

    //대미지 계산
    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0) currentHp = 0;
    }
}
