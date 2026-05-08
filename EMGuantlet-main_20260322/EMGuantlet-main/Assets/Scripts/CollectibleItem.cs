using Unity.Netcode;
using UnityEngine;

public enum CollectibleType { Diamond, Key }

[RequireComponent(typeof(UniqueEntity))]
public class CollectibleItem : NetworkBehaviour
{
    [Header("Configuración del Objeto")]
    [SerializeField] private CollectibleType collectibleType;
    [SerializeField] private string playerTag = "Player";

    private UniqueEntity myEntity;
    private bool isCollected = false;

    private void Awake()
    {
        myEntity = GetComponent<UniqueEntity>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !IsSpawned)
        {
            NetworkObject.Spawn();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (isCollected) return;

        if (!collision.CompareTag(playerTag)) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null)
        {
            Debug.LogError($"[Fallo] {collision.gameObject.name} tiene tag Player pero no tiene PlayerController.");
            return;
        }

        if (myEntity == null) return;

        bool success = false;

        if (collectibleType == CollectibleType.Key)
        {
            success = GameManager.Instance.TryAddKey(player.OwnerClientId, myEntity.EntityId);
        }
        else if (collectibleType == CollectibleType.Diamond)
        {
            success = GameManager.Instance.TryAddDiamond(player.OwnerClientId, myEntity.EntityId);
        }

        if (success)
        {
            isCollected = true;
            GetComponent<NetworkObject>().Despawn(true);
            Debug.Log($"[Servidor] {collectibleType} recogido exactamente por el Cliente {player.OwnerClientId}.");
        }
    }
}