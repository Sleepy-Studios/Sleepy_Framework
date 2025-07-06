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
                    UpdateGeometry();
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
                    UpdateGeometry();
                }
            }
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);

            if (FlipHor || FlipVer)
            {
                Vector2 rectCenter = rectTransform.rect.center;
                int vertCount = toFill.currentVertCount;
                for (int i = 0; i < vertCount; i++)
                {
                    UIVertex uiVertex = new UIVertex();
                    toFill.PopulateUIVertex(ref uiVertex, i);

                    Vector3 pos = uiVertex.position;
                    uiVertex.position = new Vector3(
                        FlipHor ? (pos.x + (rectCenter.x - pos.x) * 2) : pos.x,
                        FlipVer ? (pos.y + (rectCenter.y - pos.y) * 2) : pos.y,
                        pos.z);

                    toFill.SetUIVertex(uiVertex, i);
                }
            }
        }
    }
}