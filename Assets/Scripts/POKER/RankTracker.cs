using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Util;

namespace Poker
{
    public static class RankTracker
    {
        private static void Check(in List<Card> cards)
        {
            // check length
            if (cards.Count != 5)
            {
                throw new System.Exception("Invalid number of cards. Expected 5, got " + cards.Count);
            }
            
            // check is there any None card
            if (cards.Exists(card => card.number == Number.NONE))
            {
                throw new System.Exception("Invalid card number. None is not allowed.");
            }
        }
        
        public static bool IsRoyalFlush(in List<Card> cards)
        {
            Check(cards);
            
            return IsStraightFlush(cards, out var none) && cards[0].number == Number.X;
        }

        public static bool IsStraightFlush(in List<Card> cards, out Card highestCard)
        {
            Check(cards);
            
            if (IsFlush(cards, out var suit))
            {
                return IsStraight(cards, out highestCard);
            }

            highestCard = Card.None();
            return false;
        }

        public static bool IsStraight(in List<Card> cards, out Card highestCard)
        {
            // 복제 후 정렬
            var sortedCards = new List<Card>(cards);
            sortedCards.Sort((a, b) => a.number.GetInt().CompareTo(b.number.GetInt()));
            
            // 예외처리 : 10 J Q K A
            if(sortedCards[0].number is Number.A &&
               sortedCards[1].number is Number.X &&
                sortedCards[2].number is Number.J &&
                sortedCards[3].number is Number.Q &&
                sortedCards[4].number is Number.K)
            {
                highestCard = sortedCards[0];
                return true;
            }
            
            // checking is straight
            int start = sortedCards[0].number.GetInt();
            for (int i = 1; i < sortedCards.Count; i++)
            {
                if (sortedCards[i].number.GetInt() == start + i) continue;
                
                highestCard = Card.None();
                return false;
            }
            
            highestCard = sortedCards[4];
            return true;
        }

        public static bool IsFlush(in List<Card> cards, out Suit suit)
        {
            var curSuit = cards[0].suit;
            bool isFlush = cards.TrueForAll(card => card.suit == curSuit);
            
            suit = isFlush ? curSuit : Suit.Spade;
            return isFlush;
        }
        
        public static bool IsFourOfAKind(in List<Card> cards, out Card quadCard, out Card kicker)
        {
            Check(cards);
            
            List<IGrouping<Number, Card>> grouped = cards.GroupBy(card => card.number).ToList();
            IGrouping<Number, Card> fourGroup = grouped.FirstOrDefault(g => g.Count() == 4);

            if (fourGroup != null)
            {
                quadCard = fourGroup.First();
                
                foreach(Card c in cards)
                {
                    if (c.number == quadCard.number) continue;
                    kicker = c;
                    return true;
                }
            }

            quadCard = Card.None();
            kicker = Card.None();
            return false;
        }

        public static bool IsFullHouse(in List<Card> cards, out Card threeCard, out Card pairCard)
        {
            Check(cards);
            
            List<IGrouping<Number, Card>> grouped = cards.GroupBy(card => card.number).ToList();
            IGrouping<Number, Card> threeGroup = grouped.FirstOrDefault(g => g.Count() == 3);
            IGrouping<Number, Card> pairGroup = grouped.FirstOrDefault(g => g.Count() == 2);

            if (threeGroup != null && pairGroup != null)
            {
                threeCard = threeGroup.First();
                pairCard = pairGroup.First();
                return true;
            }

            threeCard = Card.None();
            pairCard = Card.None();
            return false;
        }

