using UnityEngine;
using UnityEngine.UI;

namespace Core.Runtime
{
    [AddComponentMenu("UI/Flippable Image", 12)]
    public class FlippableImage : Image
    {
        [SerializeField]
        protected bool FlipHor;
        [SerializeField]
        protected bool FlipVer;

        /// <summary>
        /// 是否水平翻转
        /// </summary>
        public bool FlipHorizontal
        {
            get => FlipHor;
            set
            {
                if (FlipHor != value)
                {
                    FlipHor = value;
                    SetVerticesDirty();
                }
            }
        }

        /// <summary>
        /// 是否垂直翻转
        /// </summary>
        public bool FlipVertical
        {
            get => FlipVer;
            set
            {
                if (FlipVer != value)
                {
                    FlipVer = value;
                    SetVerticesDirty();
                }
            }
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);

            if (!FlipHor && !FlipVer)
            {
                return;
            }

            var rectCenter = rectTransform.rect.center;
            var vertCount = toFill.currentVertCount;
            var uiVertex = default(UIVertex);

            for (var i = 0; i < vertCount; i++)
            {
                toFill.PopulateUIVertex(ref uiVertex, i);

                var pos = uiVertex.position;
                var newPos = new Vector3(
                    FlipHor ? rectCenter.x * 2 - pos.x : pos.x,
                    FlipVer ? rectCenter.y * 2 - pos.y : pos.y,
                    pos.z
                );
                uiVertex.position = newPos;

                toFill.SetUIVertex(uiVertex, i);
            }
        }
    }
}