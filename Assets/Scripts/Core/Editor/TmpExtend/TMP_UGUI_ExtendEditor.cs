using Core.Runtime.Extension;
using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace Core.Editor.TmpExtend
{
    [CustomEditor(typeof(TMP_UGUI_Extend))]
    public class TMP_UGUI_ExtendEditor : UnityEditor.Editor
    {
        TMP_UGUI_Extend tmpExtend;
        TMP_Text tmpText;


        private void OnEnable()
        {
            tmpExtend = target as TMP_UGUI_Extend;
            if (tmpExtend != null)
            {
                tmpText = tmpExtend.GetComponent<TMP_Text>();
            }
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            Rect rect = EditorGUILayout.GetControlRect(false, 22);
            EditorGUILayout.Space();
            GUI.Label(rect, new GUIContent("<b>阴影设置</b>"), TMP_UIStyleManager.sectionHeader);
            DrawShadowOption();

            EditorGUILayout.Space();
            rect = EditorGUILayout.GetControlRect(false, 22);
            GUI.Label(rect, new GUIContent("<b>描边设置</b>"), TMP_UIStyleManager.sectionHeader);
            DrawOutlineOption();

            EditorGUILayout.Space();
            rect = EditorGUILayout.GetControlRect(false, 22);
            GUI.Label(rect, new GUIContent("<b>超链接设置</b>"), TMP_UIStyleManager.sectionHeader);
            DrawHyperLinkOption();
        }

        /// <summary>
        /// 阴影设置
        /// </summary>
        private void DrawShadowOption()
        {
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            tmpExtend.EnableShadow = EditorGUILayout.Toggle("激活阴影", tmpExtend.EnableShadow);
            if (EditorGUI.EndChangeCheck())
            {
                if (tmpText != null)
                {
                    TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, tmpText.fontSharedMaterial);
                    if (tmpExtend.EnableShadow)
                    {
                        tmpText.fontSharedMaterial.EnableKeyword("UNDERLAY_ON");
                        tmpText.fontSharedMaterial.EnableKeyword("UNDERLAY_INNER");
                        tmpExtend.RefreshShadow();
                    }
                    else
                    {
                        tmpText.fontSharedMaterial.DisableKeyword("UNDERLAY_ON");
                        tmpText.fontSharedMaterial.DisableKeyword("UNDERLAY_INNER");
                    }

                    tmpText.enabled = false;
                    tmpText.enabled = true;
                }
            }

            EditorGUI.BeginChangeCheck();
            tmpExtend.ShadowColor = EditorGUILayout.ColorField("阴影颜色", tmpExtend.ShadowColor);
            tmpExtend.ShadowOffsetX = EditorGUILayout.Slider("阴影X偏移", tmpExtend.ShadowOffsetX, -1f, 1f);
            tmpExtend.ShadowOffsetY = EditorGUILayout.Slider("阴影Y偏移", tmpExtend.ShadowOffsetY, -1f, 1f);
            tmpExtend.ShadowDilate = EditorGUILayout.Slider("阴影宽度", tmpExtend.ShadowDilate, -1f, 1f);
            tmpExtend.ShadowSoftness = EditorGUILayout.Slider("阴影虚化", tmpExtend.ShadowSoftness, 0, 1f);
            if (EditorGUI.EndChangeCheck() && tmpExtend.EnableShadow)
            {
                tmpExtend.RefreshShadow();
                if (tmpText != null)
                {
                    TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, tmpText.fontSharedMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 描边设置
        /// </summary>
        private void DrawOutlineOption()
        {
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            tmpExtend.EnableOutline = EditorGUILayout.Toggle("激活描边", tmpExtend.EnableOutline);
            if (EditorGUI.EndChangeCheck())
            {
                if (tmpText != null)
                {
                    TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, tmpText.fontSharedMaterial);
                    if (tmpExtend.EnableOutline)
                    {
                        tmpText.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
                        tmpExtend.RefreshOutline();
                        if (tmpText != null)
                        {
                            TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, tmpText.fontSharedMaterial);
                        }
                    }
                    else
                    {
                        tmpText.fontSharedMaterial.DisableKeyword("OUTLINE_ON");
                        if (tmpText != null)
                        {
                            tmpText.outlineWidth = 0;
                            TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, tmpText.fontSharedMaterial);
                        }
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            tmpExtend.OutlineColor = EditorGUILayout.ColorField("描边颜色", tmpExtend.OutlineColor);
            tmpExtend.OutlineWidth = EditorGUILayout.Slider("描边宽度", tmpExtend.OutlineWidth, 0, 3);
            if (EditorGUI.EndChangeCheck() && tmpExtend.EnableOutline)
            {
                tmpExtend.RefreshOutline();
                if (tmpText != null)
                {
                    TMPro_EventManager.ON_MATERIAL_PROPERTY_CHANGED(true, tmpText.fontSharedMaterial);
                }
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 超链接设置
        /// </summary>
        private void DrawHyperLinkOption()
        {
            EditorGUILayout.BeginVertical();
            tmpExtend.EnableHyperLink = EditorGUILayout.Toggle("使用超链接", tmpExtend.EnableHyperLink);
            EditorGUILayout.EndVertical();
        }
    }
}