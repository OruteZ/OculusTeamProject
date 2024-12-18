// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using Actor;
using Poker;
using UnityEngine;

public class BettingSystem : MonoBehaviour
{
    public static BettingSystem Instance { get; private set; }

    [SerializeField] private int _currentBet;
    [SerializeField] private int _totalPot;
    [SerializeField] private BettingChipVisualizer _potVisualizer;
    
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

    /// <summary>
    /// Pot에 금액을 배팅합니다.
    /// </summary>
    /// <param name="add">추가하는 금액입니다.</param>
    /// <param name="amount">현재 배팅금엑의 수 입니다.</param>
    public void Bet(int add, int amount)
    {
        _lastActionWasRaise = amount > _currentBet;
        if(_lastActionWasRaise)
        {
            _currentBet = amount;
        }
        
        
        _totalPot += add;
        _potVisualizer.SetMoney(_totalPot);
    }

    public void ResetRound()
    {
        _currentBet = 0;
    }

    public bool CanCheck(BettingActor askingActor)
    {
        // 1. 현재 배팅이 0이라면 가능
        if (_currentBet == 0)
        {
            return true;
        }
        
        Round curRound = TurnSystem.Instance.GetCurrentRound();
        bool isBlindPlayer = TurnSystem.Instance.IsBlindPlayer(askingActor);
        if (curRound == Round.PreFlop && isBlindPlayer)
        {
            return true;
        }

        return false;
    }

    public int GetCurrentBet()
    {
        return _currentBet;
    }

    public int GetPot()
    {
        return _totalPot;
    }

    public bool WasLastActionRaise() => _lastActionWasRaise;

    public void ResetPot()
    {
        _totalPot = 0;
        _potVisualizer.SetMoney(_totalPot);
    }
}