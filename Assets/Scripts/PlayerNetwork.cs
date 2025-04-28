using UnityEngine;
using Unity.Netcode;

public class PlayerNetwork : NetworkBehaviour
{
    public static PlayerNetwork Instance;

    public override void OnNetworkSpawn()
    {
        if (IsLocalPlayer)
        {
            Instance = this;
            Debug.Log("PlayerNetwork 인스턴스 할당 완료 (로컬 플레이어)");
        }
        else
        {
            Debug.Log($"PlayerNetwork 스폰됨 (원격 플레이어 / ClientId: {OwnerClientId})");
        }
    }

    public void Draw(int x, int y)
    {
        DrawRequestServerRpc(x, y);
    }

    [ServerRpc]
    private void DrawRequestServerRpc(int x, int y, ServerRpcParams rpcParams = default)
    {
        GameManager.Instance?.HandleDraw(x, y, OwnerClientId);
    }

    public void RequestRematch()
    {
        RematchRequestServerRpc();
    }

    [ServerRpc]
    private void RematchRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        GameManager.Instance?.ReceiveRematchRequest(OwnerClientId);
    }

    public void RequestQuit()
    {
        QuitRequestServerRpc();
    }

    [ServerRpc]
    private void QuitRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        GameManager.Instance?.HandlePlayerQuit(OwnerClientId);
    }
}
