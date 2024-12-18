using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Poker;
using UnityEngine;

namespace Actor
{
    /// 포커 AI 클래스
    /// 실제 베팅 결정을 내리는 AI 구현
    public class BettingAI : BettingActor
    {
        // 게임 진행 단계
        private enum GameStage
        {
            PreFlop,    // 초기 카드 배분 후
            Flop,       // 첫 3장의 커뮤니티 카드 공개
            Turn,       // 4번째 커뮤니티 카드 공개
            River       // 마지막 커뮤니티 카드 공개
        }

        // 가능한 포커 핸드 타입
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
        
        // 포지션
        private enum Position
        {
            Early,   // 초기 포지션 (불리한 위치)
            Middle,  // 중간 포지션
            Late    // 후반 포지션 (유리한 위치)
        }

        // AI 상태 변수들
        private GameStage currentStage = GameStage.PreFlop;    // 현재 게임 단계
        private float suspicionLevel = 0f;                      // AI의 의심 수준 (블러핑 감지용)
        private const float MAX_SUSPICION = 100f;               // 최대 의심 수준
        private Position currentPosition;                       // 현재 포지션

        // 초기화
        private void Start()
        {
            money = 1000; // 임시 시작 금액
            
            // 게임 시작 시 포지션 설정
            UpdatePosition();
        }

        // 포지션 업데이트 
        private void UpdatePosition()
        {
            try
            {
                int playerCount = GetPlayerCount();
                int myPosition = GetMyPosition();

                // 플레이어 수에 따라 포지션 결정
                if (myPosition < playerCount / 3)
                    currentPosition = Position.Early;
                else if (myPosition < (playerCount * 2) / 3)
                    currentPosition = Position.Middle;
                else
                    currentPosition = Position.Late;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating position: {e.Message}");
                currentPosition = Position.Early;  // 에러 시 기본값
            }
        }
        
        // AI의 플레이 로직
        public override IEnumerator Play()
        {
            Debug.Log($"Player Turn : {gameObject.name}");
            yield return new WaitForSeconds(1f);

            try
            {
                UpdatePosition();
                var hand = GetContainer().GetCards();
                var communityCards = BettingManager.Instance.GetCommunityCards();

                // 핸드 유효성 검사
                if (hand == null || hand.Count == 0)
                {
                    Debug.LogError("Invalid hand");
                    Fold();
                    yield break;
                }
                
                 // 핸드 강도 평가
                var handInfo = EvaluateHandStrength(hand, communityCards);

                 // 베팅 결정
                 int decision = CalculateBettingDecision(
                     hand,
                     communityCards,
                     GetCurRoundBet(),
                     GetMoney(),
                     BettingManager.Instance.GetCurrentBet(),
                    handInfo.strength,
                    handInfo.type
                );
                
                // 결정 실행
                ExecuteDecision(decision);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during play: {e.Message}");
                Fold(); // 에러 발생시 폴드
            }
        }

         // 핸드 강도 평가 메서드
        private (float strength, HandType type) EvaluateHandStrength(List<Card> hand, List<Card> communityCards)
        {
            if (hand == null || communityCards == null)
                return (0f, HandType.Incomplete);

            var allCards = new List<Card>(hand);
            allCards.AddRange(communityCards);

            HandType handType = DetermineHandType(allCards);
            float strength = CalculateHandStrength(hand, communityCards, handType);

            return (strength, handType);
        }
        
        // 베팅 결정 실행 메서드
        private void ExecuteDecision(int decision)
        {
            try
            {
                if (decision > 0)
                {
                    // 레이즈 결정
                    Raise(decision);
                    Debug.Log($"AI Raised {decision}");
                    suspicionLevel += 5f;
                }
                else if (decision == 0)
                {
                    if (CanCheck())
                    {
                        // 체크 가능할 때
                        Check();
                        Debug.Log("AI Checked");
                    }
                    else
                    {
                        // 체크 불가능할 때 콜
                        Call();
                        Debug.Log("AI Called");
                        suspicionLevel += 2f;
                    }
                }
                else
                {
                    // 폴드 결정
                    Debug.Log("AI Folded");
                    Fold();
                }

                // 의심도 관리
                suspicionLevel = Mathf.Clamp(suspicionLevel, 0f, MAX_SUSPICION);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing decision: {e.Message}");
                Fold(); // 에러 발생시 폴드
            }
        }

