# Escena: CharSelectionScene

## Descripción general
Segunda escena del flujo de juego. Permite al jugador elegir uno de los cuatro personajes disponibles antes de iniciar la partida. Incluye un escenario decorativo generado con Tilemap. El `GameManager` no tiene GameObject propio en esta escena ya que persiste desde `MainMenu` via `DontDestroyOnLoad`.

## Jerarquía de GameObjects
CharSelectionScene ├── Main Camera ├── Directional Light ├── EventSystem ├── Grid │   └── Tilemap └── Canvas ├── ButtonBack ├── ButtonYellow ├── ButtonRed ├── ButtonPurple └── ButtonGreen


## Detalle de componentes

### Main Camera
- **Componente:** `Camera`
- **Rol:** Renderiza la vista de la pantalla de selección

### Directional Light
- **Componente:** `Light` (Directional)
- **Rol:** Iluminación ambiental estándar de la escena

### EventSystem
- **Componentes:** `EventSystem`, `StandaloneInputModule`
- **Rol:** Gestiona los eventos de entrada para los elementos UI del Canvas

### Grid
- **Componente:** `Grid`
- **Hijo:** `Tilemap`

#### → Tilemap
- **Componentes:** `Tilemap`, `TilemapRenderer`
- **Rol:** Fondo decorativo de la pantalla de selección de personaje

### Canvas
- **Render Mode:** Screen Space - Overlay
- **Script:** `CharSelectionMenuButtonsHandler.cs`
- **Rol:** Contenedor principal de la interfaz de selección de personaje

#### → ButtonBack
- **Tipo:** `Button`
- **Callback:** `OnBackButtonClicked()`
- **Comportamiento:** Navega de vuelta a `MainMenu`

#### → ButtonYellow
- **Tipo:** `Button`
- **Callback:** `OnYellowButtonClicked()`
- **Comportamiento:** Asigna `yellowCharacterStats` y llama a `GameManager.StartGame()`

#### → ButtonRed
- **Tipo:** `Button`
- **Callback:** `OnRedButtonClicked()`
- **Comportamiento:** Asigna `redCharacterStats` y llama a `GameManager.StartGame()`

#### → ButtonPurple
- **Tipo:** `Button`
- **Callback:** `OnPurpleButtonClicked()`
- **Comportamiento:** Asigna `purpleCharacterStats` y llama a `GameManager.StartGame()`

#### → ButtonGreen
- **Tipo:** `Button`
- **Callback:** `OnGreenButtonClicked()`
- **Comportamiento:** Asigna `greenCharacterStats` y llama a `GameManager.StartGame()`

---

## Scripts asociados

| Script | GameObject | Función principal |
|---|---|---|
| `CharSelectionMenuButtonsHandler.cs` | Canvas | Gestiona la selección de personaje y la navegación de vuelta al menú |

> ℹ️ `GameManager.cs` no tiene GameObject en esta escena. Persiste desde `MainMenu` mediante `DontDestroyOnLoad`.

---

## `PlayerStats` por personaje

Cada botón de personaje tiene asignado en Inspector un `PlayerStats` ScriptableObject:

| Botón | Campo en Inspector | Tipo |
|---|---|---|
| ButtonGreen | `greenCharacterStats` | `PlayerStats` |
| ButtonPurple | `purpleCharacterStats` | `PlayerStats` |
| ButtonRed | `redCharacterStats` | `PlayerStats` |
| ButtonYellow | `yellowCharacterStats` | `PlayerStats` |

---

## Flujo de interacción

1. El jugador ve los cuatro personajes disponibles en pantalla.
2. Al pulsar un botón de personaje, `selectCharacterAndStartGame()` valida que el `PlayerStats` no sea null.
3. Si es válido, llama a `GameManager.Instance.StartGame(characterStats)`.
4. `GameManager` guarda `SelectedCharacterStats`, resetea el estado de partida y carga `PlayGroundLevel`.
5. Al pulsar **Back**, se carga `MainMenu` manteniendo el `SelectedMapConfig` ya guardado en `GameManager`.

---

## Diagrama de jerarquía
Ver: `docs/scenes/CharSelectionScene_hierarchy.svg`