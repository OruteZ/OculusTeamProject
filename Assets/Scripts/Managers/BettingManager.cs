// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using Poker;
using UnityEngine;

public class BettingManager : MonoBehaviour
{
    public static BettingManager Instance { get; private set; }

    private int _currentBet;
    private int _totalPot;
    
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Bet(int add)
    {
        _currentBet += add;
        _totalPot += add;
    }

    public void ResetRound()
    {
        _currentBet = 0;
    }

    public bool CanCheck()
    {
        // 1. 현재 배팅이 0이라면 가능
        if (_currentBet == 0)
        {
            return true;
        }
        // todo : 빅 블라인드 플레이어라면 가능

        return false;
    }

    public int GetCurrentBet()
    {
        return _currentBet;
    }
}