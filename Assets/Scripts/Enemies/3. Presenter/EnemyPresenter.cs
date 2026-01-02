using UnityEngine;
using System.Collections;

public class EnemyPresenter : MonoBehaviour
{
    //각 컴포넌트 및 모델 변수 선언
    private EnemyView view;
    private EnemyModel model;
    private Transform playerTransform; //추적할 플레이어의 위치

    [Header("상태 확인")]
    private bool isHit = false;   //현재 맞고 있는 중인가?
    private bool isDead = false;  //죽었는가?

    void Awake()
    {
        //컴포넌트 및 모델 할당
        view = GetComponent<EnemyView>();
        model = GetComponent<EnemyModel>();

        //씬 전체에서 플레이어를 찾아 위치 정보를 가져옴
        var player = FindAnyObjectByType<PlayerPresenter>();
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        //피격 중이거나, 죽었거나, 플레이어를 찾지 못했으면 로직 중단
        if (isHit || isDead || playerTransform == null || model == null) return;

        //플레이어와의 거리 계산
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        //모델의 데이터를 참조하여 추적/정지 판단
        if (distance < model.detectRange && distance > model.stopRange)
        {
            //추적 범위 안이고 공격 대기 거리 밖이면 추적
            ChasePlayer();
        }
        else
        {
            //범위를 벗어나거나 너무 가까우면 정지
            Idle();
        }
    }

    //플레이어 방향으로 이동하는 로직
    private void ChasePlayer()
    {
        //플레이어 방향 벡터 계산
        Vector2 direction = (playerTransform.position - transform.position).normalized;

        //모델의 이동 속도를 View에 전달하여 물리 이동
        view.SetVelocity(direction * model.moveSpeed);

        //이동 애니메이션 실행 및 방향 전환
        view.PlayAnimation("Walk");
        view.Flip(direction.x);
    }

    //정지 상태 로직
    private void Idle()
    {
        view.SetVelocity(Vector2.zero);
        view.PlayAnimation("Idle");
    }

    //플레이어의 공격 판정에 의해 호출될 함수 (데미지 전달)
    public void OnDamaged(int damage, Vector2 knockbackForce)
    {
        //이미 죽은 적이면 중단
        if (isDead || model == null) return;

        //모델의 데이터(HP) 갱신
        model.OnTakeDamage(damage);

        //체력 확인 후 사망 또는 피격 루틴 실행
        if (model.currentHp <= 0)
        {
            StartCoroutine(DeathRoutine());
        }
        else
        {
            StartCoroutine(HitRoutine(knockbackForce));
        }
    }

    //피격 리액션 루틴
    private IEnumerator HitRoutine(Vector2 force)
    {
        isHit = true;

        view.FlashRed();            //빨간색 반짝임 효과
        view.PlayAnimation("Hit");  //피격 애니메이션 재생
        view.SetVelocity(force);    //넉백 힘 적용

        //모델에 설정된 경직 시간만큼 대기
        yield return new WaitForSeconds(model.hitRecoveryTime);

        view.SetVelocity(Vector2.zero);
        isHit = false;
    }

    //사망 루틴
    private IEnumerator DeathRoutine()
    {
        isDead = true;
        view.SetVelocity(Vector2.zero);

        view.FlashRed();
        view.PlayAnimation("Hit"); //보통 피격 모션을 짧게 보여주고 사라짐

        //잠시 대기 후 적 오브젝트 삭제
        yield return new WaitForSeconds(0.5f);
        Destroy(gameObject);
    }
}