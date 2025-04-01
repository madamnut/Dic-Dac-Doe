using Unity.Netcode;
using UnityEngine;

public class InputHandler : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        Debug.Log($"[InputHandler] OnNetworkSpawn | IsServer={IsServer}, IsClient={IsClient}");

        if (IsClient && !IsServer)
        {
            var relay = gameObject.AddComponent<PlayerInputRelay>();
            relay.isClientControlledBox = true;
            Debug.Log("[Client] Attached PlayerInputRelay for ClientBox");
        }

        if (IsServer && IsClient)
        {
            var relay = gameObject.AddComponent<PlayerInputRelay>();
            relay.isClientControlledBox = false;
            Debug.Log("[Host] Attached PlayerInputRelay for HostBox");
        }
    }
}
