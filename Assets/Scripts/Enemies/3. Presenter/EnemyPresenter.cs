using System.Collections;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class EnemyPresenter : MonoBehaviour
{
    //각 컴포넌트 및 모델 변수 선언
    private EnemyView view;
    private EnemyModel model;
    private Transform playerTransform;  //추적할 플레이어의 위치
    private Camera mainCamera;

    [Header("상태 확인")]
    public bool isHit = false;              //현재 맞고 있는 중인가?
    public bool isDead = false;             //죽었는가?
    public bool isAttacking = false;        //공격 중인가?
    public bool isAirborne = false;         //에어본 중인가?
    private bool isInCamera = false;        //적이 화면 안에 있는가?
    public bool isSpawningState = false;    //스폰 연출 중인가?

    [Header("공격 설정")]
    [SerializeField] private Transform attackPoint; //적의 공격 중심점
    [SerializeField] private Vector2 attackSize;    //적의 공격 범위
    [SerializeField] private LayerMask playerLayer; //플레이어 레이어
    private float lastAttackTime;                   //마지막 공격 시간

    void Awake()
    {
        //컴포넌트 및 모델 할당
        view = GetComponent<EnemyView>();
        model = GetComponent<EnemyModel>();
        mainCamera = Camera.main;


        //씬 전체에서 플레이어를 찾아 위치 정보를 가져옴
        var player = FindAnyObjectByType<PlayerPresenter>();
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        //피격, 사망, 공격, 에어본 중이거나 플레이어가 없으면 AI 행동 판단 중단
        if (isHit || isDead || isAttacking || isAirborne || isSpawningState || playerTransform == null || model == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        //공격 타입에 따라 사정거리 판단
        bool canAttack = false;

        if (model.attackType == EnemyModel.AttackType.Dash)
        {
            //대시 타입은 5.0f 거리 안으로 들어오면 공격 시도 (일반보다 멈)
            if (distance <= 5.0f) canAttack = true;
        }

        else
        {
            //일반 타입은 stopRange(1.5f)까지 붙어야 공격
            if (distance <= model.stopRange) canAttack = true;
        }

        if (canAttack)
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
        if (isHit || isDead || isAttacking || isAirborne || isSpawningState || playerTransform == null || model == null) return;

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

    //적 스폰 애니메이션 코루틴 실행
    public void PlaySpawnAnimation(DoorObject doorInfo = null)
    {
        StartCoroutine(SpawnRoutine(doorInfo));
    }

    //스폰 코루틴
    private IEnumerator SpawnRoutine(DoorObject doorInfo)
    {
        isSpawningState = true;
        view.SetVelocity(Vector2.zero);

        //문 안쪽에서 밖으로 나오는 듯한 느낌
        Vector3 startPos = transform.position;
        Vector3 targetPos;
        float sScale = 0.8f;
        string animName = "Spawn";

        //좌/우 문에서 나올 경우 계단에서 걸어 내려오는 느낌
        if (doorInfo != null)
        {
            //문에서 설정한 방향대로 나감 (계단이면 대각선, 옆문이면 좌우 등)
            targetPos = startPos + doorInfo.exitOffset;
            sScale = doorInfo.startScale;

            //X축 오프셋이 거의 0보다 크거나 작을 경우 Walk 애니메이션 클립 실행
            if (Mathf.Abs(doorInfo.exitOffset.x) > 0.1f || Mathf.Abs(doorInfo.exitOffset.x) < -0.1f)
            {
                animName = "Walk";
            }

            //방향 설정
            if (doorInfo.spawnFacing != 0) view.Flip(doorInfo.spawnFacing);
            else if (playerTransform != null) view.Flip(playerTransform.position.x - startPos.x);
        }

        else
        {
            //기본값 (아래로 등장)
            targetPos = startPos - new Vector3(0, 0.8f, 0);
            if (playerTransform != null) view.Flip(playerTransform.position.x - startPos.x);
        }

        //투명상태에서 점점 크게 등장
        view.SetAlpha(0f);
        view.SetScale(sScale);

        //애니메이션 초기설정값 복원
        view.PlayAnimation(animName);

        float duration = 1.0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            //Fade in 효과
            view.SetAlpha(Mathf.Lerp(0f, 1f, t));

            //원근감 효과
            view.SetScale(Mathf.Lerp(0.8f, 1f, t));

            //문에서 걸어서 나옴
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        //투명도 및 크기 고정
        view.SetAlpha(1f);
        view.SetScale(1f);
        transform.position = targetPos;

        //스폰 종료
        isSpawningState = false;
        view.PlayAnimation("Idle");
    }

    //적 공격 시도
    private void TryAttack()
    {
        //공격 쿨타임 확인
        if (Time.time - lastAttackTime > 2.0f)
        {
            if (model.attackType == EnemyModel.AttackType.Dash)
            {
                StartCoroutine(DashAttackRoutine());
            }
            else
            {
                StartCoroutine(NormalAttackRoutine());
            }
        }
        else
        {
            view.SetVelocity(Vector2.zero);
            view.PlayAnimation("Idle");
        }
    }

    //적 노멀 공격 루틴
    private IEnumerator NormalAttackRoutine()
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

    //적 대쉬 공격 루틴
    private IEnumerator DashAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        view.SetVelocity(Vector2.zero);

        //모으기 동작 실행
        view.PlayAnimation("Prepare");

        //플레이어를 향해 방향 고정
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        view.Flip(dirToPlayer.x);

        //준비 시간 동안 붉어지는 효과
        view.FlashRed();
        yield return new WaitForSeconds(model.prepareTime);

        //돌진 공격
        view.PlayAnimation("Dash");

        //돌진 시간
        float timer = 0f;

        //플레이어와 충돌 했는지 체크
        bool hasHitPlayer = false;

        //돌진 지속시간까지 동작
        while (timer < model.dashDuration)
        {
            //플레이어 방향으로 빠르게 이동
            view.SetVelocity(new Vector2(dirToPlayer.x * model.dashSpeed, 0)); 

            if (!hasHitPlayer)
            {
                Collider2D hit = Physics2D.OverlapBox(attackPoint.position, attackSize, 0, playerLayer);
                if (hit != null)
                {
                    //돌진 중에는 몸 자체가 히트박스
                    CheckPlayerHit(model.dashDamage, new Vector2(transform.localScale.x * 6f, 2f));
                    
                    //이번 돌진에서는 더 이상 때리지 않음
                    hasHitPlayer = true;
                }
            }                        
            timer += Time.deltaTime;
            yield return null;
        }

        //돌진 종료
        view.SetVelocity(Vector2.zero);
        view.PlayAnimation("Idle");
        yield return new WaitForSeconds(0.8f);
        isAttacking = false;
    }

    //히트 됐는지 확인
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

    //Y축 고정
    private void ClampPosition()
    {
        Vector3 pos = transform.position;

        //화면(Viewport) 좌표로 변환 (0~1 사이 값)
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(pos);

        if (viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1)
        {
            //화면 안에 한 번이라도 들어오면 체크
            isInCamera = true;
        }

        //화면 안에 들어온 적만 못 나가게 가둠
        if (isInCamera)
        {
            viewportPos.x = Mathf.Clamp(viewportPos.x, 0.05f, 0.95f);
            pos = mainCamera.ViewportToWorldPoint(viewportPos);
        }

        //2D 게임에서 Z축이 소수점으로 튀는 것을 방지
        pos.z = 0; 

        //PlayerModel에 설정된 바닥 한계치를 가져와 적용
        var playerModel = FindAnyObjectByType<PlayerModel>();
        if (playerModel != null)
        {
            pos.y = Mathf.Clamp(pos.y, playerModel.minAreaY, playerModel.maxAreaY);
        }

        transform.position = pos;
    }

    //플레이어 인지 후 따라다니기
    private void ChasePlayer()
    {
        //플레이어 방향 계산 (Z값 무시)
        Vector3 direction3D = (playerTransform.position - transform.position);
        Vector2 direction = new Vector2(direction3D.x, direction3D.y).normalized;

        view.SetVelocity(direction * model.moveSpeed);
        view.Flip(direction.x);
    }

    //대미지 받았을 때
    public void OnDamaged(int damage, Vector2 knockbackForce)
    {
        if (isDead || model == null || model.isInvincible) return;

        model.TakeDamage(damage);

        //UIManager 화면 갱신
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateEnemyUI(model.currentHp, model.maxHp, model.enemyName);
        }

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
        if (damage >= 50)
        {
            GetComponent<Collider2D>().enabled = false;

            isAirborne = true;
            view.PlayAnimation("Airborne");
            SoundManager.Instance.PlaySFX("Audio Clips", 30);

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
            GetComponent<Collider2D>().enabled = true;
        }
        else
        {
            //일반 피격
            view.PlayAnimation("Hit");
            view.SetVelocity(force);
            int soundIndex = Random.Range(12, 14);                      //랜덤 히트 사운드 출력
            SoundManager.Instance.PlaySFX("Audio Clips", soundIndex);
            yield return new WaitForSeconds(model.hitRecoveryTime);
            view.SetVelocity(Vector2.zero);
        }

        isHit = false;
        view.PlayAnimation("Idle");
    }

    private IEnumerator DeathRoutine()
    {
        isDead = true;
        isHit = false;
        isAttacking = false;
        isAirborne = false;

        //죽을 때 콜라이더를 꺼서 추가 타격 방지 (선택 사항)
        GetComponent<Collider2D>().enabled = false;

        //죽었을 때 깜빡거리기
        StartCoroutine(InvincibilityRoutine());

        view.SetVelocity(Vector2.zero);
        view.FlashRed();
        view.PlayAnimation("Down");

        //스테이지 매니저에게 죽음 보고
        var stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager != null)
        {
            stageManager.OnEnemyKilled();
        }

        yield return new WaitForSeconds(1.0f);

        //ObjectPool로 반환
        if (ObjectPoolManager.Instance != null && model != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(model.poolKey, gameObject);
        }
        else
        {
            //풀 매니저가 없으면 그냥 끄기
            gameObject.SetActive(false);
        }
    }

    private IEnumerator InvincibilityRoutine()
    {
        model.isInvincible = true;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();

        //3초간 무적 시간 부여 (10 = 2초)
        for (int i = 0; i < 15; i++)
        {
            sprite.color = new Color(1, 1, 1, 0.5f); // 반투명
            yield return new WaitForSeconds(0.1f);
            sprite.color = new Color(1, 1, 1, 1f);   // 불투명
            yield return new WaitForSeconds(0.1f);
        }

        model.isInvincible = false;
    }

    //기즈모를 통해 에디터에서 공격 범위를 빨간색 박스로 시각화
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackSize);
    }
}