using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(UniqueEntity))] // Requiere UniqueEntity
public class ChestController : NetworkBehaviour
{

    private bool collected = false;
    
    // Nueva variable para UniqueEntity
    private UniqueEntity uniqueEntity;
    
    // Propiedades de acceso rápido
    public string EntityId => uniqueEntity?.EntityId ?? "UNKNOWN";
    public EntityType EntityType => uniqueEntity?.Type ?? EntityType.Interactive_Chest;
    
    /// <summary>
    /// Inicializa la referencia de entidad única y valida su tipo configurado.
    /// </summary>
    private void Awake()
    {
        // Obtener UniqueEntity
        uniqueEntity = GetComponent<UniqueEntity>();
        
        // Validación del tipo correcto
        if (uniqueEntity != null && uniqueEntity.Type != EntityType.Interactive_Chest)
        {
            Debug.LogWarning($"[ChestController] {gameObject.name} tiene tipo {uniqueEntity.Type} en lugar de Interactive_Chest");
        }
    }

    /// <summary>
    /// Detecta la interacción con el jugador e intenta activar la victoria una sola vez.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // El servidor es el que gestiona la victoria
        if (!IsServer || collected) return;
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null) return;

        Debug.Log($"[{EntityType}:{EntityId}] opened by [Client:{player.OwnerClientId}]");

        if (GameManager.Instance != null && GameManager.Instance.TryTriggerVictory(player.OwnerClientId))
        {
            collected = true;
        }
    }
}
