using System.Collections;
using UnityEngine;

namespace Actor
{
    // 임시 AI입니다. 자신이 베팅 할 수 있는한 최소한의 금액을 무조건적으로 베팅합니다.
    public class BettingAI : BettingActor
    {
        public override IEnumerator Play()
        {
            Debug.Log($"Player Turn : {gameObject.name}");
            yield return new WaitForSeconds(1f);

            if (CanCheck())
            {
                Raise(10);
                Debug.Log("AI Raised 10");
                yield break;
            }
            
            if (Call() is false)
            {
                Debug.Log("AI Folded");
                Fold();
            }
            else
            {
                Debug.Log("AI Called");
            }
        }
    }
}