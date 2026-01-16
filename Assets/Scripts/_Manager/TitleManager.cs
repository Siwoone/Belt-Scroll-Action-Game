using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public string firstStageName = "Stage1";        //첫 번째 Scene 이름
    private bool isStarting = false;                //중복 실행 방지용

    // Update is called once per frame
    void Update()
    {
        if (isStarting) return;
        
        //ESC를 누르면 게임 종료
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitGame();
        }

        //이외 키를 누르면 게임 시작
        else if (Input.anyKey)
        {
            StartGame();
        }
    }

    public void ExitGame()
    {
        Debug.Log("게임 종료!");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void StartGame()
    {
        //중복 실행 방지
        isStarting = true;                      
        Debug.Log("게임 시작!");
        
        //1스테이지 로드
        SceneManager.LoadScene(firstStageName);
    }
}
