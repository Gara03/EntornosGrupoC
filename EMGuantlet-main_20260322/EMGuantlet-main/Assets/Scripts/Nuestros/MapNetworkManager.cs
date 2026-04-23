using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MapNetworkManager : NetworkBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí tu objeto que tiene el script LevelGenerator")]
    public LevelGenerator levelGenerator;
    public GameObject playerPrefab;

    private NetworkVariable<int> mapSeed = new NetworkVariable<int>(0);

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

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnAllClientsLoaded;
        base.OnDestroy();
    }

    private void OnAllClientsLoaded(string sceneName, UnityEngine.SceneManagement.LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsServer && sceneName == gameObject.scene.name)
        {
            SpawnAllPlayers();
        }
    }

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

