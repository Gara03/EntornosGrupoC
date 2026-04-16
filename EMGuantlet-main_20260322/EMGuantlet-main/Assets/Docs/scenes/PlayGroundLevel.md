# Escena: PlayGroundLevel

## Descripción general
Escena principal de gameplay. Se genera de forma procedural al iniciarse mediante `LevelGenerator`. Contiene al jugador, el mapa de juego, los spawners de enemigos (instanciados en runtime), el HUD y la cámara de seguimiento. `GameManager` persiste desde escenas anteriores vía `DontDestroyOnLoad`.

## Jerarquía de GameObjects
PlayGroundLevel ├── Main Camera          ← CameraController.cs ├── Directional Light ├── EventSystem ├── HeadUpDisplay        ← Canvas · HeadUpDisplayController.cs │   ├── YellowHUDRoot    ← root vacío del bloque HUD amarillo │   │   ├── YellowImageChar │   │   ├── YellowImageHeart │   │   ├── YellowImageHeartTens │   │   ├── YellowImageHeartUnits │   │   ├── YellowImageKey │   │   ├── YellowImageKeyUnits │   │   ├── YellowImageDiamond │   │   ├── YellowImageDiamondHundreds │   │   ├── YellowImageDiamondTens │   │   └── YellowImageDiamondUnits │   ├── RedHUDRoot        ← misma estructura para rojo │   ├── PurpleHUDRoot     ← misma estructura para morado │   └── GreenHUDRoot      ← misma estructura para verde ├── LevelGenerator       ← LevelGenerator.cs · TilemapFiller.cs └── Grid └── Tilemap          ← mapa generado en runtime └── Player       ← PlayerController.cs · UniqueEntity

> ⚙️ Enemigos, spawners, puertas, llaves y diamantes se instancian en **runtime** por `LevelGenerator` y no forman parte de la jerarquía inicial de la escena.

---

## Detalle de componentes

### Main Camera
- **Componentes:** `Camera`, `CameraController.cs`
- **Rol:** Renderiza el nivel de juego y sigue al jugador local
- **Comportamiento:** `CameraController` lee `GameManager.LocalPlayerTransform` al iniciarse y sigue al jugador con un offset configurable

### Directional Light
- **Componente:** `Light` (Directional)
- **Rol:** Iluminación ambiental estándar de la escena

### EventSystem
- **Componentes:** `EventSystem`, `StandaloneInputModule`
- **Rol:** Gestiona los eventos de entrada para el Canvas del HUD

---

### HeadUpDisplay (Canvas)
- **Render Mode:** Screen Space - Overlay
- **Script:** `HeadUpDisplayController.cs`
- **Rol:** Muestra vida, llaves y diamantes del jugador activo
- **Comportamiento:** En `Awake()` determina el bloque activo según `GameManager.SelectedCharacterStats.characterName` y oculta el resto

#### Estructura de cada bloque HUD

Cada color tiene un **GameObject raíz vacío** que actúa como contenedor activable. En singleplayer solo el bloque del personaje elegido queda activo.

| Elemento | Tipo | Rol |
|---|---|---|
| `[Color]HUDRoot` | `GameObject` vacío | Raíz del bloque. Se activa o desactiva según el personaje |
| `[Color]ImageChar` | `Image` | Icono identificativo del personaje |
| `[Color]ImageHeart` | `Image` | Icono de vida |
| `[Color]ImageHeartTens` | `Image` | Dígito de las decenas de la vida |
| `[Color]ImageHeartUnits` | `Image` | Dígito de las unidades de la vida |
| `[Color]ImageKey` | `Image` | Icono de llave |
| `[Color]ImageKeyUnits` | `Image` | Dígito de llaves |
| `[Color]ImageDiamond` | `Image` | Icono de diamante |
| `[Color]ImageDiamondHundreds` | `Image` | Dígito de las centenas de diamantes |
| `[Color]ImageDiamondTens` | `Image` | Dígito de las decenas de diamantes |
| `[Color]ImageDiamondUnits` | `Image` | Dígito de las unidades de diamantes |

#### Eventos que actualiza el HUD

| Evento | Método activado | Actualiza |
|---|---|---|
| `GameEvents.OnHealthChanged` | `UpdateHearts(int)` | Decenas y unidades de vida |
| `GameEvents.OnKeysChanged` | `UpdateKeys()` | Unidades de llaves |
| `GameEvents.OnDiamondsChanged` | `UpdateDiamonds()` | Centenas, decenas y unidades de diamantes |

