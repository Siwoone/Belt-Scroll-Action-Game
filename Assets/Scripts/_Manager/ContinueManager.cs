using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections;
using System;

public class ContinueManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject continuePanel;
    public TextMeshProUGUI continueText;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI continueCoinText;

    [Header("설정")]
    public int countdownTime = 10;
    public int continueCoin = 2;
    public string titleSceneName = "TitleScene";

    //PlayerPresenter에서 사용할 콜백
    private Action onContinueSuccessed;
    private Action onGameOver;

    public void StartContinueSequence(Action onSuccess, Action onFail)
    {
        this.onContinueSuccessed = onSuccess;
        this.onGameOver = onFail;
        //코루틴 실행을 위해 나 자신을 먼저 켠다
        this.gameObject.SetActive(true);

        //코인이 없으면 게임 오버
        if (continueCoin <= 0 )
        {
            Debug.Log("이어하기 코인 부족. 즉시 게임오버.");
            StartCoroutine(GameOverRoutine());
            return;
        }

        //코인이 있으면 패널 열고 카운트다운 시작
        continuePanel.SetActive(true);
        SoundManager.Instance.PlaySFX("Audio Clips", 17);
        StartCoroutine(CountdownRoutine());
    }

    //게임 클리어 시 호출 함수
    public void GameClear(int finalScore)
    {
        //코루틴 실행을 위해 나 자신을 먼저 켠다
        this.gameObject.SetActive(true);
        StartCoroutine(GameClearRoutine(finalScore));
    }

    private IEnumerator CountdownRoutine()
    {
        int currentCount = countdownTime;
        SoundManager.Instance.PlayBGM("Audio Clips", 3);

        while (currentCount > 0)
        {
            if (countText != null) countText.text = currentCount.ToString();
            if (continueCoinText != null) continueCoinText.text = $"Credits: {continueCoin}";

            //1초 동안 매 프레임 입력을 감지하도록 변경
            float timer = 0f;            
            while (timer < 1.0f)
            {
                //입력 성공 시 루틴 종료 바로 탈출
                if (CheckContinueInput()) yield break;  

                //흐른 시간 누적
                timer += Time.deltaTime;                
                yield return null;
            }

            //10부터 1씩 감소
            currentCount--;
            SoundManager.Instance.PlaySFX("Audio Clips", 20 + currentCount);

            if (currentCount >= 4) SoundManager.Instance.PlaySFX("Audio Clips", 14);
            if (currentCount <= 3 && currentCount >= 1) SoundManager.Instance.PlaySFX("Audio Clips", 15);
        }

        //continueCoin이 0보다 작거나 currentCount가 0이될 때까지 입력이 없으면 GameOver
        if (countText != null || continueCoin < 0) countText.text = "0";
        SoundManager.Instance.PlaySFX("Audio Clips", 16);
        yield return new WaitForSeconds(1.0f);
        StartCoroutine(GameOverRoutine());
    }

    private bool CheckContinueInput()
    {
        //키보드가 연결되어 있고 X키가 이번 프레임에 눌렸는지 확인
        bool xPressed = Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;

        if (xPressed && continueCoin > 0)
        {
            //보유하고 있는 이어하기 코인 감소
            continueCoin--;            

            //코인 UI 갱신
            if (continueCoinText != null) continueCoinText.text = $"Credits: {continueCoin}";

            //이어하기 패널 비활성화
            StopAllCoroutines();

            //패널 끄기
            continuePanel.SetActive(false);

            //이어하기 성공 콜백 전달
            onContinueSuccessed?.Invoke();            
            return true;
        }
        return false;
    }

    private IEnumerator GameOverRoutine()
    {
        Debug.Log("Game Over");
        continueText.text = "";
        countText.text = "GAME OVER";
        SoundManager.Instance.PlayBGM("Audio Clips", 4);
        yield return new WaitForSeconds(4.0f);

        //BGM 시작 후 4초 뒤에 SFX 재생
        SoundManager.Instance.PlaySFX("Audio Clips", 18);        
        onGameOver?.Invoke();

        //오브젝트 전부 파괴
        CleanupObjects();

        //20초 뒤 타이틀 화면으로 이동     
        yield return new WaitForSeconds(4.0f);
        GoToTitle();    
    }

    private IEnumerator GameClearRoutine(int score)
    {
        Debug.Log("Game Clear!");

        // 패널을 켜고 메시지 변경
        continuePanel.SetActive(true);
        if (continueText != null) continueText.text = "CONGRATULATIONS!";
        if (countText != null) countText.text = $"SCORE: {score}";

        //코인 텍스트는 숨기거나 클리어 보너스로 표시 가능 (여기선 숨김)
        if (continueCoinText != null) continueCoinText.text = "";

        //승리 BGM 재생 (없으면 4번 혹은 다른 번호 사용)
        //SoundManager.Instance.StopBGM();
        SoundManager.Instance.PlaySFX("Audio Clips", 34); // 팡파레 효과음 등      

        //결과창 보여주고 게임오버로 이동
        yield return new WaitForSeconds(4.0f);
        StartCoroutine(GameOverRoutine());
    }

    private void CleanupObjects()
    {
        //씬 이동 전 모든 적 비활성화        
        var enemies = FindObjectsByType<EnemyPresenter>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            //ObjectPoolManager가 있다면 반환, 없다면 비활성화
            if (ObjectPoolManager.Instance != null && enemy.GetComponent<EnemyModel>() != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(enemy.GetComponent<EnemyModel>().poolKey, enemy.gameObject);
            }
            else
            {
                enemy.gameObject.SetActive(false);
            }
        }
    }

    private void GoToTitle()
    {                
        SceneManager.LoadScene(titleSceneName);

        //DonDestroy 상태의 매니저 파괴
        continuePanel.SetActive(false);
        if (UIManager.Instance != null) Destroy(UIManager.Instance.gameObject);
        if (MasterManager.Instance != null) Destroy(MasterManager.Instance.gameObject);
    }
}
