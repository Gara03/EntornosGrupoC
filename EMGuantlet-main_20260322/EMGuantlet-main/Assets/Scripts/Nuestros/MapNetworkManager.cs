using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Clase encargada de generar y transmitir la semilla del mapa al resto de jugadores, asi como su spawn cuando estan preparados
/// </summary>
public class MapNetworkManager : NetworkBehaviour
{
    public static MapNetworkManager Instance { get; private set; } // Nuevo singleton

    private NetworkVariable<int> mapSeed = new NetworkVariable<int>(0); // NetworkVariable para la semilla del mapa (se mandara la semilla a todos los clientes)

    public NetworkVariable<int> globalEnemiesKilled = new NetworkVariable<int>(0); // NetworkVariable para el contador de enemigos
    public NetworkVariable<int> globalKeys = new NetworkVariable<int>(0); // NetworkVariable para el contador de llaves
    public NetworkVariable<int> globalDiamonds = new NetworkVariable<int>(0); // NetworkVariable para el contador de gemas

    [Header("Referencias")]
    public LevelGenerator levelGenerator;
    public GameObject playerPrefab;

    /// <summary>
    /// Si se trata del Host, genera una semilla de forma aleatoria para generar el mapa y espera a que carguen todos los jugadores
    /// Si se trata de los clientes, se genera el mapa a partir de la semilla del Host 
    /// </summary> 
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Método que sincroniza el contador de enemigos y recolectables y genera el mismo mapa a partir de la semilla para todos los clientes y host
    /// </summary> 
    public override void OnNetworkSpawn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateEnemiesKilledLocally(globalEnemiesKilled.Value);
            GameManager.Instance.GlobalKeys = globalKeys.Value;
            GameManager.Instance.GlobalDiamonds = globalDiamonds.Value;
        }

        // Contador de enemigos
        globalEnemiesKilled.OnValueChanged += (oldVal, newVal) => {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdateEnemiesKilledLocally(newVal);
            }
        };

        // Contador de llaves
        globalKeys.OnValueChanged += (oldVal, newVal) => {
            if (GameManager.Instance != null) GameManager.Instance.GlobalKeys = newVal;
        };

        // Contador de gemas
        globalDiamonds.OnValueChanged += (oldVal, newVal) => {
            if (GameManager.Instance != null) GameManager.Instance.GlobalDiamonds = newVal;
        };

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
    /// El serevidor suma lo que tienen todos los jugadores y lo guarda en las variables de red.
    /// </summary>
    public void RecalculateGlobals()
    {
        if (!IsServer) return; // Solo el servidor puede hacer las sumas

        int sumKeys = 0;
        int sumDiamonds = 0;

        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            sumKeys += p.KeysCount.Value;
            sumDiamonds += p.DiamondsCount.Value;
        }

        globalKeys.Value = sumKeys;
        globalDiamonds.Value = sumDiamonds;
    }

    /// <summary>
    /// Método que se encarga de añadir una kill.
    /// </summary>
    public void AddKill()
    {
        if (IsServer)
        {
            globalEnemiesKilled.Value++;
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
            Vector3 spawnPos = levelGenerator.GetPlayerSpawnPosition(i);

            GameObject playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);

            playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);
            i++;
        }
    }
}

