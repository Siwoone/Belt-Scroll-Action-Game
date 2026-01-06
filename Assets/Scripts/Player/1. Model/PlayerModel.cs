using System;
using UnityEngine;

public class PlayerModel : MonoBehaviour
{
    [Header("이동속도 설정")]
    public float moveSpeed = 7f;                //이동속도
    public float waveSpeed = 9.5f;              //웨이브속도
    public float waveDuration = 0.3f;           //웨이브거리

    [Header("플레이어 상태 및 점수")]
    public int continueCount = 2;               //남은 목숨
    public bool isInvincible = false;           //무적 상태인지 체크
    public int score = 0;                       //플레이어 점수

    [Header("플레이어 능력치")]
    public int maxHp = 200;                     //최대체력
    public int currentHp = 200;                 //현재체력
    public float comboLimitTime = 0.6f;         //콤보유지시간

    [Header("이동 범위 제한 (바닥 설정)")]
    public float minAreaY = -5.0f;              //화면 아래쪽 한계선
    public float maxAreaY = -2.2f;              //화면 위쪽(벽) 한계선

    [Header("콤보 시스템")]
    public int currentCombo = 0;                //현재 콤보 수
    public float comboTimer = 0f;               //콤보 타이머
    public float comboDuration = 2.0f;          //콤보 유지 시간

    [Header("게임 시간 설정")]
    public float gameTime = 99f; // 제한 시간 (초)
    private bool isTimeOver = false;

    private void Start()
    {
        //게임 시작 시 초기 UI 상태 동기화
        InitializeUI();
    }

    private void Update()
    {
        //콤보 타이머 감소 로직
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                ResetCombo();
            }
        }

        if(!isTimeOver && gameTime > 0)
        {
            gameTime -= Time.deltaTime;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTime((int)gameTime);
            }

            if (gameTime <= 0)
            {
                gameTime = 0;
                TimeOver();
            }
        }
    }

    private void InitializeUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHP(currentHp, maxHp);
            UIManager.Instance.UpdateScore(score);
            UIManager.Instance.UpdateLife(continueCount);
            UIManager.Instance.UpdateTime((int)gameTime);
        }
    }

    //타임 오버
    private void TimeOver()
    {
        isTimeOver = true;
        Debug.Log("<color = red>TIME OVER</color>");

        //즉사 처리
        TakeDamage(maxHp);
    }

    //대미지 계산
    public void TakeDamage(int amount)
    {
        //무적일 때는 대미지 무시 (타임 오버 제외)
        if (isInvincible && !isTimeOver) return;

        currentHp -= amount;
        if (currentHp <= 0) currentHp = 0;

        // UI 매니저에게 HP바 갱신 요청
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHP(currentHp, maxHp);            
        }
    }

    // 목숨 감소 시 UI 갱신 (PlayerPresenter의 DeathRoutine에서 호출)
    public void DecreaseLife()
    {
        continueCount--;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLife(continueCount);
        }

        if (continueCount >= 0)
        {
            gameTime = 99;
            isTimeOver = false;
        }
    }

    //점수 획득 시 UI 갱신 (적 처치 시 호출)
    public void AddScore(int amount)
    {
        //콤보 증가 및 타이머 리셋
        currentCombo++;
        comboTimer = comboDuration;

        //콤보 점수 계산
        int bonusScore = (currentCombo - 1) * 10;
        int totalScore = amount + bonusScore;

        score += totalScore;

        //UI 갱신
        if (UIManager.Instance != null)
        {
            //스코어 갱신
            UIManager.Instance.UpdateScore(score);
            UIManager.Instance.UpdateCombo(currentCombo);
        }
    }

    private void ResetCombo()
    {
        if (currentCombo > 0)
        {
            Debug.Log("콤보종료");
            currentCombo = 0;

            //콤보 UI 숨기기
            if (UIManager.Instance != null)
            {
                UIManager.Instance.HideCombo();
            }
        }
    }
}
