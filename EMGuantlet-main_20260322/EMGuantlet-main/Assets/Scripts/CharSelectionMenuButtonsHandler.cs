using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharSelectionMenuButtonsHandler : MonoBehaviour
{
    [Header("Character Stats Assets")]
    [SerializeField] private PlayerStats greenCharacterStats;
    [SerializeField] private PlayerStats purpleCharacterStats;
    [SerializeField] private PlayerStats redCharacterStats;
    [SerializeField] private PlayerStats yellowCharacterStats;

    [Header("Personajes en el Escenario Lobby")]
    public GameObject lobbyGreenCharacter;
    public GameObject lobbyPurpleCharacter;
    public GameObject lobbyRedCharacter;
    public GameObject lobbyYellowCharacter;

    [Header("UI Buttons")]
    public Button greenButton;
    public Button purpleButton;
    public Button redButton;
    public Button yellowButton;

    [Header("UI Panels")]
    public GameObject selectionPanel;
    public GameObject lobbyPanel;

    [Header("Scenes")]
    public GameObject selectionScene;
    public GameObject lobbyScene;

    [Header("Network Reference")]
    public CharSelectionNetworkManager charSelNetworkManager;

    /// <summary>
    /// Vuelve al menú principal desde la pantalla de selección de personaje.
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>
    /// Selecciona el personaje verde e inicia la partida.
    /// </summary>
    public void OnGreenButtonClicked() => charSelNetworkManager.RequestCharacter(CharacterColor.Green);

    /// <summary>
    /// Selecciona el personaje morado e inicia la partida.
    /// </summary>
    public void OnPurpleButtonClicked() => charSelNetworkManager.RequestCharacter(CharacterColor.Purple);

    /// <summary>
    /// Selecciona el personaje rojo e inicia la partida.
    /// </summary>
    public void OnRedButtonClicked() => charSelNetworkManager.RequestCharacter(CharacterColor.Red);

    /// <summary>
    /// Selecciona el personaje amarillo e inicia la partida.
    /// </summary>
    public void OnYellowButtonClicked() => charSelNetworkManager.RequestCharacter(CharacterColor.Yellow);

    public void UpdateButtonState(CharacterColor color, bool isTaken)
    {
        switch (color)
        {
            case CharacterColor.Green:
                if (greenButton != null) greenButton.interactable = !isTaken;
                if (lobbyGreenCharacter != null) lobbyGreenCharacter.SetActive(isTaken); // ¡Nueva línea!
                break;

            case CharacterColor.Purple:
                if (purpleButton != null) purpleButton.interactable = !isTaken;
                if (lobbyPurpleCharacter != null) lobbyPurpleCharacter.SetActive(isTaken); // ¡Nueva línea!
                break;

            case CharacterColor.Red:
                if (redButton != null) redButton.interactable = !isTaken;
                if (lobbyRedCharacter != null) lobbyRedCharacter.SetActive(isTaken); // ¡Nueva línea!
                break;

            case CharacterColor.Yellow:
                if (yellowButton != null) yellowButton.interactable = !isTaken;
                if (lobbyYellowCharacter != null) lobbyYellowCharacter.SetActive(isTaken); // ¡Nueva línea!
                break;
        }
    }

    public void ConfirmLocalCharacter(CharacterColor color)
    {
        PlayerStats selectedStats = null;
        switch (color)
        {
            case CharacterColor.Green: selectedStats = greenCharacterStats; break;
            case CharacterColor.Purple: selectedStats = purpleCharacterStats; break;
            case CharacterColor.Red: selectedStats = redCharacterStats; break;
            case CharacterColor.Yellow: selectedStats = yellowCharacterStats; break;
        }

        if (GameManager.Instance != null && selectedStats != null)
        {
            GameManager.Instance.SelectedCharacterStats = selectedStats;
            Debug.Log($"[UI] Personaje guardado localmente: {selectedStats.characterName}");

            if (selectionPanel != null) selectionPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);

            if (selectionScene != null) selectionScene.SetActive(false);
            if (lobbyScene != null) lobbyScene.SetActive(true);
        }
    }


    /*// <summary>
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

    private void selectCharacterAndGoLobby(PlayerStats characterStats)
    {
        if (characterStats == null)
        {
            Debug.LogError("[CharSelection] No se ha asignado PlayerStats para este personaje");
            return;
        }

        GameManager.Instance.SelectedCharacterStats = characterStats;

        SceneManager.LoadScene(SceneNames.Lobby);
    }*/
}
