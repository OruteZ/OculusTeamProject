using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class PokerUIController : MonoBehaviour
{
    [SerializeField]
    private Button button;

    [SerializeField]
    private TextMeshProUGUI buttonText;

    [SerializeField]
    public UnityEvent onButtonPressed;

    void Awake()
    {
        // 컴포넌트 찾기
        if (button == null)
        {
            button = GetComponentInChildren<Button>();
            Debug.Log($"Button found: {button != null}");
        }

        if (buttonText == null)
        {
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
            Debug.Log($"ButtonText found: {buttonText != null}");
        }
    }

    void Start()
    {
        // 버튼 이벤트 설정
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
            Debug.Log("Button listener added in Start");
        }
        else
        {
            Debug.LogError("Button is null in Start!");
        }

        // UnityEvent 초기화
        if (onButtonPressed == null)
        {
            onButtonPressed = new UnityEvent();
            Debug.Log("UnityEvent initialized");
        }
    }

    public void OnButtonClicked()
    {
        Debug.Log("Button clicked - OnButtonClicked called!");

        // 텍스트 변경으로 시각적 피드백
        if (buttonText != null)
        {
            buttonText.text = "Clicked!";
            Debug.Log("Button text changed");
        }

        // UnityEvent 호출
        onButtonPressed?.Invoke();
        Debug.Log("onButtonPressed event invoked");
    }

    // Inspector에서 호출할 수 있는 public 메서드
    public void PrintDebugMessage()
    {
        Debug.Log("PrintDebugMessage called!");

        // 버튼 색상 변경으로 시각적 피드백
        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.green;
            button.colors = colors;
            Debug.Log("Button color changed to green");
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }
}