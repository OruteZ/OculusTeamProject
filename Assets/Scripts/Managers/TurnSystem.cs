using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Actor;
using JetBrains.Annotations;
using Poker;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Util;

public class TurnSystem : MonoBehaviour
{
    public static TurnSystem Instance { get; private set; }
    
    private CardController _cardController;
    [SerializeField] private List<BettingActor> actors;
    [SerializeField] private BettingActor currentPlayer;
    [SerializeField] private int blindBtn;

    [SerializeField] private bool debugMode;
    
    [Header("Important Card Containers")]
    [SerializeField] private CardContainer communityCardContainer;
    [SerializeField] private DeckObject dealersDeck;
    
    private Round _currentRound;
    
    public UnityEvent OnRoundEnd 
    {
        get; 
        private set;
    }
    
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
        OnRoundEnd = new UnityEvent();

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
        while (true)
        {
            BettingSystem.Instance.ResetPot();
        
            // BigBline Actor 세팅
            blindBtn++;
            blindBtn %= actors.Count;
            
            var blindActor = actors[blindBtn];
            blindActor.BlindBet(BettingSystem.Instance.GetBigBlindAmount());
            
        
            // 1. Pre-Flop: 각 플레이어에게 2장씩 카드 지급
            yield return DealHoleCards();
            yield return new WaitForSeconds(1f);
        
            // Pre-Flop 베팅
            _currentRound = Round.PreFlop;
            yield return PlayBetRound(true);
        
        
            yield return new WaitForSeconds(1f);

            // 베팅 결과 플레이어가 2명이상 남아있는지 확인
            if (GetBetableActors().Count > 1)
            {
                // 2. Flop: 커뮤니티 카드 3장 오픈
                _currentRound = Round.Flop;
                yield return DealFlop();
                yield return PlayBetRound();
            }
        
            yield return new WaitForSeconds(1f);

            if (GetBetableActors().Count > 1)
            {
                // 3. Turn: 커뮤니티 카드 1장 추가
                _currentRound = Round.Turn;
                yield return DealTurn();
                yield return PlayBetRound();
            }
        
            yield return new WaitForSeconds(1f);

            if (GetBetableActors().Count > 1)
            {
                // 4. River: 커뮤니티 카드 1장 추가
                _currentRound = Round.River;
                yield return DealRiver();
                yield return PlayBetRound();
            }
        
            yield return new WaitForSeconds(1f);
        
            // 두 사람 이상 참여했는데 만약 커뮤니티 카드가 5장보다 적다면 해당 수만큼 추가로 덱에서 뽑아서 오픈
            if (communityCardContainer.GetCards().Count < 5 && GetNotFoldedActors().Count > 1)
            {
                yield return _cardController.OrderDealing(communityCardContainer, 5 - communityCardContainer.GetCards().Count);
            }

            // 쇼다운 로직 (핸드 랭킹 비교, 승자 결정)
            _currentRound = Round.Showdown;
            Showdown();
        
            // 초기화
            BettingSystem.Instance.ResetPot();
            OnRoundEnd.Invoke();
            
            // Game 종료 : 돈이 다 떨어진 플레이어는 actors에서 완전 제외
            actors = actors.Where(actor => actor.GetMoney() > 0).ToList();
            if (actors.Count < 2)
            {
                break;
            }
        }
        
        // DetermineWinner();
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

    private IEnumerator PlayBetRound(bool isPreFlop = false)
    {
        BettingSystem.Instance.ResetRound(isPreFlop);
        foreach (BettingActor actor in actors)
        {
            actor.ResetRoundBet();
        }

        List<BettingActor> activeActors = GetBetableActors();
        int startIndex = blindBtn;
        bool hasRaise = true;

        // while 루프: 레이즈 발생시 다음 사이클로 넘어감
        while (hasRaise && activeActors.Count > 0)
        {
            hasRaise = false;
        
            // 한 사이클 동안 모든 액티브 플레이어에게 액션 기회 부여
            for (int i = 0; i < activeActors.Count; i++)
            {
                yield return new WaitForSeconds(1f);
            
                BettingActor actor = activeActors[(startIndex + i) % activeActors.Count];
                currentPlayer = actor;
                if (!actor.IsBetable()) continue;

                if (debugMode)
                {
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                }

                // 플레이어 액션
                yield return actor.Play();
                Debug.Log("Actor Player Finished");
                Debug.Log("--------------------");

                // 레이즈 발생 시 다음 사이클 진행
                if (BettingSystem.Instance.WasLastActionRaise())
                {
                    Debug.Log("Raise Detected");
                    
                    int raiserIndex = (startIndex + i) % activeActors.Count;
                    startIndex = (raiserIndex + 1) % activeActors.Count;
                    hasRaise = true;
                    break;
                }
                
                // 만약 Fold하지 않은 플레이어가 단 한명이거나, 모두 올인 or Fold일 경우 배팅 라운드 강제 종료
                int nonFoldedCnt = GetNotFoldedActors().Count;
                int betableCnt = GetBetableActors().Count;
                Debug.Log($"NonFolded: {nonFoldedCnt}, Betable: {betableCnt}");
                
                if (nonFoldedCnt < 1 || betableCnt <= 1)
                {
                    break;
                }
            }

            // 한 사이클 종료 후, 레이즈가 없었다면 종료 조건 재확인
            if (!hasRaise && HasBetFinished())
            {
                break; // 베팅 라운드 종료
            }

            // 액티브 플레이어 목록 갱신
            activeActors = GetBetableActors();
        }
    }



