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

    private int _blindBtn;
    
    [SerializeField] private bool debugMode;
    
    private void Awake()
    {
        _cardController = FindObjectOfType<CardController>();
        
        // 자식 오브젝트의 BettingActor 컴포넌트를 가져옵니다.
        _actors = GetComponentsInChildren<BettingActor>().ToList();
    }
    
    private void Start()
    {
        StartCoroutine(PlayBet());
    }

    private IEnumerator DealCommunityCards()
    {
        yield return _cardController.DealCard(3);
        yield return _cardController.DealCard(1);
        yield return _cardController.DealCard(1);
    }

    private IEnumerator PlayBet()
    {
        BettingManager.Instance.ResetRound();
        foreach (BettingActor actor in _actors)
        {
            actor.ResetRoundBet();
        }
        
        // todo : 현재 For문으로 한번씩 돌면서 Actor들에게 차례를 넘겨주는데, 누군가가 Raise를 했을 때, 다시 한번씩 돌아야함.
        for (int i = 0; i < _actors.Count; i++)
        {
            BettingActor actor = _actors[i % _actors.Count];
            if (actor.CanParticipateInBetting() is false) continue;
            
            // 만약 debug Mode가 켜져있다면, Space를 누를 때까지 기다립니다.
            if (debugMode)
            {
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
            }
            
            yield return actor.Play();

            if (HasBetFinished()) break;
        }
    }

    private bool HasBetFinished()
    {
        return false;
    }

    private List<BettingActor> GetActiveActors()
    {
        return _actors.Where(actor => actor.CanParticipateInBetting()).ToList();
    }
    
    private void FoldCall()
    {
        
    }
}

public enum Round
{
    PRE_FLOP,
    FLOP,
    TURN,
    RIVER
}