using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public enum CharacterColor { Red, Green, Yellow, Purple }

public class CharSelectionNetworkManager : NetworkBehaviour
{
    private Dictionary<ulong, CharacterColor> playerSelections = new Dictionary<ulong, CharacterColor>();

    [Header("UI Reference")]
    public CharSelectionMenuButtonsHandler uiHandler;

    [Header("Taken characters")]
    public NetworkVariable<bool> isGreenTaken = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isPurpleTaken = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isRedTaken = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isYellowTaken = new NetworkVariable<bool>(false);

    /// <summary>
    /// Método encargado de mostrar qué personajes se encuentran libres para seleccionar.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        isGreenTaken.OnValueChanged += (oldVal, newVal) => uiHandler.UpdateButtonState(CharacterColor.Green, newVal);
        isPurpleTaken.OnValueChanged += (oldVal, newVal) => uiHandler.UpdateButtonState(CharacterColor.Purple, newVal);
        isRedTaken.OnValueChanged += (oldVal, newVal) => uiHandler.UpdateButtonState(CharacterColor.Red, newVal);
        isYellowTaken.OnValueChanged += (oldVal, newVal) => uiHandler.UpdateButtonState(CharacterColor.Yellow, newVal);

        uiHandler.UpdateButtonState(CharacterColor.Green, isGreenTaken.Value);
        uiHandler.UpdateButtonState(CharacterColor.Purple, isPurpleTaken.Value);
        uiHandler.UpdateButtonState(CharacterColor.Red, isRedTaken.Value);
        uiHandler.UpdateButtonState(CharacterColor.Yellow, isYellowTaken.Value);

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
    }

    /// <summary>
    /// Función llamada cuando un jugador se desconecta/vuelve atrás.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    /// <summary>
    /// Reestablece la disponibilidad del personaje cuando el jugador se desconecta.
    /// </summary>
    private void HandleClientDisconnect(ulong clientId)
    {
        if (playerSelections.TryGetValue(clientId, out CharacterColor colorToFree))
        {
            switch (colorToFree)
            {
                case CharacterColor.Green: isGreenTaken.Value = false; break;
                case CharacterColor.Purple: isPurpleTaken.Value = false; break;
                case CharacterColor.Red: isRedTaken.Value = false; break;
                case CharacterColor.Yellow: isYellowTaken.Value = false; break;
            }

            playerSelections.Remove(clientId);
            Debug.Log($"[Servidor] El jugador {clientId} se desconectó. Se liberó el personaje {colorToFree}");
        }
    }

    /// <summary>
    /// Llama a la función principal que avisa al servidor para solicitar un personaje.
    /// </summary>
    public void RequestCharacter(CharacterColor color)
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsConnectedClient)
            {
                RequestCharacterServerRpc(color);
            }
            else
            {
                Debug.LogWarning("[Red] No puedes elegir personaje porque no estás conectado como Host ni Cliente.");
            }
        }
    }

    /// <summary>
    /// Comprueba preguntando al servidor la disponibilidad del personaje seleccionado.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestCharacterServerRpc(CharacterColor requestedChar, RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        bool success = false;

        switch (requestedChar)
        {
            case CharacterColor.Green:
                if (!isGreenTaken.Value) { isGreenTaken.Value = true; success = true; }
                break;

            case CharacterColor.Purple:
                if (!isPurpleTaken.Value) { isPurpleTaken.Value = true; success = true; }
                break;

            case CharacterColor.Red:
                if (!isRedTaken.Value) { isRedTaken.Value = true; success = true; }
                break;

            case CharacterColor.Yellow:
                if (!isYellowTaken.Value) { isYellowTaken.Value = true; success = true; }
                break;
        }

        if (success)
        {
            playerSelections[clientId] = requestedChar;
            ConfirmCharacterRpc(requestedChar, clientId);
        }
    }

    /// <summary>
    /// Confirma la selección de personaje y lo comunica a todos.
    /// </summary>
    [Rpc(SendTo.Everyone)]
    public void ConfirmCharacterRpc(CharacterColor confirmedChar, ulong ownerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            uiHandler.ConfirmLocalCharacter(confirmedChar);
        }
    }
}
