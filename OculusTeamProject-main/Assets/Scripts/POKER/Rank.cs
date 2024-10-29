using System;
using System.Collections.Generic;

namespace POKER
{
    public class Rank
    {
        public HandRank handRank;
        public List<Card> kickers;
        public List<Card> cards;
        
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
    }
}