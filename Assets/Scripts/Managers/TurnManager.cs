using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ACTOR;
using Util;


public class TurnManager : Singleton<TurnManager>
{
    private List<BettingActor> _actors;
    private CardController _cardController;

    IEnumerator PlayRound()
    {
        // deal card
        // yield return _cardController.DealCard();

        for (int i = 0;; i++)
        {
            var actor = _actors[i % _actors.Count];
            yield return actor.Play();

            if (BettingManager.Instance.BetFinished()) break;
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