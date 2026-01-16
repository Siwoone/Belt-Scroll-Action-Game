using Unity.VisualScripting;
using System.Collections;
using UnityEngine;

public class EnemyView : MonoBehaviour
{
    //각 컴포넌트 변수 선언
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;

    void Awake()
    {
        //각 컴포넌트 변수 초기화
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
    }

    public void PlayAnimation(string animName)
    {
        //Presenter가 PlayAnimation을 시키면 해당 애니메이션 재생
        anim.Play(animName);
    }

    public void SetVelocity(Vector2 velocity)
    {
        //Rigidbody2D의 선형 속도 설정
        rb.linearVelocity = velocity;
    }

    public void Flip(float direction)
    {
        //적의 방향 전환
        if (direction != 0)
        {
            //direction이 양수면 오른쪽, 음수면 왼쪽으로 스케일 조정
            transform.localScale = new Vector3(direction > 0 ? 1 : -1, 1, 1);
        }
    }

    //문 등장 연출용 알파값 함수
    public void SetAlpha(float alpha)
    {
        if (sprite != null)
        {
            Color color = sprite.color;
            color.a = alpha;
            sprite.color = color;
        }
    }

    //문 등장 연출용 크기 조절 함수
    public void SetScale(float scaleRatio)
    {
        //현재 바라보는 방향(좌/우) 유지하고 크기만 조절
        float direction = transform.localScale.x >= 0 ? 1 : -1;
        transform.localScale = new Vector3(direction * scaleRatio, scaleRatio, 1);
    }

    //피격 시 하얗게 반짝이는 효과
    public void FlashRed()
    {
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        sprite.color = Color.red; //또는 하얀색
        yield return new WaitForSeconds(0.1f);
        sprite.color = Color.white;
    }
}
