using System.Collections.Generic;
using Poker;
using UnityEngine;

namespace Tester
{
    public class PokerRankCompareTester : MonoBehaviour
    {
        private static List<Card> GetRandomHand()
        {
            List<Card> cards = new List<Card>();
            for (int i = 0; i < 5; i++)
            {
                cards.Add(new Card(
                    (Number)Random.Range(1, (int)Number.LENGTH),
                    (Suit  )Random.Range(0, (int)Suit  .LENGTH)
                    ));
            }

            return cards;
        }

        private static void DebugHand(in List<Card> cards)
        {
            string str = "";
            foreach (Card card in cards)
            {
                str += card.number + " " + card.suit + " ";
            }

            Debug.Log(str);
        }
        
        [ContextMenu("Test")]
        public void Test()
        {
            List<Card> hand1 = GetRandomHand();
            List<Card> hand2 = GetRandomHand();
            
            DebugHand(hand1);
            DebugHand(hand2);
            
            Rank rank1 = RankTracker.CreateRank5Cards(hand1);
            Rank rank2 = RankTracker.CreateRank5Cards(hand2);

            Debug.Log("Rank1 : " + rank1.handRank);
            Debug.Log("Rank2 : " + rank2.handRank);
        
            if(rank1 > rank2)
                Debug.Log("Rank1 Win");
            else if (rank1 < rank2)
                Debug.Log("Rank2 Win");
            else
                Debug.Log("Draw");
        }
    }
}