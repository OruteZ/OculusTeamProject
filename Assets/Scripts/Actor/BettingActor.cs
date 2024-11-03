using System.Collections;

namespace Actor
{
    /// <summary>
    /// BettingActor는 포커의 과정중에서, 베팅담당하는 클래스들을 통칭하는 abstract class 입니다.
    /// TurnManager는 각 Actor들에게 차례를 넘겨주고, 차례가 끝나면 다음에 어떤 Actor에게 차례를 넘겨줄지 선택합니다.
    /// 이 과정에서 Callback 함수를 통한 구조는 복잡하기 때문에, 코루틴을 사용한 상호착용에 기반합니다.
    /// </summary>
    [System.Serializable]
    public abstract class BettingActor
    {
        private int _money;
        private int _curRoundBet;
        private bool _hasFolded;
        private bool _isAllIn;
        
        protected void Bet(int amount)
        {
            BettingManager.Instance.Bet(amount);
            
            _curRoundBet += amount;
            _money -= amount;
        }

        protected void Check()
        {
        }

        protected void Fold()
        {
            
        }

        protected void Raise(int amount)
        {
            BettingManager.Instance.Bet(amount);
            
            _curRoundBet += amount;
            _money -= amount;
        }

        protected bool CanBet(int amount)
        {
            if (BettingManager.Instance.CanBet(amount) is false)
            {
                return false;
            }

            if (amount > _money)
            {
                return false;
            }

            return true;
        }

        protected bool CanCheck()
        {
            return BettingManager.Instance.CanCheck();     
        }

        /// <summary>
        /// Fold했거나, All-in을 했거나 하는 등의 상황으로 베팅이 불가능한지 확인합니다.
        /// </summary>
        public bool CanParticipateInBetting()
        {
            if(_hasFolded) return false;
            if(_isAllIn) return false;
            if(CanBet(_money) is false) return false;
            
            return true;
        }

        public abstract IEnumerator Play();
        
        public void ResetRoundBet()
        {
            _curRoundBet = 0;
            _isAllIn = false;
            _hasFolded = false;
        }
    }
}