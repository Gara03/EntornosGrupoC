# Resumen de Escenas del Juego

## 1. `MainMenu`
**Rol:** Escena inicial del juego.  
**Funcionalidad principal:**
- Mostrar botón para jugar.
- Mostrar selector de mapa.
- Mostrar botón para salir del juego.

---

## 2. `CharSelectionScene`
**Rol:** Escena de selección de personaje.  
**Funcionalidad principal:**
- Mostrar los 4 personajes seleccionables.
- Permitir elegir un personaje para iniciar la partida.
- Permitir volver al menú principal.

---

## 3. `PlayGroundLevel`
**Rol:** Escena principal de gameplay.  
**Funcionalidad principal:**
- Cargar el nivel jugable tras seleccionar personaje.
- Ejecutar la partida (movimiento, combate, enemigos, ítems y progreso del mapa).

---

## 4. `VictoryScene`
**Rol:** Escena de fin de partida por victoria.  
**Condición de acceso:**
- Se carga cuando el personaje alcanza el cofre en el centro del mapa.  
**Funcionalidad principal:**
- Mostrar estadísticas de la partida.
- Permitir volver al menú principal.

---

## 5. `DeadScene`
**Rol:** Escena de fin de partida por derrota.  
**Condición de acceso:**
- Se carga cuando el personaje pierde toda la vida.  
**Funcionalidad principal:**
- Mostrar estadísticas de la partida.
- Permitir volver al menú principal.

---

## Flujo general de escenas

`MainMenu` ? `CharSelectionScene` ? `PlayGroundLevel` ? (`VictoryScene` | `DeadScene`) ? `MainMenu`