using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuButtonsHandler : MonoBehaviour
{
    [Header("Map Configs disponibles")]
    [SerializeField] private MapConfig[] availableMaps;

    [Header("UI")]
    [SerializeField] private TMP_Dropdown mapsDropdown;

    [Header("UI Errores")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private TMP_Text errorText;

    /// <summary>
    /// Inicializa el dropdown de mapas al cargar el menú principal.
    /// </summary>
    private void Start()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.Log("[MainMenu] Apagando sesión de red fantasma anterior...");
            NetworkManager.Singleton.Shutdown();
        }

        initializeMapDropdown();
    }

    /// <summary>
    /// Libera la suscripción del dropdown al destruir el objeto.
    /// </summary>
    private void OnDestroy()
    {
        if (mapsDropdown != null)
            mapsDropdown.onValueChanged.RemoveListener(onMapDropdownChanged);
    }

    /// <summary>
    /// Navega a la escena de selección de personaje si hay mapa seleccionado.
    /// </summary>
    public void OnStartHostClicked()
    {
        if (GameManager.Instance?.SelectedMapConfig == null)
        {
            Debug.LogWarning("[MainMenu] No hay mapa seleccionado.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[MainMenu] La red ya está iniciada. Ignorando clic adicional.");
            return;
        }

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        if (NetworkManager.Singleton.StartHost())
        {
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.CharSelection, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Fallo al iniciar el Host.");
        }
    }

    /// <summary>
    /// Inicia la conexión como Cliente para unirse a un Host existente.
    /// </summary>
    public void OnStartClientClicked()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[MainMenu] Ya te estás conectando. Ignorando clic adicional.");
            return;
        }

        if (GameManager.Instance?.SelectedMapConfig == null)
        {
            Debug.LogWarning("[MainMenu] Cliente: Elige un mapa para intentar unirte.");
            return;
        }

        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;

        string clientMap = GameManager.Instance.SelectedMapConfig.mapName;
        byte[] payload = System.Text.Encoding.UTF8.GetBytes(clientMap);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientRejected;

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Fallo al conectar como Cliente.");
        }
    }

    /// <summary>
    /// El Host ejecuta esto cada vez que alguien intenta entrar a la partida.
    /// </summary>
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;

        if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId) return;

        if (request.Payload == null || request.Payload.Length == 0)
        {
            response.Approved = false;
            response.Reason = "No se ha enviado información del mapa.";
            Debug.LogWarning("[Host] Se rechazó a un jugador porque su paquete de datos estaba vacío.");
            return;
        }

        string mapaSolicitado = System.Text.Encoding.UTF8.GetString(request.Payload);
        string mapaDelHost = GameManager.Instance.SelectedMapConfig.mapName;

        Debug.Log($"[Validación] Host exige: '{mapaDelHost}' | Cliente trae: '{mapaSolicitado}'");

        if (mapaSolicitado != mapaDelHost)
        {
            response.Approved = false;
            response.Reason = $"El Host está jugando en '{mapaDelHost}'. Cambia tu mapa para unirte.";
            Debug.Log($"[Host] Se rechazó a un jugador porque traía el mapa: {mapaSolicitado}");
        }
    }

    /// <summary>
    /// El Cliente ejecuta esto si el Host le rechaza la conexión.
    /// </summary>
    private void OnClientRejected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            string reason = NetworkManager.Singleton.DisconnectReason;
            Debug.LogWarning($"[Cliente] Conexión rechazada: {reason}");

            if (errorPanel != null && errorText != null)
            {
                if (string.IsNullOrEmpty(reason)) reason = "Conexión rechazada por el servidor.";

                errorText.text = reason;
                errorPanel.SetActive(true);
            }

            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientRejected;
            NetworkManager.Singleton.Shutdown();
        }
    }

    /// <summary>
    /// Registra la acción del botón de opciones del menú principal.
    /// </summary>
    public void OnOptionsButtonClicked()
    {
        Debug.Log("Options button pressed");
    }

    /// <summary>
    /// Cierra la aplicación o detiene la ejecución en el editor.
    /// </summary>
    public void OnExitButtonClicked()
    {
        Debug.Log("Exit button pressed");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Configura las opciones del dropdown y establece el mapa inicial seleccionado.
    /// </summary>
    private void initializeMapDropdown()
    {
        if (mapsDropdown == null || availableMaps == null || availableMaps.Length == 0)
        {
            Debug.LogWarning("[MainMenu] Dropdown de mapas no configurado.");
            return;
        }

        mapsDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (MapConfig map in availableMaps)
        {
            options.Add(new TMP_Dropdown.OptionData(map != null ? map.mapName : "Sin nombre"));
        }

        mapsDropdown.AddOptions(options);
        mapsDropdown.value = 0;
        mapsDropdown.RefreshShownValue();
        mapsDropdown.onValueChanged.AddListener(onMapDropdownChanged);

        applySelectedMap(0);
    }

    /// <summary>
    /// Aplica el mapa seleccionado cuando cambia el valor del dropdown.
    /// </summary>
    private void onMapDropdownChanged(int index)
    {
        applySelectedMap(index);
    }

    /// <summary>
    /// Guarda en GameManager el mapa correspondiente al índice indicado.
    /// </summary>
    private void applySelectedMap(int index)
    {
        if (availableMaps == null || index < 0 || index >= availableMaps.Length) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.SelectedMapConfig = availableMaps[index];
        Debug.Log($"[MainMenu] Mapa seleccionado: {availableMaps[index].mapName}");
    }

    /// <summary>
    /// Cierra el panel emergente de error de conexión.
    /// </summary>
    public void OnCloseErrorPanelClicked()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }
    }
}
