// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using Poker;
using UnityEngine;

public class BettingManager : MonoBehaviour
{
    public static BettingManager Instance { get; private set; }

    [SerializeField] private int _currentBet;
    [SerializeField] private int _totalPot;
    [SerializeField] private List<Card> _communityCards;
    
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
        _currentBet = Mathf.Max(_currentBet, amount);
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

    public List<Card> GetCommunityCards()
    {
        return _communityCards;
    }
}