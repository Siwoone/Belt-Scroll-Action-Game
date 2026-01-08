using UnityEngine;
using System.Collections.Generic;

public class ObjectPoolManager : MonoBehaviour
{
    //싱글톤 인스턴스
    public static ObjectPoolManager Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string key;              //풀 이름 (파이어볼, 총알, 적 등)
        public GameObject prefab;       //생성할 프리팹
        public int size;                //미리 생성할 갯수
    }

    [SerializeField] List<Pool> pools;  //인스펙터에서 설정할 풀 목록

    //풀의 이름을 키값으로 큐를 찾기 위해서
    Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        //딕셔너리 초기화
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        //각각의 풀 초기화
        foreach (Pool pool in pools)
        {
            //해당 풀의 큐 생성
            Queue<GameObject> objectQueue = new Queue<GameObject>();

            //Size 갯수만큼 오브젝트 미리 생성
            for (int i = 0; i < pool.size; i++)
            {
                //ObjectPoolManager 자식으로 생성
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.name = $"{pool.key}_{i}";
                obj.SetActive(false);           //비활성화
                objectQueue.Enqueue(obj);       //큐에 추가
            }

            //딕셔너리에 등록
            //poolDictionary[pool.key] = objectQueue;
            poolDictionary.Add(pool.key, objectQueue);          //이 방법이 가장 일반적
        }
    }

    //생성하기 => Pool에서 Object 가져오기
    public GameObject SpawnFromPool(string key, Vector3 position, Quaternion rotation)
    {
        //해당 이름의 풀이 없을 때
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool에서 {key}를 찾지 못했습니다.");
            return null;
        }

        Queue<GameObject> queue = poolDictionary[key];

        //큐가 비어 있을 때 새롭게 생성
        if (queue.Count == 0)
        {
            //원본 prefab 찾기
            Pool originalPool = pools.Find(p => p.key == key);
            if (originalPool != null)
            {
                GameObject newObj = Instantiate(originalPool.prefab, transform);
                newObj.name = $"{key}_Extra";
                newObj.SetActive(false);
                queue.Enqueue(newObj);
            }
        }

        //Queue에서 Object 가져오기
        GameObject obj = queue.Dequeue();

        //위치 및 회전 설정
        obj.transform.localPosition = position;
        obj.transform.localRotation = rotation;

        //활성화
        obj.SetActive(true);

        return obj;
    }

    //삭제하기 => Pool에 다시 집어넣기
    public void ReturnToPool(string key, GameObject obj)
    {
        //해당 이름의 풀이 없을 때
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool에서 {key}를 찾지 못했습니다.");
            return;
        }

        //비활성화
        obj.SetActive(false);

        //큐에 다시 추가        
        poolDictionary[key].Enqueue(obj);
    }
}