        // 베팅 액션 계산 메서드
        private int CalculateBettingDecision(
            List<Card> hand,
            List<Card> communityCards,
            int currentBettingAmount,
            int myMoney,
            int currentPot,
            float handStrength,
            HandType handType)
        {
            if (myMoney <= 0) return -1; // 돈이 없으면 폴드

            GameStage stage = DetermineGameStage(communityCards);
            
            // 팟 오즈 계산 (수익률)
            float potOdds = currentBettingAmount > 0 ?
                (float)currentBettingAmount / (currentPot + currentBettingAmount) : 0f;
            
            // 최소 베팅액 계산 (현재 베팅액의 2배 또는 보유 금액 중 작은 값)
            int minBet = Mathf.Min(myMoney, currentBettingAmount * 2);

            // 블러핑 확률 계산
            float bluffChance = CalculateBluffChance(stage, currentPosition, suspicionLevel);

            // 랜덤 요소 추가
            if (UnityEngine.Random.value < bluffChance)
            {
                return CalculateBluffAmount(minBet, myMoney, stage);
            }
            
            // 게임 단계별 결정
            return stage switch
            {
                GameStage.PreFlop => HandlePreFlopDecision(hand, currentBettingAmount, minBet),
                GameStage.Flop => HandleFlopDecision(handStrength, handType, potOdds, currentBettingAmount, minBet),
                GameStage.Turn => HandleTurnDecision(handStrength, handType, potOdds, currentBettingAmount, minBet),
                GameStage.River => HandleRiverDecision(handStrength, handType, potOdds, currentBettingAmount, minBet),
                _ => 0
            };
        }

        // 블러핑 확률 계산 메서드
        private float CalculateBluffChance(GameStage stage, Position position, float suspicionLevel)
        {
            float baseChance = 0.1f; // 기본 블러핑 확률

            // 게임 스테이지에 따른 조정
            baseChance += stage switch
            {
                GameStage.PreFlop => 0.05f,
                GameStage.Flop => 0.1f,
                GameStage.Turn => 0.15f,
                GameStage.River => 0.2f,
                _ => 0f
            };

            // 포지션에 따른 조정
            baseChance += position switch
            {
                Position.Late => 0.1f,   // 레이트 포지션에서 블러핑 확률 증가
                Position.Middle => 0.05f, // 미들 포지션에서 약간 증가
                _ => 0f                  // 얼리 포지션에서는 증가 없음
            };

            // 의심도에 따른 조정
            baseChance -= suspicionLevel / MAX_SUSPICION * 0.2f;

            return Mathf.Clamp(baseChance, 0.05f, 0.3f);
        }

        // 블러프 베팅액 계산 메서드
        private int CalculateBluffAmount(int minBet, int myMoney, GameStage stage)
        {
            // 게임 단계별 블러프 배수 설정
            float multiplier = stage switch
            {
                GameStage.PreFlop => 2f,
                GameStage.Flop => 2.5f,
                GameStage.Turn => 3f,
                GameStage.River => 3.5f,
                _ => 2f
            };

            return Mathf.Min((int)(minBet * multiplier), myMoney);
        }
        
        private int HandlePreFlopDecision(List<Card> hand, int currentBet, int minBet)
        {
            Card card1 = hand[0];
            Card card2 = hand[1];
            bool isPaired = card1.number == card2.number;
            bool isSuited = card1.suit == card2.suit;

            // 프리미엄 핸드 (AA, KK, QQ, JJ, AK)
            if (isPaired && (int)card1.number >= (int)Number.J ||
                (card1.number == Number.A && card2.number == Number.K))
            {
                return minBet * 3;  // 강한 레이즈
            }

            // 중간 강도 페어 (TT, 99, 88)
            if (isPaired && (int)card1.number >= (int)Number.VIII)
            {
                return currentPosition == Position.Late ? minBet * 2 : currentBet;
            }

            // 강한 에이스, 킹 (AQ, AJ, KQ, KJ)
            if ((card1.number == Number.A || card1.number == Number.K) &&
                ((int)card2.number >= (int)Number.J))
            {
                return currentPosition == Position.Late ? minBet * 2 : currentBet;
            }

            // 약한 핸드
            return currentPosition == Position.Late && IsPlayableHand(card1, card2) ? currentBet : -1;
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
                    return UnityEngine.Random.value > 0.7f ? minBet : -1;  // 30% 확률로 블러프

                default:
                    return -1;  // 약한 핸드는 폴드
            }
        }

        private static HandType DetermineHandType(List<Card> cards)
        {
            if (cards.Count < 5) return HandType.Incomplete;

            // 카드를 랭크와 슈트별로 그룹화
            var rankGroups = cards.GroupBy(c => c.number).OrderByDescending(g => g.Count());
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
            var orderedRanks = cards.Select(c => (int)c.number).Distinct().OrderBy(r => r).ToList();

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
            var orderedRanks = cards.Select(c => (int)c.number).Distinct().OrderBy(r => r).ToList();
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
            float highCardValue = hand.Max(c => (float)c.number / 14f) * 0.2f;

            // 키커 가치 추가
            float kickerValue = hand.Min(c => (float)c.number / 14f) * 0.1f;

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

            var orderedRanks = cards.Select(c => (int)c.number).OrderBy(r => r).ToList();
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
            int rankDiff = Mathf.Abs((int)card1.number - (int)card2.number);

            return (rankDiff <= 2 && isSuited) ||
                   ((int)card1.number >= (int)Number.X && (int)card2.number >= (int)Number.X);
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
