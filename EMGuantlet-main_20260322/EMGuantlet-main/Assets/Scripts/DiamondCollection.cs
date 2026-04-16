using UnityEngine;

[RequireComponent(typeof(UniqueEntity))]
public class DiamondCollection : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private UniqueEntity uniqueEntity;

    public string EntityId => uniqueEntity?.EntityId ?? "UNKNOWN";
    public EntityType EntityType => uniqueEntity?.Type ?? EntityType.Pickup_Diamond;

    /// <summary>
    /// Inicializa la referencia de entidad única y valida el tipo configurado.
    /// </summary>
    private void Awake()
    {
        uniqueEntity = GetComponent<UniqueEntity>();

        if (uniqueEntity != null && uniqueEntity.Type != EntityType.Pickup_Diamond)
        {
            Debug.LogWarning($"[DiamondCollection] {gameObject.name} tiene tipo {uniqueEntity.Type} en lugar de Pickup_Diamond");
        }
    }

    /// <summary>
    /// Detecta la colisión con el jugador e intenta recoger el diamante.
    /// </summary>
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(playerTag)) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.TryAddDiamond(player.EntityId, EntityId))
        {
            Debug.Log($"[{EntityType}:{EntityId}] collected by [Player:{player.EntityId}]");
            Destroy(gameObject);
        }
    }
}
