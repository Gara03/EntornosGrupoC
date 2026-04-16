# Scripts del proyecto: EMGauntlet

## Índice
1. [Sistema central](#1-sistema-central)
2. [Personajes](#2-personajes)
3. [Generación de nivel](#3-generación-de-nivel)
4. [Ítems y objetos interactivos](#4-ítems-y-objetos-interactivos)
5. [Interfaz de usuario](#5-interfaz-de-usuario)
6. [ScriptableObjects — Stats](#6-scriptableobjects--stats)
7. [ScriptableObjects — Configuración de mapa](#7-scriptableobjects--configuración-de-mapa)
8. [Clases de datos](#8-clases-de-datos)
9. [Clases auxiliares](#9-clases-auxiliares)
10. [Mapa de relaciones entre scripts](#10-mapa-de-relaciones-entre-scripts)

---

## 1. Sistema central

### `GameManager.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` — Singleton |
| **GameObject** | `GameManager` en `MainMenu` (persiste via `DontDestroyOnLoad`) |
| **Objetivo** | Gestor global del ciclo de vida de la partida. Punto centralizado de acceso al estado del juego |

**Responsabilidades:**
- Mantiene `SelectedCharacterStats` y `SelectedMapConfig` entre escenas.
- Gestiona `PlayerGameState` (llaves, diamantes del jugador local).
- Registra al jugador local (`RegisterLocalPlayer`) y dispara `OnLocalPlayerRegistered`.
- Centraliza las acciones de gameplay mediante el patrón `Try...` (`TryAddKey`, `TryAddDiamond`, `TryOpenDoor`, `TryTriggerVictory`).
- Controla la navegación entre escenas (`StartGame`, `TriggerGameOver`, victoria).
- Limpia `GameEvents` al descargar `PlayGroundLevel`.

**Invocado desde:** `PlayerController`, `EnemyController`, `KeyCollection`, `DiamondCollection`, `DoorController`, `ChestController`, `LevelGenerator`, `CameraController`, `HeadUpDisplayController`, `MainMenuButtonsHandler`, `CharSelectionMenuButtonsHandler`, `GameOverCanvasHandler`, `EnemyChaseController`.

---

### `GameEvents.cs`
| | |
|---|---|
| **Tipo** | Clase estática C# (no `MonoBehaviour`) |
| **GameObject** | Ninguno — acceso estático directo |
| **Objetivo** | Bus de eventos desacoplado. Permite la comunicación entre sistemas sin referencias directas |

**Eventos disponibles:**

| Evento | Disparado por | Escuchado por |
|---|---|---|
| `OnHealthChanged(int)` | `PlayerController` | `HeadUpDisplayController` |
| `OnKeysChanged()` | `PlayerGameState`, `GameManager` | `HeadUpDisplayController` |
| `OnDiamondsChanged()` | `PlayerGameState`, `GameManager` | `HeadUpDisplayController` |
| `OnEnemyKilled(int)` | `GameManager.AddEnemyKill()` | (disponible para UI futura) |
| `OnLocalPlayerRegistered(PlayerController)` | `GameManager.RegisterLocalPlayer()` | `LevelGenerator`, `CameraController`, `EnemyChaseController` |
| `OnPlayerDied()` | `PlayerController.Die()` | `GameManager` |
| `OnVictory()` | (disponible) | (disponible) |

**Limpieza:** `ClearSceneEvents()` se llama al descargar `PlayGroundLevel`. `ClearAllEvents()` solo en salida total del juego o tests.

---

### `UniqueEntity.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | Requerido en: `Player`, todos los prefabs de ítems, enemigos, puertas, cofre, spawners |
| **Objetivo** | Asigna un identificador GUID único a cada entidad del juego, preparando el sistema para sincronización en red |

**Responsabilidades:**
- Genera automáticamente un GUID en `Awake()` si el campo está vacío.
- En Editor, genera y persiste el ID en `OnValidate()`.
- Expone `EntityId` (string) y `Type` (`EntityType` enum) como propiedades de solo lectura.
- `RegenerateIdOnSpawn()` se llama al instanciar prefabs en runtime para garantizar IDs únicos.
- Dibuja gizmos de depuración por tipo de entidad.

**Invocado desde:** `CharController`, `DiamondCollection`, `KeyCollection`, `DoorController`, `ChestController`, `EnemySpawner`, `LevelGenerator`, `GameManager.RegisterLocalPlayer()`.

---

## 2. Personajes

### `CharController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` abstracto |
| **GameObject** | No se usa directamente — base para `PlayerController` y `EnemyController` |
| **Objetivo** | Define el comportamiento físico y de combate común a todos los personajes |

**Responsabilidades:**
- Carga estadísticas desde un `CharacterStats` ScriptableObject vía `LoadStats()`.
- Gestiona movimiento físico con `Rigidbody2D.MovePosition`.
- Gestiona knockback con temporizador.
- Expone `TakeDamage(int, Vector2)`, `TakeKnockback(Vector2, float)` y `Die()` como virtuales.
- Expone `EntityId` y `EntityType` como atajos al componente `UniqueEntity`.

---

### `PlayerController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` (hereda `CharController`) |
| **GameObject** | `Player` en `PlayGroundLevel` (bajo `Tilemap`) |
| **Objetivo** | Control del jugador local: entrada, movimiento, ataque y gestión de vida |

**Responsabilidades:**
- Registra al jugador en `GameManager.RegisterLocalPlayer()` en `Awake()`.
- Empieza desactivado (`SetActive(false)`) hasta que `LevelGenerator` lo posiciona.
- Lee input mediante `PlayerControls` (Input System).
- Dispara `GameEvents.HealthChanged` al recibir daño.
- Dispara `GameEvents.PlayerDied` y llama a `GameManager.TriggerGameOver()` al morir.
- `ApplyCharacterStats(PlayerStats)` permite cambiar el personaje en runtime (llamado desde `LevelGenerator`).

---

### `EnemyController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` abstracto (hereda `CharController`) |
| **GameObject** | No se usa directamente — base para los tipos de enemigo |
| **Objetivo** | Define comportamiento compartido de todos los enemigos: colisión, muerte y drops |

**Responsabilidades:**
- `OnCollisionStay2D`: si el jugador ataca, aplica daño al enemigo; si no, aplica daño al jugador.
- `Die()`: registra la baja en `GameManager.AddEnemyKill()` y llama a `SpawnDrops()`.
- `SpawnDrops()`: obtiene `EnemyDropConfig` vía `GetDropConfig()` y genera llaves o diamantes en runtime.
- `GetDropConfig()`: resuelve el `EnemyDropConfig` correcto desde `GameManager.SelectedMapConfig` según el tipo de stats (`ChaseEnemyStats` → dragon, `LemniscateEnemyStats` → goat).

---

### `EnemyChaseController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` (hereda `EnemyController`) |
| **GameObject** | Prefab de enemigo tipo dragón, instanciado por `EnemySpawner` |
| **Objetivo** | Enemigo que persigue al jugador cuando está en rango, y vaguea aleatoriamente cuando no |

**Responsabilidades:**
- Lee `chaseRange`, `wanderSpeed`, `idleChance` desde `ChaseEnemyStats`.
- Suscribe `GameEvents.OnLocalPlayerRegistered` para obtener el `Transform` del jugador.
- `Move()`: alterna entre `chasePlayer()` (dirigido hacia el jugador) y `wanderMovement()` (dirección aleatoria con timer).
- `setNewWanderDirection()`: calcula aleatoriamente nueva dirección y velocidad, o estado idle.

---

### `EnemyLemniscateController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` (hereda `EnemyController`) |
| **GameObject** | Prefab de enemigo tipo cabra, instanciado por `EnemySpawner` |
| **Objetivo** | Enemigo con movimiento en patrón de lemniscata (figura 8) alrededor de su punto de spawn |

**Responsabilidades:**
- Lee `patrolDistanceX`, `patrolDistanceY` desde `LemniscateEnemyStats`.
- `Move()`: calcula posición paramétrica `(sin(t)·dX, sin(t)·cos(t)·dY)` y usa `Rigidbody2D.MovePosition`.
- `TakeDamage()`: tras el knockback, recalcula la fase de patrulla vía `getBestPatrolTime()` para evitar saltos en la trayectoria.

---

## 3. Generación de nivel

### `LevelGenerator.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | `LevelGenerator` en `PlayGroundLevel` |
| **Objetivo** | Genera proceduralmente el mapa completo al inicio de la escena |

**Responsabilidades:**
- `ActiveConfig`: resuelve el `MapConfig` activo (`GameManager.SelectedMapConfig` o `defaultMapConfig`).
- `generateTreasureRoom()`: construye la sala central e instancia el cofre.
- `generateRings()`: itera sobre los anillos activos según `MapConfig`, construye cada anillo con tiles, paredes, puertas, spawners y decorativos.
- `tryCalculateSpawnPos()`: calcula la posición de spawn del jugador en el penúltimo anillo activo.
- `onLocalPlayerRegistered()`: recibe el evento y llama a `applySpawnAndCharacter()`.
- `applySelectedCharacter()`: aplica `PlayerStats` y `AnimatorController` al jugador.

**Invoca a:** `TilemapFiller`, `GameManager`, `GameEvents`.

---

### `TilemapFiller.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | Referenciado como componente por `LevelGenerator` |
| **Objetivo** | Escribe tiles en el Tilemap y genera el mobiliario físico de cada sala (paredes, puertas, decorativos, spawners) |

**Responsabilidades:**
- `BuildSquareRoom()`: sala cuadrada simple (sala del tesoro).
- `BuildRectangularRingRoom()`: anillo concéntrico con hueco interior.
- `fillRingMap()`: rellena solo la franja del anillo con tiles ponderados.
- `spawnWalls()`: instancia prefabs de paredes, esquinas y puertas (una aleatoria queda abierta).
- `spawnDecorativeElements()`: instancia decorativos evitando el área interior, según porcentaje.
- `spawnSpawnersInRing()`: instancia spawners de enemigos en posiciones aleatorias del anillo.
- `getRandomWeightedTile()`: selecciona un tile según peso relativo.

---

## 4. Ítems y objetos interactivos

### `EnemySpawner.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | Prefab instanciado por `TilemapFiller.spawnSpawnersInRing()` |
| **Objetivo** | Genera enemigos a intervalos de tiempo hasta alcanzar el máximo configurado |

**Responsabilidades:**
- Instancia `enemyPrefab` cada `spawnInterval` segundos hasta llegar a `totalEnemies`.
- Opcionalmente dispersa el spawn dentro de un radio aleatorio.
- Llama a `UniqueEntity.RegenerateIdOnSpawn()` en cada enemigo instanciado.

---

### `KeyCollection.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | Prefab de llave, instanciado por `EnemyController.spawnDrops()` |
| **Objetivo** | Detecta la colisión con el jugador y añade una llave al inventario |

**Responsabilidades:**
- `OnCollisionStay2D`: llama a `GameManager.TryAddKey(playerEntityId, keyEntityId)`.
- Si la operación tiene éxito, se destruye a sí mismo.

---

### `DiamondCollection.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | Prefab de diamante, instanciado por `EnemyController.spawnDrops()` |
| **Objetivo** | Detecta la colisión con el jugador y añade un diamante al inventario |

**Responsabilidades:**
- `OnCollisionStay2D`: llama a `GameManager.TryAddDiamond(playerEntityId, diamondEntityId)`.
- Si la operación tiene éxito, se destruye a sí mismo.

---

### `DoorController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | Prefab de puerta, instanciado por `TilemapFiller.spawnWalls()` |
| **Objetivo** | Gestiona la apertura de puertas consumiendo una llave del jugador |

**Responsabilidades:**
- Gestiona dos colliders: uno de trigger (detección) y uno bloqueante (físico).
- `OnTriggerEnter2D`: llama a `GameManager.TryOpenDoor(playerEntityId, doorEntityId)`.
- `OpenDoor()`: cambia el sprite, desactiva el collider bloqueante.

---

### `ChestController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | Prefab del cofre del tesoro, instanciado por `LevelGenerator.generateTreasureRoom()` |
| **Objetivo** | Detecta la colisión con el jugador y activa la condición de victoria |

**Responsabilidades:**
- `OnCollisionStay2D`: llama a `GameManager.TryTriggerVictory(playerEntityId, chestEntityId)`.
- Flag `collected` garantiza que la victoria solo se dispara una vez.

---

## 5. Interfaz de usuario

### `CameraController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | `Main Camera` en `PlayGroundLevel` |
| **Objetivo** | Sigue al jugador local con un offset configurable |

**Responsabilidades:**
- Suscribe `GameEvents.OnLocalPlayerRegistered` para actualizar el target cuando el jugador se registra.
- `LateUpdate()`: actualiza `transform.position = target.position + offset`.

---

### `HeadUpDisplayController.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | `HeadUpDisplay` (Canvas) en `PlayGroundLevel` |
| **Objetivo** | Muestra vida, llaves y diamantes del personaje activo. Soporta hasta 4 bloques simultáneos (preparado para multiplayer) |

**Responsabilidades:**
- `Awake()`: determina el bloque activo según `GameManager.SelectedCharacterStats.characterName` y oculta el resto.
- Suscribe `GameEvents.OnHealthChanged`, `OnKeysChanged`, `OnDiamondsChanged`.
- `UpdateHearts`, `UpdateKeys`, `UpdateDiamonds`: actualizan los `Image` con los sprites de dígitos correspondientes.
- `getSpriteForDigit()`: convierte un dígito 0-9 al `Sprite` asignado en Inspector.

---

### `MainMenuButtonsHandler.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | `Canvas` en `MainMenu` |
| **Objetivo** | Gestiona la navegación del menú principal y la selección de mapa |

**Responsabilidades:**
- `initializeMapDropdown()`: rellena el `TMP_Dropdown` con los `MapConfig` disponibles.
- `onMapDropdownChanged()`: actualiza `GameManager.SelectedMapConfig` al cambiar la selección.
- `OnButtonPlayClicked()`: navega a `CharSelectionScene` si hay mapa seleccionado.
- `OnExitButtonClicked()`: cierra la aplicación.

---

### `CharSelectionMenuButtonsHandler.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | `Canvas` en `CharSelectionScene` |
| **Objetivo** | Gestiona la selección de personaje y el inicio de partida |

**Responsabilidades:**
- Cada botón de personaje llama a `selectCharacterAndStartGame(PlayerStats)`.
- Valida que el `PlayerStats` no sea null antes de llamar a `GameManager.StartGame(characterStats)`.
- `OnBackButtonClicked()`: navega a `MainMenu`.

---

### `GameOverCanvasHandler.cs`
| | |
|---|---|
| **Tipo** | `MonoBehaviour` |
| **GameObject** | `Canvas` en `VictoryScene` y `DeadScene` |
| **Objetivo** | Muestra las estadísticas finales de la partida y permite volver al menú |

**Responsabilidades:**
- `Start()` → `displayGameStats()`: lee `GameManager.GetDiamonds()`, `GetKeys()`, `EnemiesKilled` y los escribe en los `TextMeshProUGUI` del panel.
- `OnBackButtonClicked()`: navega a `MainMenu`.

---

## 6. ScriptableObjects — Stats

### `CharacterStats.cs`
| | |
|---|---|
| **Tipo** | `ScriptableObject` abstracto |
| **Uso** | Base para `PlayerStats` y `EnemyStats`. No se crea como asset directamente |
| **Objetivo** | Define los campos estadísticos comunes a todos los personajes |

**Campos:** `characterName`, `characterIcon`, `moveSpeed`, `maxHealth`, `attackDamage`, `knockbackForce`, `knockbackDuration`, `animatorController`.

---

### `PlayerStats.cs`
| | |
|---|---|
| **Tipo** | `ScriptableObject` (hereda `CharacterStats`) |
| **Uso** | Asignado en Inspector de `CharSelectionMenuButtonsHandler`. Guardado en `GameManager.SelectedCharacterStats` |
| **Objetivo** | Define los parámetros específicos del jugador: ataque, cooldown y bonus de velocidad |

**Campos adicionales:** `attackDamage`, `attackCooldown`, `speedBonus`.

---

### `EnemyStats.cs`
| | |
|---|---|
| **Tipo** | `ScriptableObject` abstracto (hereda `CharacterStats`) |
| **Uso** | Base para `ChaseEnemyStats` y `LemniscateEnemyStats` |
| **Objetivo** | Define los campos comunes a todos los enemigos |

**Campos adicionales:** `speedPenalty`, `dropPrefabs` (prefabs de llave y diamante).

---

### `ChaseEnemyStats.cs`
| | |
|---|---|
| **Tipo** | `ScriptableObject` (hereda `EnemyStats`) |
| **Uso** | Asignado al campo `stats` de los prefabs de `EnemyChaseController` |
| **Objetivo** | Configura el comportamiento de persecución y vagabundeo del enemigo dragón |

**Campos adicionales:** `chaseRange`, `wanderChangeInterval`, `wanderSpeedMin`, `wanderSpeedMax`, `idleChance`.

---

### `LemniscateEnemyStats.cs`
| | |
|---|---|
| **Tipo** | `ScriptableObject` (hereda `EnemyStats`) |
| **Uso** | Asignado al campo `stats` de los prefabs de `EnemyLemniscateController` |
| **Objetivo** | Configura la amplitud de la trayectoria en lemniscata del enemigo cabra |

**Campos adicionales:** `patrolDistanceX`, `patrolDistanceY`.

---

## 7. ScriptableObjects — Configuración de mapa

### `MapConfig.cs`
| | |
|---|---|
| **Tipo** | `ScriptableObject` |
| **Uso** | Seleccionado por el jugador en `MainMenu` y guardado en `GameManager.SelectedMapConfig`. Referenciado también como `defaultMapConfig` en `LevelGenerator` |
| **Objetivo** | Define todos los parámetros de diseño de un mapa: tamaño de salas, anillos activos, spawners y probabilidades de drops |

**Estructura:**
- `treasureRoomSize`: tamaño de la sala central.
- 5 anillos opcionales: `decoratedRoom`, `tileRoom`, `woodRoom`, `brokenTileRoom`, `castleYard`. Cada uno con `enabled`, `ringWidth`, `dragonSpawnerCount`, `goatSpawnerCount`, `decorativePercentage`.
- `outerForest`: anillo exterior siempre activo con `ringWidth`.
- `dragonDropConfig`, `goatDropConfig`: configuración de drops por tipo de enemigo.

**Clases serializable internas:** `DecoratedRoomConfig`, `TileRoomConfig`, `WoodRoomConfig`, `BrokenTileRoomConfig`, `CastleYardConfig`, `ForestRingConfig`, `EnemyDropConfig`.

---

## 8. Clases de datos

### `PlayerGameState.cs`
| | |
|---|---|
| **Tipo** | Clase C# serializable (no `MonoBehaviour`) |
| **Uso** | Instanciada y gestionada internamente por `GameManager` |
| **Objetivo** | Almacena el estado de recursos del jugador durante la partida |

**Responsabilidades:**
- Expone `Keys` y `Diamonds` como propiedades con clamp automático.
- Al modificarse, dispara automáticamente `GameEvents.KeysChanged()` o `GameEvents.DiamondsChanged()`.
- `UseKey()`: devuelve `bool` indicando si había llaves disponibles (patrón Try).
- `ResetState()`: reinicia todo a cero al iniciar nueva partida.

---

### `EnemyDropConfig`
| | |
|---|---|
| **Tipo** | Clase C# serializable (interna de `MapConfig.cs`) |
| **Uso** | Campos `dragonDropConfig` y `goatDropConfig` dentro de `MapConfig` |
| **Objetivo** | Define las probabilidades y cantidades de drops por tipo de enemigo |

**Campos:** `keyDropChance` (0-1), `minDiamondDrops`, `maxDiamondDrops`.

---

## 9. Clases auxiliares

### `RingSettings` (en `LevelGenerator.cs`)
Clase serializable que agrupa los **prefabs visuales** de un anillo: tiles ponderados, pared, esquina, puerta abierta/cerrada, elemento decorativo y porcentaje de decorativos. Se asigna en el Inspector de `LevelGenerator` para cada uno de los 6 anillos fijos.

### `WeightedTile` (en `TilemapFiller.cs`)
Clase serializable que asocia un `TileBase` de Unity con un peso relativo para selección aleatoria ponderada.

### `WeightedTilemapFiller` (en `LevelGenerator.cs`)
Clase serializable con referencia a un `TilemapFiller` y un peso (actualmente en desuso, el filler se usa directamente).

### `SceneNames` (en `GameManager.cs`)
Clase estática con constantes de nombres de escena: `MainMenu`, `CharSelection`, `PlayGroundLevel`, `DeadScene`, `VictoryScene`. Evita strings literales dispersos en el código.

### `EntityType` (en `UniqueEntity.cs`)
Enum que clasifica el tipo de entidad: `Player`, `Enemy`, `Pickup_Key`, `Pickup_Diamond`, `Interactive_Door`, `Interactive_Chest`, `Spawner`.

---

## 10. Mapa de relaciones entre scripts
GameEvents (bus estático) │ ├── Disparan eventos: │   ├── PlayerController      → OnHealthChanged, OnPlayerDied │   ├── PlayerGameState       → OnKeysChanged, OnDiamondsChanged │   └── GameManager           → OnLocalPlayerRegistered, OnEnemyKilled │ └── Escuchan eventos: ├── HeadUpDisplayController → OnHealthChanged, OnKeysChanged, OnDiamondsChanged ├── CameraController        → OnLocalPlayerRegistered ├── EnemyChaseController    → OnLocalPlayerRegistered ├── LevelGenerator          → OnLocalPlayerRegistered └── GameManager             → OnPlayerDied
GameManager (singleton) │ ├── Escrito por: │   ├── MainMenuButtonsHandler        → SelectedMapConfig │   ├── CharSelectionMenuButtonsHandler → StartGame(PlayerStats) │   └── LevelGenerator                → RegisterLocalPlayer │ └── Leído por: ├── LevelGenerator     → SelectedMapConfig, SelectedCharacterStats ├── HeadUpDisplayController → GetKeys, GetDiamonds, SelectedCharacterStats ├── CameraController   → LocalPlayerTransform ├── EnemyChaseController → LocalPlayerTransform ├── EnemyController    → SelectedMapConfig (drops), AddEnemyKill ├── KeyCollection      → TryAddKey ├── DiamondCollection  → TryAddDiamond ├── DoorController     → TryOpenDoor ├── ChestController    → TryTriggerVictory └── GameOverCanvasHandler → GetDiamonds, GetKeys, EnemiesKilled
ScriptableObjects (assets en disco) │ ├── PlayerStats    → CharSelectionMenuButtonsHandler → GameManager → PlayerController ├── ChaseEnemyStats     → prefab EnemyChaseController.stats ├── LemniscateEnemyStats → prefab EnemyLemniscateController.stats └── MapConfig      → MainMenuButtonsHandler → GameManager → LevelGenerator → EnemyController.getDropConfig()
