using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameUIManager : MonoBehaviour
{
    [Header("틱택토 버튼 (키패드 순서대로 1~9, 인덱스 0~8)")]
    public Button[] gridButtons;

    void Start()
    {
        for (int i = 0; i < gridButtons.Length; i++)
        {
            int index = i;
            gridButtons[i].onClick.AddListener(() => OnButtonClicked(index));
        }
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
            else
            {
                Debug.LogWarning("PlayerNetwork 인스턴스가 없습니다. 클릭 무시됨");
            }
        }
    }

    // GameManager에서 호출: 버튼 텍스트와 상태 설정
    public void SetCellText(int index, string mark)
    {
        var text = gridButtons[index].GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = mark;
        }
        else
        {
            Debug.LogError($"Button {index} 안에 Text 컴포넌트가 없습니다.");
        }

        gridButtons[index].interactable = false;
    }
}
