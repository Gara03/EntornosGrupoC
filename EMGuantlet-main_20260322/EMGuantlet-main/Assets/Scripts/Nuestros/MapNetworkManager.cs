using Unity.Netcode;
using UnityEngine;

public class MapNetworkManager : NetworkBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí tu objeto que tiene el script LevelGenerator")]
    public LevelGenerator levelGenerator;

    private NetworkVariable<int> mapSeed = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            int randomSeed = Random.Range(1, 9999999);
            mapSeed.Value = randomSeed;

            Debug.Log($"[Red] Semilla generada en el Host: {randomSeed}. Sincronizando...");

            levelGenerator.StartGenerationWithSeed(randomSeed);
        }
        else
        {
            if (mapSeed.Value != 0)
            {
                levelGenerator.StartGenerationWithSeed(mapSeed.Value);
            }

            mapSeed.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal != 0)
                {
                    levelGenerator.StartGenerationWithSeed(newVal);
                }
            };
        }
    }
}

