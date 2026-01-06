using UnityEngine;

public class PlayerView : MonoBehaviour
{
    //각 컴포넌트 변수 선언
    private Animator anim;
    private Rigidbody2D rb;

    void Awake()
    {
        //컴포넌트 변수 초기화
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }
    
    public void PlayAnimation(string animName)
    {
        //Presenter가 PlayAnimation을 시키면 해당 애니메이션 재생
        anim.Play(animName);
    }

    public void SetVelocity(Vector2 velocity)
    {
        //Rigidbody2D의 선형 속도 설정 (넉백 이동 등 물리적 이동 처리)
        rb.linearVelocity = velocity;
    }

    public void Flip(float direction)
    {
        //플레이어의 방향 전환
        if (direction != 0)
        {
            //direction이 양수면 오른쪽, 음수면 왼쪽으로 스케일 조정
            transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
        }
    }

    public void SetTrigger(string triggerName)
    {
        //애니메이터의 트리거 설정
        anim.SetTrigger(triggerName);
    }
    
    public void SetFloat(string name, float value) => anim.SetFloat(name, value);   //애니메이터의 float 파라미터 설정
    public void SetBool(string name, bool value) => anim.SetBool(name, value);      //애니메이터의 bool 파라미터 설정
}