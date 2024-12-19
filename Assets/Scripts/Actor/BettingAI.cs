using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Poker;
using UnityEngine;

namespace Actor
{
    public class BettingAI : BettingActor
    {
        [SerializeField]
        private float currentHandStrength;
        
        public override IEnumerator Play()
        {
            Debug.Log($"Player Turn : {gameObject.name}");
            yield return new WaitForSeconds(1f);

            try
            {
                List<Card> hand = GetContainer().GetCards();
                List<Card> communityCards = TurnSystem.Instance.GetCommunityCards();

                if (hand == null || hand.Count == 0)
                {
                    Debug.LogError("Invalid hand");
                    Fold();
                    yield break;
                }
                
                currentHandStrength = EvaluateHandStrength(hand, communityCards);

                int decision = CalculateBettingDecision(
                    GetCurRoundBet(), 
                    GetMoney(), 
                    BettingSystem.Instance.GetCurrentBet(), 
                    currentHandStrength
                );
                ExecuteDecision(decision);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during play: {e.Message}");
                Fold(); 
            }
        }

        private static float EvaluateHandStrength(IReadOnlyCollection<Card> hand, List<Card> communityCards)
        {
            if (hand == null || communityCards == null)
                return (0.0f);

            List<Card> allCards = new List<Card>(hand);
            allCards.AddRange(communityCards);
            
            float strength = CalculateHandStrength(hand, communityCards);

            return (strength);
        }

        private void ExecuteDecision(int decision)
        {
            try
            {
                if (decision > 0)
                {
                    Raise(decision);
                    Debug.Log($"AI Raised {decision}");
                }
                else if (decision == 0 && CanCheck())
                {
                    Check();
                    Debug.Log("AI Checked");
                }
                else if (decision == 0)
                {
                    Call();
                    Debug.Log("AI Called");
                }
                else
                {
                    Debug.Log("AI Folded");
                    Fold();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing decision: {e.Message}");
                Fold();
            }
        }

        private static int CalculateBettingDecision(
            int currentBettingAmount,
            int myMoney,
            int currentPot,
            float handStrength
            ) {
            if (myMoney <= 0) return -1;

            // 기본 베이스 벳 계산
            int baseBet = (currentBettingAmount > 0 ? currentBettingAmount : 50);
            int smallBet = Mathf.Min(myMoney, baseBet);                // 최소기준 레이즈 혹은 콜
            int mediumRaise = Mathf.Min(myMoney, baseBet * 2);         // 중간 정도 레이즈
            int bigRaise = Mathf.Min(myMoney, baseBet * 3);            // 크게 레이즈

            // 공격적 로직:
            // handStrength > 0.7 : 매우 강한 핸드 -> 크게 레이즈 (3배)
            // handStrength > 0.5 : 꽤 좋은 핸드 -> 중간 레이즈 (2배)
            // handStrength > 0.3 : 나쁘지 않음 -> 최소한 레이즈(기본 베팅액 정도)
            // handStrength > 0.1 : 약간 약하지만 완전 폴드는 아님 -> 콜 또는 소액 베팅(0이면 체크)
            // 그 외 : 폴드

            return handStrength switch
            {
                > 0.7f => bigRaise,
                > 0.5f => mediumRaise,
                > 0.3f => smallBet,
                > 0.1f => (currentBettingAmount > 0) ? 0 : Mathf.Min(myMoney, 50),
                _ => -1
            };
        }
        
        private static float CalIncompleteCardsStrength(List<Card> cards)
        {
            if (cards == null || cards.Count == 0)
                return 0.0f;

            cards.Sort((a, b) => a.number.GetInt().CompareTo(b.number.GetInt()));
            int count = cards.Count;
            int highestCardValue = cards[^1].number.GetInt();

            // 숫자별로 그룹화(페어, 트리플 확인)
            var numberGroups = cards
                .GroupBy(c => c.number.GetInt())
                .Select(g => new { Number = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ThenByDescending(g => g.Number)
                .ToList();

            int topGroupCount = numberGroups[0].Count;
            int topGroupNumber = numberGroups[0].Number;

            // 기본 Strength 결정 (Pair/ThreeOfAKind/HighCard)
            float baseStrength;
            if (topGroupCount == 3)
            {
                // ThreeOfAKind 유사
                baseStrength = 0.6f + (topGroupNumber / 100.0f);
            }
            else if (topGroupCount == 2)
            {
                // Pair 유사
                baseStrength = 0.2f + (topGroupNumber / 100.0f);
            }
            else
            {
                // HighCard 유사
                baseStrength = (highestCardValue / 100.0f);
            }

            // 플러시 드로우 가능성 확인
            // 같은 suit를 가진 카드 수
            var suitGroups = cards
                .GroupBy(c => c.suit)
                .Select(g => new { Suit = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            int maxSuitCount = suitGroups[0].Count;
            // 플러시 드로우 보너스
            float flushBonus = 0.0f;
            if (maxSuitCount == 4)
            {
                // 거의 플러시 완성에 가까운 상태 (추가카드 1장만 맞으면 플러시)
                flushBonus = 0.15f;
            }
            else if (maxSuitCount == 3 && count < 5)
            {
                // 3장 일치이면 아직 2장의 카드 여유 -> 어느 정도 플러시 가능성
                flushBonus = 0.07f;
            }

            // 스트레이트 드로우 가능성 확인
            // 연속되는 카드의 최대 길이를 찾는다.
            int straightLength = GetLongestConsecutiveRun(cards.Select(c => c.number.GetInt()).ToArray());

            float straightBonus = 0.0f;
            if (straightLength >= 4)
            {
                // 하나만 더 받으면 스트레이트가 될 수 있는 강력한 드로우
                straightBonus = 0.15f;
            }
            else if (straightLength == 3 && count < 5)
            {
                // 두장 더 받으면 스트레이트 가능성이 있는 약한 드로우
                straightBonus = 0.07f;
            }

            // 최종 Strength
            float finalStrength = baseStrength + flushBonus + straightBonus;
            // 1.0f 초과하지 않도록 제한 (여기서는 단순히 Min사용)
            finalStrength = Math.Min(finalStrength, 1.0f);

            return finalStrength;
            }

        private static int GetLongestConsecutiveRun(IReadOnlyList<int> sortedValues)
        {
            if (sortedValues.Count == 0) return 0;

            int longest = 1;
            int current = 1;

            for (int i = 1; i < sortedValues.Count; i++)
            {
                if (sortedValues[i] == sortedValues[i - 1] + 1)
                {
                    current++;
                }
                else if (sortedValues[i] == sortedValues[i - 1])
                {
                    // 같은 값은 무시 (A, A 처럼) 
                    // 단순히 계속 진행하지만 current 카운트에는 포함하지 않음
                    // 여기서는 아무 처리 없이 continue
                }
                else
                {
                    if (current > longest) longest = current;
                    current = 1;
                }
            }

            if (current > longest) longest = current;

            return longest;
        }


        private static float CalculateHandStrength(IEnumerable<Card> hand, List<Card> communityCards)
        {
            List<Card> cards = new List<Card>(hand);
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            cards.AddRange(communityCards);

            if (cards.Count >= 5)
            {
                Rank rank = RankTracker.CreateRank5Cards(cards);
                return RankTracker.GetRankStrength(rank);
            }
            else
            {
                return CalIncompleteCardsStrength(cards);
            }
        }
    }
}
