using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Core.Runtime.DOTweenSequence
{
    public class DoTweenSequence : MonoBehaviour
    {
        [HideInInspector] [SerializeField] SequenceAnimation[] Sequence;
        [SerializeField] bool PlayOnAwake = false;
        [SerializeField] float Delay = 0;
       [SerializeField] Ease Ease = Ease.OutQuad;
       [SerializeField] int Loops = 1;
        [SerializeField] LoopType LoopType = LoopType.Restart;
       [SerializeField] UpdateType UpdateType = UpdateType.Normal;

       [SerializeField] bool IgnoreTimeScale = false;
        [SerializeField] UnityEvent OnPlay = null;
       [SerializeField] UnityEvent OnUpdate = null;
        [SerializeField] UnityEvent OnComplete = null;

        private Tween mTween;

        private void Awake()
        {
            InitTween();
            if (PlayOnAwake) DoPlay();
        }

        private void InitTween()
        {
            foreach (var item in Sequence)
            {
                var useFromValue = item.UseFromValue;
                if (!useFromValue) continue;
                var targetCom = item.Target;
                var resetValue = item.FromValue;
                switch (item.AnimationType)
                {
                    case DoTweenType.DoMove:
                    {
                        (targetCom as Transform).position = resetValue;
                        break;
                    }
                    case DoTweenType.DoMoveX:
                    {
                        (targetCom as Transform).SetPositionX(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoMoveY:
                    {
                        (targetCom as Transform).SetPositionY(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoMoveZ:
                    {
                        (targetCom as Transform).SetPositionZ(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoLocalMove:
                    {
                        (targetCom as Transform).localPosition = resetValue;
                        break;
                    }
                    case DoTweenType.DoLocalMoveX:
                    {
                        (targetCom as Transform).SetLocalPositionX(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoLocalMoveY:
                    {
                        (targetCom as Transform).SetLocalPositionY(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoLocalMoveZ:
                    {
                        (targetCom as Transform).SetLocalPositionZ(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoAnchorPos:
                    {
                        (targetCom as RectTransform).anchoredPosition = resetValue;
                        break;
                    }
                    case DoTweenType.DoAnchorPosX:
                    {
                        (targetCom as RectTransform).SetAnchoredPositionX(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoAnchorPosY:
                    {
                        (targetCom as RectTransform).SetAnchoredPositionY(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoAnchorPosZ:
                    {
                        (targetCom as RectTransform).SetAnchoredPosition3Dz(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoAnchorPos3D:
                    {
                        (targetCom as RectTransform).anchoredPosition3D = resetValue;
                        break;
                    }
                    case DoTweenType.DoColor:
                    {
                        (targetCom as UnityEngine.UI.Graphic).color = resetValue;
                        break;
                    }
                    case DoTweenType.DoFade:
                    {
                        (targetCom as UnityEngine.UI.Graphic).SetColorAlpha(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoCanvasGroupFade:
                    {
                        (targetCom as UnityEngine.CanvasGroup).alpha = resetValue.x;
                        break;
                    }
                    case DoTweenType.DoValue:
                    {
                        (targetCom as UnityEngine.UI.Slider).value = resetValue.x;
                        break;
                    }
                    case DoTweenType.DoSizeDelta:
                    {
                        (targetCom as RectTransform).sizeDelta = resetValue;
                        break;
                    }
                    case DoTweenType.DoFillAmount:
                    {
                        (targetCom as UnityEngine.UI.Image).fillAmount = resetValue.x;
                        break;
                    }
                    case DoTweenType.DoFlexibleSize:
                    {
                        (targetCom as LayoutElement).SetFlexibleSize(resetValue);
                        break;
                    }
                    case DoTweenType.DoMinSize:
                    {
                        (targetCom as LayoutElement).SetMinSize(resetValue);
                        break;
                    }
                    case DoTweenType.DoPreferredSize:
                    {
                        (targetCom as LayoutElement).SetPreferredSize(resetValue);
                        break;
                    }
                    case DoTweenType.DoScale:
                    {
                        (targetCom as Transform).localScale = resetValue;
                        break;
                    }
                    case DoTweenType.DoScaleX:
                    {
                        (targetCom as Transform).SetLocalScaleX(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoScaleY:
                    {
                        (targetCom as Transform).SetLocalScaleY(resetValue.x);
                        break;
                    }
                    case DoTweenType.DoScaleZ:
                    {
                        (targetCom as Transform).SetLocalScaleZ(resetValue.z);
                        break;
                    }
                    case DoTweenType.DoRotate:
                    {
                        (targetCom as Transform).eulerAngles = resetValue;
                        break;
                    }
                    case DoTweenType.DoLocalRotate:
                    {
                        (targetCom as Transform).localEulerAngles = resetValue;
                        break;
                    }
                }
            }
        }

        private Tween CreateTween(bool reverse = false)
        {
            if (Sequence == null || Sequence.Length == 0)
            {
                return null;
            }

            var sequence = DOTween.Sequence();
            if (reverse)
            {
                for (int i = Sequence.Length - 1; i >= 0; i--)
                {
                    var item = Sequence[i];
                    var tweener = item.CreateTween(reverse);
                    if (tweener == null)
                    {
                        Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i,
                            item.AnimationType, item.Target == null ? "null" : item.Target.GetType().Name);
                        continue;
                    }

                    switch (item.AddType)
                    {
                        case AddType.Append:
                            sequence.Append(tweener);
                            break;
                        case AddType.Join:
                            sequence.Join(tweener);
                            break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Sequence.Length; i++)
                {
                    var item = Sequence[i];
                    var tweener = item.CreateTween(reverse);
                    if (tweener == null)
                    {
                        Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i,
                            item.AnimationType, item.Target == null ? "null" : item.Target.GetType().Name);
                        continue;
                    }

                    switch (item.AddType)
                    {
                        case AddType.Append:
                            sequence.Append(tweener);
                            break;
                        case AddType.Join:
                            sequence.Join(tweener);
                            break;
                    }
                }
            }

            sequence.SetEase(Ease).SetUpdate(UpdateType, IgnoreTimeScale).SetLoops(Loops, LoopType)
                .SetDelay(Delay);
            if (OnPlay != null) sequence.OnPlay(OnPlay.Invoke);
            if (OnUpdate != null) sequence.OnUpdate(OnUpdate.Invoke);
            if (OnComplete != null) sequence.OnComplete(OnComplete.Invoke);
            sequence.SetAutoKill(true);
            return sequence;
        }

        public void Play()
        {
            DoPlay();
        }

        public Tween DoPlay()
        {
            mTween = CreateTween();
            return mTween?.Play();
        }

        public Tween DoRewind()
        {
            mTween = CreateTween(true);
            return mTween?.Play();
        }

        public void DoComplete(bool withCallback = false)
        {
            mTween?.Complete(withCallback);
        }

        public void DoKill()
        {
            mTween?.Kill();
            mTween = null;
        }

        public enum DoTweenType
        {
            DoMove,
            DoMoveX,
            DoMoveY,
            DoMoveZ,

            DoLocalMove,
            DoLocalMoveX,
            DoLocalMoveY,
            DoLocalMoveZ,

            DoScale,
            DoScaleX,
            DoScaleY,
            DoScaleZ,

            DoRotate,
            DoLocalRotate,

            DoAnchorPos,
            DoAnchorPosX,
            DoAnchorPosY,
            DoAnchorPosZ,
            DoAnchorPos3D,


            DoColor,
            DoFade,
            DoCanvasGroupFade,
            DoFillAmount,
            DoFlexibleSize,
            DoMinSize,
            DoPreferredSize,
            DoSizeDelta,
            DoValue
        }

        [Serializable]
        public class SequenceAnimation
        {
            public AddType AddType = AddType.Append;
            public DoTweenType AnimationType = DoTweenType.DoMove;
            public Component Target = null;
            public Vector4 ToValue = Vector4.zero;

            public bool UseToTarget = false;
            public Component ToTarget = null;

            public bool UseFromValue = false;
            public Vector4 FromValue = Vector4.zero;
            public bool SpeedBased = false;
            public float DurationOrSpeed = 1;
            public float Delay = 0;
            public UpdateType UpdateType = UpdateType.Normal;
            public bool CustomEase = false;
            public AnimationCurve EaseCurve;
            public Ease Ease = Ease.OutQuad;
            public int Loops = 1;
            public LoopType LoopType = LoopType.Restart;
            public bool Snapping = false;
            public UnityEvent OnPlay = null;
            public UnityEvent OnUpdate = null;
            public UnityEvent OnComplete = null;

            public Tween CreateTween(bool reverse)
            {
                Tween result = null;
                float duration = this.DurationOrSpeed;

                switch (AnimationType)
                {
                    case DoTweenType.DoMove:
                    {
                        var transform = Target as Transform;
                        Vector3 targetValue = UseToTarget ? (ToTarget as Transform).position : ToValue;
                        Vector3 startValue = UseFromValue ? FromValue : transform.position;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.position = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOMove(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoMoveX:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.SetPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveX(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoMoveY:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.SetPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveY(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoMoveZ:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.SetPositionZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveZ(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoLocalMove:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : transform.localPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.localPosition = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMove(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoLocalMoveX:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.SetLocalPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveX(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoLocalMoveY:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.SetLocalPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveY(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoLocalMoveZ:
                    {
                        var transform = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        transform.SetLocalPositionZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveZ(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoScale:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.localScale;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.localScale = startValue;
                        if (SpeedBased) duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOScale(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoScaleX:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.SetLocalScaleX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleX(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoScaleY:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.SetLocalScaleY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleY(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoScaleZ:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localScale.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.localScale.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.SetLocalScaleZ(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOScaleZ(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoRotate:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).eulerAngles : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.eulerAngles;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.eulerAngles = startValue;
                        if (SpeedBased)
                            duration = GetEulerAnglesAngle(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DORotate(targetValue, duration, RotateMode.FastBeyond360);
                    }
                        break;
                    case DoTweenType.DoLocalRotate:
                    {
                        var com = Target as Transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localEulerAngles : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : com.localEulerAngles;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.localEulerAngles = startValue;
                        if (SpeedBased)
                            duration = GetEulerAnglesAngle(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOLocalRotate(targetValue, duration, RotateMode.FastBeyond360);
                    }
                        break;
                    case DoTweenType.DoAnchorPos:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : rectTransform.anchoredPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        rectTransform.anchoredPosition = startValue;
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoAnchorPosX:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        rectTransform.SetAnchoredPositionX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPosX(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoAnchorPosY:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.y;
                        if (reverse)
                        {
                            var swapValue = startValue;
                            startValue = targetValue;
                            targetValue = swapValue;
                        }

                        rectTransform.SetAnchoredPositionY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPosY(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoAnchorPosZ:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition3D.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        rectTransform.SetAnchoredPosition3Dz(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos3DZ(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoAnchorPos3D:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D : (Vector3)ToValue;
                        var startValue = UseFromValue ? (Vector3)FromValue : rectTransform.anchoredPosition3D;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        rectTransform.anchoredPosition3D = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOAnchorPos3D(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoSizeDelta:
                    {
                        var rectTransform = Target as RectTransform;
                        var targetValue = UseToTarget ? (ToTarget as RectTransform).sizeDelta : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : rectTransform.sizeDelta;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        rectTransform.sizeDelta = startValue;
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = rectTransform.DOSizeDelta(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoColor:
                    {
                        var com = Target as UnityEngine.UI.Graphic;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Graphic).color : (Color)ToValue;
                        var startValue = UseFromValue ? (Color)FromValue : com.color;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.color = startValue;
                        if (SpeedBased)
                            duration = Vector4.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOColor(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoFade:
                    {
                        var com = Target as UnityEngine.UI.Graphic;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Graphic).color.a : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.color.a;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.SetColorAlpha(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFade(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoCanvasGroupFade:
                    {
                        var com = Target as UnityEngine.CanvasGroup;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.CanvasGroup).alpha : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.alpha;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.alpha = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFade(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoValue:
                    {
                        var com = Target as UnityEngine.UI.Slider;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Slider).value : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.value;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.value = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOValue(targetValue, duration, Snapping);
                    }
                        break;

                    case DoTweenType.DoFillAmount:
                    {
                        var com = Target as UnityEngine.UI.Image;
                        var targetValue = UseToTarget ? (ToTarget as UnityEngine.UI.Image).fillAmount : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : com.fillAmount;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.fillAmount = startValue;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = com.DOFillAmount(targetValue, duration);
                    }
                        break;
                    case DoTweenType.DoFlexibleSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetFlexibleSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetFlexibleSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.SetFlexibleSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOFlexibleSize(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoMinSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetMinSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetMinSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.SetMinSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOMinSize(targetValue, duration, Snapping);
                    }
                        break;
                    case DoTweenType.DoPreferredSize:
                    {
                        var com = Target as LayoutElement;
                        var targetValue = UseToTarget ? (ToTarget as LayoutElement).GetPreferredSize() : (Vector2)ToValue;
                        var startValue = UseFromValue ? (Vector2)FromValue : com.GetPreferredSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }

                        com.SetPreferredSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = com.DOPreferredSize(targetValue, duration, Snapping);
                    }
                        break;
                }

                if (result != null)
                {
                    result.SetAutoKill(true).SetTarget(Target.gameObject).SetLoops(Loops, LoopType).SetUpdate(UpdateType);
                    if (Delay > 0) result.SetDelay(Delay);
                    if (CustomEase) result.SetEase(EaseCurve);
                    else result.SetEase(Ease);

                    if (OnPlay != null) result.OnPlay(OnPlay.Invoke);
                    if (OnUpdate != null) result.OnUpdate(OnUpdate.Invoke);
                    if (OnComplete != null) result.OnComplete(OnComplete.Invoke);
                }

                return result;
            }

            public static float GetEulerAnglesAngle(Vector3 euler1, Vector3 euler2)
            {
                // 计算差值
                Vector3 delta = euler2 - euler1;
                delta.x = Mathf.DeltaAngle(euler1.x, euler2.x);
                delta.y = Mathf.DeltaAngle(euler1.y, euler2.y);
                delta.z = Mathf.DeltaAngle(euler1.z, euler2.z);

                float angle = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                return (angle + 360) % 360;
            }
        }

        public enum AddType
        {
            Append,
            Join
        }
    }
}