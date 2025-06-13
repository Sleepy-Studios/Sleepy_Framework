using Core.Runtime.DOTweenSequence;
using DG.DOTweenEditor;
using DG.Tweening;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core.Editor.DOTweenSequence
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(DoTweenSequence))]
    public class DoTweenSequenceInspector : UnityEditor.Editor
    {
        SerializedProperty mSequence;
        ReorderableList mSequenceList;

        GUIContent mPlayBtnContent;
        GUIContent mRewindBtnContent;
        GUIContent mResetBtnContent;
        private GUILayoutOption mBtnHeight;

        private void OnEnable()
        {
            mPlayBtnContent = EditorGUIUtility.TrIconContent("d_PlayButton@2x", "播放");
            mRewindBtnContent = EditorGUIUtility.TrIconContent("d_preAudioAutoPlayOff@2x", "倒放");
            mResetBtnContent = EditorGUIUtility.TrIconContent("d_preAudioLoopOff@2x", "重置");
            mBtnHeight = GUILayout.Height(35);
            mSequence = serializedObject.FindProperty("Sequence");
            mSequenceList = new ReorderableList(serializedObject, mSequence);
            mSequenceList.drawElementCallback = OnDrawSequenceItem;
            mSequenceList.elementHeightCallback = index =>
            {
                var item = mSequence.GetArrayElementAtIndex(index);
                return EditorGUI.GetPropertyHeight(item);
            };
            mSequenceList.drawHeaderCallback = OnDrawSequenceHeader;
            mSequenceList.onAddCallback = OnAddSequenceItem;
        }

        private void OnAddSequenceItem(ReorderableList list)
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;
            var element = list.serializedProperty.GetArrayElementAtIndex(index);
            // 设置默认值
            element.FindPropertyRelative("DurationOrSpeed").floatValue = 1f;
        }

        public override void OnInspectorGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(mPlayBtnContent, mBtnHeight))
                    {
                        if (DOTweenEditorPreview.isPreviewing)
                        {
                            DOTweenEditorPreview.Stop(true, true);
                            (target as DoTweenSequence).DoKill();
                        }

                        DOTweenEditorPreview.PrepareTweenForPreview((target as DoTweenSequence).DoPlay());
                        DOTweenEditorPreview.Start();
                    }

                    if (GUILayout.Button(mRewindBtnContent, mBtnHeight))
                    {
                        if (DOTweenEditorPreview.isPreviewing)
                        {
                            DOTweenEditorPreview.Stop(true, true);
                            (target as DoTweenSequence).DoKill();
                        }

                        DOTweenEditorPreview.PrepareTweenForPreview((target as DoTweenSequence).DoRewind());
                        DOTweenEditorPreview.Start();
                    }

                    if (GUILayout.Button(mResetBtnContent, mBtnHeight))
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        (target as DoTweenSequence).DoKill();
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            serializedObject.Update();
            mSequenceList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // 监听场景关闭事件
            EditorSceneManager.sceneClosed += OnSceneClosed;
            // 监听播放模式状态变化
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnSceneClosed(Scene scene)
        {
            // 当场景关闭时，停止 DOTween 编辑器预览
            DOTweenEditorPreview.Stop(true, true);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            // 当退出播放模式时，停止 DOTween 编辑器预览
            if (stateChange == PlayModeStateChange.ExitingPlayMode)
            {
                DOTweenEditorPreview.Stop(true, true);
            }
        }
