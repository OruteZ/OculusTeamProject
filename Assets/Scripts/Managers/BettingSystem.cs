// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Linq;
using Actor;
using Poker;
using UnityEngine;

/// <summary>
/// 각각의 팟 정보를 담는 클래스
/// </summary>
[Serializable]
public class Pot
{
    public int TotalAmount;
    public List<BettingActor> EligiblePlayers = new List<BettingActor>();
    
    public Pot(int initialAmount, List<BettingActor> eligiblePlayers)
    {
        TotalAmount = initialAmount;
        EligiblePlayers = new List<BettingActor>(eligiblePlayers);
    }
}

public class BettingSystem : MonoBehaviour
{
    public static BettingSystem Instance { get; private set; }

    [SerializeField] private int blindAmount = 10;
    [SerializeField] private int _currentBet;
    [SerializeField] private BettingChipVisualizer _potVisualizer;

    // 기존에는 하나의 팟만 관리했지만 이제 메인팟과 사이드팟들을 리스트로 관리
    [SerializeField] private List<Pot> _pots = new List<Pot>();

    // 각 플레이어별 해당 라운드 기여액 추적
    private readonly Dictionary<BettingActor, int> _playerContributions = new Dictionary<BettingActor, int>();

    private bool _lastActionWasRaise;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Check()
    {
        _lastActionWasRaise = false;
    }
    
    public void Fold(BettingActor actor)
    {
        _lastActionWasRaise = false;
    }

    // 새로운 라운드 시작 시 초기화
    public void ResetRound(bool containsBlind = false)
    {
        _currentBet = containsBlind ? blindAmount : 0;
        _lastActionWasRaise = false;
        _playerContributions.Clear();
        _pots.Clear();
        
        // 초기 메인 팟
        _pots.Add(new Pot(_currentBet, TurnSystem.Instance.GetNotFoldedActors()));
        UpdatePotVisualizer();
    }

    public void ResetPot()
    {
        // 라운드 전체가 끝나고 다음 라운드나 다음 핸드로 넘어갈 때 초기화하는 용도
        _playerContributions.Clear();
        _pots.Clear();
        _pots.Add(new Pot(0, TurnSystem.Instance.GetNotFoldedActors()));
        UpdatePotVisualizer();
    }

    /// <summary>
    /// 플레이어가 금액을 배팅할 때 호출.
    /// add는 이번 액션으로 추가되는 금액, amount는 현재까지 그 플레이어가 낸 총 베팅 금액(이번 액션으로 갱신)이라고 가정.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="add"></param>
    /// <param name="newTotalContributionOfPlayer">이번 액션 후 플레이어가 총 낸 금액</param>
    public void Bet(BettingActor player, int add, int newTotalContributionOfPlayer)
    {
        _playerContributions.TryAdd(player, 0);

        // 플레이어의 총 기여액 갱신
        _playerContributions[player] = newTotalContributionOfPlayer;

        // 현재 베팅보다 높은 금액이면 Raise
        _lastActionWasRaise = newTotalContributionOfPlayer > _currentBet;
        if (_lastActionWasRaise)
        {
            _currentBet = newTotalContributionOfPlayer;
        }

        // add만큼의 금액을 팟에 넣어야 하는데, 올인 상황을 가정해보자.
        // 플레이어가 올인으로 인해 사이드 팟을 형성해야 하는지 판단
        DistributeToPots(player);
        
        UpdatePotVisualizer();
    }

    /// <summary>
    /// 플레이어가 낸 금액을 Pots에 분배하는 로직
    /// </summary>
    private void DistributeToPots(BettingActor player)
    {
        // 현재 모든 플레이어의 기여액 목록
        var contributions = _playerContributions.Values.ToList();
        contributions.Sort();

        // 가장 작은 기여액을 기준으로 사이드팟 분리가 일어난다.
        // 예: 어떤 플레이어가 올인해서 최소 기여액이 예전보다 작아졌다면,
        // 해당 최소치로 메인팟을 확정하고, 그 이상 낸 플레이어들에 대해 새로운 사이드팟을 형성

        // Pots를 재정렬하기 전에 현재까지의 최소 기여액들에 따라 팟을 나눈다.
        // 기본적으로 각 팟은 특정 기여액 구간을 담당하게 된다.

        // 현재 모든 플레이어가 낸 금액 중에서 이전에 처리되지 않은 기여액 범위를 찾아 팟 분리
        RecalculatePots();
    }

