# EMGauntlet

> Juego de mazmorras _top-down_ desarrollado en Unity como base para la práctica de conversión a multijugador de la asignatura **Entornos Multijugador** — Curso 25/26, Grado en Diseño y Desarrollo de Videojuegos.

![Unity](https://img.shields.io/badge/Unity-6%20LTS-black?logo=unity) ![C#](https://img.shields.io/badge/C%23-9.0-purple?logo=csharp) ![License](https://img.shields.io/badge/license-MIT-blue)

---

## Descripción

**EMGuantlet** es un dungeon crawler singleplayer inspirado en el arcade clásico [Gauntlet (Atari, 1985)](https://en.wikipedia.org/wiki/Gauntlet_(1985_video_game)). El jugador elige uno de cuatro personajes, explora un castillo generado proceduralmente mediante anillos concéntricos y debe alcanzar el cofre del tesoro situado en la sala central, combatiendo enemigos, recogiendo llaves para abrir puertas y acumulando diamantes por el camino.

El proyecto está diseñado desde el inicio con **patrones preparatorios para red**: identificadores únicos por entidad (`UniqueEntity`), patrón `Try...` en el gestor de juego y un bus de eventos desacoplado (`GameEvents`). Estos patrones son el punto de partida de la práctica académica de conversión a multijugador con **Netcode for GameObjects**.

---

## Características

- **Mapa procedimental** generado a partir de `MapConfig` (ScriptableObject): número de anillos, tipos de tiles, densidad de spawners y probabilidades de drops son configurables sin tocar el código.
- **Cuatro personajes** con estadísticas diferenciadas: Yellow, Red, Purple y Green (velocidad, daño, salud y knockback variables).
- **Dos tipos de enemigos:**
  - *Cabra* (`EnemyChaseController`): persigue al jugador cuando está en rango; vagabundea aleatoriamente cuando no.
  - *Dragón* (`EnemyLemniscateController`): patrulla en trayectoria de lemniscata (figura 8) alrededor de su punto de spawn.
- **Sistema de drops**: los enemigos sueltan llaves y diamantes según la configuración del mapa.
- **Condición de victoria**: alcanzar el cofre de la sala central.
- **Arquitectura orientada a multijugador**: patrón Singleton en `GameManager`, bus de eventos estático `GameEvents`, GUID por entidad en `UniqueEntity`.

---

## Capturas de pantalla

> *(Próximamente)*

---

## Requisitos

| Elemento | Versión |
|---|---|
| Unity | 6 LTS |
| .NET | Framework 4.7.1 |
| Input System | Unity Input System |
| TextMeshPro | Incluido en el proyecto |

---

## Instalación y ejecución

```bash
# Clona el repositorio
git clone https://github.com/neur0nid/EMGuantlet.git
```

1. Abre el proyecto en **Unity 6 LTS** desde Unity Hub.
2. En el menú **File → Build Profiles**, selecciona la plataforma deseada.
3. Abre la escena `Assets/Scenes/MainMenu` y pulsa **Play** en el editor.

---

## Estructura del proyecto

```
Assets/
├── Scripts/          # Código fuente C# (MonoBehaviours, ScriptableObjects, datos auxiliares)
│   ├── Config/       # MapConfig.cs — configuración de mapa
│   └── Stats/        # CharacterStats, PlayerStats, EnemyStats y subclases
├── Prefabs/          # Prefabs de personajes, enemigos, ítems, puertas, spawners
├── Scenes/           # MainMenu, CharSelectionScene, PlayGroundLevel, DeadScene, VictoryScene
├── ScriptableObjects/ # Assets de configuración (MapConfigs, Stats)
├── Art/              # Sprites y tilesets (ver créditos)
├── Input/            # InputActions (Unity Input System)
├── Settings/         # Configuración de render y audio
└── Docs/
    ├── scenes_overview.md      # Arquitectura de escenas y jerarquías de GameObjects
    ├── scenes_flow.svg         # Diagrama de flujo entre escenas
    ├── scripts_overview.md     # Ficha técnica de cada script
    ├── scripts_relations.svg   # Diagrama de relaciones entre scripts (v1)
    └── scenes/                 # Documentación detallada por escena
        ├── MainMenu.md
        ├── MainMenu_hierarchy.svg
        ├── CharSelectionScene.md
        ├── CharSelectionScene_hierarchy.svg
        ├── PlayGroundLevel.md
        ├── PlayGroundLevel_hierarchy.svg
        ├── EndScenes.md
        └── EndScenes_hirarchy.svg
```

---

## Créditos de arte

Los assets gráficos utilizados en este proyecto provienen de **Kenney.nl** y se distribuyen bajo licencia [CC0 1.0 Universal (dominio público)](https://creativecommons.org/publicdomain/zero/1.0/):

- **Scribble Dungeons** — [kenney.nl/assets/scribble-dungeons](https://kenney.nl/assets/scribble-dungeons)
- **Scribble Platformer** — [kenney.nl/assets/scribble-platformer](https://kenney.nl/assets/scribble-platformer)

---

## Licencia

Este proyecto se distribuye bajo la licencia **MIT**. Consulta el archivo `LICENSE` para más información.
