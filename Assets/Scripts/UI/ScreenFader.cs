using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading;
using Unity.Mathematics;


public class ScreenFader : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public float fadeDuration = 1.0f;           //화면이 변하는 시간

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        //씬 이동 시 암전 상태로 시작
        if (canvasGroup != null )
        {
            canvasGroup.alpha = 1.0f;
            StartCoroutine(FadeInRoutine());
        }
    }

    public IEnumerator FadeInRoutine()
    {
        //타이머 초기화
        float timer = 0f;

        while (timer < fadeDuration)
        {
            //타이머 증가
            timer += Time.deltaTime;

            //알파값을 1부터 0까지 서서히 감소
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }

        //알파값 0으로 고정
        canvasGroup.alpha = 0f;

        //터치, 클릭 가능하게 해제
        canvasGroup.blocksRaycasts = false;
    }

    public IEnumerator FadeOutRoutine()
    {
        //터치, 클릭 불가능하게 설정
        canvasGroup.blocksRaycasts = true;

        //타이머 초기화
        float timer = 0f;

        while (timer < fadeDuration)
        {
            //타이머 증가
            timer += Time.deltaTime;

            //알파값을 0부터 1까지 서서히 증가
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        //알파값 1로 고정
        canvasGroup.alpha = 1f;
    }
}
