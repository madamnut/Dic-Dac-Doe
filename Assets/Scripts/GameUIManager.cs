using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameUIManager : MonoBehaviour
{
    [Header("틱택토 버튼 (키패드 순서대로 1~9, 인덱스 0~8)")]
    public Button[] gridButtons;

    [Header("턴 표시 텍스트")]
    public Text turnIndicatorText;

    [Header("게임 결과 UI")]
    public GameObject resultPanel;
    public Text resultText;

    [Header("Rematch & Quit")]
    public Button rematchButton;
    public Button quitButton;
    public Image hostFlag;
    public Image clientFlag;

    private bool localRequestedRematch = false;
    private bool opponentQuit = false;

    void Start()
    {
        for (int i = 0; i < gridButtons.Length; i++)
        {
            int index = i;
            gridButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (rematchButton != null)
            rematchButton.onClick.AddListener(OnRematchClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    void OnButtonClicked(int index)
    {
        int x = index % 3;
        int y = index / 3;

        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient)
        {
            if (PlayerNetwork.Instance != null)
            {
                PlayerNetwork.Instance.Draw(x, y);
            }
        }
    }

    public void SetCellText(int index, string mark)
    {
        var text = gridButtons[index].GetComponentInChildren<Text>();
        if (text != null)
            text.text = mark;

        gridButtons[index].interactable = false;
    }

    public void SetTurnText(bool isMyTurn)
    {
        if (turnIndicatorText != null)
            turnIndicatorText.text = isMyTurn ? "Your Turn" : "Opponent's Turn";
    }

    public void ShowResult(string result, bool isWin)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = result;
            resultText.color = isWin ? new Color(0.1f, 0.8f, 0.3f) : new Color(0.8f, 0.2f, 0.2f); // 초록 / 빨강
        }

        ResetFlags();
    }

    private void ResetFlags()
    {
        SetImageAlpha(hostFlag, 0.3f);
        SetImageAlpha(clientFlag, 0.3f);
        hostFlag.color = Color.white;
        clientFlag.color = Color.white;
        localRequestedRematch = false;
        opponentQuit = false;
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    public void MarkRematch()
    {
        localRequestedRematch = true;
        bool isHost = NetworkManager.Singleton.IsHost;
        Image target = isHost ? hostFlag : clientFlag;
        SetImageAlpha(target, 1f);
    }

    public void MarkOpponentQuit()
    {
        opponentQuit = true;
        bool isHost = NetworkManager.Singleton.IsHost;
        Image target = isHost ? clientFlag : hostFlag;
        target.color = Color.red;
    }

    private void OnRematchClicked()
    {
        if (opponentQuit)
        {
            Debug.Log("리매치 불가: 상대가 종료함");
            return;
        }

        PlayerNetwork.Instance.RequestRematch();
        MarkRematch();
    }

    private void OnQuitClicked()
    {
        PlayerNetwork.Instance.RequestQuit();
    }
}
