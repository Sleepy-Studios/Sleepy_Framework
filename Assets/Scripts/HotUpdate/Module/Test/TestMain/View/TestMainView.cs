using UnityEngine;
using Core;

namespace HotUpdate
{
    public partial class TestMainView : MonoBehaviour
    {
        private void Awake()
        {
            // 初始化组件
            BindButtonEvents();
        }

        private void OnDestroy()
        {
            ReleaseComponent();
        }

        #region Button Events

        private void OnTestBtnClick()
        {
            // TODO: 实现按钮点击逻辑
        }

        #endregion
    }
}
