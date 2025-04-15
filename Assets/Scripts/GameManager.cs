using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("게임 보드")]
    public GameUIManager gameUI;

    private string[,] board = new string[3, 3];
    private string currentTurn = "X";

    private ulong xPlayerId;
    private ulong oPlayerId;

    private Queue<Vector2Int> xMarks = new Queue<Vector2Int>();
    private Queue<Vector2Int> oMarks = new Queue<Vector2Int>();

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            var clients = NetworkManager.Singleton.ConnectedClientsList;
            if (clients.Count >= 2)
            {
                xPlayerId = clients[0].ClientId;
                oPlayerId = clients[1].ClientId;
                Debug.Log($"턴 순서 설정 완료: X={xPlayerId}, O={oPlayerId}");
            }
        }
    }

    public void HandleDraw(int x, int y, ulong clientId)
    {
        if (!IsPlayerTurn(clientId))
        {
            Debug.Log($"클릭 무시됨: Client {clientId}의 턴이 아님");
            return;
        }

        if (!string.IsNullOrEmpty(board[x, y]))
        {
            Debug.Log($"위치 ({x}, {y}) 는 이미 사용됨");
            return;
        }

        string mark = currentTurn;
        board[x, y] = mark;

        Debug.Log($"Client {clientId} → {mark} 가 ({x}, {y}) 선택");

        Queue<Vector2Int> markQueue = (mark == "X") ? xMarks : oMarks;
        markQueue.Enqueue(new Vector2Int(x, y));

        if (markQueue.Count > 3)
        {
            Vector2Int oldest = markQueue.Dequeue();
            board[oldest.x, oldest.y] = null;
            ClearCellClientRpc(oldest.x, oldest.y);
        }

        UpdateCellClientRpc(x, y, mark);

        if (CheckWin(mark))
        {
            Debug.Log($"{mark} 승리!");
            ShowResultClientRpc($"{mark} 승리!");
            return;
        }

        if (CheckDraw())
        {
            Debug.Log("무승부!");
            ShowResultClientRpc("무승부!");
            return;
        }

        currentTurn = (currentTurn == "X") ? "O" : "X";
        Debug.Log($"턴 변경됨: 현재 턴 → {currentTurn}");
    }

    private bool IsPlayerTurn(ulong clientId)
    {
        if (currentTurn == "X" && clientId == xPlayerId) return true;
        if (currentTurn == "O" && clientId == oPlayerId) return true;
        return false;
    }

    [ClientRpc]
    private void UpdateCellClientRpc(int x, int y, string mark)
    {
        int index = y * 3 + x;
        gameUI.SetCellText(index, mark);
    }

    [ClientRpc]
    private void ClearCellClientRpc(int x, int y)
    {
        int index = y * 3 + x;

        var text = gameUI.gridButtons[index].GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = "";
        }

        gameUI.gridButtons[index].interactable = true;
    }

    [ClientRpc]
    private void ShowResultClientRpc(string result)
    {
        Debug.Log($"게임 결과: {result}");
    }

    private bool CheckWin(string mark)
    {
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] == mark && board[i, 1] == mark && board[i, 2] == mark) return true;
            if (board[0, i] == mark && board[1, i] == mark && board[2, i] == mark) return true;
        }

        if (board[0, 0] == mark && board[1, 1] == mark && board[2, 2] == mark) return true;
        if (board[0, 2] == mark && board[1, 1] == mark && board[2, 0] == mark) return true;

        return false;
    }

    private bool CheckDraw()
    {
        foreach (var cell in board)
        {
            if (string.IsNullOrEmpty(cell)) return false;
        }
        return true;
    }
}