    /// <summary>
    /// 현재 플레이어 기여액(_playerContributions)을 바탕으로 팟을 재계산하여
    /// 메인팟과 사이드팟을 형성하는 메서드.
    /// </summary>
    private void RecalculatePots()
    {
        // 모든 플레이어 기여액 정렬
        List<BettingActor> activePlayers = TurnSystem.Instance.GetNotFoldedActors();
        List<BettingActor> sortedContributions = activePlayers
            .Where(p => _playerContributions.ContainsKey(p))
            .OrderBy(c => _playerContributions[c])
            .ToList();

        // 기여액이 0인 경우(아직 베팅 안한 플레이어)가 있을 수 있으니 제외
        sortedContributions = sortedContributions.Where(p => _playerContributions[p] > 0).ToList();

        if (sortedContributions.Count == 0)
        {
            // 아직 아무도 베팅 안했으면 초기 상태 그대로
            if (_pots.Count == 0)
                _pots.Add(new Pot(0, TurnSystem.Instance.GetNotFoldedActors()));
            return;
        }

        // 모든 플레이어의 기여액
        Dictionary<BettingActor, int> contributionsMap = new Dictionary<BettingActor, int>(_playerContributions);

        // 새로운 Pots 리스트를 생성하여 다시 빌드
        List<Pot> newPots = new List<Pot>();

        int lastCutOff = 0;
        // 각 구간별 사이드팟 형성
        // 예: 기여액이 [10, 10, 50, 100] 이라면
        // 첫번째 컷오프: 10칩까지는 모든 플레이어가 냈으니 메인팟 형성 (플레이어4명*10=40)
        // 나머지 플레이어 중 10 초과 낸 사람들만 다음 팟 형성
        // 다음 컷오프: 50칩까지 낸 사람들(3명: 10 이미 뺀 상태서 40씩 추가) 사이드팟
        // 다음 컷오프: 100칩까지 낸 사람들(1명만 추가로 50 더) 또 다른 사이드팟
        List<int> uniqueAmounts = sortedContributions.Select(p => contributionsMap[p]).Distinct().ToList();

        // 현재 Eligible Player는 아직 폴드하지 않은 모든 플레이어
        List<BettingActor> eligiblePlayersForPot = activePlayers.ToList();

        foreach (int cutOff in uniqueAmounts)
        {
            int amountForThisPot = 0;
            List<BettingActor> potEligiblePlayers = new List<BettingActor>();

            // 이 구간에 속하는 플레이어 계산
            foreach (BettingActor pl in eligiblePlayersForPot)
            {
                if (!contributionsMap.ContainsKey(pl)) continue;

                // 이 플레이어가 이번 컷오프 이상 냈다면, (cutOff - lastCutOff) 만큼 이번 팟에 기여
                int toAdd = Math.Max(0, Math.Min(contributionsMap[pl], cutOff) - lastCutOff);
                amountForThisPot += toAdd;
            }

            // 이번 팟에 포함되는 플레이어는 최소 lastCutOff 이상 낸 모든 플레이어
            potEligiblePlayers.AddRange(eligiblePlayersForPot);

            // 팟 생성
            Pot newPot = new Pot(amountForThisPot, potEligiblePlayers);
            newPots.Add(newPot);

            lastCutOff = cutOff;

            // 다음 사이드팟에서는 cutoff 금액 이상 못낸 플레이어는 제외
            eligiblePlayersForPot = eligiblePlayersForPot
                .Where(pl => contributionsMap.ContainsKey(pl) && contributionsMap[pl] > cutOff)
                .ToList();
            
            if (eligiblePlayersForPot.Count == 0)
                break;
        }

        _pots = newPots;
    }

    /// <summary>
    /// Check 가능 여부 판정
    /// </summary>
    /// <param name="askingActor"></param>
    /// <returns></returns>
    public bool CanCheck(BettingActor askingActor)
    {
        // 1. 현재 베팅이 0이라면 가능
        if (_currentBet == 0)
        {
            return true;
        }

        Round curRound = TurnSystem.Instance.GetCurrentRound();
        bool isBlindPlayer = TurnSystem.Instance.IsBlindActor(askingActor);
        return curRound == Round.PreFlop && isBlindPlayer;
    }

    public int GetCurrentBet()
    {
        return _currentBet;
    }

    /// <summary>
    /// 모든 팟의 총합
    /// </summary>
    /// <returns></returns>
    public int GetPot()
    {
        return _pots.Sum(p => p.TotalAmount);
    }

    public bool WasLastActionRaise() => _lastActionWasRaise;

    private void UpdatePotVisualizer()
    {
        _potVisualizer.SetMoney(GetPot());
    }

    /// <summary>
    /// 쇼다운 시 각 팟별로 승자 결정 로직이 필요할 수 있음.
    /// 여기서는 단순히 Pot 리스트를 반환하는 메서드를 제공.
    /// </summary>
    public List<Pot> GetAllPots()
    {
        return _pots;
    }

    public int GetBigBlindAmount()
    {
        return blindAmount;
    }
}
