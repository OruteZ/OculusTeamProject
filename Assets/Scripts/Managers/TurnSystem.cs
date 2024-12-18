using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Actor;
using JetBrains.Annotations;
using Poker;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }
    
    private CardController _cardController;
    [SerializeField] private List<BettingActor> actors;
    [SerializeField] private int blindBtn;

    [SerializeField] private bool debugMode;
    [SerializeField] private CardContainer communityCardContainer;
    private Round _currentRound;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        _cardController = FindObjectOfType<CardController>();
        actors = FindObjectsByType<BettingActor>(FindObjectsSortMode.None).ToList();

        if (communityCardContainer == null)
        {
            throw new System.Exception("CommunityCardContainer가 할당되지 않았습니다.");
        }
        
        blindBtn = 0;
    }
    
    private void OnDestroy()
    {
        Instance = null;
    }
    
    private void Start()
    {
        // ReSharper disable once NotDisposedResource
        StartCoroutine(GameFlow());
    }

    [MustDisposeResource]
    private IEnumerator GameFlow()
    {
        BettingSystem.Instance.ResetPot();
        
        // BigBline Actor 세팅
        blindBtn++;
        blindBtn %= actors.Count;
        
        // 1. Pre-Flop: 각 플레이어에게 2장씩 카드 지급
        yield return DealHoleCards();
        
        yield return new WaitForSeconds(1f);
        
        // Pre-Flop 베팅
        _currentRound = Round.PreFlop;
        yield return PlayBetRound();
        
        
        yield return new WaitForSeconds(1f);

        // 베팅 결과 플레이어가 2명이상 남아있는지 확인
        if (GetActiveActors().Count > 1)
        {
            // 2. Flop: 커뮤니티 카드 3장 오픈
            _currentRound = Round.Flop;
            yield return DealFlop();
            yield return PlayBetRound();
        }
        
        yield return new WaitForSeconds(1f);

        if (GetActiveActors().Count > 1)
        {
            // 3. Turn: 커뮤니티 카드 1장 추가
            _currentRound = Round.Turn;
            yield return DealTurn();
            yield return PlayBetRound();
        }
        
        yield return new WaitForSeconds(1f);

        if (GetActiveActors().Count > 1)
        {
            // 4. River: 커뮤니티 카드 1장 추가
            _currentRound = Round.River;
            yield return DealRiver();
            yield return PlayBetRound();
        }
        
        yield return new WaitForSeconds(1f);

        // 쇼다운 로직 (핸드 랭킹 비교, 승자 결정)
        _currentRound = Round.Showdown;
        Showdown();
    }

    private IEnumerator DealHoleCards()
    {
        // 각 플레이어에게 2장씩 지급
        // CardController의 DealToPlayer(actor, count) 같은 메서드를 가정
        foreach (BettingActor actor in actors)
        {
            yield return _cardController.OrderDealing(actor.GetContainer(), 2);
        }
    }

    private IEnumerator DealFlop()
    {
        // 커뮤니티 카드 3장
        yield return _cardController.OrderDealing(communityCardContainer, 3);
    }

    private IEnumerator DealTurn()
    {
        // 커뮤니티 카드 1장
        yield return _cardController.OrderDealing(communityCardContainer,1);
    }

    private IEnumerator DealRiver()
    {
        // 커뮤니티 카드 1장
        yield return _cardController.OrderDealing(communityCardContainer,1);
    }

    private IEnumerator PlayBetRound()
    {
        // 라운드 초기화
        BettingSystem.Instance.ResetRound();
        foreach (BettingActor actor in actors)
        {
            actor.ResetRoundBet();
        }

        List<BettingActor> activeActors = GetActiveActors();
        int startIndex = blindBtn; // 빅 블라인드 플레이어부터 시작
        bool hasRaise = true;

        // 누군가 레이즈를 하면, 그 시점부터 다음 플레이어를 시작점으로 하여 정확히 한 바퀴를 돈다.
        // hasRaise가 true인 한 반복
        while (hasRaise && activeActors.Count > 0)
        {
            hasRaise = false;

            for (int i = 0; i < activeActors.Count; i++)
            {
                yield return new WaitForSeconds(1f);
                
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
                if (BettingSystem.Instance.WasLastActionRaise())
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

        int currentBet = BettingSystem.Instance.GetCurrentBet();
        // 모든 액터가 currentBet에 맞춰 콜 또는 체크한 상태인지 확인
        return activeActors.All(actor => actor.GetCurRoundBet() == currentBet);

        // 여기까지 왔으면 모두가 currentBet에 맞춰서 정렬된 상태
    }

    private List<BettingActor> GetActiveActors()
    {
        return actors.Where(actor => actor.CanParticipateInBetting()).ToList();
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
        
        List<Card> communityCards = communityCardContainer.GetCards();
        
        BettingActor winner = actors[0];
        Rank winnerRank = RankTracker.GetPossibleMaxRank(winner.GetContainer().GetCards(), communityCards);
        
        foreach (BettingActor actor in actors)
        {
            Rank currentRank = RankTracker.GetPossibleMaxRank(actor.GetContainer().GetCards(), communityCards);

            if (currentRank.CompareTo(winnerRank) <= 0) continue;
            winner = actor;
            winnerRank = currentRank;
        }
        
        // 승자에게 팟 전달
        winner.AddMoney(BettingSystem.Instance.GetPot());
    }

    public List<Card> GetCommunityCards()
    {
        return communityCardContainer.GetCards();
    }
    
    public Round GetCurrentRound()
    {
        return _currentRound;
    }
    
    public bool IsBlindPlayer(BettingActor actor)
    {
        return actors.IndexOf(actor) == blindBtn;
    }

    public int GetActorCount()
    {
        return actors.Count;
    }

    public int GetPosition(BettingActor bettingAI)
    {
        return actors.IndexOf(bettingAI);
    }
}

public enum Round
{
    PreFlop,
    Flop,
    Turn,
    River,
    Showdown
}
