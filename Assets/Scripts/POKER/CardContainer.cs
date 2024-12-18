using System.Collections.Generic;
using System.Linq;
using Poker;
using UnityEngine;

public class CardContainer : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private float cardSpacing = 0.05f; // 카드 간 간격(로컬 x축)
    [SerializeField] private float fanAngle = 10f; // 카드들이 펼쳐질 때 회전 각도
    [SerializeField] private Vector3 centerOffset = Vector3.zero; // 핸드 중앙 오프셋
    
    private readonly List<CardObject> _cards = new List<CardObject>();

    /// <summary>
    /// 손에 카드를 추가한다.
    /// CardObject는 이미 Instantiate 된 상태로 넘어온다고 가정.
    /// </summary>
    public void AddCard(CardObject card)
    {
        // 카드의 parent를 Hand로 설정
        card.transform.SetParent(this.transform, worldPositionStays: false);
        _cards.Add(card);
        UpdateCardPositions();
    }

    /// <summary>
    /// 해당 카드를 손에서 제거한다.
    /// </summary>
    public void RemoveCard(CardObject card)
    {
        if (!_cards.Remove(card)) return;
        
        card.transform.SetParent(null); // 핸드로부터 분리
        UpdateCardPositions();
    }

    /// <summary>
    /// 모든 카드를 제거한다.
    /// </summary>
    public void Clear()
    {
        foreach (CardObject c in _cards)
        {
            c.transform.SetParent(null);
        }
        _cards.Clear();
        // 굳이 UpdateCardPositions 필요 없음(카드 없으니)
    }

    /// <summary>
    /// 현재 손에 있는 카드들을 재배치한다.
    /// </summary>
    private void UpdateCardPositions()
    {
        int count = _cards.Count;
        if (count == 0) return;

        // 중앙을 기준으로 카드 배치
        // 예: 카드가 N장일 때, 중앙 인덱스를 기준으로 좌우로 퍼짐.
        // 인덱스 (0 ... N-1), 중앙 인덱스 mid = (N-1)/2.0f
        float mid = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            CardObject card = _cards[i];
            float indexOffset = i - mid; 
            
            // x축으로 cardSpacing만큼 펼쳐지고, fanAngle을 중심으로 회전
            float xPos = indexOffset * cardSpacing;
            float angle = -indexOffset * fanAngle; // 중앙 기준으로 각도 변동

            // 카드의 위치와 회전 설정
            Vector3 cardPos = centerOffset + new Vector3(xPos, 0f, 0f);
            Quaternion cardRot = Quaternion.Euler(0f, angle, 0f);

            Transform tsf = card.transform;
            tsf.localPosition = cardPos;
            tsf.localRotation = cardRot;
        }
    }

    /// <summary>
    /// 현재 손에 있는 카드 목록 반환
    /// </summary>
    public List<CardObject> GetCardObjects()
    {
        return _cards.ToList();
    }
    
    public List<Card> GetCards()
    {
        return _cards.Select(card => card.GetCard()).ToList();
    }
    
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 기존 매트릭스 백업
        Matrix4x4 oldMatrix = Gizmos.matrix;

        // 기즈모 색상 설정
        Gizmos.color = Color.green;

        // 회전값 설정 (transform.rotation.y 값만을 Euler로 추출)
        Quaternion rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    
        // 행렬 설정: 위치, 회전, 스케일
        Gizmos.matrix = Matrix4x4.TRS(transform.position + centerOffset, rotation, Vector3.one);

        // 로컬 좌표계 기준으로 중앙에 큐브를 그립니다.
        // Gizmos.matrix가 미리 변환됐기 때문에 여기서는 Vector3.zero를 사용
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(5 * cardSpacing, 0.1f, 0.1f));

        // 매트릭스 복원
        Gizmos.matrix = oldMatrix;
    }
    
    private void OnValidate()
    {
        UpdateCardPositions();
    }
#endif
}