    private bool HasBetFinished()
    {
        // 베팅 종료 조건:
        // 1. 모든 액터가 콜 또는 체크로 동일한 베팅 액수에 도달했거나
        // 2. 한 명 이상의 액터만 남아서 베팅 의사가 없거나
        // 이 경우 베팅 라운드 종료.

        // 간단한 판정 로직 예:
        if (GetNotFoldedActors().Count <= 1)
        {
            return true;
        }
        
        List<BettingActor> activeActors = GetBetableActors();
        int currentBet = BettingSystem.Instance.GetCurrentBet();
        // 모든 액터가 currentBet에 맞춰 콜 또는 체크한 상태인지 확인
        return activeActors.All(actor => actor.GetCurRoundBet() == currentBet);
        // 여기까지 왔으면 모두가 currentBet에 맞춰서 정렬된 상태
    }

    private List<BettingActor> GetBetableActors()
    {
        return actors.Where(actor => actor.IsBetable()).ToList();
    }

   private void Showdown()
{
    // 사이드 팟을 포함한 모든 팟을 가져온다.
    List<Pot> pots = BettingSystem.Instance.GetAllPots();

    // 커뮤니티 카드
    List<Card> communityCards = communityCardContainer.GetCards();

    int potIdx = 0;
    // 각 팟에 대해 승자를 결정
    foreach (Pot pot in pots)
    {
        Debug.Log($"Pot {++potIdx} with {pot.TotalAmount} chips");
        
        // 해당 팟에 참여 자격이 있는 플레이어 목록(폴드하지 않았고, 올인이나 체크 상관없이 여전히 핸드 유지 중)
        List<BettingActor> eligiblePlayers = pot.EligiblePlayers
            .Where(p => !p.GetHasFolded()) // 폴드한 플레이어 제외
            .ToList();
        
        // eligiblePlayers가 없을 경우(모두 폴드), pot은 그냥 홀딩되거나(실제 게임 룰에선 거의 없음) 무승부 처리가 될 수 있음.
        // 여기서는 eligiblePlayers가 없으면 그냥 다음 팟으로 넘어감
        if (eligiblePlayers.Count == 0)
        {
            continue;
        }
        
        if (eligiblePlayers.Count == 1)
        {
            // 한 명만 남은 경우, 해당 플레이어에게 팟 전체를 줌
            BettingActor winner = eligiblePlayers[0];
            winner.AddMoney(pot.TotalAmount);

            Debug.Log("All other players folded");
            Debug.Log($"Winner: {winner.name}");
            Debug.Log($"Split Amount: {pot.TotalAmount}");
            continue;
        }

        // 각 플레이어의 핸드 랭크를 구하고 최고 랭크 비교
        Rank bestRank = null;
        List<BettingActor> winners = new List<BettingActor>();

        foreach (BettingActor actor in eligiblePlayers)
        {
            Debug.Log($"Actor: {actor.name}");
            
            Rank currentRank = RankTracker.GetPossibleMaxRank(actor.GetContainer().GetCards(), communityCards);
            if (currentRank == null)
            {
                Debug.LogWarning("Rank is null");
                continue;
            }
            Debug.Log($"Rank: {currentRank}");

            if (bestRank == null || currentRank.CompareTo(bestRank) > 0)
            {
                // 더 높은 랭크 발견 시 갱신
                bestRank = currentRank;
                winners.Clear();
                winners.Add(actor);
            }
            else if (currentRank.CompareTo(bestRank) == 0)
            {
                // 랭크 동률 발생 시, 우승자 리스트에 추가
                winners.Add(actor);
            }
        }

        // 우승자들에게 팟 분배
        int potAmount = pot.TotalAmount;
        int splitAmount = potAmount / winners.Count;

        foreach (BettingActor winner in winners)
        {
            Debug.Log($"Winner: {winner.name}");
            Debug.Log($"Split Amount: {splitAmount}");
            
            winner.AddMoney(splitAmount);
        }
    }
}


    public List<Card> GetCommunityCards()
    {
        return communityCardContainer.GetCards();
    }
    
    public Round GetCurrentRound()
    {
        return _currentRound;
    }
    
    public bool IsBlindActor(BettingActor actor)
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

    public List<BettingActor> GetNotFoldedActors()
    {
        return actors.Where(
            actor => !actor.GetHasFolded())
            .ToList();
    }

    public CardContainer GetCommunityCardContainer()
    {
        return communityCardContainer;
    }

    public DeckObject GetDeck()
    {
        return dealersDeck;
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
