using System.Collections;
using System.Collections.Generic;
using Poker;
using UnityEngine;

namespace Actor
{
    // 임시 AI입니다. 자신이 베팅 할 수 있는한 최소한의 금액을 무조건적으로 베팅합니다.
    public class BettingAI : BettingActor
    {
        public override IEnumerator Play()
        {
            Debug.Log($"Player Turn : {gameObject.name}");
            yield return new WaitForSeconds(1f);

            if (CanCheck())
            {
                Raise(10);
                Debug.Log("AI Raised 10");
                yield break;
            }
            
            if (Call() is false)
            {
                Debug.Log("AI Folded");
                Fold();
            }
            else
            {
                Debug.Log("AI Called");
            }
        }

        /// <summary>
        /// AI의 베팅 판단을 계산합니다.
        /// </summary>
        /// <param name="hand">AI의 현재 패 목록입니다. 2~5개의 값을 가집니다.</param>
        /// <param name="communityCards">현재 공용 카드의 목록입니다. 3~5개의 값을 가집니다.</param>
        /// <param name="currentBettingAmount">지금 걸려있는 배팅 금액입니다.</param>
        /// <param name="myMoney">현재 AI가 가지고있는 금액입니다.</param>
        /// <param name="currentPot">현재 판돈입니다.</param>
        /// <returns> -1 : Fold, 0 : check or Call, more then 0 : Raise, or bet</returns>
        private static int CalculateBettingDecision(
            List<Card> hand,
            List<Card> communityCards,
            int currentBettingAmount,
            int myMoney,
            int currentPot)
        {

            return 0;
        }
    }
}