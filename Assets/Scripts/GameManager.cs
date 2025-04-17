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
                Debug.Log($"턴 순서 설정 완료: X={xPlayerId}, O={oPlayerId}");

                UpdateTurnIndicatorClientRpc(currentTurn);
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
            ShowResultClientRpc($"{mark} 승리!", mark);
            return;
        }

        if (CheckDraw())
        {
            ShowResultClientRpc("무승부!", "Draw");
            return;
        }

        currentTurn = (currentTurn == "X") ? "O" : "X";
        UpdateTurnIndicatorClientRpc(currentTurn);
    }

    private bool IsPlayerTurn(ulong clientId)
    {
        return (currentTurn == "X" && clientId == xPlayerId) || (currentTurn == "O" && clientId == oPlayerId);
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
    private void ShowResultClientRpc(string result, string winnerMark)
    {
        if (PlayerNetwork.Instance == null || gameUI == null) return;

        ulong myId = PlayerNetwork.Instance.OwnerClientId;
        string myMark = (myId == xPlayerId) ? "X" : (myId == oPlayerId) ? "O" : "";

        if (winnerMark == "Draw")
            gameUI.ShowResult("Draw", false);
        else if (myMark == winnerMark)
            gameUI.ShowResult("You Win!!", true);
        else
            gameUI.ShowResult("You Lose..", false);
    }

    [ClientRpc]
    private void UpdateTurnIndicatorClientRpc(string nextTurn)
    {
        if (PlayerNetwork.Instance != null)
        {
            bool isMyTurn = (nextTurn == "X" && PlayerNetwork.Instance.OwnerClientId == xPlayerId) ||
                            (nextTurn == "O" && PlayerNetwork.Instance.OwnerClientId == oPlayerId);
            gameUI.SetTurnText(isMyTurn);
        }
    }

    public void ReceiveRematchRequest(ulong clientId)
    {
        if (quitRequests.Count > 0)
        {
            Debug.Log("리매치 불가: 한 명 이상이 퀴트함");
            return;
        }

        rematchRequests.Add(clientId);
        Debug.Log($"Rematch 요청 수신: {clientId}");

        if (rematchRequests.Contains(xPlayerId) && rematchRequests.Contains(oPlayerId))
        {
            Debug.Log("양쪽 모두 리매치 요청 → 게임 리셋");
            ResetGame();
        }
    }

    public void HandlePlayerQuit(ulong clientId)
    {
        quitRequests.Add(clientId);
        Debug.Log($"플레이어 퀴트: {clientId}");

        ShowOpponentQuitClientRpc();

        if (NetworkManager.Singleton.IsHost)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
            }
            else
            {
                NetworkManager.Singleton.Shutdown();
                SceneManager.LoadScene("Connect");
            }
        }
        else
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Connect");
        }
    }

    [ClientRpc]
    private void ShowOpponentQuitClientRpc()
    {
        if (gameUI != null)
            gameUI.MarkOpponentQuit();
    }

    private void ResetGame()
    {
        board = new string[3, 3];
        xMarks.Clear();
        oMarks.Clear();
        currentTurn = "X";
        rematchRequests.Clear();
        quitRequests.Clear();

        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                ClearCellClientRpc(i, j);

        UpdateTurnIndicatorClientRpc(currentTurn);
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
