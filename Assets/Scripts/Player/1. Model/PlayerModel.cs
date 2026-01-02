using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("이동속도 설정")]
    public float moveSpeed = 7f;                //이동속도
    public float waveSpeed = 9.5f;              //웨이브속도
    public float waveDuration = 0.3f;           //웨이브거리

    [Header("플레이어 능력치")]
    public int maxHp = 100;                     //최대체력
    public int currentHp = 100;                 //현재체력
    public float comboLimitTime = 0.5f;         //콤보유지시간

    //대미지 계산
    public void OnTakeDamage(int amount)
    {
        currentHp -= amount;
        if (currentHp <= 0) currentHp = 0;
    }
}
