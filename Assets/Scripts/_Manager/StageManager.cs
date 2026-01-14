using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    //Enemy Wave 정보를 담을 클래스
    [System.Serializable]
    public class EnemyWave
    {
        public string waveName;                                 //구분을 위한 웨이브 이름
        public float triggerXPosition;                          //특정 X좌표를 넘어가면 웨이브 시작
        public List<EnemySpawnInfo> enemies;                    //나올 적 목록
        [HideInInspector] public bool hasTriggered = false;     //발동 되었는지 체크
    }

    [System.Serializable]
    public class EnemySpawnInfo
    {
        public string enemyPoolKey;                             //ObjectPoolManager에 등록된 키
        public enum SpawnSide { Left, Right, Door, Custom };    //스폰 위치
        public SpawnSide spawnSide = SpawnSide.Right;
        public Vector3 spawnOffset;                             //커스텀 선택시 플레이어/카메라 기준 스폰 위치
        public Transform specificSpawnPoint;                    //특정 오브젝트(에: 문)에서 등장 
    }

    [Header("스테이지 설정")]
    [SerializeField] private List<EnemyWave> waves;
    [SerializeField] private string nextSceneName;      //스테이지 클리어 후 이동할 Scene 이름

    //Inspector에서 직접 번호를 지정할 수도 있게 변수 추가 (기본값 -1이면 자동 감지)
    [Tooltip("재생할 BGM 번호. -1이면 씬 이름(Stage1=0, Stage2=1...)에 따라 자동 설정됩니다.")]
    public int stageBgmIndex = -1;

    [Header("게임 시간 설정")]
    public float gameTime = 99f; // 제한 시간 (초)
    private bool isTimeOver = false;

    [Header("참조")]
    private CameraController cameraController;
    private Transform playerTransform;
    private PlayerModel playerModel;
    private PlayerPresenter playerPresenter;
    private PlayerView playerView;
    private Camera mainCamera;

    //화면 효과 제어
    private ScreenFader screenFader;

    //현재 전투 상태 관리
    private int currentEnemyCount = 0;
    private bool isBattleActive = false;
    private bool isSpawning = false;
    private int currentWaveIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //참조한 컴포넌트 초기화
        mainCamera = Camera.main;
        cameraController = FindAnyObjectByType<CameraController>();
        playerModel = FindAnyObjectByType<PlayerModel>();
        playerPresenter = FindAnyObjectByType<PlayerPresenter>();
        screenFader = FindAnyObjectByType<ScreenFader>();

        if (playerPresenter != null)
        {
            playerTransform = playerPresenter.transform;            
            playerModel = playerPresenter.GetComponent<PlayerModel>();
            playerView = playerPresenter.GetComponent<PlayerView>();
        }        
        
        if (playerModel == null) playerModel = FindAnyObjectByType<PlayerModel>();
        if (UIManager.Instance != null) UIManager.Instance.UpdateTime((int)gameTime);

        //스테이지 별 BGM 실행
        PlayStageBGM();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTransform == null || isTimeOver) return;

        //아직 작동하지 않은 EnemyWave가 있는지 확인
        if (currentWaveIndex < waves.Count && !isBattleActive && !isSpawning)
        {
            EnemyWave wave = waves[currentWaveIndex];

            //플레이어가 Trigger 라인을 넘었고, 현재 전투중이 아니라면 실행
            if (!wave.hasTriggered && playerTransform.position.x >= wave.triggerXPosition)
            {
                StartCoroutine(StartWaveRoutine(wave));
            }
        }

        if (!isTimeOver && gameTime > 0)
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
    
    //Enemy Wave 시작 로직
    private IEnumerator StartWaveRoutine(EnemyWave wave)
    {
        Debug.Log($"<color=yellow>웨이브 시작: {wave.waveName}</color>");

        isBattleActive = true;
        isSpawning = true;
        wave.hasTriggered = true;

        //카메라 고정
        if (cameraController != null) cameraController.LockCamera(true);

        //적 스폰
        foreach (var info in wave.enemies)
        {
            //스폰 위치 계산
            float finalX = 0f;
            float finalY = 0f;
            bool isDoorSpawn = false;

            //Door 스폰
            if (info.spawnSide == EnemySpawnInfo.SpawnSide.Door && info.specificSpawnPoint != null)
            {
                isDoorSpawn = true;
                finalX = info.specificSpawnPoint.position.x;
                finalY = info.specificSpawnPoint.position.y;

                //문 열림 효과
                DoorObject door = info.specificSpawnPoint.GetComponent<DoorObject>();
                if (door != null) door.OpenDoor();
            }

            //화면 기준으로 스폰 위치 계산
            else if (info.spawnSide == EnemySpawnInfo.SpawnSide.Custom)
            {
                finalX = wave.triggerXPosition + info.spawnOffset.x;
                finalY = info.spawnOffset.y != 0 ? info.spawnOffset.y : -3.0f;
            }

            else
            {
                float screenWidth = mainCamera.orthographicSize * mainCamera.aspect;
                float cameraX = mainCamera.transform.position.x;

                if (info.spawnSide == EnemySpawnInfo.SpawnSide.Right)
                {
                    finalX = cameraX + screenWidth + info.spawnOffset.x + 1f;
                }

                else
                {
                    finalX = cameraX - screenWidth + info.spawnOffset.x - 1f;
                }
            }

            //오프셋 Y가 0이면 랜덤, 아니면 입력한 고정값 사용 (매복 적 등 대응)
            if (info.spawnOffset.y != 0)
            {
                finalY = info.spawnOffset.y;
            }
            else if (playerModel != null)
            {
                finalY = Random.Range(playerModel.minAreaY, playerModel.maxAreaY);
            }
            else
            {
                finalY = -3.0f;
            }

            Vector3 spawnPos = new Vector3(finalX, finalY, 0);

            //ObjectPoolManager를 통해 적 생성
            GameObject enemyObj = ObjectPoolManager.Instance.SpawnFromPool(info.enemyPoolKey, spawnPos, Quaternion.identity);

            if (enemyObj != null)
            {
                currentEnemyCount++;

                //적이 죽었을 때 플레이어에게 알릴 수 있도록 구독
                EnemyModel enemyModel = enemyObj.GetComponent<EnemyModel>();
                EnemyPresenter enemyPresenter = enemyObj.GetComponent<EnemyPresenter>();

                if (enemyModel != null)
                {
                    //풀링 키 주입 및 초기화
                    enemyModel.poolKey = info.enemyPoolKey;

                    //체력 및 상태 리셋
                    enemyModel.Initialize();
                }

                if (enemyPresenter != null)
                {
                    //프레젠터 변수 초기화 (혹시 꼬였을 경우 대비)
                    enemyPresenter.isDead = false;
                    enemyPresenter.isHit = false;
                    enemyPresenter.isAttacking = false;                    

                    //문에서 나올 때는 Spawn 애니메이션 재생
                    if (isDoorSpawn)
                    {
                        //enemyPresenter.PlaySpawnAnimation();
                        //info.specificSpawnPoint에 붙은 DoorObject 컴포넌트를 찾아서 넘겨줌
                        DoorObject door = info.specificSpawnPoint.GetComponent<DoorObject>();
                        enemyPresenter.PlaySpawnAnimation(door);
                    }

                    //적 스폰 후 플레이어를 바라봄
                    if (playerTransform != null)
                    {
                        float dir = playerTransform.position.x - enemyObj.transform.position.x;
                        enemyPresenter.GetComponent<EnemyView>().Flip(dir);
                    }
                }

                //다시 활성화 될 때 콜라이더 켜기 (죽을 때 껐다면)
                //enemyObj.GetComponent<Collider2D>().enabled = true;
                Collider2D col = enemyObj.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
            }

            //문에서 나올 때는 텀을 조금 더 줌
            float waitTime = isDoorSpawn ? 3.0f : 0.2f;

            //3초마다 순차적으로 스폰
            yield return new WaitForSeconds(waitTime);
        }

        isSpawning = false;
        currentWaveIndex++;
    }

    //스테이지 별 BGM 실행
    public void PlayStageBGM()
    {
        //null 체크
        if (SoundManager.Instance == null) return;

        //BGM 인덱스 초기화
        int bgmIndex = 0;

        //Inspector에서 직접 번호를 정해줬다면 그걸 최우선으로 사용
        if (stageBgmIndex >= 0)
        {
            bgmIndex = stageBgmIndex;
        }

        //씬 이름을 보고 자동 결정
        else
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (sceneName.Contains("Stage1")) bgmIndex = 0;
            if (sceneName.Contains("Stage2")) bgmIndex = 1;
            if (sceneName.Contains("Stage3")) bgmIndex = 2;
        }

        // oundManager 초기화 시 등록한 키값 "Audio Clips"를 사용해 재생
        SoundManager.Instance.PlayBGM("Audio Clips", bgmIndex);
        Debug.Log($"<color=cyan>🎵 BGM 재생: 인덱스 {bgmIndex}</color>");
    }

    //타임 오버
    private void TimeOver()
    {
        isTimeOver = true;
        Debug.Log("<color=red>TIME OVER</color>");

        //타임오버 즉사 처리 시 플레이어가 보고 있는 위치에 따라 날아가는 방향 변경
        if (playerView.transform.localScale.x > 0)
        {   
            //왼쪽
            playerPresenter.OnDamaged(9999, new Vector2(transform.localScale.x * -6f, 2f));
        }
        else
        {
            //오른쪽
            playerPresenter.OnDamaged(9999, new Vector2(transform.localScale.x * 6f, 2f));
        }
    }

    //시간 초기화
    public void ResetTime()
    {
        gameTime = 99f;
        isTimeOver = false;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTime((int)gameTime);
        }
    }

    public void OnEnemyKilled()
    {
        currentEnemyCount--;
        Debug.Log($"<color=yellow>적 처치! 남은 적: {currentEnemyCount}</color>");

        if (currentEnemyCount <= 0 && isBattleActive && !isSpawning)
        {
            EndWave();
        }
    }

    private void EndWave()
    {
        Debug.Log("<color=green>웨이브 클리어! 이동 가능</color>");
        isBattleActive = false;
        currentEnemyCount = 0;

        if (cameraController != null) cameraController.LockCamera(false);

        //모든 Wave를 클리어했다면 다음으로 이동
        if (currentWaveIndex >= waves.Count)
        {
            Debug.Log("스테이지 클리어!");
            StartCoroutine(StageClearRoutine());
        }
        else
        {
            //화살표 UI 표시
        }
    }

    private IEnumerator StageClearRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        //Scene을 바로 넘기지 않고 암전 후 이동
        if (screenFader != null)
        {
            // 페이드 아웃(어두워짐)이 끝날 때까지 대기
            yield return StartCoroutine(screenFader.FadeOutRoutine());
        }

        //다음 Scene 로드
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
