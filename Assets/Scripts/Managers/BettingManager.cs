// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using Poker;
using UnityEngine;

public class BettingManager : MonoBehaviour
{
    public static BettingManager Instance { get; private set; }

    public int currentBet;
    public int totalBet;
    
    public int totalPot;
    
    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Bet(int amount)
    {
        if (amount < currentBet)
        {
            Debug.LogError("Invalid Bet amount");
            return;
        }
        
        currentBet = amount;
        totalBet += amount;
        totalPot += amount;
    }
    
    public bool CanBet(int amount)
    {
        return amount >= currentBet;
    }

    public void ResetRound()
    {
        currentBet = 0;
        totalBet = 0;
    }

    public bool CanCheck()
    {
        // 1. 현재 배팅이 0이라면 가능
        if (currentBet == 0)
        {
            return true;
        }
        // todo : 빅 블라인드 플레이어라면 가능

        return false;
    }

    public bool BetFinished()
    {
        // todo : 연속 몇개의 배팅을 받았는지에 대해서 판단
        return false;
    }
}