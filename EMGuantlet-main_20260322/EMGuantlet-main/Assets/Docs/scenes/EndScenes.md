# Escenas de fin de partida: VictoryScene y DeadScene

## Descripción general

Ambas escenas comparten estructura idéntica. Se cargan al finalizar la partida y muestran las estadísticas obtenidas, permitiendo volver al menú principal. `GameManager` persiste desde escenas anteriores vía `DontDestroyOnLoad` y es la fuente de los datos mostrados.

| Escena | Condición de acceso | Mensaje |
|---|---|---|
| `VictoryScene` | El jugador alcanza el cofre del tesoro | Victoria |
| `DeadScene` | El jugador pierde toda la vida | Game Over |

---

## Jerarquía de GameObjects (compartida)
[VictoryScene | DeadScene] ├── Main Camera ├── Directional Light ├── EventSystem ├── Grid │   └── Tilemap └── Canvas └── Panel ├── TitleText ├── JewelsValueText ├── KeysValueText ├── EnemiesKilledText └── ButtonBack

---

## Detalle de componentes

### Main Camera
- **Componente:** `Camera`
- **Rol:** Renderiza la pantalla de fin de partida

### Directional Light
- **Componente:** `Light` (Directional)
- **Rol:** Iluminación ambiental estándar

### EventSystem
- **Componentes:** `EventSystem`, `StandaloneInputModule`
- **Rol:** Gestiona los eventos de entrada para el Canvas

### Grid
- **Componente:** `Grid`
- **Hijo:** `Tilemap`

#### → Tilemap
- **Componentes:** `Tilemap`, `TilemapRenderer`
- **Rol:** Fondo decorativo de la pantalla de fin de partida

### Canvas
- **Render Mode:** Screen Space - Overlay
- **Script:** `GameOverCanvasHandler.cs`
- **Rol:** Contenedor principal de la interfaz de fin de partida

#### → Panel
- **Tipo:** `Image` (panel de fondo)
- **Rol:** Agrupa visualmente los elementos de estadísticas

#### → Panel / TitleText
- **Tipo:** `TextMeshProUGUI`
- **Rol:** Muestra el título de la pantalla ("Victoria" o "Game Over")
- **Contenido:** Estático, definido en el prefab de cada escena

#### → Panel / JewelsValueText
- **Tipo:** `TextMeshProUGUI`
- **Campo inspector:** `jewelsValueText`
- **Valor mostrado:** `GameManager.Instance.GetDiamonds()`

#### → Panel / KeysValueText
- **Tipo:** `TextMeshProUGUI`
- **Campo inspector:** `keysValueText`
- **Valor mostrado:** `GameManager.Instance.GetKeys()`

#### → Panel / EnemiesKilledText
- **Tipo:** `TextMeshProUGUI`
- **Campo inspector:** `enemiesKilledText`
- **Valor mostrado:** `GameManager.Instance.EnemiesKilled`

#### → Panel / ButtonBack
- **Tipo:** `Button`
- **Callback:** `OnBackButtonClicked()`
- **Comportamiento:** Carga `MainMenu` vía `SceneManager.LoadScene(SceneNames.MainMenu)`

---

## Scripts asociados

| Script | GameObject | Función principal |
|---|---|---|
| `GameOverCanvasHandler.cs` | Canvas | Lee estadísticas de `GameManager` y las muestra en pantalla. Gestiona la vuelta al menú |

> ℹ️ `GameManager.cs` no tiene GameObject en estas escenas. Persiste desde escenas anteriores mediante `DontDestroyOnLoad`. Los datos de partida (`Diamonds`, `Keys`, `EnemiesKilled`) se preservan hasta que se llama a `ResetGameData()` en el siguiente `StartGame()`.

---

## Flujo de interacción

1. La escena se carga desde `GameManager` tras invocar `loadDeadScene()` o `loadVictoryScene()` con un delay de `delayBeforeScene` segundos.
2. `GameOverCanvasHandler.Start()` llama a `displayGameStats()` que lee los valores de `GameManager` y los escribe en los `TextMeshProUGUI`.
3. El jugador visualiza las estadísticas finales.
4. Al pulsar **Volver**, se carga `MainMenu` y el ciclo comienza de nuevo.

---

## Diferencias entre escenas

| Elemento | VictoryScene | DeadScene |
|---|---|---|
| Condición de acceso | `TryTriggerVictory()` | `TriggerGameOver()` |
| Título visual | "Victoria" | "Game Over" |
| Música / efectos | (pendiente) | (pendiente) |
| Script de Canvas | `GameOverCanvasHandler.cs` | `GameOverCanvasHandler.cs` |

> El script es el mismo en ambas escenas. La diferencia es únicamente visual (título y estética del panel).

---

## Diagrama de jerarquía
Ver: `docs/scenes/EndScenes_hierarchy.svg`