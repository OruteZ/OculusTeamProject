using System;
using System.Collections;
using UnityEngine;
using Poker; // Card 구조체를 사용한다고 가정

    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class CardObject : MonoBehaviour
    {
        [Header("Card Info")]
        [SerializeField] private Card _card;

        private bool _isFrontShowing;
        
        // 이동 코루틴 중복 방지용
        private Coroutine _moveCoroutine;

        #region Public Interface

        public void Initialize(Card card)
        {
            _card = card;
        }
        
        public Card GetCard() => _card;

        public bool IsFrontShowing() => _isFrontShowing;

        public void ShowFront()
        {
            _isFrontShowing = true;
            // 앞면 보여주는 로직 (Mesh나 Material 교체) 필요시 구현
        }

        public void ShowBack()
        {
            _isFrontShowing = false;
            // 뒷면 보여주는 로직 필요시 구현
        }

        /// <summary>
        /// 카드를 특정 Transform (ex: Hand의 Transform) 위치로 이동시키고 회전도 해당 트랜스폼과 일치시키는 기능.
        /// duration 동안 부드럽게 이동.
        /// </summary>
        /// <param name="targetTransform">목적지 Transform</param>
        /// <param name="duration">이동에 걸리는 시간</param>
        /// <param name="onComplete">이동 완료시 호출되는 콜백</param>
        public void MoveToTransform(Transform targetTransform, float duration, Action onComplete = null)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }
            _moveCoroutine = StartCoroutine(MoveRoutine(targetTransform.position, targetTransform.rotation, duration, onComplete));
        }

        /// <summary>
        /// 카드를 특정 세계 좌표와 회전값으로 이동시킨다.
        /// duration 동안 부드럽게 이동.
        /// </summary>
        /// <param name="targetPosition">목적지 위치(World Space)</param>
        /// <param name="targetRotation">목적지 회전</param>
        /// <param name="duration">이동에 걸리는 시간</param>
        /// <param name="onComplete">이동 완료시 호출되는 콜백</param>
        public void MoveToPosition(Vector3 targetPosition, Quaternion targetRotation, float duration, Action onComplete = null)
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }
            _moveCoroutine = StartCoroutine(MoveRoutine(targetPosition, targetRotation, duration, onComplete));
        }

        #endregion

        #region Private Methods

        private IEnumerator MoveRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration, Action onComplete)
        {
            Vector3 startPos = transform.position;
            Quaternion startRot = transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 선형 보간 대신 Ease-in-Out 곡선 등 원하는 easing 함수를 적용할 수도 있음.
                transform.position = Vector3.Lerp(startPos, targetPosition, t);
                transform.rotation = Quaternion.Slerp(startRot, targetRotation, t);

                yield return null;
            }

            // 이동 완료
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            _moveCoroutine = null;
            onComplete?.Invoke();
        }

        #endregion
    }
