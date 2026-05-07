using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[System.Serializable]
public class WeightedTilemapFiller
{
    public TilemapFiller tilemapFiller;
    [Range(0, 100)] public float weight = 1f;
}

[System.Serializable]
public class RingSettings
{
    [Header("Configuración visual del anillo (prefabs)")]
    [SerializeField] public string name = "Anillo";
    [SerializeField] public WeightedTile[] weightedTiles;
    [SerializeField] public GameObject wallPrefab;
    [SerializeField] public GameObject cornerPrefab;
    [SerializeField] public GameObject openDoor;
    [SerializeField] public GameObject closedDoor;
    [SerializeField] public GameObject decorativeElement;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("Porcentaje de tiles del anillo que tendrán elemento decorativo (0 = ninguno, 1 = todos)")]
    public float decorativeElementPercentage = 0.05f;
}

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] private TilemapFiller tilemapFiller;

    [Header("Configuración por defecto (fallback sin selección de menú)")]
    [SerializeField] private MapConfig defaultMapConfig;

    [Header("Sala del tesoro (prefabs)")]
    [SerializeField] private GameObject treasurePrefab;
    [SerializeField] private WeightedTile[] treasureRoomTiles;
    [SerializeField] private GameObject treasureRoomWallPrefab;
    [SerializeField] private GameObject treasureRoomCornerPrefab;
    [SerializeField] public GameObject treasureRoomOpenDoor;
    [SerializeField] public GameObject treasureRoomClosedDoor;

    [Header("Prefabs de spawners por tipo de enemigo")]
    [SerializeField] private GameObject dragonSpawnerPrefab;
    [SerializeField] private GameObject goatSpawnerPrefab;

    [Header("Anillos del castillo (solo prefabs, en orden fijo)")]
    [Tooltip("Orden: 0=Decorada, 1=Baldosas, 2=Madera, 3=Baldosas rotas, 4=Patio, 5=Bosque exterior")]
    [SerializeField] private RingSettings[] castleRings;

    private Tilemap tilemap;
    private bool hasPendingSpawn = false;
    private Vector3 pendingSpawnPos;
    private MapConfig activeConfig => GameManager.Instance?.SelectedMapConfig ?? defaultMapConfig;

    /// <summary>
    /// Inicializa referencias de escena y suscribe el evento de registro de jugador local.
    /// </summary>
    private void Awake()
    {
        tilemap = FindFirstObjectByType<Tilemap>();
        //GameEvents.OnLocalPlayerRegistered += onLocalPlayerRegistered;
    }

    /// <summary>
    /// Libera la suscripción al evento de registro del jugador local.
    /// </summary>
    private void OnDestroy()
    {
        GameEvents.OnLocalPlayerRegistered -= onLocalPlayerRegistered;
    }

    /// <summary>
    /// Inicia la generación del mapa asegurando que la semilla es igual para todos en la red.
    /// </summary>
    public void StartGenerationWithSeed(int seed)
    {
        Debug.Log($"[LevelGenerator] Generando mapa multijugador con semilla: {seed}");

        Random.InitState(seed);

        generateLevel();
    }

    /// <summary>
    /// Genera la sala del tesoro y los anillos del mapa.
    /// </summary>
    private void generateLevel()
    {
        generateTreasureRoom();
        generateRings();
        clearSpawnArea();
        clearDoorAreas();
    }

    private void clearSpawnArea()
    {
        Vector3 basePos = GetPlayerSpawnPosition(0);
        Vector3 clusterCenter = basePos + new Vector3(0.75f, 0.75f, 0f);


        Collider2D[] obstacles = Physics2D.OverlapCircleAll(clusterCenter, 2.0f);

        foreach (Collider2D col in obstacles)
        {
            if (col.GetComponent<Tilemap>() != null) continue;

            UniqueEntity entity = col.GetComponent<UniqueEntity>();
            if (entity != null)
            {
                if (entity.Type != EntityType.Pickup_Key && entity.Type != EntityType.Pickup_Diamond)
                    continue;
            }

            string objName = col.gameObject.name.ToLower();
            if (objName.Contains("wall") || objName.Contains("corner") || objName.Contains("door") || objName.Contains("puerta"))
            {
                continue;
            }

            Destroy(col.gameObject);
        }

    }

    private void clearDoorAreas()
    {
        // 1. Buscamos todos los objetos del mapa
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int clearedDoors = 0;

        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();

            // 2. Identificamos si el objeto es una puerta
            if (objName.Contains("door") || objName.Contains("puerta"))
            {
                // 3. Limpiamos un radio de 1.5 metros (suficiente para pasar) alrededor de la puerta
                Collider2D[] obstacles = Physics2D.OverlapCircleAll(obj.transform.position, 1.5f);

                foreach (Collider2D col in obstacles)
                {
                    if (col.GetComponent<Tilemap>() != null) continue;

                    string colName = col.gameObject.name.ToLower();
                    // Protegemos estructuras vitales
                    if (colName.Contains("wall") || colName.Contains("corner") || colName.Contains("door") || colName.Contains("puerta")) continue;

                    UniqueEntity entity = col.GetComponent<UniqueEntity>();
                    if (entity != null && (entity.Type == EntityType.Pickup_Key || entity.Type == EntityType.Pickup_Diamond)) continue;

                    Destroy(col.gameObject);
                }
                clearedDoors++;
            }
        }
    }

    /// <summary>
    /// Construye la sala del tesoro e instancia el cofre en el centro.
    /// </summary>
    private void generateTreasureRoom()
    {
        if (tilemapFiller == null)
        {
            Debug.LogWarning("[LevelGenerator] No se ha asignado el TilemapFiller.");
            return;
        }

        int roomSize = activeConfig != null ? activeConfig.treasureRoomSize : 7;

        tilemapFiller.BuildSquareRoom(
            tilemap,
            roomSize,
            treasureRoomTiles,
            null,
            treasureRoomWallPrefab,
            treasureRoomCornerPrefab,
            null,
            treasureRoomClosedDoor
        );

        Vector3 center = new Vector3(0f, 0f, -0.1f);
        if (treasurePrefab != null)
        {
            GameObject chest = Instantiate(treasurePrefab, center, Quaternion.identity);

            // Se usa Network Object ya que el cofre debe de sincronizarse para todos los clientes
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                var networkObject = chest.GetComponent<Unity.Netcode.NetworkObject>();
                if (networkObject != null && !networkObject.IsSpawned)
                {
                    networkObject.Spawn();
                }
            }

            UniqueEntity uniqueEntity = chest.GetComponent<UniqueEntity>();
            if (uniqueEntity != null) uniqueEntity.RegenerateIdOnSpawn();
        }
    }

    /// <summary>
    /// Construye los anillos activos según la configuración de mapa seleccionada.
    /// </summary>
    private void generateRings()
    {
        if (castleRings == null || castleRings.Length == 0 || tilemapFiller == null) return;

        MapConfig cfg = activeConfig;
        int roomSize = cfg != null ? cfg.treasureRoomSize : 7;
        Vector2Int innerSize = new Vector2Int(roomSize, roomSize);

        int outermostWallIndex = -1;
        for (int i = castleRings.Length - 2; i >= 0; i--)
        {
            if (isRingEnabled(cfg, i))
            {
                outermostWallIndex = i;
                break; 
            }
        }

        Debug.Log($"[LevelGenerator] El muro exterior dinámico detectado es: {outermostWallIndex}");

        for (int i = 0; i < castleRings.Length; i++)
        {
            RingSettings ring = castleRings[i];
            if (ring == null) continue;

            bool isLastRing = i == castleRings.Length - 1;
            bool isEnabled = isLastRing || isRingEnabled(cfg, i);

            if (!isEnabled) continue;

            int ringWidth = getRingWidth(cfg, i);
            GameObject[] spawners = buildSpawnersArray(cfg, i);
            float decorativePercentage = getDecorativePercentage(cfg, i, ring);

            GameObject openDoorToPass = (i == outermostWallIndex) ? ring.openDoor : null;

            tilemapFiller.BuildRectangularRingRoom(
                tilemap,
                innerSize,
                ringWidth,
                ring.weightedTiles,
                spawners,
                ring.wallPrefab,
                ring.cornerPrefab,
                openDoorToPass, 
                isLastRing ? null : ring.closedDoor,
                ring.decorativeElement,
                decorativePercentage
            );

            innerSize = new Vector2Int(
                innerSize.x + 2 * ringWidth,
                innerSize.y + 2 * ringWidth
            );
        }
    }

    /// <summary>
    /// Determina si un anillo intermedio está activado en la configuración del mapa.
    /// </summary>
    private bool isRingEnabled(MapConfig cfg, int index)
    {
        if (cfg == null) return true;

        return index switch
        {
            0 => cfg.decoratedRoom.enabled,
            1 => cfg.tileRoom.enabled,
            2 => cfg.woodRoom.enabled,
            3 => cfg.brokenTileRoom.enabled,
            4 => cfg.castleYard.enabled,
            _ => true
        };
    }

    /// <summary>
    /// Obtiene el ancho del anillo en función de la configuración activa.
    /// </summary>
    private int getRingWidth(MapConfig cfg, int index)
    {
        if (cfg == null) return 8;

        return index switch
        {
            0 => cfg.decoratedRoom.ringWidth,
            1 => cfg.tileRoom.ringWidth,
            2 => cfg.woodRoom.ringWidth,
            3 => cfg.brokenTileRoom.ringWidth,
            4 => cfg.castleYard.ringWidth,
            5 => cfg.outerForest.ringWidth,
            _ => 8
        };
    }

    /// <summary>
    /// Obtiene el porcentaje de decorativos del anillo según configuración o fallback visual.
    /// </summary>
    private float getDecorativePercentage(MapConfig cfg, int index, RingSettings ring)
    {
        if (cfg == null) return ring.decorativeElementPercentage;

        return index switch
        {
            0 => cfg.decoratedRoom.decorativePercentage,
            1 => cfg.tileRoom.decorativePercentage,
            2 => cfg.woodRoom.decorativePercentage,
            3 => cfg.brokenTileRoom.decorativePercentage,
            4 => cfg.castleYard.decorativePercentage,
            5 => cfg.outerForest.decorativePercentage,
            _ => ring.decorativeElementPercentage
        };
    }

    /// <summary>
    /// Construye el array de prefabs de spawners según el conteo de enemigos por anillo.
    /// </summary>
    private GameObject[] buildSpawnersArray(MapConfig cfg, int index)
    {
        if (cfg == null) return null;

        int dragons = 0;
        int goats = 0;

        switch (index)
        {
            case 0: dragons = cfg.decoratedRoom.dragonSpawnerCount; goats = cfg.decoratedRoom.goatSpawnerCount; break;
            case 1: dragons = cfg.tileRoom.dragonSpawnerCount; goats = cfg.tileRoom.goatSpawnerCount; break;
            case 2: dragons = cfg.woodRoom.dragonSpawnerCount; goats = cfg.woodRoom.goatSpawnerCount; break;
            case 3: dragons = cfg.brokenTileRoom.dragonSpawnerCount; goats = cfg.brokenTileRoom.goatSpawnerCount; break;
            case 4: dragons = cfg.castleYard.dragonSpawnerCount; goats = cfg.castleYard.goatSpawnerCount; break;
            case 5: dragons = cfg.outerForest.dragonSpawnerCount; goats = cfg.outerForest.goatSpawnerCount; break;
        }

        int total = dragons + goats;
        if (total == 0) return null;

        GameObject[] spawners = new GameObject[total];
        int idx = 0;

        for (int d = 0; d < dragons; d++)
            spawners[idx++] = dragonSpawnerPrefab;

        for (int g = 0; g < goats; g++)
            spawners[idx++] = goatSpawnerPrefab;

        return spawners;
    }

    /// <summary>
    /// Calcula y prepara la posición de spawn del jugador para aplicarla al comenzar.
    ///  ¡¡¡¡¡¡ SE TIENE QUE CAMBIAR PARA EL RESTO DE CONFIGURACIONES DE MAPAS !!!!!!
    /// </summary>
    public Vector3 GetPlayerSpawnPosition(int playerIndex = 0)
    {
        MapConfig cfg = activeConfig;
        int totalWidth = cfg != null ? cfg.treasureRoomSize : 7;
        int lastRingWidth = 8; // Grosor por defecto

        if (castleRings != null)
        {
            for (int i = 0; i < castleRings.Length; i++)
            {
                bool isLastRing = i == castleRings.Length - 1;
                bool isEnabled = isLastRing || isRingEnabled(cfg, i);

                if (isEnabled)
                {
                    lastRingWidth = getRingWidth(cfg, i);
                    totalWidth += 2 * lastRingWidth;
                }
            }
        }

        // Esquina inferior izquierda absoluta del mapa
        float xMin = -totalWidth / 2f;
        float yMin = -totalWidth / 2f;

        // El margen se ajusta proporcionalmente al ancho del pasillo final.
        // Si el anillo mide 4, el margen es ~1.5, dejando a los jugadores perfectamente en el centro del pasillo.
        float margin = Mathf.Max(1.5f, lastRingWidth * 0.35f);

        Vector3 basePos = new Vector3(xMin + margin, yMin + margin, -0.1f);

        Vector3[] clusterOffsets = new Vector3[]
        {
            new Vector3(0, 0, 0),     
            new Vector3(1.5f, 0, 0),   
            new Vector3(0, 1.5f, 0),   
            new Vector3(1.5f, 1.5f, 0)  
        };

        return basePos + clusterOffsets[playerIndex % clusterOffsets.Length];
    }


    ///////// FUNCIONES QUE SE USABAN PARA EL LOCAL (SIN BORRAR)

    /// <summary>
    /// Aplica el spawn pendiente cuando se registra el jugador local.
    /// </summary>
    private void onLocalPlayerRegistered(PlayerController player)
    {
        if (player == null || !hasPendingSpawn) return;

        applySpawnAndCharacter(player, pendingSpawnPos);
        hasPendingSpawn = false;
    }

    /// <summary>
    /// Activa y posiciona al jugador y aplica su configuración visual y de estadísticas.
    /// </summary>
    private void applySpawnAndCharacter(PlayerController player, Vector3 spawnPos)
    {
        player.gameObject.SetActive(true);
        player.transform.position = spawnPos;
        applySelectedCharacter(player);
    }

    /// <summary>
    /// Aplica al jugador las estadísticas y el animator del personaje seleccionado.
    /// </summary>
    private void applySelectedCharacter(PlayerController player)
    {
        if (GameManager.Instance == null || GameManager.Instance.SelectedCharacterStats == null)
        {
            Debug.LogWarning("[LevelGenerator] No hay personaje seleccionado, usando configuración por defecto.");
            return;
        }

        PlayerStats selectedStats = GameManager.Instance.SelectedCharacterStats;
        player.ApplyCharacterStats(selectedStats);

        if (selectedStats.animatorController != null)
        {
            Animator animator = player.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = selectedStats.animatorController;
                Debug.Log($"[LevelGenerator] Animator cambiado a: {selectedStats.animatorController.name}");
            }
        }

        Debug.Log($"[LevelGenerator] Personaje aplicado: {selectedStats.characterName}");
    }
}
