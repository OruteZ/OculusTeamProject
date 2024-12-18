using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // 필요시 사용, XRInput 처리용
using UnityEngine.UI; // UI 처리용
using Actor;

public class BettingPlayer : BettingActor
{
    [Header("VR UI References")]
    [SerializeField] private GameObject bettingUI; // VR 환경에서 띄울 UI(예: World Space Canvas)
    [SerializeField] private Button checkCallButton;
    [SerializeField] private Button foldButton;
    [SerializeField] private Button raiseButton;
    [SerializeField] private Slider raiseAmountSlider; // Raise 액수를 결정하는 슬라이더
    [SerializeField] private Text moneyText;
    [SerializeField] private Text currentBetText;
    [SerializeField] private Text potText;
    [SerializeField] private Text warningText;

    [SerializeField] private XRSimpleInteractable xrUIInteractable; // XR UI 버튼과 이벤트 처리용

    private bool _actionSelected;
    private enum PlayerAction { NONE, CHECK_CALL, FOLD, RAISE }
    private PlayerAction _selectedAction = PlayerAction.NONE;
    private int _selectedRaiseAmount;

    private void CheckNull()
    {
        if (bettingUI == null)
        {
            Debug.LogError("Betting UI is not set!");
        }
        if (checkCallButton == null)
        {
            Debug.LogError("Check/Call button is not set!");
        }
        if (foldButton == null)
        {
            Debug.LogError("Fold button is not set!");
        }
        if (raiseButton == null)
        {
            Debug.LogError("Raise button is not set!");
        }
        if (raiseAmountSlider == null)
        {
            Debug.LogError("Raise amount slider is not set!");
        }
        if (moneyText == null)
        {
            Debug.LogError("Money text is not set!");
        }
        if (currentBetText == null)
        {
            Debug.LogError("Current bet text is not set!");
        }
        if (potText == null)
        {
            Debug.LogError("Pot text is not set!");
        }
        if (warningText == null)
        {
            Debug.LogError("Warning text is not set!");
        }
    }

    private void Awake()
    {
        CheckNull();
        
        // 버튼 이벤트 등록
        checkCallButton.onClick.AddListener(OnCheckCallPressed);
        foldButton.onClick.AddListener(OnFoldPressed);
        raiseButton.onClick.AddListener(OnRaisePressed);
        
        // 슬라이더 값 변경 이벤트
        raiseAmountSlider.onValueChanged.AddListener(OnRaiseAmountChanged);
        
        // UI는 초기에는 비활성화
        bettingUI.SetActive(false);
    }

    /// <summary>
    /// BettingActor에서 구현해야 하는 핵심 메서드.
    /// 이 코루틴은 플레이어의 선택을 대기하고, 그 후 액션을 수행한 뒤 종료한다.
    /// </summary>
    public override IEnumerator Play()
    {
        // UI 갱신
        UpdateUI();

        // UI 표시
        bettingUI.SetActive(true);

        // 초기 상태 설정
        _actionSelected = false;
        _selectedAction = PlayerAction.NONE;
        warningText.text = "";

        // 플레이어 입력 대기
        while (_actionSelected == false)
        {
            yield return null;
        }

        // 액션 수행
        switch (_selectedAction)
        {
            case PlayerAction.CHECK_CALL:
                HandleCheckOrCall();
                break;
            case PlayerAction.FOLD:
                Fold();
                break;
            case PlayerAction.RAISE:
                HandleRaise();
                break;
            case PlayerAction.NONE:
            default:
                throw new ArgumentOutOfRangeException();
        }

        // UI 숨기기
        bettingUI.SetActive(false);
    }

    private void UpdateUI()
    {
        // 플레이어 정보 및 현재 베팅 상황 UI 반영
        moneyText.text = $"Money: {GetMoney()}";
        int curBet = BettingManager.Instance.GetCurrentBet();
        currentBetText.text = $"Current Bet: {curBet}";
        potText.text = $"Pot: {BettingManager.Instance.GetPot()}";

        // Check/Call 버튼의 텍스트를 상황에 맞게 변경
        if (CanCheck())
        {
            checkCallButton.GetComponentInChildren<Text>().text = "Check";
        }
        else
        {
            checkCallButton.GetComponentInChildren<Text>().text = "Call";
        }

        // Raise 슬라이더 초기값 조정 (예: 최소 베팅 단위 만큼)
        // 필요하다면 최소 베팅액, 혹은 플레이어 소지금에 따른 최대치 설정
        raiseAmountSlider.minValue = 10; 
        raiseAmountSlider.maxValue = GetMoney();
        raiseAmountSlider.value = 10; // 초기값
    }

    private void OnCheckCallPressed()
    {
        // Check/Call 가능한지 검사
        if (CanCheck() || Callable())
        {
            _selectedAction = PlayerAction.CHECK_CALL;
            _actionSelected = true;
        }
        else
        {
            warningText.text = "You cannot check or call now!";
        }
    }

    private void OnFoldPressed()
    {
        // 폴드는 언제나 가능(일반적으로)
        _selectedAction = PlayerAction.FOLD;
        _actionSelected = true;
    }

    private void OnRaisePressed()
    {
        // Raise 가능한지 검사
        int amount = _selectedRaiseAmount;
        if (amount > 0 && amount <= GetMoney())
        {
            _selectedAction = PlayerAction.RAISE;
            _actionSelected = true;
        }
        else
        {
            warningText.text = "Invalid raise amount!";
        }
    }

    private void OnRaiseAmountChanged(float val)
    {
        _selectedRaiseAmount = Mathf.RoundToInt(val);
    }

    private void HandleCheckOrCall()
    {
        // Check 가능한 상태면 Check, 아니면 Call
        if (CanCheck())
        {
            Check();
        }
        else
        {
            // 콜 수행
            bool success = Call();
            if (!success)
            {
                // 콜 실패 시 경고 처리(이 상황은 로직상 잘 안나오겠지만)
                Debug.LogError("Call failed. Something is wrong with the logic.");
            }
        }
    }

    private void HandleRaise()
    {
        // 레이즈 수행
        int amount = _selectedRaiseAmount;
        bool success = Raise(amount);
        if (!success)
        {
            warningText.text = "Cannot raise that amount!";
            // 다시 액션 선택하도록 할 수도 있지만, 
            // 여기서는 단순히 실패하면 턴 종료(또는 다시 액션 유도를 할 수 있음)
        }
    }

    // 필요하다면 XR Input 관리 코드를 추가해서 VR Controller 버튼과 이벤트를 UI 버튼 클릭과 연동할 수 있다.
    // 예: VR Controller의 Trigger가 Check/Call 버튼을 누른 것처럼 작동하도록 하는 XR 이벤트 핸들러.
}
