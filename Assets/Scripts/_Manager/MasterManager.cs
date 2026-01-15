using UnityEngine;

public class MasterManager : MonoBehaviour
{
    public static MasterManager Instance;

    public int savedScore = 0;
    public int savedLives = 2;  //기본 목숨
    public int savedHp = 200;   //기본 체력

    public bool isFirstStage = true;

    private void Awake()
    {
        //싱글톤
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else
        {
            Destroy(gameObject);
        }
    }
    
    //씬 이동 시 데이터 저장
    public void SavePlayerData(int score, int lives, int hp)
    {
        savedScore = score;
        savedLives = lives;
        savedHp = hp;
        isFirstStage = false;
        Debug.Log($"<color=cyan>데이터 저장 완료: HP {hp}, Score {score}</color>");
    }
}
