using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(UniqueEntity))]
public class DoorController : NetworkBehaviour
{
    [SerializeField] private Sprite openDoorSprite;

    // Variable en red para comprobar el estado de la puerta
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private Collider2D triggerCollider;
    private Collider2D blockingCollider;
    private SpriteRenderer spriteRenderer;
    private UniqueEntity uniqueEntity;

    public string EntityId => uniqueEntity?.EntityId ?? "UNKNOWN";
    public EntityType EntityType => uniqueEntity?.Type ?? EntityType.Interactive_Door;

    /// <summary>
    /// Inicializa componentes de la puerta y valida la configuración de entidad.
    /// </summary>
    private void Awake()
    {
        uniqueEntity = GetComponent<UniqueEntity>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        cacheColliders();
    }

    /// <summary>
    /// Si es el host, spawnea todas las puertas.
    /// </summary>
    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && !netObj.IsSpawned)
            {
                netObj.Spawn(); // Se spawnean las puertas en red
            }
        }
    }

    /// <summary>
    /// Gestiona el spawn de las puertas.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Se suscribe a los cambios en la puerta: abierta/cerrada
        isOpen.OnValueChanged += OnDoorStateChanged;

        // Si alguien se conecta tarde y la puerta ya estaba abierta se actualiza el sprite
        if (isOpen.Value)
        {
            OpenDoor();
        }
    }

    /// <summary>
    /// Gestiona el despawn de las puertas.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        isOpen.OnValueChanged -= OnDoorStateChanged;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Gestiona la interacción de apertura cuando entra un jugador en el trigger.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (isOpen.Value || !other.CompareTag("Player")) return;

        if (!other.TryGetComponent(out PlayerController player)) return;
        if (GameManager.Instance == null) return;

        // Se valida si el jugador tiene llave 
        if (GameManager.Instance.TryOpenDoor(player.OwnerClientId, EntityId))
        {
            Debug.Log($"[Servidor] Puerta {EntityId} abierta por el Cliente {player.OwnerClientId}.");
            isOpen.Value = true;
        }
        else
        {
            Debug.Log($"[Servidor] El Cliente {player.OwnerClientId} chocó con la puerta, pero NO tiene llaves.");
        }
    }

    /// <summary>
    /// Cambia el estado de la puerta.
    /// </summary>
    private void OnDoorStateChanged(bool previousValue, bool newValue)
    {
        if (newValue && !previousValue)
        {
            OpenDoor();
        }
    }

    /// <summary>
    /// Actualiza el sprite de la puerta segun su estado.
    /// </summary>
    private void OpenDoor()
    {
        if (openDoorSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = openDoorSprite;
        }

        if (blockingCollider != null)
        {
            blockingCollider.enabled = false;
        }
    }

    /// <summary>
    /// Localiza y almacena los colliders de trigger y bloqueo de la puerta.
    /// </summary>
    private void cacheColliders()
    {
        Collider2D[] colliders = GetComponents<Collider2D>();
        foreach (Collider2D col in colliders)
        {
            if (col.isTrigger)
                triggerCollider = col;
            else
                blockingCollider = col;
        }

        if (triggerCollider == null || blockingCollider == null)
        {
            Debug.LogWarning($"[DoorController] A la puerta {gameObject.name} le falta un Collider. Necesita uno IsTrigger (para detectar) y uno normal (para chocar).");
        }
    }
}