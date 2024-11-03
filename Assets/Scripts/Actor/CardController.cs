using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Actor
{
    public class CardController : MonoBehaviour
    {
        private bool _cardDealing;
        
        public IEnumerator DealCard(int count)
        {
            _cardDealing = true;
            
            yield return new WaitUntil(() => _cardDealing is false);
        }
    }
}
