using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerPresenter : MonoBehaviour
{
    private PlayerModel model; 

    //각 컴포넌트 변수 선언
    private PlayerView view;
    private InputBuffer inputBuffer;

    private Vector2 moveInput;
    private string lastRecordedDir = "";
    
    [Header("5타 콤보 설정")]
    private int comboStep = 0;
    private float lastAttackTime;    

    public bool isWaveStepping = false;
    private bool isAttacking = false;

    void Awake()
    {
        //컴포넌트 할당
        if (view == null) view = FindAnyObjectByType<PlayerView>();
        if (inputBuffer == null) inputBuffer = FindAnyObjectByType<InputBuffer>();
    }

    void Update()
    {
        if (view == null || inputBuffer == null) return;

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

        //짧은 시간 뒤에 속도를 다시 0으로 만들어 공격 위치 고정
        Invoke("StopMovement", 0.15f);

        //애니메이션 재생 시간에 맞춰 공격 상태 리셋 (약 0.5초)
        Invoke("ResetAttackState", 0.5f);
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
        if (view == null) return;

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
}