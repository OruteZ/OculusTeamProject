using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BettingChipVisualizer : MonoBehaviour
{
    [SerializeField] private BettingChipDB bettingChipDB;
    [SerializeField] private int totalMoney;
    [SerializeField] private List<GameObject> bettingChipObjects;
    
    private void Awake()
    {
        if(bettingChipDB == null)
            Debug.LogError("BettingChipDB is null");
    }

    private void Reload(int money)
    {
        this.totalMoney = money;
        foreach (GameObject chip in bettingChipObjects)
        {
            Destroy(chip);
        }
        bettingChipObjects.Clear();
        
        Dictionary<int, int> chipCount = GetChipCount(money);
        List<BettingChip> bettingChips = bettingChipDB.BettingChips;
        
        foreach (BettingChip chip in bettingChips)
        {
            int value = chip.Value;
            int count = chipCount[value];
            for (int i = 0; i < count; i++)
            {
                GameObject chipPrefab = chip.ChipPrefab;
                bettingChipObjects.Add(Instantiate(chipPrefab, transform));
            }
        }
    }
    
    private Dictionary<int, int> GetChipCount(int money)
    {
        Dictionary<int, int> chipCount = new Dictionary<int, int>();
        List<BettingChip> bettingChips = bettingChipDB.BettingChips;
        
        foreach (BettingChip chip in bettingChips)
        {
            int value = chip.Value;
            int count = money / value;
            money -= count * value;
            chipCount.Add(value, count);
        }

        return chipCount;
    }
}