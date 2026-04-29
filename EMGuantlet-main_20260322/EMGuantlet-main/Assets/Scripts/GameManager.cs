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
        if (LocalPlayerEntity != null && playerStates.TryGetValue(LocalPlayerEntity.EntityId, out PlayerGameState state))
            return state.Keys;
        return 0;
    }

    /// <summary>
    /// Devuelve la cantidad actual de diamantes del jugador local.
    /// </summary>
    public int GetDiamonds()
    {
        if (LocalPlayerEntity != null && playerStates.TryGetValue(LocalPlayerEntity.EntityId, out PlayerGameState state))
            return state.Diamonds;
        return 0;
    }

    /// <summary>
    /// Intenta añadir una llave al inventario del jugador actual.
    /// </summary>
    public bool TryAddKey(string playerEntityId, string keyEntityId)
    {
        if (playerStates.TryGetValue(playerEntityId, out PlayerGameState state))
        {
            state.AddKey();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Intenta añadir un diamante al inventario del jugador actual.
    /// </summary>
    public bool TryAddDiamond(string playerEntityId, string diamondEntityId)
    {
        if (playerStates.TryGetValue(playerEntityId, out PlayerGameState state))
        {
            state.AddDiamond();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Intenta abrir una puerta consumiendo una llave del jugador actual.
    /// </summary>
    public bool TryOpenDoor(string playerEntityId, string doorEntityId)
    {
        if (playerStates.TryGetValue(playerEntityId, out PlayerGameState state))
        {
            state.UseKey();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Intenta activar la condición de victoria para el jugador actual.
    /// </summary>
    public bool TryTriggerVictory(string playerEntityId, string chestEntityId)
    {
        if (playerStates.TryGetValue(playerEntityId, out PlayerGameState state))
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
        Debug.Log($"[GameManager] Victoria. Keys: {GetKeys()}, Diamonds: {GetDiamonds()}, Enemies: {EnemiesKilled}");
        Invoke(nameof(loadVictoryScene), delayBeforeScene);
    }

    /// <summary>
    /// Carga la escena de victoria del juego.
    /// </summary>
    private void loadVictoryScene()
    {
        SceneManager.LoadScene(SceneNames.VictoryScene);
    }

    /// <summary>
    /// Registra en consola el estado del juego cuando el jugador muere.
    /// </summary>
    private void onPlayerDeath(ulong clientId)
    {
        Debug.Log($"[GameManager] Jugador {clientId} muerto. Keys: {GetKeys()}, Diamonds: {GetDiamonds()}, Enemies: {EnemiesKilled}");
    }
}