#endif

        private void OnDrawSequenceHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Animation Sequences");
        }

        private void OnDrawSequenceItem(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = mSequence.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(rect, element, true);
        }
    }

    [CustomPropertyDrawer(typeof(DoTweenSequence.SequenceAnimation))]
    public class SequenceTweenMoveDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var onPlay = property.FindPropertyRelative("OnPlay");
            var onUpdate = property.FindPropertyRelative("OnUpdate");
            var onComplete = property.FindPropertyRelative("OnComplete");
            return EditorGUIUtility.singleLineHeight * 11 + (property.isExpanded
                ? (EditorGUI.GetPropertyHeight(onPlay) + EditorGUI.GetPropertyHeight(onUpdate) +
                   EditorGUI.GetPropertyHeight(onComplete))
                : 0);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.indentLevel++;
            var target = property.FindPropertyRelative("Target");
            var addType = property.FindPropertyRelative("AddType");
            var tweenType = property.FindPropertyRelative("AnimationType");
            var toValue = property.FindPropertyRelative("ToValue");
            var useToTarget = property.FindPropertyRelative("UseToTarget");
            var toTarget = property.FindPropertyRelative("ToTarget");
            var useFromValue = property.FindPropertyRelative("UseFromValue");
            var fromValue = property.FindPropertyRelative("FromValue");
            var duration = property.FindPropertyRelative("DurationOrSpeed");
            var speedBased = property.FindPropertyRelative("SpeedBased");
            var delay = property.FindPropertyRelative("Delay");
            var customEase = property.FindPropertyRelative("CustomEase");
            var ease = property.FindPropertyRelative("Ease");
            var easeCurve = property.FindPropertyRelative("EaseCurve");
            var loops = property.FindPropertyRelative("Loops");
            var loopType = property.FindPropertyRelative("LoopType");
            var updateType = property.FindPropertyRelative("UpdateType");
            var snapping = property.FindPropertyRelative("Snapping");
            var onPlay = property.FindPropertyRelative("OnPlay");
            var onUpdate = property.FindPropertyRelative("OnUpdate");
            var onComplete = property.FindPropertyRelative("OnComplete");

            var lastRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(lastRect, addType);

            EditorGUI.BeginChangeCheck();
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(lastRect, target);
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(lastRect, tweenType);

            if (EditorGUI.EndChangeCheck())
            {
                var fixedComType = GetFixedComponentType(target.objectReferenceValue as Component,
                    (DoTweenSequence.DoTweenType)tweenType.enumValueIndex);
                if (fixedComType != null)
                {
                    target.objectReferenceValue = fixedComType;
                }
            }

            if (target.objectReferenceValue != null && null == GetFixedComponentType(
                    target.objectReferenceValue as Component, (DoTweenSequence.DoTweenType)tweenType.enumValueIndex))
            {
                lastRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.HelpBox(lastRect,
                    string.Format("{0}不支持{1}",
                        target.objectReferenceValue == null ? "Target" : target.objectReferenceValue.GetType().Name,
                        tweenType.enumDisplayNames[tweenType.enumValueIndex]), MessageType.Error);
            }

            const float itemWidth = 110;
            const float setBtnWidth = 30;
            //Delay, Snapping
            lastRect.y += EditorGUIUtility.singleLineHeight;
            var horizontalRect = lastRect;
            horizontalRect.width -= setBtnWidth + itemWidth;
            EditorGUI.PropertyField(horizontalRect, delay);
            horizontalRect.x += setBtnWidth + horizontalRect.width;
            horizontalRect.width = itemWidth;
            snapping.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Snapping", snapping.boolValue);

            //From Value
            lastRect.y += EditorGUIUtility.singleLineHeight;
            horizontalRect = lastRect;
            horizontalRect.width -= setBtnWidth + itemWidth;

            //ToTarget
            lastRect.y += EditorGUIUtility.singleLineHeight;
            var toRect = lastRect;
            toRect.width -= setBtnWidth + itemWidth;

            //To Value
            var dotweenTp = (DoTweenSequence.DoTweenType)tweenType.enumValueIndex;
            switch (dotweenTp)
            {
                case DoTweenSequence.DoTweenType.DoMoveX:
                case DoTweenSequence.DoTweenType.DoMoveY:
                case DoTweenSequence.DoTweenType.DoMoveZ:
                case DoTweenSequence.DoTweenType.DoLocalMoveX:
                case DoTweenSequence.DoTweenType.DoLocalMoveY:
                case DoTweenSequence.DoTweenType.DoLocalMoveZ:
                case DoTweenSequence.DoTweenType.DoAnchorPosX:
                case DoTweenSequence.DoTweenType.DoAnchorPosY:
                case DoTweenSequence.DoTweenType.DoAnchorPosZ:
                case DoTweenSequence.DoTweenType.DoFade:
                case DoTweenSequence.DoTweenType.DoCanvasGroupFade:
                case DoTweenSequence.DoTweenType.DoFillAmount:
                case DoTweenSequence.DoTweenType.DoValue:
                case DoTweenSequence.DoTweenType.DoScaleX:
                case DoTweenSequence.DoTweenType.DoScaleY:
                case DoTweenSequence.DoTweenType.DoScaleZ:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    var value = fromValue.vector4Value;
                    value.x = EditorGUI.FloatField(horizontalRect, "From", value.x);
                    fromValue.vector4Value = value;
                    EditorGUI.EndDisabledGroup();

                    if (!useToTarget.boolValue)
                    {
                        value = toValue.vector4Value;
                        value.x = EditorGUI.FloatField(toRect, "To", value.x);
                        toValue.vector4Value = value;
                    }
                }
                    break;
                case DoTweenSequence.DoTweenType.DoAnchorPos:
                case DoTweenSequence.DoTweenType.DoFlexibleSize:
                case DoTweenSequence.DoTweenType.DoMinSize:
                case DoTweenSequence.DoTweenType.DoPreferredSize:
                case DoTweenSequence.DoTweenType.DoSizeDelta:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector2Field(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector2Field(toRect, "To", toValue.vector4Value);
                }
                    break;
                case DoTweenSequence.DoTweenType.DoMove:
                case DoTweenSequence.DoTweenType.DoLocalMove:
                case DoTweenSequence.DoTweenType.DoAnchorPos3D:
                case DoTweenSequence.DoTweenType.DoScale:
                case DoTweenSequence.DoTweenType.DoRotate:
                case DoTweenSequence.DoTweenType.DoLocalRotate:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector3Field(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector3Field(toRect, "To", toValue.vector4Value);
                }
                    break;
                case DoTweenSequence.DoTweenType.DoColor:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.ColorField(horizontalRect, "From", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.ColorField(toRect, "To", toValue.vector4Value);
                }
                    break;
            }

            if (useToTarget.boolValue)
            {
                toTarget.objectReferenceValue = EditorGUI.ObjectField(toRect, "To", toTarget.objectReferenceValue,
                    target.objectReferenceValue != null ? target.objectReferenceValue.GetType() : typeof(Component),
                    true);

                if (toTarget.objectReferenceValue == null)
                {
                    lastRect.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.HelpBox(lastRect, "To target cannot be null.", MessageType.Error);
                }
            }

            horizontalRect.x += horizontalRect.width;
            horizontalRect.width = setBtnWidth;
            if (useFromValue.boolValue && GUI.Button(horizontalRect, "Set"))
            {
                SetValueFromTarget(dotweenTp, target, fromValue);
            }

            horizontalRect.x += setBtnWidth;
            horizontalRect.width = itemWidth;
            useFromValue.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Enable", useFromValue.boolValue);

            toRect.x += toRect.width;
            toRect.width = setBtnWidth;
            if (!useToTarget.boolValue && GUI.Button(toRect, "Set"))
            {
                SetValueFromTarget(dotweenTp, target, toValue);
            }

            toRect.x += setBtnWidth;
            toRect.width = itemWidth;
            useToTarget.boolValue = EditorGUI.ToggleLeft(toRect, "ToTarget", useToTarget.boolValue);

            //Duration
            lastRect.y += EditorGUIUtility.singleLineHeight;
            horizontalRect = lastRect;
            horizontalRect.width -= setBtnWidth + itemWidth;
            EditorGUI.PropertyField(horizontalRect, duration);
            horizontalRect.x += setBtnWidth + horizontalRect.width;
            horizontalRect.width = itemWidth;
            speedBased.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Use Speed", speedBased.boolValue);

            //Ease
            lastRect.y += EditorGUIUtility.singleLineHeight;
            horizontalRect = lastRect;
            horizontalRect.width -= setBtnWidth + itemWidth;
            if (customEase.boolValue)
                EditorGUI.PropertyField(horizontalRect, easeCurve);
            else
                EditorGUI.PropertyField(horizontalRect, ease);
            horizontalRect.x += setBtnWidth + horizontalRect.width;
            horizontalRect.width = itemWidth;
            customEase.boolValue = EditorGUI.ToggleLeft(horizontalRect, "Use Curve", customEase.boolValue);

            //Loops
            lastRect.y += EditorGUIUtility.singleLineHeight;
            horizontalRect = lastRect;
            horizontalRect.width -= setBtnWidth + itemWidth;
            EditorGUI.PropertyField(horizontalRect, loops);
            horizontalRect.x += setBtnWidth + horizontalRect.width;
            horizontalRect.width = itemWidth;
            EditorGUI.BeginDisabledGroup(loops.intValue == 1);
            loopType.enumValueIndex =
                (int)(LoopType)EditorGUI.EnumPopup(horizontalRect, (LoopType)loopType.enumValueIndex);
            EditorGUI.EndDisabledGroup();
            //UpdateType
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(lastRect, updateType);

            //Events
            lastRect.y += EditorGUIUtility.singleLineHeight;
            property.isExpanded = EditorGUI.Foldout(lastRect, property.isExpanded, "Animation Events");
            if (property.isExpanded)
            {
                //OnPlay
                lastRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(lastRect, onPlay);

                //OnUpdate
                lastRect.y += EditorGUI.GetPropertyHeight(onPlay);
                EditorGUI.PropertyField(lastRect, onUpdate);

                //OnComplete
                lastRect.y += EditorGUI.GetPropertyHeight(onUpdate);
                EditorGUI.PropertyField(lastRect, onComplete);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void SetValueFromTarget(DoTweenSequence.DoTweenType tweenType, SerializedProperty target,
            SerializedProperty value)
        {
            if (target.objectReferenceValue == null) return;
            var targetCom = target.objectReferenceValue;
            switch (tweenType)
            {
                case DoTweenSequence.DoTweenType.DoMove:
                {
                    value.vector4Value = (targetCom as Transform).position;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.x;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.y;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).position.z;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoLocalMove:
                {
                    value.vector4Value = (targetCom as Transform).localPosition;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoLocalMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.x;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoLocalMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.y;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoLocalMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localPosition.z;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoAnchorPos:
                {
                    value.vector4Value = (targetCom as RectTransform).anchoredPosition;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoAnchorPosX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition.x;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoAnchorPosY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition.y;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoAnchorPosZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as RectTransform).anchoredPosition3D.z;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoAnchorPos3D:
                {
                    value.vector4Value = (targetCom as RectTransform).anchoredPosition3D;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoColor:
                {
                    value.vector4Value = (targetCom as UnityEngine.UI.Graphic).color;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Graphic).color.a;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoCanvasGroupFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.CanvasGroup).alpha;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoValue:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Slider).value;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoSizeDelta:
                {
                    value.vector4Value = (targetCom as RectTransform).sizeDelta;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoFillAmount:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Image).fillAmount;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoFlexibleSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetFlexibleSize();
                    break;
                }
                case DoTweenSequence.DoTweenType.DoMinSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetMinSize();
                    break;
                }
                case DoTweenSequence.DoTweenType.DoPreferredSize:
                {
                    value.vector4Value = (targetCom as LayoutElement).GetPreferredSize();
                    break;
                }
                case DoTweenSequence.DoTweenType.DoScale:
                {
                    value.vector4Value = (targetCom as Transform).localScale;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoScaleX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.x;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoScaleY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.y;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoScaleZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as Transform).localScale.z;
                    value.vector4Value = tmpValue;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoRotate:
                {
                    value.vector4Value = (targetCom as Transform).eulerAngles;
                    break;
                }
                case DoTweenSequence.DoTweenType.DoLocalRotate:
                {
                    value.vector4Value = (targetCom as Transform).localEulerAngles;
                    break;
                }
            }
        }

        private static Component GetFixedComponentType(Component com, DoTweenSequence.DoTweenType tweenType)
        {
            if (com == null) return null;
            switch (tweenType)
            {
                case DoTweenSequence.DoTweenType.DoMove:
                case DoTweenSequence.DoTweenType.DoMoveX:
                case DoTweenSequence.DoTweenType.DoMoveY:
                case DoTweenSequence.DoTweenType.DoMoveZ:
                case DoTweenSequence.DoTweenType.DoLocalMove:
                case DoTweenSequence.DoTweenType.DoLocalMoveX:
                case DoTweenSequence.DoTweenType.DoLocalMoveY:
                case DoTweenSequence.DoTweenType.DoLocalMoveZ:
                case DoTweenSequence.DoTweenType.DoScale:
                case DoTweenSequence.DoTweenType.DoScaleX:
                case DoTweenSequence.DoTweenType.DoScaleY:
                case DoTweenSequence.DoTweenType.DoScaleZ:
                    return com.gameObject.GetComponent<Transform>();
                case DoTweenSequence.DoTweenType.DoAnchorPos:
                case DoTweenSequence.DoTweenType.DoAnchorPosX:
                case DoTweenSequence.DoTweenType.DoAnchorPosY:
                case DoTweenSequence.DoTweenType.DoAnchorPosZ:
                case DoTweenSequence.DoTweenType.DoAnchorPos3D:
                case DoTweenSequence.DoTweenType.DoSizeDelta:
                    return com.gameObject.GetComponent<RectTransform>();
                case DoTweenSequence.DoTweenType.DoColor:
                case DoTweenSequence.DoTweenType.DoFade:
                    return com.gameObject.GetComponent<UnityEngine.UI.Graphic>();
                case DoTweenSequence.DoTweenType.DoCanvasGroupFade:
                    return com.gameObject.GetComponent<UnityEngine.CanvasGroup>();
                case DoTweenSequence.DoTweenType.DoFillAmount:
                    return com.gameObject.GetComponent<UnityEngine.UI.Image>();
                case DoTweenSequence.DoTweenType.DoFlexibleSize:
                case DoTweenSequence.DoTweenType.DoMinSize:
                case DoTweenSequence.DoTweenType.DoPreferredSize:
                    return com.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
                case DoTweenSequence.DoTweenType.DoValue:
                    return com.gameObject.GetComponent<UnityEngine.UI.Slider>();
            }

            return null;
        }
    }
}

