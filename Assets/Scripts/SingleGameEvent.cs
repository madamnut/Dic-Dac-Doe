using System.Collections.Generic;
using UnityEngine;

public class SingleGameEvent : MonoBehaviour
{
    // Inspector에서 할당할 O와 X 마크 프리팹
    public GameObject OMarkPrefab;
    public GameObject XMarkPrefab;
    
    // 정사각형 오브젝트의 태그 (예: "Square")
    public string squareTag = "Square";

    // 턴 관리: true면 O의 턴, false면 X의 턴 (초기값: O부터 시작)
    private bool isOTurn = true;

    // 3x3 보드 상태 (빈 칸은 ' ')
    private char[,] board = new char[3, 3];

    // 각 플레이어가 배치한 마크의 좌표를 기록 (순서대로)
    private List<Vector2Int> oMarkPositions = new List<Vector2Int>();
    private List<Vector2Int> xMarkPositions = new List<Vector2Int>();

    // 각 좌표에 배치된 마크의 GameObject를 저장 (제거할 때 사용)
    private Dictionary<Vector2Int, GameObject> oMarkObjects = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> xMarkObjects = new Dictionary<Vector2Int, GameObject>();

    private void Start()
    {
        // 보드 배열을 빈 칸(' ')으로 초기화
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                board[i, j] = ' ';
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Mouse button pressed. Processing input...");

            // 1) 마우스 스크린 좌표를 월드 좌표로 변환 (2D)
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log("Mouse world position: " + mousePos);

            // 2) OverlapPoint2D를 사용해 해당 좌표의 Collider2D 검색
            Collider2D hitCollider = Physics2D.OverlapPoint(mousePos);
            if (hitCollider != null)
            {
                Debug.Log("OverlapPoint2D hit: " + hitCollider.gameObject.name);
                if (hitCollider.CompareTag(squareTag))
                {
                    // 3) 정사각형 오브젝트의 이름에서 좌표 추출
                    // 이름은 "Square_row_col" 형식이어야 함 (예: "Square_0_0")
                    string[] parts = hitCollider.gameObject.name.Split('_');
                    if (parts.Length >= 3)
                    {
                        int r, c;
                        if (int.TryParse(parts[1], out r) && int.TryParse(parts[2], out c))
                        {
                            Vector2Int coord = new Vector2Int(r, c);
                            // 4) 해당 보드 좌표가 이미 마크되어 있으면 무시
                            if (board[r, c] != ' ')
                            {
                                Debug.Log($"Square ({r},{c}) already marked.");
                                return;
                            }

                            // 5) 현재 턴에 따라 마크 배치 전, 만약 이미 3개의 마크가 있다면 가장 오래된 마크 제거
                            if (isOTurn)
                            {
                                Debug.Log($"O's turn. Placing mark at ({r},{c}).");
                                if (oMarkPositions.Count >= 3)
                                {
                                    Vector2Int oldest = oMarkPositions[0];
                                    oMarkPositions.RemoveAt(0);
                                    if (oMarkObjects.ContainsKey(oldest))
                                    {
                                        Destroy(oMarkObjects[oldest]);
                                        oMarkObjects.Remove(oldest);
                                        Debug.Log($"Removed oldest O mark at ({oldest.x},{oldest.y}).");
                                    }
                                    board[oldest.x, oldest.y] = ' ';
                                }
                                board[r, c] = 'O';
                                GameObject newOMark = Instantiate(OMarkPrefab, hitCollider.transform.position, Quaternion.identity, hitCollider.transform);
                                newOMark.name = "O";
                                oMarkPositions.Add(coord);
                                oMarkObjects[coord] = newOMark;
                            }
                            else
                            {
                                Debug.Log($"X's turn. Placing mark at ({r},{c}).");
                                if (xMarkPositions.Count >= 3)
                                {
                                    Vector2Int oldest = xMarkPositions[0];
                                    xMarkPositions.RemoveAt(0);
                                    if (xMarkObjects.ContainsKey(oldest))
                                    {
                                        Destroy(xMarkObjects[oldest]);
                                        xMarkObjects.Remove(oldest);
                                        Debug.Log($"Removed oldest X mark at ({oldest.x},{oldest.y}).");
                                    }
                                    board[oldest.x, oldest.y] = ' ';
                                }
                                board[r, c] = 'X';
                                GameObject newXMark = Instantiate(XMarkPrefab, hitCollider.transform.position, Quaternion.identity, hitCollider.transform);
                                newXMark.name = "X";
                                xMarkPositions.Add(coord);
                                xMarkObjects[coord] = newXMark;
                            }

                            // 6) 턴 전환 (O ↔ X)
                            isOTurn = !isOTurn;

                            // 7) 승리 조건 체크
                            CheckWin();
                        }
                        else
                        {
                            Debug.Log("Failed to parse coordinates from square name: " + hitCollider.gameObject.name);
                        }
                    }
                    else
                    {
                        Debug.Log("Square name format incorrect. Expected format: Square_row_col");
                    }
                }
                else
                {
                    Debug.Log("Hit object does not have the tag '" + squareTag + "'.");
                }
            }
            else
            {
                Debug.Log("OverlapPoint2D did not hit any collider.");
            }
        }
    }

    // 승리 조건: 행, 열, 두 대각선 중 하나가 모두 같은 마크로 채워졌는지 체크
    private void CheckWin()
    {
        // 행 체크
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] != ' ' &&
                board[i, 0] == board[i, 1] &&
                board[i, 1] == board[i, 2])
            {
                Debug.Log("Winner: " + board[i, 0]);
                return;
            }
        }

        // 열 체크
        for (int j = 0; j < 3; j++)
        {
            if (board[0, j] != ' ' &&
                board[0, j] == board[1, j] &&
                board[1, j] == board[2, j])
            {
                Debug.Log("Winner: " + board[0, j]);
                return;
            }
        }

        // 대각선 체크 (좌상 → 우하)
        if (board[0, 0] != ' ' &&
            board[0, 0] == board[1, 1] &&
            board[1, 1] == board[2, 2])
        {
            Debug.Log("Winner: " + board[0, 0]);
            return;
        }

        // 대각선 체크 (우상 → 좌하)
        if (board[0, 2] != ' ' &&
            board[0, 2] == board[1, 1] &&
            board[1, 1] == board[2, 0])
        {
            Debug.Log("Winner: " + board[0, 2]);
            return;
        }
    }
}
