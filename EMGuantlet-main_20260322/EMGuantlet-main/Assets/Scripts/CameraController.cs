using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    private Transform target;

    /// <summary>
    /// Inicializa la referencia al jugador local y suscribe el evento de registro.
    /// </summary>
    private void Start()
    {
        if (GameManager.Instance != null)
        {
            target = GameManager.Instance.LocalPlayerTransform;
            GameEvents.OnLocalPlayerRegistered += handlePlayerRegistered;
        }
    }

    /// <summary>
    /// Libera la suscripción al evento al destruir el objeto.
    /// </summary>
    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameEvents.OnLocalPlayerRegistered -= handlePlayerRegistered;
    }

    /// <summary>
    /// Actualiza la posición de la cámara para seguir al objetivo.
    /// </summary>
    private void LateUpdate()
    {
        if (target != null)
            transform.position = target.position + offset;
    }

    /// <summary>
    /// Actualiza el objetivo de la cámara cuando se registra el jugador local.
    /// </summary>
    private void handlePlayerRegistered(PlayerController player)
    {
        target = player != null ? player.transform : null;
    }
}
