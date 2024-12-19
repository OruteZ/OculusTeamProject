using System;

namespace Poker
{
    public enum Number
    {
        NONE,
        A = 1,
        II = 2,
        III = 3,
        IV = 4,
        V = 5,
        VI = 6,
        VII = 7,
        VIII = 8,
        IX = 9,
        X = 10,
        J = 11,
        Q = 12,
        K = 13,
        
        LENGTH = 14,
    }

    public enum Suit
    {
        Spade,
        Heart,
        Diamond,
        Club,
        
        Length = 4,
    }

    [System.Serializable]
    public struct Card : IComparable<Card>
    {
        public Number number;
        public Suit suit;

        public Card(Number number, Suit suit)
        {
            this.number = number;
            this.suit = suit;
        }

        public override string ToString()
        {
            return suit + " " + number;
        }
        
        public static Card None() => new Card(Number.NONE, Suit.Spade);

        public int CompareTo(Card other)
        {
            int numberComparison = number.GetInt().CompareTo(other.number.GetInt());
            if (numberComparison != 0) return numberComparison;
            return suit.CompareTo(other.suit);
        }
    }
}