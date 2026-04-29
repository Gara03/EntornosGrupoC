using System;

public static class GameEvents
{
    public static event Action<ulong, int> OnHealthChanged;
    public static event Action<ulong> OnKeysChanged;
    public static event Action<ulong> OnDiamondsChanged;
    public static event Action<int> OnEnemyKilled;
    public static event Action<PlayerController> OnLocalPlayerRegistered;
    public static event Action<ulong> OnPlayerDied;
    public static event Action OnVictory;

    /// <summary>
    /// Notifica un cambio en la salud del jugador.
    /// </summary>
    public static void HealthChanged(ulong clientId, int newHealth)
    {
        OnHealthChanged?.Invoke(clientId, newHealth);
    }

    /// <summary>
    /// Notifica un cambio en el número de llaves del jugador.
    /// </summary>
    public static void KeysChanged(ulong clientId)
    {
        OnKeysChanged?.Invoke(clientId);
    }

    /// <summary>
    /// Notifica un cambio en el número de diamantes del jugador.
    /// </summary>
    public static void DiamondsChanged(ulong clientId)
    {
        OnDiamondsChanged?.Invoke(clientId);
    }

    /// <summary>
    /// Notifica el total actualizado de enemigos eliminados.
    /// </summary>
    public static void EnemyKilled(int totalKills)
    {
        OnEnemyKilled?.Invoke(totalKills);
    }

    /// <summary>
    /// Notifica que el jugador local ha sido registrado en el sistema.
    /// </summary>
    public static void LocalPlayerRegistered(PlayerController player)
    {
        OnLocalPlayerRegistered?.Invoke(player);
    }

    /// <summary>
    /// Notifica que el jugador ha muerto.
    /// </summary>
    public static void PlayerDied(ulong clientId)
    {
        OnPlayerDied?.Invoke(clientId);
    }

    /// <summary>
    /// Notifica que se ha alcanzado la condición de victoria.
    /// </summary>
    public static void Victory()
    {
        OnVictory?.Invoke();
    }

    /// <summary>
    /// Limpia los eventos asociados al ciclo de vida de una escena.
    /// </summary>
    public static void ClearSceneEvents()
    {
        OnHealthChanged = null;
        OnKeysChanged = null;
        OnDiamondsChanged = null;
        OnEnemyKilled = null;
        OnLocalPlayerRegistered = null;
    }

    /// <summary>
    /// Limpia todos los eventos registrados en el sistema global.
    /// </summary>
    public static void ClearAllEvents()
    {
        ClearSceneEvents();
        OnPlayerDied = null;
        OnVictory = null;
    }
}