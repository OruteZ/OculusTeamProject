using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace Actor
{
    public class CardController : MonoBehaviour
    {

        [SerializeField] private InputActionReference shuffle;
        [SerializeField] private InputActionReference deal;

        // Deck과의 레퍼런스를 저장하기 위한 변수
        private DeckObject _currentDeck;

        // Deck의 XRGrabInteractable을 참조하기 위한 변수
        [SerializeField] private XRGrabInteractable deckGrabInteractable;
        
        [Header("Dealing Info")]
        [SerializeField] private bool dealingFlag;
        [SerializeField] private CardContainer dealingTarget;
        [SerializeField] private int dealingCount;

        private WaitUntil _dealingWaitUntil;

        private void Awake()
        {
            shuffle.action.performed += OnShuffle;
            deal.action.performed += OnDeal;
            _dealingWaitUntil = new WaitUntil(() => dealingFlag == false);

            if (deckGrabInteractable != null)
            {
                // Grab 이벤트에 대한 리스너 등록
                deckGrabInteractable.selectEntered.AddListener(OnDeckGrabbed);
                deckGrabInteractable.selectExited.AddListener(OnDeckReleased);
            }
            else
            {
                Debug.LogWarning("Deck XRGrabInteractable이 할당되지 않았습니다.");
            }
        }

        private void OnDestroy()
        {
            if (deckGrabInteractable != null)
            {
                // 리스너 해제
                deckGrabInteractable.selectEntered.RemoveListener(OnDeckGrabbed);
                deckGrabInteractable.selectExited.RemoveListener(OnDeckReleased);
            }

            shuffle.action.performed -= OnShuffle;
            deal.action.performed -= OnDeal;
        }

        private void OnDeal(InputAction.CallbackContext context)
        {
            if(_currentDeck == null)
            {
                Debug.LogWarning("Deck이 할당되지 않았습니다.");
                return;
            }

            if (_currentDeck.GetHeadingContainer() != dealingTarget)
            {
                Debug.LogWarning("Deal 대상이 아닌 Container에 Deal을 시도하고 있습니다.");
                // todo : 의심도 증가
                return;
            }

            CardObject card = _currentDeck.PopTopCard();
            if (card == null)
            {
                Debug.LogWarning("Deck에서 카드를 가져오는 데 실패했습니다.");
                return;
            }
            
            dealingTarget.AddCard(card);
        }

        private void OnShuffle(InputAction.CallbackContext context)
        {
            Debug.Log("Shuffle");
            if (_currentDeck != null)
            {
                _currentDeck.Shuffle();
            }
        }

        // Deck이 Grab되었을 때 호출되는 메서드
        private void OnDeckGrabbed(SelectEnterEventArgs args)
        {
            if(deckGrabInteractable.TryGetComponent(out _currentDeck))
            {
                Debug.Log("Deck grabbed and reference registered.");
                // 추가적인 초기화 로직이 필요하면 여기에 구현
            }
            else
            {
                Debug.LogWarning("Grab된 Deck 오브젝트에 Deck 컴포넌트가 없습니다.");
            }
        }

        // Deck이 Release되었을 때 호출되는 메서드
        private void OnDeckReleased(SelectExitEventArgs args)
        {
            if (_currentDeck != null)
            {
                Debug.Log("Deck released and reference removed.");
                _currentDeck = null;
                // 추가적인 해제 로직이 필요하면 여기에 구현
            }
        }

        public IEnumerator DealToContainer(CardContainer target, int i)
        {
            dealingFlag = true;
            dealingTarget = target;
            dealingCount = i;
            
            // 카드를 Deck에서 뽑아서 actor에게 전달하는 로직
            yield return _dealingWaitUntil;
        }
    }
}
