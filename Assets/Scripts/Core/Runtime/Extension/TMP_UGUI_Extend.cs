using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Runtime.Extension
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TMP_UGUI_Extend : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text tmpText;

        #region 超链接参数

        [SerializeField]
        private bool EEnableHyperLink;
        /// <summary>
        /// 启用超链接
        /// </summary>
        public bool EnableHyperLink { get => EEnableHyperLink; set => EEnableHyperLink = value; }
        public List<Action<string>> HyperLinkClickActions;

        #endregion

        #region 阴影参数

        [SerializeField]
        public bool EnableShadow;
        [SerializeField]
        public Color ShadowColor = Color.black;
        [SerializeField]
        [Range(-1, 1)]
        public float ShadowOffsetX = 0.2f;
        [SerializeField]
        [Range(-1, 1)]
        public float ShadowOffsetY = -0.2f;
        [SerializeField]
        [Range(-1, 1)]
        public float ShadowDilate;
        [SerializeField]
        [Range(0, 1)]
        public float ShadowSoftness;

        #endregion

        #region 描边参数

        [SerializeField]
        public bool EnableOutline;
        [SerializeField]
        public Color OutlineColor = Color.blue;
        [SerializeField]
        [Range(0, 5)]
        public float OutlineWidth = 1;

        #endregion

        private void Awake()
        {
            tmpText = GetComponent<TMP_Text>();
            HyperLinkClickActions ??= new List<Action<string>>(2);
            RefreshShadow();
            RefreshOutline();
        }

        private void OnDestroy()
        {
            tmpText = null;
            if (HyperLinkClickActions != null)
            {
                HyperLinkClickActions.Clear();
                HyperLinkClickActions = null;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (EEnableHyperLink)
            {
                if (HyperLinkClickActions == null) return;
                int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, Input.mousePosition, null);
                if (linkIndex != -1)
                {
                    TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
                    Action<string> callBack = null;
                    if (HyperLinkClickActions.Count <= 0)
                        callBack = Application.OpenURL;
                    else if (linkIndex >= HyperLinkClickActions.Count) callBack = HyperLinkClickActions[0];
                    else callBack = HyperLinkClickActions[linkIndex];
                    callBack?.Invoke(linkInfo.GetLinkID());
                }
            }
        }

        public void SetShadowState()
        {
            TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, tmpText.fontSharedMaterial);
            if (EnableShadow)
            {
                tmpText.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
                tmpText.fontSharedMaterial.EnableKeyword("UNDERLAY_INNER");
                RefreshShadow();
            }
            else
            {
                tmpText.fontSharedMaterial.DisableKeyword("UNDERLAY_ON");
                tmpText.fontSharedMaterial.DisableKeyword("UNDERLAY_INNER");
            }
        }

        /// <summary>
        /// 刷新阴影数据
        /// </summary>
        public void RefreshShadow()
        {
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
            if (!tmpText || !EnableShadow) return;

            // 直接使用材质属性ID设置阴影参数，而不是尝试通过TextMeshPro属性
            Material material = tmpText.fontSharedMaterial;
            if (material != null)
            {
                int underlayColorID = Shader.PropertyToID("_UnderlayColor");
                int underlayOffsetXid = Shader.PropertyToID("_UnderlayOffsetX");
                int underlayOffsetYid = Shader.PropertyToID("_UnderlayOffsetY");
                int underlayDilateID = Shader.PropertyToID("_UnderlayDilate");
                int underlaySoftnessID = Shader.PropertyToID("_UnderlaySoftness");

                material.SetColor(underlayColorID, ShadowColor);
                material.SetFloat(underlayOffsetXid, ShadowOffsetX);
                material.SetFloat(underlayOffsetYid, ShadowOffsetY);
                material.SetFloat(underlayDilateID, ShadowDilate);
                material.SetFloat(underlaySoftnessID, ShadowSoftness);

                // 确保更新生效
                tmpText.UpdateMeshPadding();
            }
        }

        /// <summary>
        /// 刷新描边数据
        /// </summary>
        public void RefreshOutline()
        {
            if (tmpText == null)
                tmpText = GetComponent<TMP_Text>();

            if (tmpText == null || !EnableOutline) return;

            tmpText.outlineColor = this.OutlineColor;
            if (OutlineWidth > 3)
                OutlineWidth = 3;
            tmpText.outlineWidth = this.OutlineWidth / 6f;
        }
    }
}