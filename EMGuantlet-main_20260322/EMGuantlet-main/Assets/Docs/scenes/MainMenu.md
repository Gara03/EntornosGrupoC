# Escena: MainMenu

## Descripción general
Primera escena del juego. Permite seleccionar el mapa, acceder a opciones y comenzar la partida. Incluye un escenario decorativo generado con Tilemap.

## Jerarquía de GameObjects
MainMenu ├── Main Camera ├── Directional Light ├── EventSystem ├── GameManager ├── Grid │   └── Tilemap └── Canvas ├── ButtonPlay ├── ButtonOptions ├── ButtonExit └── MapsDropdown


## Detalle de componentes

### Main Camera
- **Componente:** `Camera`
- **Rol:** Renderiza la vista del menú principal

### Directional Light
- **Componente:** `Light` (Directional)
- **Rol:** Iluminación ambiental estándar de la escena

### EventSystem
- **Componentes:** `EventSystem`, `StandaloneInputModule`
- **Rol:** Gestiona los eventos de entrada para los elementos UI del Canvas

### GameManager
- **Script:** `GameManager.cs`
- **Comportamiento:** Singleton con `DontDestroyOnLoad`
- **Rol:** Persiste entre escenas. Almacena `SelectedMapConfig` y `SelectedCharacterStats`

### Grid
- **Componente:** `Grid`
- **Hijo:** `Tilemap`

#### → Tilemap
- **Componentes:** `Tilemap`, `TilemapRenderer`
- **Rol:** Fondo decorativo del menú principal

### Canvas
- **Render Mode:** Screen Space - Overlay
- **Script:** `MainMenuButtonsHandler.cs`
- **Rol:** Contenedor principal de la interfaz de usuario

#### → ButtonPlay
- **Tipo:** `Button`
- **Callback:** `OnButtonPlayClicked()`
- **Comportamiento:** Navega a `CharSelectionScene` si hay `SelectedMapConfig` válido

#### → ButtonOptions
- **Tipo:** `Button`
- **Callback:** `OnOptionsButtonClicked()`
- **Comportamiento:** Pendiente de implementación

#### → ButtonExit
- **Tipo:** `Button`
- **Callback:** `OnExitButtonClicked()`
- **Comportamiento:** `Application.Quit()` en build / `EditorApplication.isPlaying = false` en editor

#### → MapsDropdown
- **Tipo:** `TMP_Dropdown`
- **Listener:** `OnMapDropdownChanged(int index)`
- **Comportamiento:** Rellenado dinámicamente con los `MapConfig[]` disponibles. Actualiza `GameManager.SelectedMapConfig` al cambiar

---

## Scripts asociados

| Script | GameObject | Función principal |
|---|---|---|
| `GameManager.cs` | GameManager | Singleton persistente. Gestiona estado global de la partida |
| `MainMenuButtonsHandler.cs` | Canvas | Navegación de menú y selección de mapa |

---

## Flujo de interacción

1. `MainMenuButtonsHandler.Start()` rellena el dropdown con los `MapConfig` disponibles y registra el primero por defecto en `GameManager.SelectedMapConfig`.
2. El jugador puede cambiar el mapa seleccionado desde el dropdown.
3. **Play** → navega a `CharSelectionScene` (requiere `SelectedMapConfig` válido).
4. **Options** → pendiente de implementación.
5. **Exit** → cierra la aplicación.

---

## Diagrama de jerarquía
Ver: `docs/scenes/MainMenu_hierarchy.svg`