using System;

namespace POKER
{
    public class Rank
    {
        public HandRank handRank;
        public Card[] cards;
        public Card firstKicker;
        public Card secondKicker;
        
        public Rank(HandRank handRank, Card[] cards, Card firstKicker, Card secondKicker)
        {
            if (cards.Length != 5)
            {
                throw new Exception("Invalid number of cards. Expected 5, got " + cards.Length);
            }
            
            this.handRank = handRank;
            this.cards = cards;
            this.firstKicker = firstKicker;
            this.secondKicker = secondKicker;
        }
    }
}

