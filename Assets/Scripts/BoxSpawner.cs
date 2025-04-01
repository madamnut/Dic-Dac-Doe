using Unity.Netcode;
using UnityEngine;

public class BoxSpawner : NetworkBehaviour
{
    public GameObject boxPrefab;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[BoxSpawner] OnNetworkSpawn | IsServer={IsServer}");
        if (!IsServer) return;

        SpawnBox("HostBox", new Vector3(-3f, 0f, 0f), Color.blue);
        SpawnBox("ClientBox", new Vector3(3f, 0f, 0f), Color.red);
    }

    void SpawnBox(string name, Vector3 position, Color color)
    {
        Debug.Log($"[Server] Spawning {name} at {position}");

        GameObject box = Instantiate(boxPrefab, position, Quaternion.identity);
        box.name = name;

        var sr = box.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
            Debug.Log($"[Server] Set color of {name} to {color}");
        }

        var netObj = box.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[Server] NetworkObject missing on prefab for {name}");
            return;
        }

        netObj.Spawn();
        Debug.Log($"[Server] Spawned {name} successfully");
    }
}
