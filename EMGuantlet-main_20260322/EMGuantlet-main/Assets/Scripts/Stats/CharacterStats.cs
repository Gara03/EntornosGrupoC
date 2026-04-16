using UnityEngine;

/// <summary>
/// Clase base abstracta para las estadísticas de todos los personajes (jugadores y enemigos).
/// No se puede crear directamente como asset, solo sus clases hijas.
/// </summary>
public abstract class CharacterStats : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("Nombre del personaje")]
    public string characterName = "Character";
    
    [Tooltip("Icono o sprite representativo del personaje")]
    public Sprite characterIcon;
    
    [Header("Estadísticas de Movimiento")]
    [Range(1f, 10f)]
    [Tooltip("Velocidad base de movimiento")]
    public float moveSpeed = 3f;
    
    [Header("Estadísticas de Combate")]
    [Range(1, 99)]
    [Tooltip("Salud máxima del personaje")]
    public int maxHealth = 99;
    
    [Range(1, 200)]
    [Tooltip("Daño que inflige con sus ataques")]
    public int attackDamage = 50;
    
    [Header("Knockback")]
    [Range(1f, 30f)]
    [Tooltip("Fuerza del knockback al recibir daño")]
    public float knockbackForce = 10f;
    
    [Range(0.1f, 2f)]
    [Tooltip("Duración del knockback en segundos")]
    public float knockbackDuration = 0.2f;
    
    [Header("Visual y Animación")]
    [Tooltip("Controller de animaciones del personaje")]
    public RuntimeAnimatorController animatorController;
}