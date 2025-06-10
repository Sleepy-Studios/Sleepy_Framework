using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HotUpdate.GameUtils
{
    /// <summary>
    /// 公用的UI工具类
    /// </summary>
    public static class UIUtil
    {
        #region 颜色设置

        /// <summary>
        /// 设置Text文字颜色
        /// </summary>
        /// <param name="text">Text组件</param>
        /// <param name="colorHex">16进制颜色值</param>
        public static void SetTextHexColor(Text text, string colorHex)
        {
            if (text == null) return;
            ColorUtility.TryParseHtmlString(colorHex, out var color);
            text.color = color;
        }

        /// <summary>
        /// 设置TextMeshProUGUI文字颜色
        /// </summary>
        /// <param name="text">TextMeshProUGUI组件</param>
        /// <param name="colorHex">16进制颜色值</param>
        public static void SetTextHexColor(TextMeshProUGUI text, string colorHex)
        {
            if (text == null) return;
            ColorUtility.TryParseHtmlString(colorHex, out var color);
            text.color = color;
        }

        /// <summary>
        /// 获取16进制颜色值对应的Color
        /// </summary>
        /// <param name="colorHex">16进制颜色值</param>
        /// <returns>Color颜色</returns>
        public static Color GetHexColor(string colorHex)
        {
            ColorUtility.TryParseHtmlString(colorHex, out var color);
            return color;
        }

        /// <summary>
        /// 设置图片颜色
        /// </summary>
        /// <param name="img">Image组件</param>
        /// <param name="colorHex">16进制颜色值</param>
        public static void SetImageHexColor(Image img, string colorHex)
        {
            if (img == null) return;
            ColorUtility.TryParseHtmlString(colorHex, out var color);
            img.color = color;
        }

        #endregion

        #region 文本处理

        /// <summary>
        /// 设置带最大宽度限制的Text文本
        /// </summary>
        /// <param name="text">Text组件</param>
        /// <param name="content">文本内容</param>
        /// <param name="maxWidth">最大宽度</param>
        public static void SetMaxContentText(Text text, string content, float maxWidth)
        {
            if (text == null) return;
            var contentSizeFilter = text.GetComponent<ContentSizeFitter>();
            contentSizeFilter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            text.text = content;
            contentSizeFilter.SetLayoutHorizontal();
            if (text.preferredWidth > maxWidth)
            {
                contentSizeFilter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                var rect = text.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(maxWidth, rect.sizeDelta.y);
            }
        }

        /// <summary>
        /// 设置带最大宽度限制的TextMeshProUGUI文本
        /// </summary>
        /// <param name="text">TextMeshProUGUI组件</param>
        /// <param name="content">文本内容</param>
        /// <param name="maxWidth">最大宽度</param>
        public static void SetMaxContentText(TextMeshProUGUI text, string content, float maxWidth)
        {
            if (text == null) return;
            var contentSizeFilter = text.GetComponent<ContentSizeFitter>();
            contentSizeFilter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            text.text = content;
            contentSizeFilter.SetLayoutHorizontal();
            if (text.preferredWidth > maxWidth)
            {
                contentSizeFilter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                var rect = text.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(maxWidth, rect.sizeDelta.y);
            }
        }

        /// <summary>
        /// 将整型转换成科学计数法形式(K/M)
        /// </summary>
        public static string GetStandardFromNum(int num)
        {
            string result;
            if (num >= 1000000)
            {
                // 直接将整数转换为浮点数后再进行除法，避免精度丢失
                float a = num / 1000000f;
                result = $"{a:N1}M";
            }
            else if (num >= 100000)
            {
                // 直接将整数转换为浮点数后再进行除法，避免精度丢失
                float a = num / 1000f;
                result = $"{a:N1}K";
            }
            else
            {
                result = num.ToString();
            }

            return result;
        }

        /// <summary>
        /// 将长整型转换成扩展科学计数法形式(M/B/T)
        /// </summary>
        public static string GetStandardFromNumTwo(long value)
        {
            if (value > 9999999 && value <= 999999999) // 10M-999M
            {
                float result = (value / 100000f) / 10f;
                return result + "M";
            }

            if (value > 999999999 && value <= 999999999999) // 1B-999B
            {
                float result = (value / 100000000f) / 10f;
                return result + "B";
            }

            if (value > 999999999999 && value <= 999999999999999) // 1T-999T
            {
                float result = (value / 100000000000f) / 10f;
                return result + "T";
            }

            return value.ToString();
        }

        /// <summary>
        /// 为文本添加富文本颜色标签
        /// </summary>
        public static string ColorText(string text, string colorHex)
        {
            return $"<color={colorHex}>{text}</color>";
        }

        /// <summary>
        /// 为文本添加富文本大小标签
        /// </summary>
        public static string SizeText(string text, int size)
        {
            return $"<size={size}>{text}</size>";
        }

        #endregion

        #region 动画与可见性

        /// <summary>
        /// 重置Animator状态
        /// </summary>
        public static void ResetAnimator(Animator animator, string animName)
        {
            if (animator == null) return;
            animator.Play(animName, 0, 0);
            animator.Update(0);
            animator.enabled = false;
        }

        /// <summary>
        /// 通用弹出动画
        /// </summary>
        public static void CommonPopUpAnimation(Transform transform)
        {
            if (transform == null) return;
            transform.localScale = Vector3.zero;
            DOTween.Kill(transform);
            transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutExpo);
        }

        /// <summary>
        /// 通过CanvasGroup控制对象可见性
        /// </summary>
        public static void SetActiveByCanvasGroup(this GameObject gameObj, bool isVisible)
        {
            if (gameObj == null) return;
            var canvasGroup = gameObj.GetOrAddComponent<CanvasGroup>();
            canvasGroup.alpha = isVisible ? 1 : 0;
            canvasGroup.blocksRaycasts = isVisible;
            canvasGroup.interactable = isVisible;
        }

        /// <summary>
        /// 检查并通过CanvasGroup控制对象可见性
        /// </summary>
        public static void SetActiveByCanvasGroupAndCheck(this GameObject gameObj, bool isVisible)
        {
            if (gameObj == null) return;
            if (!gameObj.activeSelf && isVisible)
            {
                gameObj.SetActive(true);
            }

            SetActiveByCanvasGroup(gameObj, isVisible);
        }

        /// <summary>
        /// 通过缩放控制对象可见性
        /// </summary>
        public static void SetActiveByScale(this GameObject gameObj, bool isVisible)
        {
            if (gameObj == null) return;
            gameObj.transform.localScale = isVisible ? Vector3.one : Vector3.zero;
        }

        /// <summary>
        /// 通过缩放控制对象可见性，指定原始缩放值
        /// </summary>
        public static void SetActiveByScale(this GameObject gameObj, bool isVisible, Vector3 scale)
        {
            if (gameObj == null) return;
            gameObj.transform.localScale = isVisible ? scale : Vector3.zero;
        }

        private static Vector3 outOfScreenPos = new Vector3(12000f, 12000f, 12000f);
        private static Dictionary<int, Vector3> outOfScreenPosDic = new Dictionary<int, Vector3>();

        /// <summary>
        /// 通过位置控制对象可见性
        /// </summary>
        public static void SetActiveByPos(this GameObject gameObj, bool isVisible)
        {
            if (gameObj == null) return;
            var hash = gameObj.GetInstanceID();
            if (!outOfScreenPosDic.ContainsKey(hash))
            {
                outOfScreenPosDic.Add(hash, gameObj.transform.localPosition);
            }

            if (isVisible)
            {
                gameObj.transform.localPosition = outOfScreenPosDic[hash];
                outOfScreenPosDic.Remove(hash);
            }
            else
                gameObj.transform.localPosition += outOfScreenPos;
        }

        /// <summary>
        /// 淡入UI元素
        /// </summary>
        public static void FadeIn(this CanvasGroup canvasGroup, float duration = 0.5f, Action onComplete = null)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            DOTween.Kill(canvasGroup);
            canvasGroup.DOFade(1f, duration).OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// 淡出UI元素
        /// </summary>
        public static void FadeOut(this CanvasGroup canvasGroup, float duration = 0.5f, Action onComplete = null)
        {
            if (canvasGroup == null) return;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            DOTween.Kill(canvasGroup);
            canvasGroup.DOFade(0f, duration).OnComplete(() => onComplete?.Invoke());
        }

        #endregion

        #region UI组件操作

        /// <summary>
        /// 获取或添加组件
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null) return null;
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        /// <summary>
        /// 设置图片
        /// </summary>
        public static void SetSprite(this Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.color = sprite == null ? new Color(1, 1, 1, 0) : new Color(1, 1, 1, 1);
        }

        /// <summary>
        /// 为按钮添加点击事件
        /// </summary>
        public static void AddButtonClick(this Button button, UnityAction onClick)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
        }

        /// <summary>
        /// 根据路径查找子对象
        /// </summary>
        public static Transform FindChildRecursively(this Transform parent, string childName)
        {
            if (parent == null) return null;

            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform found = FindChildRecursively(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// 获取子对象组件
        /// </summary>
        public static T GetComponentInChildren<T>(this GameObject parent, string childName) where T : Component
        {
            if (parent == null) return null;

            Transform child = parent.transform.FindChildRecursively(childName);
            if (child == null) return null;

            return child.GetComponent<T>();
        }

        /// <summary>
        /// 设置圆形图片填充量
        /// </summary>
        public static void SetImageFillAmount(this Image image, float fillAmount)
        {
            if (image == null) return;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillAmount = Mathf.Clamp01(fillAmount);
        }

        /// <summary>
        /// 设置水平布局组间距
        /// </summary>
        public static void SetHorizontalSpacing(this HorizontalLayoutGroup layout, float spacing)
        {
            if (layout == null) return;
            layout.spacing = spacing;
        }

        /// <summary>
        /// 设置垂直布局组间距
        /// </summary>
        public static void SetVerticalSpacing(this VerticalLayoutGroup layout, float spacing)
        {
            if (layout == null) return;
            layout.spacing = spacing;
        }

        #endregion

        #region UI事件扩展

        /// <summary>
        /// 添加拖拽事件
        /// </summary>
        public static void AddDragEvents(this GameObject go, Action<PointerEventData> onBeginDrag = null,
            Action<PointerEventData> onDrag = null,
            Action<PointerEventData> onEndDrag = null)
        {
            if (go == null) return;

            EventTrigger trigger = go.GetOrAddComponent<EventTrigger>();

            if (onBeginDrag != null)
            {
                EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
                beginDragEntry.eventID = EventTriggerType.BeginDrag;
                beginDragEntry.callback.AddListener(data => { onBeginDrag((PointerEventData)data); });
                trigger.triggers.Add(beginDragEntry);
            }

            if (onDrag != null)
            {
                EventTrigger.Entry dragEntry = new EventTrigger.Entry();
                dragEntry.eventID = EventTriggerType.Drag;
                dragEntry.callback.AddListener(data => { onDrag((PointerEventData)data); });
                trigger.triggers.Add(dragEntry);
            }

            if (onEndDrag != null)
            {
                EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
                endDragEntry.eventID = EventTriggerType.EndDrag;
                endDragEntry.callback.AddListener(data => { onEndDrag((PointerEventData)data); });
                trigger.triggers.Add(endDragEntry);
            }
        }

        /// <summary>
        /// 添加鼠标悬停事件
        /// </summary>
        public static void AddPointerEvents(this GameObject go, Action<PointerEventData> onPointerEnter = null,
            Action<PointerEventData> onPointerExit = null,
            Action<PointerEventData> onPointerDown = null,
            Action<PointerEventData> onPointerUp = null)
        {
            if (go == null) return;

            EventTrigger trigger = go.GetOrAddComponent<EventTrigger>();

            if (onPointerEnter != null)
            {
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerEnter;
                entry.callback.AddListener(data => { onPointerEnter((PointerEventData)data); });
                trigger.triggers.Add(entry);
            }

            if (onPointerExit != null)
            {
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerExit;
                entry.callback.AddListener(data => { onPointerExit((PointerEventData)data); });
                trigger.triggers.Add(entry);
            }

            if (onPointerDown != null)
            {
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerDown;
                entry.callback.AddListener(data => { onPointerDown((PointerEventData)data); });
                trigger.triggers.Add(entry);
            }

            if (onPointerUp != null)
            {
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerUp;
                entry.callback.AddListener(data => { onPointerUp((PointerEventData)data); });
                trigger.triggers.Add(entry);
            }
        }

        #endregion
    }
}