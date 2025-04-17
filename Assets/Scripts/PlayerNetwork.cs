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
        Debug.Log($"클라이언트 요청: Draw({x}, {y})");
        DrawRequestServerRpc(x, y);
    }

    [ServerRpc]
    private void DrawRequestServerRpc(int x, int y, ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[서버] 좌표 수신: ({x}, {y}) from Client {OwnerClientId}");

        if (GameManager.Instance != null)
            GameManager.Instance.HandleDraw(x, y, OwnerClientId);
        else
            Debug.LogError("GameManager.Instance 가 null입니다. 서버에서 처리 실패");
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
