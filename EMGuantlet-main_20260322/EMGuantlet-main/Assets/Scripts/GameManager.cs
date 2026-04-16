using UnityEngine;
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
    public static GameManager Instance { get; private set; }

    public PlayerController LocalPlayerController { get; private set; }
    public Transform LocalPlayerTransform => LocalPlayerController != null ? LocalPlayerController.transform : null;
    public UniqueEntity LocalPlayerEntity { get; private set; }

    public int EnemiesKilled { get; private set; }
    public PlayerStats SelectedCharacterStats { get; set; }
    public MapConfig SelectedMapConfig { get; set; }

    [SerializeField] private float delayBeforeScene = 0.5f;

    private PlayerGameState playerState;

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

        playerState = new PlayerGameState("PLAYER_1");
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
        SetPlayerData(entity);
        GameEvents.LocalPlayerRegistered(player);
    }

    /// <summary>
    /// Inicializa el estado del jugador con el identificador de su entidad.
    /// </summary>
    public void SetPlayerData(UniqueEntity playerEntity)
    {
        if (playerEntity == null || string.IsNullOrEmpty(playerEntity.EntityId)) return;
        playerState = new PlayerGameState(playerEntity.EntityId);
    }

    /// <summary>
    /// Reinicia los datos de partida del jugador y estadísticas globales.
    /// </summary>
    public void ResetGameData()
    {
        playerState?.ResetState();
        EnemiesKilled = 0;
    }

    /// <summary>
    /// Incrementa el contador global de enemigos eliminados.
    /// </summary>
    public void AddEnemyKill()
    {
        EnemiesKilled++;
        GameEvents.EnemyKilled(EnemiesKilled);
    }

    /// <summary>
    /// Devuelve la cantidad actual de llaves del jugador local.
    /// </summary>
    public int GetKeys()
    {
        return playerState?.Keys ?? 0;
    }

    /// <summary>
    /// Devuelve la cantidad actual de diamantes del jugador local.
    /// </summary>
    public int GetDiamonds()
    {
        return playerState?.Diamonds ?? 0;
    }

    /// <summary>
    /// Intenta añadir una llave al inventario del jugador actual.
    /// </summary>
    public bool TryAddKey(string playerEntityId, string keyEntityId)
    {
        if (playerState == null) return false;
        playerState.AddKey();
        return true;
    }

    /// <summary>
    /// Intenta añadir un diamante al inventario del jugador actual.
    /// </summary>
    public bool TryAddDiamond(string playerEntityId, string diamondEntityId)
    {
        if (playerState == null) return false;
        playerState.AddDiamond();
        return true;
    }

    /// <summary>
    /// Intenta abrir una puerta consumiendo una llave del jugador actual.
    /// </summary>
    public bool TryOpenDoor(string playerEntityId, string doorEntityId)
    {
        if (playerState == null) return false;
        return playerState.UseKey();
    }

    /// <summary>
    /// Intenta activar la condición de victoria para el jugador actual.
    /// </summary>
    public bool TryTriggerVictory(string playerEntityId, string chestEntityId)
    {
        if (playerState == null) return false;
        victoryAchieved();
        return true;
    }

    /// <summary>
    /// Guarda el personaje seleccionado, reinicia datos y carga el nivel de juego.
    /// </summary>
    public void StartGame(PlayerStats selectedCharacter)
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
    }

    /// <summary>
    /// Inicia el flujo de fin de partida por muerte del jugador.
    /// </summary>
    public void TriggerGameOver()
    {
        Debug.Log($"[GameManager] Game Over. Keys: {GetKeys()}, Diamonds: {GetDiamonds()}, Enemies: {EnemiesKilled}");
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
        SceneManager.LoadScene(SceneNames.DeadScene);
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
    private void onPlayerDeath()
    {
        Debug.Log($"[GameManager] Jugador muerto. Keys: {GetKeys()}, Diamonds: {GetDiamonds()}, Enemies: {EnemiesKilled}");
    }
}



