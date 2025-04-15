using UnityEngine;

public class ResolutionManager : MonoBehaviour
{
    [Header("창 크기 설정")]
    public int width = 720;
    public int height = 720;
    public bool fullScreen = false;

    void Start()
    {
        Screen.SetResolution(width, height, fullScreen);
        Debug.Log($"해상도 설정됨: {width}x{height}, 전체화면: {fullScreen}");
    }
}
