using System.Collections;
using System.Collections.Generic;
using Poker;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.Serialization;

namespace Actor
{
    /// <summary>
    /// BettingActor는 포커의 과정중에서, 베팅담당하는 클래스들을 통칭하는 abstract class 입니다.
    /// TurnManager는 각 Actor들에게 차례를 넘겨주고, 차례가 끝나면 다음에 어떤 Actor에게 차례를 넘겨줄지 선택합니다.
    /// 이 과정에서 Callback 함수를 통한 구조는 복잡하기 때문에, 코루틴을 사용한 상호착용에 기반합니다.
    /// </summary>
    [System.Serializable]
    public abstract class BettingActor : MonoBehaviour
    {
        [SerializeField]
        protected int money;
        
        [SerializeField]
        private int _curRoundBet;
        
        [SerializeField]
        private bool _hasFolded;
        
        [SerializeField]
        private bool _isAllIn;
        
        [SerializeField]
        private BettingChipVisualizer _bettingChipVisualizer;
        
        [SerializeField]
        private CardContainer _cards;
        
        
        protected bool Call()
        {
            if (CanCheck())
            {
                Debug.LogWarning("Check를 할 수 있으면 Call을 할 수 없습니다. 임시로 Check를 호출합니다.");
                return Check();
            }
            
            int curRoundBet = BettingSystem.Instance.GetCurrentBet();
            int amount = curRoundBet - _curRoundBet;
            
            if(amount < 0) return false;
            if (amount < money) return false;
            
            _curRoundBet += amount;
            money -= amount;
            BettingSystem.Instance.Bet(amount, _curRoundBet);
            _bettingChipVisualizer.SetMoney(money);
            
            return true;
        }
        
        protected bool Callable()
        {
            int curRoundBet = BettingSystem.Instance.GetCurrentBet();
            
            // call을 하기 위해서 필요한 금액
            int amount = curRoundBet - _curRoundBet;
            
            if (amount < 0) return false;
            if (money < amount) return false;
            
            return true;
        }
        
        protected bool Raise(int amount)
        {
            if (amount < 0) return false;
            if (amount < money) return false;
            
            _curRoundBet += amount;
            money -= amount;
            BettingSystem.Instance.Bet(amount, _curRoundBet);
            _bettingChipVisualizer.SetMoney(money);
            
            return true;
        }

        protected bool Check()
        {
            if(CanCheck() is false) return false;
            return true;
        }

        protected bool Fold()
        {
            _hasFolded = true;
            return true;
        }
        protected bool CanCheck()
        {
            return BettingSystem.Instance.CanCheck(this);     
        }

        /// <summary>
        /// Fold했거나, All-in을 했거나 하는 등의 상황으로 베팅이 불가능한지 확인합니다.
        /// </summary>
        public bool CanParticipateInBetting()
        {
            if(_hasFolded) return false;
            if(_isAllIn) return false;
            
            return Callable();
        }

        public abstract IEnumerator Play();
        
        public void ResetRoundBet()
        {
            _curRoundBet = 0;
            _isAllIn = false;
            _hasFolded = false;
        }
        
        #region GETTER
        
        public int GetMoney()
        {
            return money;
        }
        
        public int GetCurRoundBet()
        {
            return _curRoundBet;
        }
        
        public bool GetHasFolded()
        {
            return _hasFolded;
        }
        
        public bool GetIsAllIn()
        {
            return _isAllIn;
        }
        
        #endregion

        public void AddMoney(int getPot)
        {
            money += getPot;
            _bettingChipVisualizer.SetMoney(money);
        }

        public CardContainer GetContainer()
        {
            return _cards;
        }
    }
}