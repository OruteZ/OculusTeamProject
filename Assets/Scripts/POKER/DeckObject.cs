using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using Poker;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

[RequireComponent(typeof(XRGrabInteractable))]
public class DeckObject : MonoBehaviour
{
    [SerializeField] private CardDatabase cardDatabase;
    
    [Header("Deck Layout Settings")]
    [SerializeField] private float cardStackOffset = 0.002f;    // 카드 한 장마다 쌓이는 높이 혹은 z축 오프셋
    [SerializeField] private bool slightAngleVariation = true;
    [SerializeField] private float maxAngleOffset = 2f;         // 카드가 약간 기울어지도록 하는 정도
    
    [Header("Runtime Info")]
    [SerializeField] private CardContainer headingContainer;       // 카드를 나눠줄 대상 Hand
    
    private List<CardObject> _cards = new List<CardObject>();

    // VR 상호작용을 위한 컴포넌트 (예: XRGrabInteractable)
    // 이것이 잡혔을 때 특정 제스처나 버튼 입력으로 카드 슬라이드
    [SerializeField] private XRGrabInteractable grabInteractable; // 필요시

    private void Awake()
    {
        TryGetComponent(out grabInteractable);
        
        InitializeDeck(cardDatabase.GetAllCards());
    }

    /// <summary>
    /// 덱에 카드들을 초기 셋업한다.
    /// 주로 게임 시작 시 DeckController나 CardController에서 카드를 Instantiate한 뒤 이 메서드에 넘겨줄 수 있다.
    /// </summary>
    public void InitializeDeck(List<CardObject> initialCards)
    {
        // _card에 있는 모든 obj 삭제
        foreach (CardObject card in _cards) if (card != null) DestroyImmediate(card.gameObject);
        _cards.Clear();
        
        _cards = initialCards;
        foreach (CardObject card in _cards)
        {
            card.transform.SetParent(this.transform, worldPositionStays: false);
        }
        
        UpdateDeckLayout();
    }

    
    [ContextMenu("Initialize Deck")]
    private void EditorInitialize()
    {
        InitializeDeck(cardDatabase.GetAllCards());
    }

    /// <summary>
    /// 외부에서 요청 시 덱을 셔플한다.
    /// Shuffle 후 UpdateDeckLayout으로 카드들의 시각적 배치 갱신.
    /// </summary>
    public void Shuffle()
    {
        for (int i = 0; i < _cards.Count / 2; i++)
        {
            int rnd = Random.Range(i, _cards.Count);
            (_cards[rnd], _cards[i]) = (_cards[i], _cards[rnd]);
        }

        UpdateDeckLayout();
    }

    /// <summary>
    /// 덱의 카드 오브젝트들을 스택 형태로 정렬.
    /// 카드 개수에 따라 Y 또는 Z 방향으로 Offset을 주어 위로 쌓은 것처럼 표현 가능.
    /// </summary>
    private void UpdateDeckLayout()
    {
        // 덱에 남아있는 카드 수에 따라 카드들을 스택 형태로 정렬
        // 예: 맨 아래 카드부터 시작해서 위로 쌓음
        for (int i = 0; i < _cards.Count; i++)
        {
            CardObject card = _cards[i];
            if (card == null) continue;
            
            card.transform.SetParent(transform, worldPositionStays: false);

            // 카드 위치: 덱의 transform을 기준으로 위로 쌓이게 (예: localPosition y 증가)
            Vector3 pos = new Vector3(0, i * cardStackOffset, 0); // 카드가 뒤로 쌓이는 형태
            Quaternion rot = Quaternion.identity * Quaternion.Euler(90, 0, 0); // 카드가 뒤집혀있는 형태

            // 약간의 랜덤 각도 변화를 주어 자연스러운 느낌
            if (slightAngleVariation)
            {
                float angle = Random.Range(-maxAngleOffset, maxAngleOffset);
                rot *= Quaternion.Euler(0, angle, 0);
            }

            Transform tsf = card.transform;
            tsf.localPosition = pos;
            tsf.localRotation = rot;
        }
    }

    /// <summary>
    /// 덱에 남아있는 카드 수 반환
    /// </summary>
    public int GetCardCount()
    {
        return _cards.Count;
    }

    /// <summary>
    /// 덱 비우기(필요시)
    /// </summary>
    public void ClearDeck()
    {
        _cards.Clear();
        // 실제 카드 오브젝트 삭제 필요시 처리
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// 특정 조작에 따라서 맨 위에 있는 카드를 바꿔치기 합니다.
    /// </summary>
    /// <param name="newCard"></param>
    /// <returns></returns>
    public CardObject PopTopCard(CardObject newCard = null)
    {
        if (_cards.Count == 0) return null;
        
        CardObject topCard = _cards.FindLast(card => card != null);
        _cards.Remove(topCard);
        
        if(newCard == null) _cards.Add(newCard);
        
        UpdateDeckLayout();
        return topCard;
    }

    private void RayCastToCardContainer()
    {
        // 1. 만약 들고있는 경우가 아니라면 return
        if (!grabInteractable.isSelected) return;
        
        grabInteractable.interactorsHovering.Clear();

        // 2. Raycast를 통해 CardContainer를 찾아 dealingTarget으로 할당
        // CardContainer에 LayerMask 적용
        Transform tsf = transform;
        LayerMask mask = LayerMask.GetMask("CardContainer");
        
        if (Physics.Raycast(tsf.position, tsf.forward, out RaycastHit hit, 50f, mask))
        {
            hit.transform.TryGetComponent(out headingContainer);
        }
        else
        {
            headingContainer = null;
        }
    }

    private void Update()
    {
        if (!grabInteractable.isSelected) return;
        
        RayCastToCardContainer();
    }
    
    public CardContainer GetHeadingContainer()
    {
        return headingContainer;
    }
}
