using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;   

    [Header("UI 컴포넌트 연결")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI lifeText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI timeText;

    private Coroutine comboHideRoutine;
    private Coroutine bumpRoutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
        }
    }

    //HP바 업데이트
    public void UpdateHP(int currentHp, int maxHp)
    {
        if (hpSlider != null)
        {
            //슬라이더 값을 0~1 사이 비율로 설정            
            //hpSlider.minValue = 0;
            //hpSlider.maxValue = maxHp;
            hpSlider.value = (float)currentHp / maxHp;
        }
    }

    //목숨 카운트 업데이트
    public void UpdateLife(int count)
    {
        if (lifeText != null)
        {
            lifeText.text = count.ToString($"JIN={count}");
        }
    }

    //점수 업데이트 (예: 000123 형식)
    public void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            //숫자를 6자리 포맷으로 (빈 자리는 0으로 채움)
            scoreText.text = score.ToString("D6");
        }
    }

    //타임 업데이트
    public void UpdateTime(int time)
    {
        if (timeText != null)
        {
            //시간이 10초 이하면 빨간색으로 경고 표시
            if (time <= 10)
            {
                timeText.color = Color.red;
            }

            timeText.text = time.ToString();
        }
    }


    //콤보 카운트 업데이트
    public void UpdateCombo(int comboCount)
    {
        if (comboText == null) return;

        //콤보 카운트가 1을 넘으면 표시
        if (comboCount > 1)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"Combo {comboCount}";

            //콤보 텍스트 튀어오르는 효과 실행
            if (bumpRoutine != null)
            {
                StopCoroutine(bumpRoutine);
            }

            bumpRoutine = StartCoroutine(BumpEffect(comboText.transform));
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    //콤보가 끊겼을 때 UI 숨기기
    public void HideCombo()
    {
        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
        }
    }

    private IEnumerator BumpEffect(Transform target)
    {
        //순간적으로 텍스트 커짐
        target.localScale = Vector3.one * 1.5f;

        //애니메이션 지속 시간
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            //부드럽게 본 크기로 돌아옴
            target.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, elapsed / duration);
            yield return null;
        }

        //1로 고정
        target.localScale = Vector3.one;
    }
}