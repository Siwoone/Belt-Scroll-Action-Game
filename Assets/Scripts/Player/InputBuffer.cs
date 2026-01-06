using UnityEngine;
using System.Collections.Generic;

public class InputBuffer : MonoBehaviour
{
    /// <summary>
    /// 입력된 키와 입력 시간을 저장하는 구조체
    /// </summary>
    private struct BufferedInput
    {
        public string command;
        public float timestamp;
    }

    //입력 버퍼 리스트
    private List<BufferedInput> buffer = new List<BufferedInput>();

    [SerializeField]
    private float bufferTimeout = 0.5f; //입력 버퍼 유지 시간

    private PlayerPresenter presenter;

    void Awake()
    {
        presenter = GetComponent<PlayerPresenter>();
    }

    void Update()
    {
        //오래된 입력은 매 프레임 삭제해서 버퍼를 깨끗하게 유지
        ClearOldInputs();
        CheckCommands();
    }

    public void RecordInput(string commandName)
    {
        //새로운 입력을 버퍼에 추가
        buffer.Add(new BufferedInput
        {
            command = commandName,
            timestamp = Time.time
        });

        Debug.Log($"입력 기록됨: {commandName} (현재 버퍼 개수: {buffer.Count})");
    }

    private void ClearOldInputs()
    {
        //리스트를 뒤에서부터 검사하여 오래된 데이터를 지움
        for (int i = buffer.Count -1; i >= 0; i--)
        {
            if(Time.time - buffer[i].timestamp > bufferTimeout)
            {
                buffer.RemoveAt(i);
            }
        }
    }

    private void CheckCommands()
    {
        //GetCurrentCommandString()과 동일한 로직을 사용하여 판단합니다.
        string seq = GetCurrentCommandString();

        //커맨드 체크
        //if (seq.Contains("Forward Forward")) //66 커맨드 웨이브
        if (seq.Contains("Forward Down ForwardDown")) //623 커맨드 웨이브
        {
            presenter.StartWaveStep();
            //웨이브 발동 시 방향키 기록은 지워줍니다.
            buffer.Clear();            
        }

        //초풍 체크: 웨이브 중 공격(Attack)이 들어왔는가?
        if (presenter.isWaveStepping && seq.Contains("Attack"))
        {
            presenter.ExecuteWindFist();
            buffer.Clear();
        }
    }


    private string GetCurrentCommandString()
    {
        string result = "";
        foreach(var input in buffer)
        {
            result += input.command + " ";
        }
        return result;
    }
}
