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
        private enum GameStage
        {
            PreFlop,
            Flop,
            Turn,
            River
        }

        private enum HandType
        {
            Incomplete,
            HighCard,
            Pair,
            TwoPair,
            ThreeOfAKind,
            Straight,
            Flush,
            FullHouse,
            FourOfAKind,
            StraightFlush,
            Draw
        }

        private enum Position
        {
            Early,
            Middle,
            Late
        }

        private GameStage currentStage = GameStage.PreFlop;
        private float suspicionLevel = 0f;
        private const float MAX_SUSPICION = 100f;
        private Position currentPosition;

        private void Start()
        {
            // 게임 시작 시 포지션 설정
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            int playerCount = GetPlayerCount();
            int myPosition = GetMyPosition();

            if (myPosition < playerCount / 3)
                currentPosition = Position.Early;
            else if (myPosition < (playerCount * 2) / 3)
                currentPosition = Position.Middle;
            else
                currentPosition = Position.Late;
        }

        public override IEnumerator Play()
        {
            Debug.Log($"Player Turn : {gameObject.name}");
            yield return new WaitForSeconds(1f);

            UpdatePosition();
            var handInfo = EvaluateHandStrength(GetHand(), GetCommunityCards());

            int decision = CalculateBettingDecision(
                GetHand(),
                GetCommunityCards(),
                GetCurrentBet(),
                GetMoney(),
                GetCurrentPot(),
                handInfo.strength,
                handInfo.type
            );

            ExecuteDecision(decision);
        }

        private void ExecuteDecision(int decision)
        {
            if (decision > 0)
            {
                Raise(decision);
                Debug.Log($"AI Raised {decision}");
                suspicionLevel += 5f;
            }
            else if (decision == 0)
            {
                if (CanCheck())
                {
                    Check();
                    Debug.Log("AI Checked");
                }
                else if (Call())
                {
                    Debug.Log("AI Called");
                    suspicionLevel += 2f;
                }
                else
                {
                    Debug.Log("AI Folded - Couldn't call");
                    Fold();
                }
            }
            else
            {
                Debug.Log("AI Folded - Strategic decision");
                Fold();
            }

            // 의심도 관리
            suspicionLevel = Mathf.Clamp(suspicionLevel, 0f, MAX_SUSPICION);
        }

        private static int CalculateBettingDecision(
            List<Card> hand,
            List<Card> communityCards,
            int currentBettingAmount,
            int myMoney,
            int currentPot,
            float handStrength,
            HandType handType)
        {
            GameStage stage = DetermineGameStage(communityCards);
            float potOdds = currentBettingAmount > 0 ?
                (float)currentBettingAmount / (currentPot + currentBettingAmount) : 0f;
            int minBet = Mathf.Min(myMoney, currentBettingAmount * 2);

            return stage switch
            {
                GameStage.PreFlop => HandlePreFlopDecision(hand, currentBettingAmount, minBet),
                GameStage.Flop => HandleFlopDecision(handStrength, handType, potOdds, currentBettingAmount, minBet),
                GameStage.Turn => HandleTurnDecision(handStrength, handType, potOdds, currentBettingAmount, minBet),
                GameStage.River => HandleRiverDecision(handStrength, handType, potOdds, currentBettingAmount, minBet),
                _ => 0
            };
        }

        private static int HandlePreFlopDecision(List<Card> hand, int currentBet, int minBet)
        {
            Card card1 = hand[0];
            Card card2 = hand[1];
            bool isPaired = card1.rank == card2.rank;
            bool isSuited = card1.suit == card2.suit;

            // 프리미엄 핸드 (AA, KK, QQ, JJ, AK)
            if (isPaired && (int)card1.rank >= (int)Rank.Jack ||
                (card1.rank == Rank.Ace && card2.rank == Rank.King))
            {
                return minBet * 3;  // 강한 레이즈
            }

            // 중간 강도 페어 (TT, 99, 88)
            if (isPaired && (int)card1.rank >= (int)Rank.Eight)
            {
                return Position == Position.Late ? minBet * 2 : currentBet;
            }

            // 강한 에이스, 킹 (AQ, AJ, KQ, KJ)
            if ((card1.rank == Rank.Ace || card1.rank == Rank.King) &&
                ((int)card2.rank >= (int)Rank.Jack))
            {
                return Position == Position.Late ? minBet * 2 : currentBet;
            }

            // 약한 핸드
            return Position == Position.Late && IsPlayableHand(card1, card2) ? currentBet : -1;
        }

        private static int HandleFlopDecision(float handStrength, HandType handType, float potOdds, int currentBet, int minBet)
        {
            switch (handType)
            {
                case HandType.FullHouse:
                case HandType.Flush:
                case HandType.Straight:
                    return minBet * 3;  // 적극적 레이즈

                case HandType.ThreeOfAKind:
                case HandType.TwoPair:
                    return minBet * 2;  // 밸류벳

                case HandType.Draw:
                    if (handStrength > potOdds + 0.2f)
                        return currentBet;  // 세미블러프
                    return 0;  // 체크/콜

                default:
                    return handStrength > potOdds ? 0 : -1;  // 약한 핸드는 체크 또는 폴드
            }
        }

        private static int HandleTurnDecision(float handStrength, HandType handType, float potOdds, int currentBet, int minBet)
        {
            switch (handType)
            {
                case HandType.FullHouse:
                case HandType.Flush:
                case HandType.Straight:
                    return minBet * 3;  // 강한 레이즈

                case HandType.ThreeOfAKind:
                case HandType.TwoPair:
                    return minBet * 2;  // 강한 밸류벳

                case HandType.Draw:
                    if (handStrength > potOdds + 0.3f)
                        return currentBet;  // 높은 드로우 확률시 베팅
                    return 0;  // 체크/콜

                default:
                    return -1;  // 약한 핸드는 폴드
            }
        }

        private static int HandleRiverDecision(float handStrength, HandType handType, float potOdds, int currentBet, int minBet)
        {
            switch (handType)
            {
                case HandType.FullHouse:
                case HandType.Flush:
                case HandType.Straight:
                    return minBet * 3;  // 최대 밸류벳

                case HandType.ThreeOfAKind:
                case HandType.TwoPair:
                    return minBet * 2;  // 강한 밸류벳

                case HandType.Draw:
                    return Random.value > 0.7f ? minBet : -1;  // 30% 확률로 블러프

                default:
                    return -1;  // 약한 핸드는 폴드
            }
        }

        private static HandType DetermineHandType(List<Card> cards)
        {
            if (cards.Count < 5) return HandType.Incomplete;

            // 카드를 랭크와 슈트별로 그룹화
            var rankGroups = cards.GroupBy(c => c.rank).OrderByDescending(g => g.Count());
            var suitGroups = cards.GroupBy(c => c.suit);

            // 스트레이트 플러시 체크
            if (HasStraightFlush(cards)) return HandType.StraightFlush;

            // 포카드 체크
            if (rankGroups.Any(g => g.Count() == 4)) return HandType.FourOfAKind;

            // 풀하우스 체크
            if (rankGroups.Any(g => g.Count() == 3) && rankGroups.Any(g => g.Count() == 2))
                return HandType.FullHouse;

            // 플러시 체크
            if (suitGroups.Any(g => g.Count() >= 5)) return HandType.Flush;

            // 스트레이트 체크
            if (HasStraight(cards)) return HandType.Straight;

            // 트리플 체크
            if (rankGroups.Any(g => g.Count() == 3)) return HandType.ThreeOfAKind;

            // 투페어 체크
            if (rankGroups.Count(g => g.Count() == 2) == 2) return HandType.TwoPair;

            // 원페어 체크
            if (rankGroups.Any(g => g.Count() == 2)) return HandType.Pair;

            // 드로우 체크
            if (IsDrawing(cards)) return HandType.Draw;

            return HandType.HighCard;
        }

        private static bool HasStraightFlush(List<Card> cards)
        {
            foreach (var suitGroup in cards.GroupBy(c => c.suit))
            {
                if (suitGroup.Count() >= 5 && HasStraight(suitGroup.ToList()))
                    return true;
            }
            return false;
        }

        private static bool HasStraight(List<Card> cards)
        {
            var orderedRanks = cards.Select(c => (int)c.rank).Distinct().OrderBy(r => r).ToList();

            for (int i = 0; i <= orderedRanks.Count - 5; i++)
            {
                if (orderedRanks[i + 4] - orderedRanks[i] == 4)
                    return true;
            }

            // Ace-low 스트레이트 체크 (A,2,3,4,5)
            if (orderedRanks.Contains(14)) // Ace
            {
                var lowStraight = new[] { 2, 3, 4, 5 };
                if (lowStraight.All(r => orderedRanks.Contains(r)))
                    return true;
            }

            return false;
        }

        private static bool IsDrawing(List<Card> cards)
        {
            // 플러시 드로우
            bool hasFlushDraw = cards.GroupBy(c => c.suit).Any(g => g.Count() == 4);

            // 스트레이트 드로우
            var orderedRanks = cards.Select(c => (int)c.rank).Distinct().OrderBy(r => r).ToList();
            bool hasStraightDraw = false;

            for (int i = 0; i < orderedRanks.Count - 3; i++)
            {
                if (orderedRanks[i + 3] - orderedRanks[i] == 3)
                {
                    hasStraightDraw = true;
                    break;
                }
            }

            return hasFlushDraw || hasStraightDraw;
        }

        private static float CalculateHandStrength(List<Card> hand, List<Card> communityCards, HandType handType)
        {
            float baseStrength = (float)handType / 10f; // 기본 핸드 타입 강도

            // 하이카드 가치 추가
            float highCardValue = hand.Max(c => (float)c.rank / 14f) * 0.2f;

            // 키커 가치 추가
            float kickerValue = hand.Min(c => (float)c.rank / 14f) * 0.1f;

            // 드로우 가치 계산
            float drawValue = 0f;
            if (handType == HandType.Draw)
            {
                var allCards = new List<Card>(hand);
                allCards.AddRange(communityCards);

                if (allCards.GroupBy(c => c.suit).Any(g => g.Count() == 4))
                    drawValue = 0.3f; // 플러시 드로우
                else
                    drawValue = 0.2f; // 스트레이트 드로우
            }

            return Mathf.Clamp01(baseStrength + highCardValue + kickerValue + drawValue);
        }

        private static HandType AnalyzeDraws(List<Card> cards)
        {
            bool hasFlushDraw = cards.GroupBy(c => c.suit).Any(g => g.Count() == 4);
            if (hasFlushDraw) return HandType.Draw;

            var orderedRanks = cards.Select(c => (int)c.rank).OrderBy(r => r).ToList();
            int gaps = 0;
            for (int i = 0; i < orderedRanks.Count - 1; i++)
            {
                gaps += orderedRanks[i + 1] - orderedRanks[i] - 1;
                if (gaps > 1) break;
            }

            return gaps <= 1 ? HandType.Draw : HandType.HighCard;
        }

        private static bool IsPlayableHand(Card card1, Card card2)
        {
            bool isSuited = card1.suit == card2.suit;
            int rankDiff = Mathf.Abs((int)card1.rank - (int)card2.rank);

            return (rankDiff <= 2 && isSuited) ||
                   ((int)card1.rank >= (int)Rank.Ten && (int)card2.rank >= (int)Rank.Ten);
        }

        private static GameStage DetermineGameStage(List<Card> communityCards)
        {
            return communityCards.Count switch
            {
                0 => GameStage.PreFlop,
                3 => GameStage.Flop,
                4 => GameStage.Turn,
                5 => GameStage.River,
                _ => GameStage.PreFlop
            };
        }

        // Helper methods for position-based play
        private int GetPlayerCount() => 6; // 예시 값
        private int GetMyPosition() => 2;  // 예시 값
    }
}
