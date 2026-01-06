using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class PlayerPresenter : MonoBehaviour
{    
    //각 컴포넌트 변수 선언    
    private PlayerModel model;
    [SerializeField] private PlayerView view;
    [SerializeField] private InputBuffer inputBuffer;

    private Vector2 moveInput;
    private string lastRecordedDir = "";
    
    [Header("5타 콤보 설정")]
    private int comboStep = 0;
    private float lastAttackTime;

    [Header("공격 판정 설정")]
    [SerializeField] private Transform attackPoint;    //공격 위치
    [SerializeField] private Vector2 attackSize;        //공격 범위
    [SerializeField] private LayerMask enemyLayer;      //적 레이어

    public bool isWaveStepping = false;
    private bool isAttacking = false;
    private bool isHit = false;
    private bool isAirborne = false;
    private bool isDead = false;

    void Awake()
    {
        //컴포넌트 할당
        if (model == null) model = FindAnyObjectByType<PlayerModel>();
        if (view == null) view = FindAnyObjectByType<PlayerView>();
        if (inputBuffer == null) inputBuffer = FindAnyObjectByType<InputBuffer>();
        if (attackPoint == null) attackPoint = transform.Find("AttackPoint");
    }

    void Update()
    {
        if (view == null || inputBuffer == null || model == null) return;

        //공격이나 웨이브 중이 아닐 때만 일반 이동 상태를 View에 전달
        if (!isWaveStepping && !isAttacking)
        {
            view.SetFloat("MoveX", moveInput.x);
            view.SetFloat("MoveY", moveInput.y);
            view.SetBool("isMoving", moveInput != Vector2.zero);
            view.Flip(moveInput.x);
        }

        //일정 시간이 지나면 콤보 단계 초기화
        if (Time.time - lastAttackTime > model.comboLimitTime)
        {
            comboStep = 0;
        }

        DetectDirectionChange();

        ClampPosition();
    }

    //화면 밖으로 나가지 않도록 위치 제한
    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(pos);
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);
        transform.position = Camera.main.ViewportToWorldPoint(viewportPos);

        //Y축 위치를 모델에 설정한 최소/최대값 사이로 가둠
        pos.y = Mathf.Clamp(pos.y, model.minAreaY, model.maxAreaY);
        transform.position = pos;
    }

    //Input System 메시지 수신
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    //기본 공격 및 콤보 로직
    public void OnAttack()
    {
        if (inputBuffer == null) return;

        inputBuffer.RecordInput("Attack");

        if (isWaveStepping)
        {
            ExecuteWindFist();
        }

        //현재 공격 애니메이션 재생 중이라면 중복 입력 방지
        if (isAttacking) return;

        StartCoroutine(BasicAttackRoutine());
    }

    //기본 5타 콤보 루틴
    private IEnumerator BasicAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        comboStep++;

        //5타를 초과하면 1타로 복귀
        if (comboStep > 5) comboStep = 1;

        //애니메이션 실행
        string attackAnimName = "Attack" + comboStep;
        view.PlayAnimation(attackAnimName);

        //공격 시 전진 가속도 부여
        float forwardForce = 0f;
        if (comboStep == 4) forwardForce = 0.5f;
        if (comboStep == 5) forwardForce = 2f;

        view.SetVelocity(new Vector2(transform.localScale.x * forwardForce, 0));

        //애니메이션 프레임 길이에 맞춰 대기
        float attackDuration = 0.2f; //1~3타 기본값
        if (comboStep == 4) attackDuration = 0.45f; //4타
        else if (comboStep == 5) attackDuration = 0.7f;

        //애니메이션 재생 직후 대미지 판정
        //콤보 단계에 따라 대미지 차등 적용
        int damage = comboStep * 10;                                    //예: 1타=10, 2타=20, ..., 5타=50
        ChackHit(damage, new Vector2(transform.localScale.x * 1f, 0f)); //넉백 백터

        yield return new WaitForSeconds(attackDuration);

        //공격 정지 및 상태 복구
        view.SetVelocity(Vector2.zero);
        isAttacking = false;

        //콤보 단계에 따른 후처리 및 Idle 복귀
        if (comboStep == 5) comboStep = 0;
        view.PlayAnimation("Idle");
    }

    //웨이브 스텝
    public void StartWaveStep()
    {
        if (isWaveStepping || isAttacking) return;
        StartCoroutine(WaveStepRoutine());
    }

    private IEnumerator WaveStepRoutine()
    {
        isWaveStepping = true;
        view.PlayAnimation("Wave");

        float timer = 0f;
        float dashDir = transform.localScale.x;

       
        while (timer < model.waveDuration)
        {
            // 공격(초풍)이 입력되면 코루틴을 즉시 종료
            if (isAttacking) yield break;

            view.SetVelocity(new Vector2(dashDir * model.waveSpeed, 0));
            timer += Time.deltaTime;
            yield return null;
        }

        // 끝났을 때 공격 중이 아니면 멈춤
        if (!isAttacking)
        {
            StopMovement();
            isWaveStepping = false;
            view.PlayAnimation("Idle");
        }
    }

    //초풍 발동 (웨이브 중 공격 입력 시 호출)
    public void ExecuteWindFist()
    {        
        if (view == null) return;

        isAttacking = true;
        isWaveStepping = false;
        StopAllCoroutines(); //진행 중인 웨이브 루틴 중단

        //웨이브 속도를 0으로 초기화
        view.SetVelocity(Vector2.zero);
        //초풍 애니메이션 실행 및 앞으로 조금 전진
        view.PlayAnimation("WindFist");        
        view.SetVelocity(new Vector2(transform.localScale.x * 4f, 0));

        //초풍 공격 판정
        int damage = 80;
        ChackHit(damage, new Vector2(transform.localScale.x * 6f, 2f)); //넉백 백터       

        //짧은 시간 뒤에 속도를 다시 0으로 만들어 공격 위치 고정
        Invoke("StopMovement", 0.15f);

        //애니메이션 재생 시간에 맞춰 공격 상태 리셋 (약 0.5초)
        Invoke("ResetAttackState", 0.5f);
    }

    private void ChackHit(int damage, Vector2 knockback)
    {
        if (attackPoint == null) return;

        //공격 포인트 위치에서 지정한 크기만큼의 박스 안에 있는 모든 콜라이더 검출        
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackPoint.position, attackSize, 0f, enemyLayer);

        foreach (Collider2D enemy in hitEnemies)
        {
            //적의 Presenter 컴포넌트에 대미지 및 넉백 정보 전달
            EnemyPresenter enemyPresenter = enemy.GetComponent<EnemyPresenter>();
            if (enemyPresenter != null)
            {
                enemyPresenter.OnDamaged(damage, knockback);
                Debug.Log($"<color=cyan>{enemy.name}에게 {damage} 데미지!</color>");
            }
        }
    }

    private void StopMovement()
    {
        if (view != null) view.SetVelocity(Vector2.zero);
    }

    private void ResetAttackState() 
    { 
        isAttacking = false; 
        if(view != null)
        {
            view.PlayAnimation("Idle");
        }
    }

    void FixedUpdate()
    {
        if (view == null || model == null) return;

        //기술 사용 중이 아닐 때의 일반 이동 물리 처리
        if (!isWaveStepping && !isAttacking)
            view.SetVelocity(moveInput * model.moveSpeed);
    }

    //방향 전환을 감지하여 인풋 버퍼에 기록
    private void DetectDirectionChange()
    {
        if (inputBuffer == null) return;

        string currentDir = "";
        float lookDir = transform.localScale.x; // 1이면 오른쪽, -1이면 왼쪽

        //캐릭터가 보는 방향이 앞으로 판단
        bool isForward = (lookDir > 0 && moveInput.x > 0.3f) || (lookDir < 0 && moveInput.x < -0.3f);
        bool isDown = moveInput.y < -0.5f;

        if (isForward && isDown) currentDir = "ForwardDown"; 
        else if (isDown) currentDir = "Down";               
        else if (isForward) currentDir = "Forward";         

        if (currentDir != "" && currentDir != lastRecordedDir)
        {
            inputBuffer.RecordInput(currentDir);
            lastRecordedDir = currentDir;
            Debug.Log($"버퍼 기록: {currentDir}"); // 디버깅용 로그
        }
        else if (moveInput == Vector2.zero)
        {
            lastRecordedDir = "";
        }
    }

    public void OnDamaged(int damage, Vector2 knockbackForce)
    {
        if (isDead || model == null) return;

        model.TakeDamage(damage);

        //새로운 타격 시 이전의 모든 루틴(공격, 피격)을 중단하고 다시 시작
        StopAllCoroutines();
        isAttacking = false;

        if (model.currentHp <= 0)
        {
            //사망처리
            StartCoroutine(DeathRoutine());
        }
        else
        {
            //피격처리
            StartCoroutine(HitRoutine(damage, knockbackForce));
        }
    }

    private IEnumerator HitRoutine(int damage, Vector2 force)
    {
        isHit = true;

        //맞기 전 원래 서 있던 지면의 Y값을 저장 (에어본 후 복귀용)
        float groundY = transform.position.y;

        //큰 대미지를 받았을 때 에어본 처리
        if (damage >= 50)
        {
            isAirborne = true;
            view.PlayAnimation("Airborne");

            //위로 솟구치는 힘 적용
            view.SetVelocity(force);

            //공중 체류 시간 적용
            yield return new WaitForSeconds(0.6f);

            //가짜 중력 적용: 아래로 빠르게 하당
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
            view.PlayAnimation("WakeUp");
            yield return new WaitForSeconds(0.55f);

            isAirborne = false;
        }
        else
        {
            //일반 피격
            view.PlayAnimation("Hit");
            view.SetVelocity(force);
            yield return new WaitForSeconds(0.4f);
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
        view.PlayAnimation("Down");

        yield return new WaitForSeconds(1.0f);

        if (model.cuntinueCount < 0)
        { 
            Destroy(gameObject); 
        }
        //else
        //{
        //    Instantiate();
        //}
    }


    //기즈모를 통해 에디터에서 공격 범위를 빨간색 박스로 시각화
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackSize);
    }
}