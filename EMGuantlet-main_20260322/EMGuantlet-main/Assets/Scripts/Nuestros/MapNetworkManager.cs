using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// .
/// </summary>
public class MapNetworkManager : NetworkBehaviour
{
    private NetworkVariable<int> mapSeed = new NetworkVariable<int>(0);

    [Header("Referencias")]
    public LevelGenerator levelGenerator;
    public GameObject playerPrefab;

    /// <summary>
    /// Si se trata del Host, genera una semilla de forma aleatoria para generar el mapa y espera a que carguen todos los jugadores
    /// Si se trata de los clientes, se genera el mapa a partir de la semilla del Host 
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            int randomSeed = Random.Range(1, 999999);
            mapSeed.Value = randomSeed;
            levelGenerator.StartGenerationWithSeed(randomSeed);

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnAllClientsLoaded;
        }
        else
        {
            if (mapSeed.Value != 0) levelGenerator.StartGenerationWithSeed(mapSeed.Value);
            mapSeed.OnValueChanged += (oldVal, newVal) => {
                if (newVal != 0) levelGenerator.StartGenerationWithSeed(newVal);
            };
        }
    }

    /// <summary>
    /// Método que se encarga de eliminar los clientes que queden si se finaliza la conexión.
    /// </summary>
    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnAllClientsLoaded;
        base.OnDestroy();
    }

    /// <summary>
    /// Método que se encarga de asegurar que todos los jugadores se han cargado para instanciarlos.
    /// </summary>
    private void OnAllClientsLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsServer && sceneName == gameObject.scene.name)
        {
            SpawnAllPlayers();
        }
    }

    /// <summary>
    /// Instancia todos los jugadores en el mapa.
    /// </summary>
    private void SpawnAllPlayers()
    {
        Vector3 basePos = levelGenerator.GetPlayerSpawnPosition();

        int i = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Vector3 spawnPos = basePos + new Vector3(i * 1.5f, 0, 0);

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

            playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);
            i++;
        }
    }
}

