using UnityEngine;

[CreateAssetMenu(fileName = "NewChaseEnemyStats", menuName = "Game/Stats/Chase Enemy Stats")]
public class ChaseEnemyStats : EnemyStats
{
    [Header("Chase Behavior")]
    [Range(1f, 20f)]
    [Tooltip("Distancia máxima a la que detecta y persigue al jugador")]
    public float chaseRange = 10f;

    [Header("Wander Behavior (fuera de rango)")]
    [Range(0.5f, 5f)]
    [Tooltip("Tiempo en segundos entre cambios de dirección al vagar")]
    public float wanderChangeInterval = 2f;

    [Range(0f, 1f)]
    [Tooltip("Velocidad mínima al vagar (como porcentaje de moveSpeed)")]
    public float wanderSpeedMin = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Velocidad máxima al vagar (como porcentaje de moveSpeed)")]
    public float wanderSpeedMax = 0.7f;

    [Range(0f, 1f)]
    [Tooltip("Probabilidad de quedarse quieto en cada cambio de dirección")]
    public float idleChance = 0.2f;

}