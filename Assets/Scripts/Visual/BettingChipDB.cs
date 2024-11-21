using System;
using System.Collections.Generic;
using UnityEngine;

public class BettingChipDB : ScriptableObject
{
    [SerializeField] private List<BettingChip> _bettingChips;

    public List<BettingChip> BettingChips => _bettingChips;

    private void OnValidate()
    {
        // 비싼거부터 정렬
        _bettingChips.Sort((a, b) => b.Value.CompareTo(a.Value));
    }
}

[System.Serializable]
public class BettingChip
{
    [SerializeField] private int _value;
    [SerializeField] private GameObject _chipPrefab;

    public int Value => _value;
    public GameObject ChipPrefab => _chipPrefab;
}