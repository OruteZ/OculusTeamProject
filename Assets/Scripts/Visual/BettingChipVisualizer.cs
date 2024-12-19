using System.Collections.Generic;
using UnityEngine;

public class BettingChipVisualizer : MonoBehaviour
{
    [SerializeField] private BettingChipDB bettingChipDB;
    [SerializeField] private int totalMoney;
    [SerializeField] private List<GameObject> bettingChipObjects;
    [SerializeField] private float rad = 0.1f; // 칩 스택의 배치 반지름
    [SerializeField] private float chipHeight = 0.005f; // 칩 하나의 높이
    [SerializeField] private float offsetRange = 0.01f; // 불안정함을 위한 오프셋 범위

    private void Awake()
    {
        if (bettingChipDB == null)
            Debug.LogError("BettingChipDB is null");
    }

    // Reload 메서드를 public으로 변경하고, 파라미터 제거
    [ContextMenu("Reload")]
    private void Reload()
    {
        foreach (GameObject chip in bettingChipObjects)
        {
            DestroyImmediate(chip);
        }
        bettingChipObjects.Clear();

        Dictionary<int, int> chipCount = GetChipCount(totalMoney);
        List<BettingChip> bettingChips = bettingChipDB.BettingChips;

        List<GameObject> newChips = new List<GameObject>();
        int chipTypeIndex = 0;
        int totalTypes = bettingChips.Count;

        foreach (BettingChip chip in bettingChips)
        {
            int value = chip.Value;
            int count = chipCount.GetValueOrDefault(value, 0);
            if (count == 0)
                continue;

            GameObject chipPrefab = chip.ChipPrefab;

            // 각 칩 종류별로 위치 지정
            float angle = chipTypeIndex * Mathf.PI * 2f / totalTypes;
            float x = Mathf.Cos(angle) * rad;
            float z = Mathf.Sin(angle) * rad;
            Vector3 basePosition = new Vector3(x, 0, z);

            for (int i = 0; i < count; i++)
            {
                GameObject newChip = Instantiate(chipPrefab, transform);

                // 불안정함을 위한 x, z 오프셋 추가
                float xOffset = Random.Range(-offsetRange, offsetRange);
                float zOffset = Random.Range(-offsetRange, offsetRange);
                float yPosition = i * chipHeight; // 위로 쌓기

                Vector3 position = basePosition + new Vector3(xOffset, yPosition, zOffset);
                newChip.transform.localPosition = position;

                newChips.Add(newChip);
            }

            chipTypeIndex++;
        }

        bettingChipObjects.AddRange(newChips);
    }
    
    public void SetMoney(int money)
    {
        totalMoney = money;
        Reload();
    }

    private Dictionary<int, int> GetChipCount(int money)
    {
        Dictionary<int, int> chipCount = new Dictionary<int, int>();
        List<BettingChip> bettingChips = bettingChips = bettingChipDB.BettingChips;

        foreach (BettingChip chip in bettingChips)
        {
            int value = chip.Value;
            int count = money / value;
            money -= count * value;
            if (count > 0)
                chipCount.Add(value, count);
        }

        return chipCount;
    }
}
