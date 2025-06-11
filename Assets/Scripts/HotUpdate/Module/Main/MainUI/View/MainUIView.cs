using UnityEngine;
using Core;
using UnityEditor;
using YooAsset;

namespace HotUpdate
{
    public partial class MainUIView : MonoBehaviour
    {
        private void Awake()
        {
            Button_settingBtn.onClick.AddListener(OpenSettingView);
            Button_quitBtn.onClick.AddListener(QuitGame);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            
        }

        private void OpenSettingView()
        {
            var handle =  YooAssets.LoadAssetSync<GameObject>("SettingsUI") ;
            Instantiate(handle.AssetObject as GameObject) ;
        }
        
    }
}
