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
        public string enemyPoolKey;     //ObjectPoolManager에 등록된 키
        public Vector3 spawnOffset;     //플레이어/카메라 기준 스폰 위치
    }

    [Header("스테이지 설정")]
    [SerializeField] private List<EnemyWave> waves;
    [SerializeField] private string nextSceneName;      //스테이지 클리어 후 이동할 Scene 이름

    [Header("게임 시간 설정")]
    public float gameTime = 99f; // 제한 시간 (초)
    private bool isTimeOver = false;

    [Header("참조")]
    private CameraController cameraController;
    private Transform playerTransform;
    private PlayerModel playerModel;
    private PlayerPresenter playerPresenter;
    private PlayerView playerView;

    //현재 전투 상태 관리
    private int currentEnemyCount = 0;
    private bool isBattleActive = false;
    private bool isSpawning = false;
    private int currentWaveIndex = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //참조한 컴포넌트 초기화
        cameraController = FindAnyObjectByType<CameraController>();
        playerModel = FindAnyObjectByType<PlayerModel>();
        playerPresenter = FindAnyObjectByType<PlayerPresenter>();
        
        if (playerPresenter != null)
        {
            playerTransform = playerPresenter.transform;            
            playerModel = playerPresenter.GetComponent<PlayerModel>();
            playerView = playerPresenter.GetComponent<PlayerView>();
        }        
        
        if (playerModel == null) playerModel = FindAnyObjectByType<PlayerModel>();
        if (UIManager.Instance != null) UIManager.Instance.UpdateTime((int)gameTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerTransform == null || isTimeOver) return;

        //아직 작동하지 않은 EnemyWave가 있는지 확인
        if (!isBattleActive || isSpawning || currentWaveIndex < waves.Count)
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
            //Vector3 spawnPos = new Vector3(wave.triggerXPosition, 0, 0) + info.spawnOffset;
            float finalX = wave.triggerXPosition + info.spawnOffset.x;
            float finalY = 0f;

            //if (playerModel != null)
            //{
            //    //설정한 minY, maxY 값 내에서 랜덤 생성
            //    float randomY = Random.Range(playerModel.minAreaY, playerModel.maxAreaY);
            //    spawnPos.y = randomY;                
            //}
            //else
            //{
            //    //-3.0 기본값 설정
            //    spawnPos.y = -3.0f;
            //}

            //spawnPos.z = 0;

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
                }

                //다시 활성화 될 때 콜라이더 켜기 (죽을 때 껐다면)
                //enemyObj.GetComponent<Collider2D>().enabled = true;
                Collider2D col = enemyObj.GetComponent<Collider2D>();
                if (col != null) col.enabled = true;
            }

            //0.2초마다 순차적으로 스폰
            yield return new WaitForSeconds(0.2f);
        }

        isSpawning = false;
        currentWaveIndex++;
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

        //다음 Scene 로드
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
