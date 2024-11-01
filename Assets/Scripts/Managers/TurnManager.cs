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

    IEnumerator PlayRound()
    {
        // deal card
        // yield return _cardController.DealCard();

        for (int i = 0;; i++)
        {
            var actor = _actors[i % _actors.Count];
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