---

### LevelGenerator
- **Scripts:** `LevelGenerator.cs`, `TilemapFiller.cs`
- **Rol:** Genera el mapa proceduralmente al iniciarse la escena
- **Comportamiento:** Lee `GameManager.SelectedMapConfig` (o `defaultMapConfig` como fallback), genera la sala del tesoro y los anillos concéntricos, instancia enemigos y elementos decorativos, posiciona al jugador

#### Elementos instanciados en runtime por LevelGenerator

| Elemento | Tipo | Descripción |
|---|---|---|
| Paredes y esquinas | `GameObject` prefab | Borde de cada sala y anillo |
| Puertas | `GameObject` prefab | `DoorController.cs` + `UniqueEntity` |
| Cofre del tesoro | `GameObject` prefab | `ChestController.cs` + `UniqueEntity` |
| Spawners de enemigos | `GameObject` prefab | `EnemySpawner.cs` + `UniqueEntity` |
| Elementos decorativos | `GameObject` prefab | Objetos visuales sin lógica |
| Enemigos | `GameObject` prefab | `EnemyChaseController.cs` o `EnemyLemniscateController.cs` + `UniqueEntity` |
| Llaves | `GameObject` prefab | `KeyCollection.cs` + `UniqueEntity` |
| Diamantes | `GameObject` prefab | `DiamondCollection.cs` + `UniqueEntity` |

---

### Grid → Tilemap
- **Componentes:** `Grid`, `Tilemap`, `TilemapRenderer`
- **Rol:** Contenedor del mapa generado. `TilemapFiller` escribe los tiles en runtime

#### → Player
- **Scripts:** `PlayerController.cs`, `UniqueEntity`
- **Comportamiento inicial:** Se activa deshabilitado (`SetActive(false)`) y es reposicionado por `LevelGenerator.ApplySpawnAndCharacter()` una vez generado el nivel
- **Registro:** En `Awake()`, `PlayerController` llama a `GameManager.RegisterLocalPlayer()`, que dispara `GameEvents.OnLocalPlayerRegistered`

---

## Scripts asociados

| Script | GameObject | Función principal |
|---|---|---|
| `CameraController.cs` | Main Camera | Sigue al jugador local con offset configurable |
| `HeadUpDisplayController.cs` | HeadUpDisplay | Gestiona los 4 bloques de HUD y actualiza los dígitos en pantalla |
| `LevelGenerator.cs` | LevelGenerator | Generación procedural del mapa y posicionado del jugador |
| `TilemapFiller.cs` | LevelGenerator | Escribe tiles y genera paredes, puertas, spawners y decorativos |
| `PlayerController.cs` | Player | Control de movimiento, ataque y vida del jugador |
| `UniqueEntity` | Player (y todos los prefabs instanciados) | Identificador único para sincronización |

> ℹ️ `GameManager.cs` no tiene GameObject en esta escena. Persiste desde `MainMenu` mediante `DontDestroyOnLoad`.

---

## Flujo de inicialización

1. `LevelGenerator.Start()` verifica que `GameManager.SelectedMapConfig` sea válido (asigna `defaultMapConfig` si no lo es).
2. `GenerateLevel()` genera la sala del tesoro y los anillos del castillo con tiles, paredes, puertas, spawners y decorativos.
3. `PreparePlayerSpawn()` calcula la posición de spawn en el penúltimo anillo activo.
4. `Player.Awake()` (ya ejecutado antes) llama a `GameManager.RegisterLocalPlayer()` → dispara `OnLocalPlayerRegistered`.
5. `LevelGenerator` recibe el evento y llama a `ApplySpawnAndCharacter()`: activa al jugador, lo posiciona y aplica las stats del personaje seleccionado.
6. `CameraController` recibe también `OnLocalPlayerRegistered` y comienza a seguir al jugador.
7. `HeadUpDisplayController.Awake()` activa el bloque HUD correspondiente al personaje seleccionado.

---

## Condiciones de fin de partida

| Condición | Origen | Destino |
|---|---|---|
| Jugador sin vida | `PlayerController.Die()` → `GameManager.TriggerGameOver()` | `DeadScene` |
| Jugador alcanza el cofre | `ChestController.OnCollisionStay2D()` → `GameManager.TryTriggerVictory()` | `VictoryScene` |

---

## Diagrama de jerarquía
Ver: `docs/scenes/PlayGroundLevel_hierarchy.svg`