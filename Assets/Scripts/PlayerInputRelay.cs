using Unity.Netcode;
using UnityEngine;

public class PlayerInputRelay : NetworkBehaviour
{
    public bool isClientControlledBox; // false = HostBox 조작, true = ClientBox 조작

    void Start()
    {
        Debug.Log($"[PlayerInputRelay] Started | IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}");
    }

    void Update()
    {
        if (!IsClient || IsServer) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);

        if (input != Vector2.zero)
        {
            Debug.Log($"[Client {OwnerClientId}] Input detected: {input}");
            SendInputServerRpc(input);
        }
    }

    [ServerRpc]
    void SendInputServerRpc(Vector2 input)
    {
        string boxName = isClientControlledBox ? "ClientBox" : "HostBox";
        GameObject target = GameObject.Find(boxName);

        if (target != null)
        {
            float speed = 5f;
            Vector3 delta = (Vector3)input.normalized * speed * Time.deltaTime;
            target.transform.Translate(delta);

            Debug.Log($"[Server] Moving {boxName} by {delta} (input: {input})");
        }
        else
        {
            Debug.LogWarning($"[Server] Could not find GameObject named {boxName}");
        }
    }
}