        public static bool IsThreeOfAKind(in List<Card> cards, out Card threeCard, out Card firstKicker, out Card secondKicker)
        {
            Check(cards);
            
            List<IGrouping<Number, Card>> grouped = cards.GroupBy(card => card.number).ToList();
            IGrouping<Number, Card> threeGroup = grouped.FirstOrDefault(g => g.Count() == 3);
            
            firstKicker = Card.None();
            secondKicker = Card.None();
            threeCard = Card.None();

            if (threeGroup == null) return false;
            threeCard = threeGroup.First();

            foreach (Card c in cards)
            {
                if (c.number == threeCard.number) continue;
                    
                if (firstKicker.number == Number.NONE)
                {
                    firstKicker = c;
                }
                else
                {
                    secondKicker = c;
                        
                    // bigger should be first kicker
                    if (firstKicker.number.GetInt() < secondKicker.number.GetInt())
                    {
                        (firstKicker, secondKicker) = (secondKicker, firstKicker);
                            
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsTwoPair(in List<Card> cards, out List<Card> pairCards, out Card kicker)
        {
            Check(cards);
            
            var grouped = cards.GroupBy(card => card.number).Where(g => g.Count() == 2).ToList();

            if (grouped.Count == 2)
            {
                pairCards = grouped.Select(g => g.First()).ToList();

                foreach (Card c in cards)
                {
                    if (pairCards.Exists(pair => pair.number == c.number)) continue;
                    
                    kicker = c;
                    return true;
                }
            }

            pairCards = new List<Card> { Card.None(), Card.None() };
            kicker = Card.None();
            return false;
        }

        public static bool IsOnePair(in List<Card> cards, out Card pairCard, out List<Card> kickers)
        {
            Check(cards);
            
            List<IGrouping<Number, Card>> grouped = cards.GroupBy(card => card.number).ToList();
            IGrouping<Number, Card> pairGroup = grouped.FirstOrDefault(g => g.Count() == 2);

            kickers = new List<Card>();
            
            if (pairGroup != null)
            {
                pairCard = pairGroup.First();
                
                foreach(Card c in cards)
                {
                    if (c.number == pairCard.number) continue;
                    kickers.Add(c);
                    
                    if (kickers.Count == 3) break;
                }
                
                kickers.Sort((a, b) => b.number.GetInt().CompareTo(a.number.GetInt()));
                return true;
            }

            pairCard = Card.None();
            return false;
        }

        public static bool IsHighCard(in List<Card> cards, out Card highCard)
        {
            Check(cards);
            
            highCard = cards.OrderByDescending(card => card.number.GetInt()).First();
            return true;
        }
        
        
        public static Rank CreateRank5Cards(in List<Card> cards)
        {
            if (IsRoyalFlush(cards))
            {
                return new Rank(HandRank.RoyalFlush, cards, new List<Card>());
            }

            if (IsStraightFlush(cards, out Card highestCard))
            {
                return new Rank(HandRank.StraightFlush, cards, new List<Card> { highestCard });
            }

            if (IsFourOfAKind(cards, out Card quadCard, out Card fkKicker))
            {
                return new Rank(HandRank.FourOfAKind, cards, new List<Card> { quadCard, fkKicker });
            }

            if (IsFullHouse(cards, out Card threeCard, out Card pairCard))
            {
                return new Rank(HandRank.FullHouse, cards, new List<Card> { threeCard, pairCard });
            }

            if (IsFlush(cards, out Suit suit))
            {
                return new Rank(HandRank.Flush, cards, new List<Card>());
            }

            if (IsStraight(cards, out highestCard))
            {
                return new Rank(HandRank.Straight, cards, new List<Card> { highestCard });
            }

            if (IsThreeOfAKind(cards, out Card tkThreeCard, out Card firstKicker, out Card secondKicker))
            {
                return new Rank(HandRank.ThreeOfAKind, cards, new List<Card> { tkThreeCard, firstKicker, secondKicker });
            }

            if (IsTwoPair(cards, out List<Card> pairCards, out Card kicker))
            {
                return new Rank(HandRank.TwoPair, cards, new List<Card> { pairCards[0], pairCards[1], kicker });
            }
            
            if(IsOnePair(cards, out Card pair, out List<Card> kickers))
            {
                return new Rank(HandRank.Pair, cards, new List<Card> { pair, kickers[0], kickers[1], kickers[2] });
            }

            Card highest = Card.None();
            foreach (Card card in cards)
            {
                if (card.number.GetInt() > highest.number.GetInt())
                {
                    highest = card;
                }
            }
         
            var ret = new Rank(HandRank.HighCard, cards, new List<Card>
            {
                highest
            });
            
            return ret;
        }

        /// <summary>
        /// 해당 카드 내에서 조합 가능한 가장 높은 Rank를 반환합니다.
        /// </summary>
        /// <param name="hands"></param>
        /// <param name="communityCards"></param>
        /// <returns></returns>
        /// <exception cref="Exception">카드의 개수가 5개 미만일 경우 발생합니다.</exception>
        public static Rank GetPossibleMaxRank(in List<Card> hands, in List<Card> communityCards = null)
        {
            Debug.Log("RankTracker.cs : GetPossibleMaxRank called with hands : " +
                      hands.Count + " communityCards : " + 
                      communityCards?.Count
            );
            
            List<Card> cards = new (hands); 
            if (communityCards != null) cards.AddRange(communityCards);
            else
            {
                Debug.LogWarning("Community cards are null");
            }

            if (cards.Count < 5)
            {
                throw new Exception("Invalid number of cards. Expected 5 or more, got " + cards.Count);
            }

            List<List<Card>> combinations = Combinatorics.GetCombinations(cards, 5);
            Rank bestRank = null;

            if (combinations.Count == 0)
            {
                throw new Exception("No combination found.");
            }

            foreach (List<Card> combination in combinations)
            {
                Rank rank = CreateRank5Cards(combination);
                if (bestRank == null || rank.handRank > bestRank.handRank)
                {
                    bestRank = rank;
                }
            }

            return bestRank;
        }

        /// <summary>
        /// 각 카드의 Rank가 얼마나 강한 streangth를 갖는지 표현합니다.
        /// 0 ~ 1의 값을 가집니다.
        /// </summary>
        /// <param name="rank"></param>
        /// <returns></returns>
        public static float GetRankStrength(Rank rank)
        {
            switch (rank.handRank)
            {
                case HandRank.RoyalFlush:
                    return 1.0f;
                case HandRank.StraightFlush:
                    return 0.98f;
                case HandRank.FourOfAKind:
                    return 0.9f;
                case HandRank.FullHouse:
                    return 0.8f;
                case HandRank.Flush:
                    return 0.7f;
                case HandRank.Straight:
                    return 0.65f;
                case HandRank.ThreeOfAKind:
                    return 0.6f;
                case HandRank.TwoPair:
                    return 0.4f + (rank.cards[0].number.GetInt() / 100.0f);
                case HandRank.Pair:
                    return 0.2f + (rank.cards[0].number.GetInt() / 100.0f);
                case HandRank.HighCard:
                    return 0.0f + (rank.cards[0].number.GetInt() / 100.0f);
                default:
                    return 0.0f;
            }
        }
    }


    public enum HandRank
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
        RoyalFlush
    }
}