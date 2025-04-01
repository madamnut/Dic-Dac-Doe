using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public InputField codeInput;
    public Text codeDisplay;

    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
    }

    async void OnHostClicked()
    {
        string code = await RelayManager.Instance.CreateRelay();
        if (!string.IsNullOrEmpty(code))
        {
            codeDisplay.text = "Join Code: " + code;
        }
    }

    async void OnJoinClicked()
    {
        string inputCode = codeInput.text.Trim();
        if (!string.IsNullOrEmpty(inputCode))
        {
            bool success = await RelayManager.Instance.JoinRelay(inputCode);
            if (!success)
                codeDisplay.text = "접속 실패";
        }
    }
}
