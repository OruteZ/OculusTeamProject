using System;
using System.Collections.Generic;

namespace Poker
{
    public class Rank : IComparable<Rank>
    {
        private bool Equals(Rank other)
        {
            return handRank == other.handRank && Equals(kickers, other.kickers) && Equals(cards, other.cards);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Rank)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)handRank, kickers, cards);
        }

        public readonly HandRank handRank;
        public readonly List<Card> kickers;
        public readonly List<Card> cards;
        
        public Rank(HandRank handRank, List<Card> cards, List<Card> kickers)
        {
            if (cards.Count != 5)
            {
                throw new Exception("Invalid number of cards. Expected 5, got " + cards.Count);
            }
            
            this.handRank = handRank;
            this.cards = cards;
            this.kickers = kickers;
        }


        // 1 : this > other
        // 0 : this == other
        // -1 : this < other
        public int CompareTo(Rank other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            
            if (handRank != other.handRank)
            {
                return handRank.CompareTo(other.handRank);
            }
            
            // handRank가 같을 경우 : kickers 비교
            for (int i = 0; i < kickers.Count; i++)
            {
                if (kickers[i].number != other.kickers[i].number)
                {
                    return kickers[i].number.GetInt().CompareTo(other.kickers[i].number.GetInt());
                }
            }
            
            return 0;
        }
        
        
        
        
        public static bool operator >(Rank left, Rank right)
        {
            return left.CompareTo(right) > 0;
        }
        
        public static bool operator <(Rank left, Rank right)
        {
            return left.CompareTo(right) < 0;
        }
        
        public static bool operator ==(Rank left, Rank right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            return left.CompareTo(right) == 0;
        }
        
        public static bool operator !=(Rank left, Rank right)
        {
            if (ReferenceEquals(left, null))
            {
                return !ReferenceEquals(right, null);
            }
            return left.CompareTo(right) != 0;
        }
        
        public override string ToString()
        {
            string str = $"{handRank} - ";;
            foreach (Card card in cards)
            {
                str += card.number + " " + card.suit + " | ";
            }

            return str;
        }
    }
}