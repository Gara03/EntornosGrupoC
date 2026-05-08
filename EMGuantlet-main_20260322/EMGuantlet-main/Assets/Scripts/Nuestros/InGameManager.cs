using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Clase encargada de gestionar la desconexion ingame
/// </summary>
public class InGameManager : NetworkBehaviour
{
    [Header("Paneles UI")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TextMeshProUGUI warningText;

    private bool isShuttingDown = false;

    /// <summary>
    /// Se ejecuta en cuanto el jugador se conecta.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }

        if (IsServer)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = InGameApprovalCheck;
            Debug.Log("[Host] Partida iniciada. Se cierran las puertas a nuevos jugadores.");
        }
    }

    /// <summary>
    /// Se ejecuta cuando el jugador se desconecta.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
        }

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
        }
    }

    /// <summary>
    /// Metodo que rechaza a cualquier jugador que intente unirse cuando la partida ya ha empezado.
    /// </summary>
    private void InGameApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = false;
        response.Reason = "Partida en curso, espera a que acabe.";

        Debug.Log("[Host] Conexión rechazada: Alguien intentó unirse a una partida en curso.");
    }

    private void Start()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);
    }

    /// <summary>
    /// Boton de salir
    /// </summary>
    public void OnClickOpenExitMenu()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(true);
    }

    /// <summary>
    /// Boton de "Cancelar" en el panel de confirmacion
    /// </summary>
    public void OnClickCancelExit()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    /// <summary>
    /// Boton de "Salir" en el panel de confirmacion
    /// </summary>
    public void OnClickConfirmExit()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        if (IsServer)
        {
            // Si es el host, cierra la conexion para los clientes
            Debug.Log("[Host] Iniciando cierre ordenado de servidor...");
            NotifyServerClosingClientRpc("El Host ha cerrado la partida. Volviendo al menú principal.");

            Invoke(nameof(ShutdownAndReturn), 1f); 
        }
        else
        {
            // Si es el cliente se va sin afectar a la partida
            Debug.Log("[Cliente] Abandonando la partida...");
            ShutdownAndReturn();
        }
    }

    /// <summary>
    /// Al desconectarse el host se notifica a todos los clientes
    /// </summary>
    [ClientRpc]
    private void NotifyServerClosingClientRpc(string message)
    {
        // El Host no necesita ver este panel porque ya se fue
        if (IsServer) return;

        Debug.Log("[Red] Aviso del Host recibido: " + message);

        // Se muestra el panel de aviso con el mensaje
        if (warningPanel != null && warningText != null)
        {
            warningText.text = message;
            warningPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Boton de "volver" en el panel de aviso de los clientes
    /// </summary>
    public void OnClickAcceptWarningAndLeave()
    {
        ShutdownAndReturn();
    }

    /// <summary>
    /// Metodo para comprobar si un jugador se ha desconectado
    /// </summary>
    private void OnPlayerDisconnected(ulong clientId)
    {
        if (isShuttingDown) return; 

        // Si el que se desconecta es el host o el propio jugador
        if (clientId == NetworkManager.ServerClientId || clientId == NetworkManager.Singleton.LocalClientId)
        {
            isShuttingDown = true;

            // Se muestra el panel
            if (warningPanel != null && warningText != null)
            {
                warningText.text = "Se ha perdido la conexión con el servidor.";
                warningPanel.SetActive(true);
            }
        }
        else if (IsServer) // Si es el host y se ha desconectado otro jugador
        {
            Debug.Log($"[Host] El cliente {clientId} se ha desconectado bruscamente.");
        }
    }

    /// <summary>
    /// Cierra la conexion y se carga el menu de inicio
    /// </summary>
    private void ShutdownAndReturn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Se resetea la seleccion de personaje
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SelectedCharacterStats = null;
        }

        SceneManager.LoadScene(SceneNames.MainMenu);
    }
}
