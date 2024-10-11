namespace POKER
{
    public enum Number
    {
        None,
        A = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        J = 11,
        Q = 12,
        K = 13,
    }

    public enum Suit
    {
        Spade,
        Heart,
        Diamond,
        Club
    }

    public struct Card
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
        
        public static Card None() => new Card(Number.None, Suit.Spade);
    }
}