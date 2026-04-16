using UnityEngine;

/// <summary>
/// Proporciona un identificador único y un tipo a cada entidad del juego.
/// </summary>
public class UniqueEntity : MonoBehaviour
{
    [Header("Entity Identification")]
    [SerializeField]
    [Tooltip("ID único generado automáticamente. No modificar manualmente.")]
    private string entityId;

    [SerializeField]
    [Tooltip("Tipo de entidad para clasificación y debugging.")]
    private EntityType entityType;

    /// <summary>
    /// Obtiene el identificador único de la entidad.
    /// </summary>
    public string EntityId => entityId;

    /// <summary>
    /// Obtiene el tipo de la entidad.
    /// </summary>
    public EntityType Type => entityType;

    /// <summary>
    /// Garantiza que la entidad tenga un identificador válido al inicializarse.
    /// </summary>
    private void Awake()
    {
        if (string.IsNullOrEmpty(entityId))
        {
            generateNewId();
            Debug.LogWarning($"[UniqueEntity] {gameObject.name} no tenía ID asignado. Generando nuevo ID: {entityId}");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Genera un identificador en editor cuando el campo está vacío.
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(entityId) && !Application.isPlaying)
        {
            generateNewId();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    /// <summary>
    /// Dibuja información visual de depuración de la entidad en la escena.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = getGizmoColor();
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        string shortId = string.IsNullOrEmpty(entityId) ? "NO-ID" : entityId.Substring(0, 8);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, $"{entityType}\n{shortId}");
    }
#endif

    /// <summary>
    /// Regenera el identificador cuando la entidad se instancia dinámicamente.
    /// </summary>
    public void RegenerateIdOnSpawn()
    {
        generateNewId();
    }

    /// <summary>
    /// Crea un nuevo identificador único mediante GUID.
    /// </summary>
    private void generateNewId()
    {
        entityId = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Regenera manualmente el identificador desde el menú contextual del inspector.
    /// </summary>
    [ContextMenu("Generate New ID")]
    private void regenerateId()
    {
        string oldId = entityId;
        generateNewId();

        Debug.Log($"[UniqueEntity] {gameObject.name} - ID regenerado\nAntiguo: {oldId}\nNuevo: {entityId}");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    /// <summary>
    /// Muestra en consola la información principal de la entidad.
    /// </summary>
    [ContextMenu("Show Entity Info")]
    private void showEntityInfo()
    {
        Debug.Log($"=== Entity Info ===\n" +
                  $"Name: {gameObject.name}\n" +
                  $"ID: {entityId}\n" +
                  $"Type: {entityType}\n" +
                  $"Position: {transform.position}\n" +
                  $"==================");
    }

#if UNITY_EDITOR
    /// <summary>
    /// Devuelve el color de depuración asociado al tipo de entidad.
    /// </summary>
    private Color getGizmoColor()
    {
        switch (entityType)
        {
            case EntityType.Player:
                return Color.green;
            case EntityType.Enemy:
                return Color.red;
            case EntityType.Pickup_Key:
                return Color.yellow;
            case EntityType.Pickup_Diamond:
                return Color.cyan;
            case EntityType.Interactive_Door:
                return Color.blue;
            case EntityType.Interactive_Chest:
                return Color.magenta;
            case EntityType.Spawner:
                return Color.gray;
            default:
                return Color.white;
        }
    }
#endif
}

/// <summary>
/// Define los tipos de entidades disponibles en el juego.
/// </summary>
public enum EntityType
{
    [Tooltip("Jugador controlado por humano")]
    Player,

    [Tooltip("Enemigo NPC")]
    Enemy,

    [Tooltip("Llave coleccionable")]
    Pickup_Key,

    [Tooltip("Diamante coleccionable")]
    Pickup_Diamond,

    [Tooltip("Puerta interactiva")]
    Interactive_Door,

    [Tooltip("Cofre del nivel")]
    Interactive_Chest,

    [Tooltip("Spawner de enemigos")]
    Spawner
}