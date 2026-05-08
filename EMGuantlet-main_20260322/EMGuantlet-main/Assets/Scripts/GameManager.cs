using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public static class SceneNames
{
    public const string MainMenu = "MainMenu";
    public const string CharSelection = "CharSelectionScene";
    public const string PlaygroundLevel = "PlaygroundLevel";
    public const string DeadScene = "DeadScene";
    public const string VictoryScene = "VictoryScene";
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private float delayBeforeScene = 0.5f;
    private System.Collections.Generic.Dictionary<string, PlayerGameState> playerStates = new System.Collections.Generic.Dictionary<string, PlayerGameState>();

    public static GameManager Instance { get; private set; }

    public PlayerController LocalPlayerController { get; private set; }
    public Transform LocalPlayerTransform => LocalPlayerController != null ? LocalPlayerController.transform : null;
    public UniqueEntity LocalPlayerEntity { get; private set; }

    public int EnemiesKilled { get; private set; }
    public PlayerStats SelectedCharacterStats { get; set; }
    public MapConfig SelectedMapConfig { get; set; }

    public int GlobalKeys { get; set; }
    public int GlobalDiamonds { get; set; }

    /// <summary>
    /// Inicializa el singleton del juego y sus datos persistentes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneUnloaded += onSceneUnloaded;
    }

    /// <summary>
    /// Libera suscripciones globales al destruir el gestor.
    /// </summary>
    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= onSceneUnloaded;
    }

    /// <summary>
    /// Suscribe callbacks de eventos persistentes del juego.
    /// </summary>
    private void OnEnable()
    {
        GameEvents.OnPlayerDied += onPlayerDeath;
    }

    /// <summary>
    /// Desuscribe callbacks de eventos persistentes del juego.
    /// </summary>
    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= onPlayerDeath;
    }

    /// <summary>
    /// Registra el jugador local activo y publica su evento de registro.
    /// </summary>
    public void RegisterLocalPlayer(PlayerController player, UniqueEntity entity)
    {
        LocalPlayerController = player;
        LocalPlayerEntity = entity;
        SetPlayerData(entity, player.OwnerClientId);
        GameEvents.LocalPlayerRegistered(player);
    }

    /// <summary>
    /// Inicializa el estado del jugador con el identificador de su entidad.
    /// </summary>
    public void SetPlayerData(UniqueEntity playerEntity, ulong clientId)
    {
        if (playerEntity == null || string.IsNullOrEmpty(playerEntity.EntityId)) return;
        if (!playerStates.ContainsKey(playerEntity.EntityId))
        {
            playerStates[playerEntity.EntityId] = new PlayerGameState(playerEntity.EntityId, clientId);
        }
    }

    /// <summary>
    /// Reinicia los datos de partida del jugador y estadísticas globales.
    /// </summary>
    public void ResetGameData()
    {
        playerStates.Clear();
        EnemiesKilled = 0;
        GlobalKeys = 0;     
        GlobalDiamonds = 0;
    }

    /// <summary>
    /// Actualiza el contador global de enemigos eliminados desde el servidor.
    /// </summary>
    public void UpdateEnemiesKilledLocally(int totalKills)
    {
        EnemiesKilled = totalKills;
        GameEvents.EnemyKilled(EnemiesKilled);
    }

    /// <summary>
    /// Devuelve la cantidad actual de llaves del jugador local.
    /// </summary>
    public int GetKeys()
    {
        if (LocalPlayerController != null)
            return LocalPlayerController.KeysCount.Value;

        return 0;
    }

    /// <summary>
    /// Devuelve la cantidad actual de diamantes del jugador local.
    /// </summary>
    public int GetDiamonds()
    {
        if (LocalPlayerController != null)
            return LocalPlayerController.DiamondsCount.Value;
            
        return 0;
    }

    public bool TryAddKey(ulong clientId, string keyEntityId)
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.OwnerClientId == clientId)
            {
                p.KeysCount.Value++;
                return true;
            }
        }
        return false;
    }

    public bool TryAddDiamond(ulong clientId, string diamondEntityId)
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.OwnerClientId == clientId)
            {
                p.DiamondsCount.Value++;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Intenta abrir una puerta consumiendo una llave del jugador actual.
    /// </summary>
    public bool TryOpenDoor(ulong clientId, string doorEntityId)
    {
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer) return false;

        // Se busca al jugador
        if (Unity.Netcode.NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            if (client.PlayerObject.TryGetComponent(out PlayerController p))
            {
                // Se comprueba si tiene la llave
                if (p.KeysCount.Value > 0)
                {
                    p.KeysCount.Value--; 
                    return true;
                }
                else
                {
                    Debug.Log($"[GameManager] El cliente {clientId} no tiene llaves para abrir la puerta {doorEntityId}.");
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Intenta activar la condición de victoria para el jugador actual.
    /// </summary>
    public bool TryTriggerVictory(string playerEntityId, string chestEntityId)
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)    // solo el server tiene permiso para procesar la victoria
        {
            victoryAchieved();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Reestablece los datos del juego.
    /// </summary>
    public void PrepareMultiplayerGame()
    {
        ResetGameData();
    }

    ///// METODOS ANTIGUOS (LOCAL) DE START

    /// <summary>
    /// Guarda el personaje seleccionado, reinicia datos y carga el nivel de juego.
    /// </summary>
    /*public void StartGame(PlayerStats selectedCharacter)
    {
        if (selectedCharacter == null)
        {
            Debug.LogError("[GameManager] StartGame llamado sin personaje seleccionado.");
            return;
        }

        Debug.Log($"selected character is {selectedCharacter.characterName}");
        SelectedCharacterStats = selectedCharacter;
        ResetGameData();

        SceneManager.LoadScene(SceneNames.PlaygroundLevel);
    }

    /// <summary>
    /// Guarda mapa y personaje seleccionados e inicia la partida.
    /// </summary>
    public void StartGame(PlayerStats selectedCharacter, MapConfig selectedMap)
    {
        SelectedMapConfig = selectedMap;
        StartGame(selectedCharacter);
    }*/




    /// <summary>
    /// Inicia el flujo de fin de partida por muerte del jugador.
    /// </summary>
    public void TriggerGameOver()
    {
        Debug.Log($"[GameManager] Procesando muerte de jugador local.");

        Invoke(nameof(loadDeadScene), delayBeforeScene);
    }

    /// <summary>
    /// Limpia los eventos de escena cuando se descarga el nivel jugable.
    /// </summary>
    private void onSceneUnloaded(Scene scene)
    {
        if (scene.name == SceneNames.PlaygroundLevel)
        {
            GameEvents.ClearSceneEvents();
        }
    }

    /// <summary>
    /// Carga la escena de derrota del jugador.
    /// </summary>
    private void loadDeadScene()
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            // PARA EL HOST: Carga aditiva para no cerrar el servidor
            SceneManager.LoadScene(SceneNames.DeadScene, LoadSceneMode.Additive);

            // Desactiva la cámara de juego para que se vea la de DeadScene
            if (LocalPlayerController != null)
            {
                // Puedes buscar la cámara principal y apagarla
                Camera.main.gameObject.SetActive(false);
            }
        }
        else
        {
            // PARA EL CLIENTE: Carga normal, se desconecta y se va a su pantalla
            SceneManager.LoadScene(SceneNames.DeadScene);
        }
    }

    /// <summary>
    /// Registra logs de victoria y programa la carga de la escena final.
    /// </summary>
    private void victoryAchieved()
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            int sumKeys = 0;
            int sumDiamonds = 0;

            PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            foreach (var p in allPlayers)
            {
                sumKeys += p.KeysCount.Value;
                sumDiamonds += p.DiamondsCount.Value;
            }
            if (LocalPlayerController != null)
            {
                LocalPlayerController.SyncVictoryStatsRpc(sumKeys, sumDiamonds, EnemiesKilled);
            }
        }
        Invoke(nameof(loadVictoryScene), delayBeforeScene);
    }

    /// <summary>
    /// Carga la escena de victoria del juego.
    /// </summary>
    private void loadVictoryScene()
    {
        if (Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.VictoryScene, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Registra en consola el estado del juego cuando el jugador muere.
    /// </summary>
    private void onPlayerDeath(ulong clientId)
    {
        Debug.Log($"[GameManager] Jugador {clientId} muerto. Keys: {GetKeys()}, Diamonds: {GetDiamonds()}, Enemies: {EnemiesKilled}");
    }
}



