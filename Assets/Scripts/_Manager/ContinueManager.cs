using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class ContinueManager : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject continuePanel;
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

        continuePanel.SetActive(true);
        SoundManager.Instance.PlaySFX("Audio Clips", 17);
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        int currentCount = countdownTime;
        int currentCoin = continueCoin;

        while (currentCount > 0)
        {
            if (countText != null) countText.text = currentCount.ToString();

            //1초 대기 (중간에 입력을 받기 위해 0.1초씩 쪼개서 대기하거나 Update에서 입력 처리)
            for (int i = 0; i < 10; i++)
            {
                if (CheckContinueInput()) yield break; // 입력 성공 시 루틴 종료
                yield return new WaitForSeconds(0.1f);
            }

            //10부터 1씩 감소
            currentCount--;
            currentCoin--;
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
        if (Input.GetKeyDown(KeyCode.X))
        {
            //이어하기 패널 비활성화
            StopAllCoroutines();
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
        SoundManager.Instance.PlayBGM("Audio Clips", 4);
        yield return new WaitForSeconds(4.0f);

        //BGM 시작 후 4초 뒤에 SFX 재생
        SoundManager.Instance.PlaySFX("Audio Clips", 18);
        continuePanel.SetActive(false);
        onGameOver?.Invoke();                        

        // 타이틀 화면으로 이동
        SceneManager.LoadScene(titleSceneName);
    }
}
