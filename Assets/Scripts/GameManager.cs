using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
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

    private HashSet<ulong> rematchRequests = new HashSet<ulong>();
    private HashSet<ulong> quitRequests = new HashSet<ulong>();

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
                UpdateTurnIndicatorClientRpc(currentTurn, xPlayerId, oPlayerId);
            }
        }
    }

    public void HandleDraw(int x, int y, ulong clientId)
    {
        if (!IsPlayerTurn(clientId)) return;
        if (!string.IsNullOrEmpty(board[x, y])) return;

        string mark = currentTurn;
        board[x, y] = mark;

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
            FinishGame(mark);
            return;
        }

        if (CheckDraw())
        {
            FinishGame("Draw");
            return;
        }

        currentTurn = (currentTurn == "X") ? "O" : "X";
        UpdateTurnIndicatorClientRpc(currentTurn, xPlayerId, oPlayerId);
    }

    private void FinishGame(string winnerMark)
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            bool isDraw = winnerMark == "Draw";
            bool isWin = !isDraw && (
                (winnerMark == "X" && client.ClientId == xPlayerId) ||
                (winnerMark == "O" && client.ClientId == oPlayerId)
            );
            ShowResultClientRpc(client.ClientId, isWin, isDraw);
        }
    }

    private bool IsPlayerTurn(ulong clientId)
    {
        return (currentTurn == "X" && clientId == xPlayerId) ||
               (currentTurn == "O" && clientId == oPlayerId);
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
        if (text != null) text.text = "";
        gameUI.gridButtons[index].interactable = true;
    }

    [ClientRpc]
    private void UpdateTurnIndicatorClientRpc(string nextTurn, ulong xId, ulong oId)
    {
        bool isMyTurn = (nextTurn == "X" && NetworkManager.Singleton.LocalClientId == xId) ||
                        (nextTurn == "O" && NetworkManager.Singleton.LocalClientId == oId);
        gameUI.SetTurnText(isMyTurn);
    }

    [ClientRpc]
    private void ShowResultClientRpc(ulong targetClientId, bool isWin, bool isDraw)
    {
        if (NetworkManager.Singleton.LocalClientId == targetClientId)
        {
            gameUI.ShowResult(isWin, isDraw);
        }
    }

    [ClientRpc]
    private void HideResultPanelClientRpc()
    {
        gameUI.HideResultPanel();
    }

    public void ReceiveRematchRequest(ulong clientId)
    {
        if (quitRequests.Count > 0)
        {
            Debug.Log("리매치 불가: 한 명 이상이 퀴트함");
            return;
        }

        rematchRequests.Add(clientId);

        if (rematchRequests.Contains(xPlayerId) && rematchRequests.Contains(oPlayerId))
        {
            ResetGame();
        }
    }

    public void HandlePlayerQuit(ulong clientId)
    {
        quitRequests.Add(clientId);
        gameUI.MarkOpponentQuit();

        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            // 자기 자신이 퀴트한 경우
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Connect");
        }
        else if (NetworkManager.Singleton.IsHost)
        {
            // 상대방 클라이언트를 서버가 끊어줌
            NetworkManager.Singleton.DisconnectClient(clientId);
        }
    }

    private void ResetGame()
    {
        board = new string[3, 3];
        xMarks.Clear();
        oMarks.Clear();
        currentTurn = "X";
        rematchRequests.Clear();
        quitRequests.Clear();

        HideResultPanelClientRpc(); // ⭐ 모든 클라이언트에서 결과창 끄기

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                ClearCellClientRpc(i, j);

        UpdateTurnIndicatorClientRpc(currentTurn, xPlayerId, oPlayerId);
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
