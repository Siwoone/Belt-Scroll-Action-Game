using UnityEngine;
using System.Collections;

public class DoorObject : MonoBehaviour
{
    [Header("문 오브젝트")]
    [SerializeField] private Sprite openSprite;         //문 열렸을 때
    [SerializeField] private Sprite closedSprite;      //문 닫혔을 때
    [SerializeField] private float openDuration = 1.0f; //문 열려있는 시간

    private SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (closedSprite == null) closedSprite = spriteRenderer.sprite;
    }

    public void OpenDoor()
    {
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
