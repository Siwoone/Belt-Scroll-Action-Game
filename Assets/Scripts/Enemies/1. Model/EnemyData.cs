using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{    
    [Header("기본 능력치 설계도")]
    public string enemyName = "Enemy";
    public int damage = 10;
    public float moveSpeed = 3f;
    public float detectRange = 10f;
    public float stopRange = 1.5f;
    public int maxHp = 50;
    public float hitRecoveryTime = 0.4f;
}
