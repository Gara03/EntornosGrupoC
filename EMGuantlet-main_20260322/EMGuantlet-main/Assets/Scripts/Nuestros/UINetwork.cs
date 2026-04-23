using Unity.Netcode;
using UnityEngine;

public class UINetwork : MonoBehaviour
{
    private NetworkManager m_NetworkManager;

    private void Awake()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.gameObject != this.gameObject)
        {
            Debug.Log("[Red] Clon del NetworkManager detectado al volver al menú. Destruyendo...");
            Destroy(this.gameObject);
            return;
        }

        m_NetworkManager = GetComponent<NetworkManager>();
    }

    private void OnGUI()
    {
        if (m_NetworkManager == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        if (!m_NetworkManager.IsClient && !m_NetworkManager.IsServer)
        {
            //StartButtons();
        }
        else
        {
            StatusLabels();

        }

        GUILayout.EndArea();
    }

    /*private void StartButtons()
    {
        if (GUILayout.Button("Host")) m_NetworkManager.StartHost();
        if (GUILayout.Button("Client")) m_NetworkManager.StartClient();
        if (GUILayout.Button("Server")) m_NetworkManager.StartServer();
    }*/

    private void StatusLabels()
    {
        var mode = m_NetworkManager.IsHost ?
            "Host" : m_NetworkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
            m_NetworkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }
}
