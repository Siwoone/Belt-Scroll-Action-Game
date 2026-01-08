using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("추적 대상 설정")]
    [SerializeField] public Transform player;

    [Header("카메라 설정")]
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private float xOffset = 2.5f;                          //캐릭터를 카메라의 약간 왼쪽으로 둠

    [Header("카메라 제한")]
    public float minX;                                                      //현재 카메라가 갈 수 있는 최소 X좌표
    public float maxX = 100f;                                               //맵의 끝

    public bool isLocked = false;                                           //전투 중 카메라 고정 여부
    
    private float fixedY;                                                   //게임 시작 시 Y, Z축 높이 고정
    private float fixedZ;

    void Start()
    {
        //null 체크, 없으면 PlayerPresenter.cs가 붙은 Object를 찾는다
        if (player == null)
        {
            var p = FindAnyObjectByType<PlayerPresenter>();
            if (p != null) player = p.transform;
        }

        //시작 시 카메라 위치값을 최소값으로 초기화
        minX = transform.position.x;
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (player == null) return;

        //목표의 위치 계산
        float targetX = player.position.x + xOffset;

        //역행 방지
        //플레이어가 뒤로 가도 카메라는 min에서 고정
        if (targetX < minX) targetX = minX;

        //맵 끝 제한
        if (targetX > maxX) targetX = maxX;

        //전투 중 카메라 고정 상태
        if (!isLocked)
        {
            //앞으로 전진할 때 mixX 값을 갱신
            if (targetX > minX) minX = targetX;
        }

        //실제 카메라 이동 로직
        //전투 중(isLocked)이어도 minX 위치는 유지해야 하므로 minX를 기준으로 렌더링
        Vector3 desiredPosition = new Vector3(minX, transform.position.y, fixedZ);

        //Lerp를 이용해 부드럽게 이동 처리
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        transform.position = smoothedPosition;        
    }

    //전투 시 카메라 고정
    public void LockCamera(bool lockState)
    {
        isLocked = lockState;
    }
}
