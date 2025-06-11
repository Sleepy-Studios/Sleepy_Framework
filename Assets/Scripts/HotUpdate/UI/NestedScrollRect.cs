using Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate
{
    public class NestedScrollRect : ScrollRect
    {
        private ScrollRect parentScrollRect;
        private bool? isHandledByThis = null; // 使用可空布尔值表示未初始化状态

        protected override void Awake()
        {
            base.Awake();
            parentScrollRect = transform.parent.GetComponentInParent<ScrollRect>();
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            // 重置状态
            isHandledByThis = null;

            // 确定拖拽方向并锁定
            bool shouldHandleByThis = (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y) && horizontal) ||
                                      (Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x) && vertical);
            isHandledByThis = shouldHandleByThis;

            if (shouldHandleByThis)
            {
                base.OnBeginDrag(eventData);
                if (parentScrollRect != null)
                    parentScrollRect.OnEndDrag(eventData);
            }
            else if (parentScrollRect != null)
            {
                parentScrollRect.OnBeginDrag(eventData);
            }
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (isHandledByThis == true)
            {
                base.OnDrag(eventData);
            }
            else if (isHandledByThis == false && parentScrollRect != null)
            {
                parentScrollRect.OnDrag(eventData);
            }
            else
            {
                Log.Info("重新判断拖拽方向");
                // 如果尚未确定（理论上不应该发生），重新判断一次
                isHandledByThis = (Mathf.Abs(eventData.delta.x) > Mathf.Abs(eventData.delta.y) && horizontal) ||
                                  (Mathf.Abs(eventData.delta.y) > Mathf.Abs(eventData.delta.x) && vertical);
                OnDrag(eventData);
            }
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (isHandledByThis == true)
            {
                base.OnEndDrag(eventData);
            }
            else if (isHandledByThis == false && parentScrollRect != null)
            {
                parentScrollRect.OnEndDrag(eventData);
            }

            // 重置状态
            isHandledByThis = null;
        }
    }
}