using UnityEngine;

[CreateAssetMenu(fileName = "NewLemniscateEnemyStats", menuName = "Game/Stats/Lemniscate Enemy Stats")]
public class LemniscateEnemyStats : EnemyStats
{
    [Header("Lemniscate Patrol")]
    [Range(0.5f, 10f)]
    [Tooltip("Distancia horizontal de la patrulla en forma de lemniscata")]
    public float patrolDistanceX = 2f;
    
    [Range(0.5f, 10f)]
    [Tooltip("Distancia vertical de la patrulla en forma de lemniscata")]
    public float patrolDistanceY = 1f;
}