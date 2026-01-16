using UnityEngine;
using System.Collections;

public class DoorObject : MonoBehaviour
{
    [Header("문 오브젝트")]
    [SerializeField] private Sprite openSprite;             //문 열렸을 때
    [SerializeField] private Sprite closedSprite;           //문 닫혔을 때
    [SerializeField] private float openDuration = 1.0f;     //문 열려있는 시간


    [Header("적 등장 연출 설정")]
    //적이 문 위치에서 등장 시 오프셋만큼 이동하여 등장
    //(아래로 (0, -0.8, 0), 오른쪽 계단 (1.5, -1, 0), 왼쪽 옆문 (-1.5, 0, 0))
    public Vector3 exitOffset = new Vector3(0, -0.8f, 0);

    
    public float startScale = 0.8f;                     //등장 시 크기 (0.8이면 멀리서 커지며 등장, 1이면 크기 변화 없음)
    public float spawnFacing = 0;                       //등장 시 바라볼 방향 (0이면 플레이어 자동 추적, 1이면 오른쪽, -1이면 왼쪽)

    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (closedSprite == null && spriteRenderer != null) closedSprite = spriteRenderer.sprite;
    }

    public void OpenDoor()
    {
        StopAllCoroutines();
        StartCoroutine(OpenDoorRourine());
    }

    private IEnumerator OpenDoorRourine()
    {
        //문 열림
        if (openSprite != null) spriteRenderer.sprite = openSprite;
        else spriteRenderer.color = Color.black;                     //이미지가 없으면 색으로 대체

        yield return new WaitForSeconds(openDuration);

        // 문 닫힘
        if (openSprite != null) spriteRenderer.sprite = closedSprite;
        else spriteRenderer.color = Color.white;
    }
}
