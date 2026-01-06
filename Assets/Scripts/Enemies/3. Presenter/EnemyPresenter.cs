using System.Collections;
using UnityEngine;
using System;

public class EnemyPresenter : MonoBehaviour
{
    //각 컴포넌트 및 모델 변수 선언
    private EnemyView view;
    private EnemyModel model;
    private Transform playerTransform;  //추적할 플레이어의 위치

    [Header("상태 확인")]
    public bool isHit = false;          //현재 맞고 있는 중인가?
    private bool isDead = false;        //죽었는가?
    public bool isAttacking = false;    //공격 중인가?
    public bool isAirborne = false;     // 에어본 중인가?

    [Header("공격 설정")]
    [SerializeField] private Transform attackPoint; //적의 공격 중심점
    [SerializeField] private Vector2 attackSize;    //적의 공격 범위
    [SerializeField] private LayerMask playerLayer; //플레이어 레이어 (Player)
    private float lastAttackTime;                   //마지막 공격 시간

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
        //피격, 사망, 공격, 에어본 중이거나 플레이어가 없으면 AI 행동 판단 중단
        if (isHit || isDead || isAttacking || isAirborne || playerTransform == null || model == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        //공격 사거리 안이면 공격 시도
        if (distance <= model.stopRange)
        {
            TryAttack();
        }
        //추적 범위 안이면 이동 애니메이션 재생
        else if (distance < model.detectRange)
        {
            view.PlayAnimation("Walk");
        }
        //그 외에는 정지
        else
        {
            view.PlayAnimation("Idle");
        }
    }

    void FixedUpdate()
    {
        //물리적인 이동과 위치 고정은 FixedUpdate에서 처리
        if (isHit || isDead || isAttacking || isAirborne || playerTransform == null || model == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance > model.stopRange && distance < model.detectRange)
        {
            ChasePlayer();
        }
        else
        {
            view.SetVelocity(Vector2.zero);
        }

        //매 물리 프레임마다 바닥 범위를 벗어나지 못하게 고정
        ClampPosition();
    }

    private void TryAttack()
    {
        //공격 쿨타임 확인
        if (Time.time - lastAttackTime > 2.0f)
        {
            StartCoroutine(EnemyAttackRoutine());
        }
        else
        {
            view.SetVelocity(Vector2.zero);
            view.PlayAnimation("Idle");
        }
    }

    // 적 공격 루틴
    private IEnumerator EnemyAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        view.SetVelocity(Vector2.zero); //공격 시작 시 제자리 정지

        //공격 애니메이터 실행
        view.PlayAnimation("Attack1");

        //공격 판정이 발생하는 프레임까지 대기
        yield return new WaitForSeconds(0.2f);

        //플레이어가 범위 안에 있는지 체크
        CheckPlayerHit(model.damage, new Vector2(transform.localScale.x * 1f, 0f));

        //공격 후딜레이
        yield return new WaitForSeconds(0.15f);

        isAttacking = false;
    }

    private void CheckPlayerHit(int damage, Vector2 knockback)
    {
        if (attackPoint == null) return;

        //지정된 범위 내에 플레이어 레이어가 있는지 확인
        Collider2D[] hitPlayer = Physics2D.OverlapBoxAll(attackPoint.position, attackSize, 0f, playerLayer);
        
        foreach (Collider2D player in hitPlayer)
        {
            PlayerPresenter playerPresenter = player.GetComponent<PlayerPresenter>();
            if (player != null)
            {
                //플레이어에게 데미지를 전달하는 로직
                playerPresenter.OnDamaged(damage, knockback);
                Debug.Log($"<color=red>플레이어가 적에게 {damage} 만큼 맞았습니다!</color>");
            }
        }
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.z = 0; //2D 게임에서 Z축이 소수점으로 튀는 것을 방지

        //PlayerModel에 설정된 바닥 한계치를 가져와 적용
        var playerModel = FindAnyObjectByType<PlayerModel>();
        if (playerModel != null)
        {
            pos.y = Mathf.Clamp(pos.y, playerModel.minAreaY, playerModel.maxAreaY);
        }

        transform.position = pos;
    }

    private void ChasePlayer()
    {
        //플레이어 방향 계산 (Z값 무시)
        Vector3 direction3D = (playerTransform.position - transform.position);
        Vector2 direction = new Vector2(direction3D.x, direction3D.y).normalized;

        view.SetVelocity(direction * model.moveSpeed);
        view.Flip(direction.x);
    }

    public void OnDamaged(int damage, Vector2 knockbackForce)
    {
        if (isDead || model == null) return;

        model.TakeDamage(damage);

        //새로운 타격 시 이전의 모든 루틴(공격, 피격)을 중단하고 다시 시작
        StopAllCoroutines();
        isAttacking = false; //공격 중 맞으면 공격 취소

        if (model.currentHp <= 0)
        {
            StartCoroutine(DeathRoutine());
        }
        else
        {
            StartCoroutine(HitRoutine(damage, knockbackForce));
        }
    }

    private IEnumerator HitRoutine(int damage, Vector2 force)
    {
        isHit = true;
        view.FlashRed();

        //맞기 전 원래 서 있던 지면의 Y값을 저장 (에어본 후 복귀용)
        float groundY = transform.position.y;

        //초풍급 데미지를 받았을 때 에어본 처리
        if (damage >= 80)
        {
            isAirborne = true;
            view.PlayAnimation("Airborne");

            //위로 솟구치는 힘 적용
            view.SetVelocity(force);

            //0.6초간 공중 체류
            yield return new WaitForSeconds(0.6f);

            //가짜 중력 적용: 아래로 빠르게 하강
            view.SetVelocity(new Vector2(0, -10f));

            //현재 위치가 원래 지면 높이보다 위에 있는 동안 계속 대기
            while (transform.position.y > groundY)
            {
                yield return null;
            }

            //바닥에 닿으면 위치를 정확히 고정하고 속도 초기화
            Vector3 landedPos = transform.position;
            landedPos.y = groundY;
            transform.position = landedPos;
            view.SetVelocity(Vector2.zero);

            //바닥에 쓰러지는 연출
            view.PlayAnimation("Down");
            yield return new WaitForSeconds(0.6f);

            isAirborne = false;
        }
        else
        {
            //일반 피격
            view.PlayAnimation("Hit");
            view.SetVelocity(force);
            yield return new WaitForSeconds(model.hitRecoveryTime);
            view.SetVelocity(Vector2.zero);
        }

        isHit = false;
        view.PlayAnimation("Idle");
    }

    private IEnumerator DeathRoutine()
    {
        isDead = true;
        isAttacking = false;
        isAirborne = false;

        view.SetVelocity(Vector2.zero);
        view.FlashRed();
        view.PlayAnimation("Down");

        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    //기즈모를 통해 에디터에서 공격 범위를 빨간색 박스로 시각화
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackSize);
    }
}