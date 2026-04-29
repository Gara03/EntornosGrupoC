using System;
using UnityEngine;

/// <summary>
/// Representa el estado persistente de recursos del jugador durante la partida.
/// </summary>
[Serializable]
public class PlayerGameState
{
    public const int MAX_KEYS = 9;
    public const int MAX_DIAMONDS = 999;

    public string playerId;
    public ulong ownerClientId; // Necesario para saber a qué HUD avisar

    public int keys = 0;
    public int diamonds = 0;

    /// <summary>
    /// Inicializa el estado del jugador con su identificador único y su ID de red.
    /// </summary>
    public PlayerGameState(string entityId, ulong clientId)
    {
        playerId = entityId;
        ownerClientId = clientId;
    }

    public int Keys
    {
        get => keys;
        set
        {
            keys = Mathf.Clamp(value, 0, MAX_KEYS);
            GameEvents.KeysChanged(ownerClientId);
        }
    }

    public int Diamonds
    {
        get => diamonds;
        set
        {
            diamonds = Mathf.Clamp(value, 0, MAX_DIAMONDS);
            GameEvents.DiamondsChanged(ownerClientId);
        }
    }

    public void AddKey()
    {
        if (keys < MAX_KEYS)
        {
            Keys++; // Esto dispara el evento automáticamente por el setter
        }
    }

    public void AddDiamond()
    {
        if (diamonds < MAX_DIAMONDS)
        {
            Diamonds++;
        }
    }

    public bool UseKey()
    {
        if (keys > 0)
        {
            Keys--;
            return true;
        }
        return false;
    }

    public void ResetState()
    {
        keys = 0;
        diamonds = 0;
        GameEvents.KeysChanged(ownerClientId);
        GameEvents.DiamondsChanged(ownerClientId);
    }
}