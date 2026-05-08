using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverCanvasHandler : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI jewelsValueText;
    [SerializeField] private TextMeshProUGUI keysValueText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;

    /// <summary>
    /// Inicializa la pantalla mostrando las estadísticas finales de la partida.
    /// </summary>
    private void Start()
    {
        UnityEngine.EventSystems.EventSystem[] eventSystems = FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
        if (eventSystems.Length > 1)
        {
            Destroy(eventSystems[eventSystems.Length - 1].gameObject);
        }

        displayGameStats();
    }

    /// <summary>
    /// Carga el menú principal al pulsar el botón de volver.
    /// </summary>
    public void OnBackButtonClicked()
    {
        // apaga la red al volver al menu principal
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Actualiza los textos del panel con diamantes, llaves y enemigos eliminados.
    /// </summary>
    private void displayGameStats()
    {
        if (GameManager.Instance == null) return;

        if (jewelsValueText != null)
            jewelsValueText.text = GameManager.Instance.GlobalDiamonds.ToString();

        if (keysValueText != null)
            keysValueText.text = GameManager.Instance.GlobalKeys.ToString();

        if (enemiesKilledText != null)
            enemiesKilledText.text = GameManager.Instance.EnemiesKilled.ToString();
    }
}
