using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] GameObject startGameButton;

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
        else
        {
            Debug.Log("El host es el unico que puede empezar la partida.");
        }
    }

    /// <summary>
    /// Valida la selección del personaje y delega el inicio de partida en GameManager.
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

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += DisconnectedFromServer;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= DisconnectedFromServer;
        }
    }

    private void DisconnectedFromServer(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log("El Host ha cerrado la sesión o se ha perdido la conexión.");

            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
