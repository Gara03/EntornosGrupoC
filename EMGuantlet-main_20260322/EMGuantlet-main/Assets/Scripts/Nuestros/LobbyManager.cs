using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("UI Button")]
    [SerializeField] GameObject startGameButton;

    /// <summary>
    /// El botón de "Jugar" solo se encontrará disponible en el Host.
    /// </summary>
    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            startGameButton.SetActive(false);
        }
    }

    /// <summary>
    /// Valida la selección del personaje y delega el inicio de partida en GameManager.
    /// </summary>
    public void OnStartButtonClicked()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PrepareMultiplayerGame();
            }
            NetworkManager.Singleton.SceneManager.LoadScene(SceneNames.PlaygroundLevel, LoadSceneMode.Single);
        }
        else // Por si acaso
        {
            Debug.Log("El host es el unico que puede empezar la partida.");
        }
    }

    /// <summary>
    /// Vuelve al menú principal y cierra la conexión.
    /// </summary>
    public void OnReturnButtonClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        GameManager.Instance.SelectedCharacterStats = null;

        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Se conecta al jugador.
    /// </summary>
    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += DisconnectedFromServer;
        }
    }

    /// <summary>
    /// Se desconecta al jugador.
    /// </summary>
    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= DisconnectedFromServer;
        }
    }

    /// <summary>
    /// Si el Host se sale, se echa a todos los clientes.
    /// </summary>
    private void DisconnectedFromServer(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("El Host ha cerrado la sesión o se ha perdido la conexión.");

            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
