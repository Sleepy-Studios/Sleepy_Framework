using UnityEngine;
using UnityEngine.UI;

public static class UIExtension
{
    #region Unity UI Extension

    public static void SetAnchoredPositionX(this RectTransform rectTransform, float anchoredPositionX)
    {
        var value = rectTransform.anchoredPosition;
        value.x = anchoredPositionX;
        rectTransform.anchoredPosition = value;
    }

    public static void SetAnchoredPositionY(this RectTransform rectTransform, float anchoredPositionY)
    {
        var value = rectTransform.anchoredPosition;
        value.y = anchoredPositionY;
        rectTransform.anchoredPosition = value;
    }

    public static void SetAnchoredPosition3DZ(this RectTransform rectTransform, float anchoredPositionZ)
    {
        var value = rectTransform.anchoredPosition3D;
        value.z = anchoredPositionZ;
        rectTransform.anchoredPosition3D = value;
    }

    public static void SetColorAlpha(this UnityEngine.UI.Graphic graphic, float alpha)
    {
        var value = graphic.color;
        value.a = alpha;
        graphic.color = value;
    }

    public static void SetFlexibleSize(this LayoutElement layoutElement, Vector2 flexibleSize)
    {
        layoutElement.flexibleWidth = flexibleSize.x;
        layoutElement.flexibleHeight = flexibleSize.y;
    }

    public static Vector2 GetFlexibleSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.flexibleWidth, layoutElement.flexibleHeight);
    }

    public static void SetMinSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.minWidth = size.x;
        layoutElement.minHeight = size.y;
    }

    public static Vector2 GetMinSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.minWidth, layoutElement.minHeight);
    }

    public static void SetPreferredSize(this LayoutElement layoutElement, Vector2 size)
    {
        layoutElement.preferredWidth = size.x;
        layoutElement.preferredHeight = size.y;
    }

    public static Vector2 GetPreferredSize(this LayoutElement layoutElement)
    {
        return new Vector2(layoutElement.preferredWidth, layoutElement.preferredHeight);
    }

    #endregion

    public enum ToastStyle : uint
    {
        Blue = 0,
        Yellow = 1,
        Green = 2,
        Red = 3,
        White = 4
    }
}