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
            this.handRank = handRank;
            this.cards = cards;
            this.kickers = kickers;
        }
    }
}

