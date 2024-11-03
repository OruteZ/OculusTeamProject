using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Actor;
using UnityEngine;
using Util;


public class TurnManager : MonoBehaviour
{
    private CardController _cardController;
    [SerializeField] private List<BettingActor> _actors;
    [SerializeField] private BettingManager _bettingManager;

    private int blindBtn;
    
    IEnumerator DealCommunityCards()
    {
        yield return _cardController.DealCard(3);
        yield return _cardController.DealCard(1);
        yield return _cardController.DealCard(1);
    }

    IEnumerator PlayBet()
    {
        _bettingManager.ResetRound();
        foreach (BettingActor actor in _actors)
        {
            actor.ResetRoundBet();
        }
        
        for (int i = 0;; i++)
        {
            BettingActor actor = _actors[i % _actors.Count];
            if (actor.CanParticipateInBetting() is false) continue;
            
            yield return actor.Play();

            if (_bettingManager.BetFinished()) break;
        }
    }
}

public enum Round
{
    PRE_FLOP,
    FLOP,
    TURN,
    RIVER
}