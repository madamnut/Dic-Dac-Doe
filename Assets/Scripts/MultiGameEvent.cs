using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class MultiGameEvent : NetworkBehaviour
{
    // Inspector에 할당할 O와 X 마크 프리팹
    public GameObject OMarkPrefab;
    public GameObject XMarkPrefab;
    
    // 3x3 보드 상태 (빈 칸은 ' ')
    private char[,] board = new char[3, 3];
    
    // 턴 관리: true = O(호스트)의 턴, false = X(클라이언트)의 턴
    // 서버가 권한을 가지고 관리하며 클라이언트에 동기화됨
    private NetworkVariable<bool> isOTurn = new NetworkVariable<bool>(true);
    
    // 각 플레이어가 배치한 마크의 좌표(순서대로 기록)
    private List<Vector2Int> oPositions = new List<Vector2Int>();
    private List<Vector2Int> xPositions = new List<Vector2Int>();

    // 각 좌표에 배치된 마크의 GameObject (제거할 때 사용)
    private Dictionary<Vector2Int, GameObject> oMarkObjects = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> xMarkObjects = new Dictionary<Vector2Int, GameObject>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // 보드 초기화 (빈 칸은 공백 문자)
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    board[r, c] = ' ';
                }
            }
        }
    }

    // 플레이어 입력: 로컬 클라이언트는 이 함수를 호출해 서버에 마크 배치를 요청함
    // (예를 들어, 클릭한 정사각형의 이름("Square_r_c")에서 r, c를 파싱해 호출)
    [ServerRpc(RequireOwnership = false)]
    public void PlaceMarkServerRpc(int row, int col, ServerRpcParams rpcParams = default)
    {
        // 보드의 해당 칸이 비어있지 않으면 무시
        if (board[row, col] != ' ')
        {
            Debug.Log($"Square ({row},{col}) already marked.");
            return;
        }

        // 플레이어 구분: 호스트(플레이어 O)는 서버의 로컬 클라이언트 ID와 같고, 나머지는 플레이어 X
        ulong senderId = rpcParams.Receive.SenderClientId;
        ulong hostId = NetworkManager.Singleton.LocalClientId; // 서버의 클라이언트 ID = 호스트 ID

        bool isSenderO = (senderId == hostId);
        bool isSenderX = !isSenderO;

        // 현재 턴에 따라 올바른 플레이어만 진행할 수 있음
        if (isOTurn.Value && !isSenderO)
        {
            Debug.Log("Not O's turn. Reject move from client " + senderId);
            return;
        }
        if (!isOTurn.Value && !isSenderX)
        {
            Debug.Log("Not X's turn. Reject move from client " + senderId);
            return;
        }

        // 현재 턴에 따른 마크
        char mark = isOTurn.Value ? 'O' : 'X';
        board[row, col] = mark;
        Vector2Int coord = new Vector2Int(row, col);

        // 만약 해당 플레이어가 이미 3개의 마크를 보유 중이면, 가장 오래된 마크 제거
        if (mark == 'O')
        {
            if (oPositions.Count >= 3)
            {
                Vector2Int oldest = oPositions[0];
                oPositions.RemoveAt(0);
                if (oMarkObjects.ContainsKey(oldest))
                {
                    RemoveMarkClientRpc(oldest.x, oldest.y, 'O');
                    oMarkObjects.Remove(oldest);
                    Debug.Log($"Removed oldest O mark at ({oldest.x},{oldest.y}).");
                }
                board[oldest.x, oldest.y] = ' ';
            }
            oPositions.Add(coord);
        }
        else // mark == 'X'
        {
            if (xPositions.Count >= 3)
            {
                Vector2Int oldest = xPositions[0];
                xPositions.RemoveAt(0);
                if (xMarkObjects.ContainsKey(oldest))
                {
                    RemoveMarkClientRpc(oldest.x, oldest.y, 'X');
                    xMarkObjects.Remove(oldest);
                    Debug.Log($"Removed oldest X mark at ({oldest.x},{oldest.y}).");
                }
                board[oldest.x, oldest.y] = ' ';
            }
            xPositions.Add(coord);
        }

        // 모든 클라이언트에 마크 배치 업데이트 전파
        UpdateMarkClientRpc(row, col, mark);

        // 턴 전환 (O ↔ X)
        isOTurn.Value = !isOTurn.Value;

        // 승리 조건 체크
        CheckWin();
    }

    // 클라이언트에서 해당 정사각형에 마크 프리팹을 생성하도록 업데이트 전파
    [ClientRpc]
    private void UpdateMarkClientRpc(int row, int col, char mark)
    {
        // 정사각형 이름은 "Square_row_col"이어야 함 (예: "Square_0_0")
        string squareName = $"Square_{row}_{col}";
        GameObject square = GameObject.Find(squareName);
        if (square != null)
        {
            GameObject prefab = (mark == 'O') ? OMarkPrefab : XMarkPrefab;
            GameObject markObj = Instantiate(prefab, square.transform.position, Quaternion.identity, square.transform);
            markObj.name = mark.ToString();

            // 서버에서 저장하는 딕셔너리 업데이트
            if (mark == 'O')
            {
                oMarkObjects[new Vector2Int(row, col)] = markObj;
            }
            else
            {
                xMarkObjects[new Vector2Int(row, col)] = markObj;
            }
        }
        else
        {
            Debug.Log("Square not found: " + squareName);
        }
    }

    // 클라이언트에서 해당 좌표의 마크를 제거하도록 전파
    [ClientRpc]
    private void RemoveMarkClientRpc(int row, int col, char mark)
    {
        string squareName = $"Square_{row}_{col}";
        GameObject square = GameObject.Find(squareName);
        if (square != null)
        {
            Transform child = square.transform.Find(mark.ToString());
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
        else
        {
            Debug.Log("Square not found for removal: " + squareName);
        }
    }

    // 승리 조건 체크: 행, 열, 대각선 검사
    private void CheckWin()
    {
        // 행 검사
        for (int r = 0; r < 3; r++)
        {
            if (board[r, 0] != ' ' && board[r, 0] == board[r, 1] && board[r, 1] == board[r, 2])
            {
                Debug.Log("Winner: " + board[r, 0]);
                return;
            }
        }
        // 열 검사
        for (int c = 0; c < 3; c++)
        {
            if (board[0, c] != ' ' && board[0, c] == board[1, c] && board[1, c] == board[2, c])
            {
                Debug.Log("Winner: " + board[0, c]);
                return;
            }
        }
        // 대각선 검사 (좌상 → 우하)
        if (board[0, 0] != ' ' && board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
        {
            Debug.Log("Winner: " + board[0, 0]);
            return;
        }
        // 대각선 검사 (우상 → 좌하)
        if (board[0, 2] != ' ' && board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
        {
            Debug.Log("Winner: " + board[0, 2]);
            return;
        }
    }
}
