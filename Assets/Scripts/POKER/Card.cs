namespace POKER
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
    }

    public enum Suit
    {
        SPADE,
        HEART,
        DIAMOND,
        CLUB
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
        
        public static Card None() => new Card(Number.NONE, Suit.SPADE);
    }
}