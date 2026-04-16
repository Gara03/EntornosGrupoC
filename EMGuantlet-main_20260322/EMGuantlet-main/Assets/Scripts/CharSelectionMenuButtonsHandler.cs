using UnityEngine;
using UnityEngine.SceneManagement;

public class CharSelectionMenuButtonsHandler : MonoBehaviour
{
    [Header("Character Stats Assets")]
    [SerializeField] private PlayerStats greenCharacterStats;
    [SerializeField] private PlayerStats purpleCharacterStats;
    [SerializeField] private PlayerStats redCharacterStats;
    [SerializeField] private PlayerStats yellowCharacterStats;

    /// <summary>
    /// Vuelve al menú principal desde la pantalla de selección de personaje.
    /// </summary>
    public void OnBackButtonClicked()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Selecciona el personaje verde e inicia la partida.
    /// </summary>
    public void OnGreenButtonClicked()
    {
        selectCharacterAndStartGame(greenCharacterStats);
    }

    /// <summary>
    /// Selecciona el personaje morado e inicia la partida.
    /// </summary>
    public void OnPurpleButtonClicked()
    {
        selectCharacterAndStartGame(purpleCharacterStats);
    }

    /// <summary>
    /// Selecciona el personaje rojo e inicia la partida.
    /// </summary>
    public void OnRedButtonClicked()
    {
        selectCharacterAndStartGame(redCharacterStats);
    }

    /// <summary>
    /// Selecciona el personaje amarillo e inicia la partida.
    /// </summary>
    public void OnYellowButtonClicked()
    {
        selectCharacterAndStartGame(yellowCharacterStats);
    }

    /// <summary>
    /// Valida la selección del personaje y delega el inicio de partida en GameManager.
    /// </summary>
    private void selectCharacterAndStartGame(PlayerStats characterStats)
    {
        if (characterStats == null)
        {
            Debug.LogError("[CharSelection] No se ha asignado PlayerStats para este personaje");
            return;
        }

        GameManager.Instance?.StartGame(characterStats);
    }
}
