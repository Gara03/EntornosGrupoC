using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public enum CharacterColor { Red, Green, Yellow, Purple }

public class CharSelectionNetworkManager : NetworkBehaviour
{
    [Header("UI Reference")]
    public CharSelectionMenuButtonsHandler uiHandler;

    public NetworkVariable<bool> isGreenTaken = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isPurpleTaken = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isRedTaken = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isYellowTaken = new NetworkVariable<bool>(false);

    private Dictionary<ulong, CharacterColor> playerSelections = new Dictionary<ulong, CharacterColor>();

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

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

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

    [Rpc(SendTo.Everyone)]
    public void ConfirmCharacterRpc(CharacterColor confirmedChar, ulong ownerClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == ownerClientId)
        {
            uiHandler.ConfirmLocalCharacter(confirmedChar);
        }
    }
}
