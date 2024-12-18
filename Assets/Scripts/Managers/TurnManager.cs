using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Actor;
using Poker;
using UnityEngine;
using Util;

public class TurnManager : MonoBehaviour
{
    private CardController _cardController;
    [SerializeField] private List<BettingActor> _actors;

    private int _blindBtn;

    [SerializeField] private bool debugMode;
    
    [SerializeField] private CardContainer _communityCardContainer;
    
    private void Awake()
    {
        _cardController = FindObjectOfType<CardController>();
        _actors = GetComponentsInChildren<BettingActor>().ToList();
    }
    
    private void Start()
    {
        StartCoroutine(GameFlow());
    }

    private IEnumerator GameFlow()
    {
        BettingManager.Instance.ResetPot();
        
        // 1. Pre-Flop: 각 플레이어에게 2장씩 카드 지급
        yield return DealHoleCards();
        
        // Pre-Flop 베팅
        yield return PlayBetRound();

        // 베팅 결과 플레이어가 2명이상 남아있는지 확인
        if (GetActiveActors().Count > 1)
        {
            // 2. Flop: 커뮤니티 카드 3장 오픈
            yield return DealFlop();
            yield return PlayBetRound();
        }

        if (GetActiveActors().Count > 1)
        {
            // 3. Turn: 커뮤니티 카드 1장 추가
            yield return DealTurn();
            yield return PlayBetRound();
        }

        if (GetActiveActors().Count > 1)
        {
            // 4. River: 커뮤니티 카드 1장 추가
            yield return DealRiver();
            yield return PlayBetRound();
        }

        // 쇼다운 로직 (핸드 랭킹 비교, 승자 결정)
        Showdown();
    }

    private IEnumerator DealHoleCards()
    {
        // 각 플레이어에게 2장씩 지급
        // CardController의 DealToPlayer(actor, count) 같은 메서드를 가정
        foreach (BettingActor actor in _actors)
        {
            yield return _cardController.DealToContainer(actor.GetContainer(), 2);
        }
    }

    private IEnumerator DealFlop()
    {
        // 커뮤니티 카드 3장
        yield return _cardController.DealToContainer(_communityCardContainer, 3);
    }

    private IEnumerator DealTurn()
    {
        // 커뮤니티 카드 1장
        yield return _cardController.DealToContainer(_communityCardContainer,1);
    }

    private IEnumerator DealRiver()
    {
        // 커뮤니티 카드 1장
        yield return _cardController.DealToContainer(_communityCardContainer,1);
    }

    private IEnumerator PlayBetRound()
    {
        // 라운드 초기화
        BettingManager.Instance.ResetRound();
        foreach (BettingActor actor in _actors)
        {
            actor.ResetRoundBet();
        }

        List<BettingActor> activeActors = GetActiveActors();
        int startIndex = 0; 
        bool hasRaise = true;

        // 누군가 레이즈를 하면, 그 시점부터 다음 플레이어를 시작점으로 하여 정확히 한 바퀴를 돈다.
        // hasRaise가 true인 한 반복
        while (hasRaise && activeActors.Count > 0)
        {
            hasRaise = false;

            for (int i = 0; i < activeActors.Count; i++)
            {
                BettingActor actor = activeActors[(startIndex + i) % activeActors.Count];
                if (actor.CanParticipateInBetting() == false) continue;

                // Debug 모드에서 Space키 대기
                if (debugMode)
                {
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                }

                // 플레이어 액션 수행
                yield return actor.Play();

                // 레이즈가 발생했다면, 해당 플레이어의 다음 플레이어부터 한 바퀴 다시 돈다.
                if (BettingManager.Instance.LastActionWasRaise())
                {
                    // 레이즈를 한 플레이어의 인덱스
                    int raiserIndex = (startIndex + i) % activeActors.Count;

                    // 다음 플레이어를 시작점으로 설정
                    startIndex = (raiserIndex + 1) % activeActors.Count;

                    hasRaise = true;
                    break; // 현재 for 루프 종료 -> while 루프 다음 사이클에서 다시 시작
                }

                // 베팅이 종료 조건에 도달했다면
                if (HasBetFinished())
                {
                    break;
                }
            }

            activeActors = GetActiveActors();
        }
    }


    private bool HasBetFinished()
    {
        // 베팅 종료 조건:
        // 1. 모든 액터가 콜 또는 체크로 동일한 베팅 액수에 도달했거나
        // 2. 한 명 이상의 액터만 남아서 베팅 의사가 없거나
        // 이 경우 베팅 라운드 종료.

        // 간단한 판정 로직 예:
        List<BettingActor> activeActors = GetActiveActors();
        if (activeActors.Count <= 1)
        {
            return true;
        }

        int currentBet = BettingManager.Instance.GetCurrentBet();
        // 모든 액터가 currentBet에 맞춰 콜 또는 체크한 상태인지 확인
        return activeActors.All(actor => actor.GetCurRoundBet() == currentBet);

        // 여기까지 왔으면 모두가 currentBet에 맞춰서 정렬된 상태
    }

    private List<BettingActor> GetActiveActors()
    {
        return _actors.Where(actor => actor.CanParticipateInBetting()).ToList();
    }
    
    private void FoldCall()
    {
        // 필요시 구현
    }

    // 필요하다면 쇼다운 등 추가 로직
    private void Showdown()
    {
        // 남은 플레이어들 중 승자 결정 로직
        // RankTracker 등을 활용하여 승자 결정
        
        var communityCards = BettingManager.Instance.GetCommunityCards();
        
        BettingActor winner = _actors[0];
        Rank winnerRank = RankTracker.GetPossibleMaxRank(winner.GetContainer().GetCards(), communityCards);
        
        foreach (BettingActor actor in _actors)
        {
            Rank currentRank = RankTracker.GetPossibleMaxRank(actor.GetContainer().GetCards(), communityCards);

            if (currentRank.CompareTo(winnerRank) <= 0) continue;
            winner = actor;
            winnerRank = currentRank;
        }
        
        // 승자에게 팟 전달
        winner.AddMoney(BettingManager.Instance.GetPot());
    }
}

public enum Round
{
    PRE_FLOP,
    FLOP,
    TURN,
    RIVER
}
