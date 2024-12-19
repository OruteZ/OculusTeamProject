using System;
using System.Collections.Generic;
using System.Linq;
using Poker;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Poker/CardDatabase")]
public class CardDatabase : ScriptableObject
{
    [System.Serializable]
    public struct CardEntry
    {
        public Suit suit;
        public Number number;
        public GameObject cardPrefab;
    }

    [SerializeField] private CardEntry[] cardObjEntries;
    private Dictionary<(Suit, Number), GameObject> _cardDict;

    public void OnEnable()
    {
        #if UNITY_EDITOR
        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        #endif
        _cardDict = new Dictionary<(Suit, Number), GameObject>();
        foreach (CardEntry entry in cardObjEntries)
        {
            _cardDict[(entry.suit, entry.number)] = entry.cardPrefab;
        }
    }

    public GameObject GetPrefab(Suit suit, Number number)
    {
        cardObjEntries = cardObjEntries ?? Array.Empty<CardEntry>();
        
        foreach (CardEntry entry in cardObjEntries)
        {
            if (entry.suit == suit && entry.number == number)
            {
                return entry.cardPrefab;
            }
        }
        
        return null;
    }
    
    public List<CardObject> GetAllCards()
    {
        // 모든 Prefab을 생성하고 CardObject로 변환
        List<CardObject> cards = new ();
        foreach (CardEntry entry in cardObjEntries)
        {
            GameObject cardPrefab = Instantiate(entry.cardPrefab);
            CardObject cardObject = cardPrefab.GetComponent<CardObject>();
            // null일경우 추가
            if (cardObject == null)
            {
                cardObject = cardPrefab.AddComponent<CardObject>();
            }

            cardObject.Initialize(new Card(entry.number, entry.suit));
            cards.Add(cardObject);
        }
        
        return cards;
    }
    
    #if UNITY_EDITOR
    [Header("Editor Settings")]
    [Tooltip("Spade, Heart, Diamond, Club 순서로 에셋 위치")]
    [SerializeField] private string[] cardPrefabPath;
    
    [ContextMenu("Create Database")]
    private void CreateDatabase()
    {
        if (cardPrefabPath.Length != 4)
        {
            Debug.LogError("Card Prefab Path must have 4 elements.");
            return;
        }
        
        // resources에서 load할 경우 문자열 순서되로 정렬되어 다음과 같은 순서를 가짐
        Number[] numbers =
        {
            Number.X,
            Number.II,
            Number.III,
            Number.IV,
            Number.V,
            Number.VI,
            Number.VII,
            Number.VIII,
            Number.IX,
            Number.A,
            Number.J,
            Number.K,
            Number.Q
        };
        
        cardObjEntries = new CardEntry[52];
        for (int i = 0; i < 4; i++)
        {
            Suit suit = (Suit) i;
            string path = cardPrefabPath[i];
            GameObject[] prefabs = Resources.LoadAll<GameObject>(path);
            if(prefabs.Length != 13) 
            {
                Debug.LogError("Card Prefab Path must have 13 elements.");
                Debug.LogError($"Suit: {suit}, Prefab Count: {prefabs.Length}");
                return;
            }
            
            for (int j = 0; j < 13; j++)
            {
                Number number = numbers[j];
                GameObject prefab = prefabs[j];
                cardObjEntries[i * 13 + j] = new CardEntry
                {
                    suit = suit,
                    number = number,
                    cardPrefab = prefab
                };
            }
        }
        
        System.Array.Sort(cardObjEntries, (a, b) =>
        {
            int suitComp = a.suit.CompareTo(b.suit);
            return suitComp != 0 ? suitComp : a.number.CompareTo(b.number);
        });
    }
    
    #endif
}