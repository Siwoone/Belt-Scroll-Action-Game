using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSorter : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    //정밀도 조절 (값이 클수록 더 세밀하게 구분)
    //100을 곱하면 Y좌표 0.01 차이까지 구분해서 순서를 정함
    private const int SORTING_ORDER_MULTIPLIER = 100;

    //기준이 될 오프셋 (필요시 조정)
    //모든 캐릭터가 같은 값을 쓰면 됨
    private const int SORTING_ORDER_OFFSET = 5000;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    //모든 이동이 끝난 후(LateUpdate)에 그리기 순서를 정함
    void LateUpdate()
    {
        if (spriteRenderer != null)
        {
            //Y값이 높을수록(위쪽) -> 뒤로 가야 함 -> Order가 작아야 함
            //Y값이 낮을수록(아래쪽) -> 앞으로 와야 함 -> Order가 커야 함

            //예: Y가 3.5이면 -> -350
            //예: Y가 -2.0이면 -> 200
            //여기에 OFFSET을 더해서 음수가 너무 커지지 않게 관리할 수도 있음

            spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.y * -SORTING_ORDER_MULTIPLIER) + SORTING_ORDER_OFFSET;
        }
    }
}
