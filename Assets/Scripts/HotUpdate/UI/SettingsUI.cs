using System.Collections.Generic;
using HotUpdate.GameUtils;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HotUpdate.UI
{
    /// <summary>
    /// 窗口模式枚举
    /// </summary>
    public enum WindowModeEnum
    {
        Fullscreen, // 全屏模式
        Windowed, // 窗口模式
        Borderless // 无边框模式
    }

    /// <summary>
    /// 帧率选项枚举
    /// </summary>
    public enum FrameRateEnum
    {
        FPS30 = 30,
        FPS60 = 60,
        FPS120 = 120,
        Unlimited = -1
    }

    public class SettingsUI : MonoBehaviour
    {
        private TMP_Dropdown resolutionDropdown;
        private TMP_Dropdown frameRateDropdown;
        private Toggle toggleFullscreen;
        private Toggle toggleWindowed;
        private Toggle toggleBorderless;
        private Button applyButton;
        private Button cancelButton;

        // 新增音频相关UI控件
        private Slider musicSlider;
        private Slider soundSlider;
        private Toggle musicToggle;
        private Toggle soundToggle;
        private TextMeshProUGUI musicText;
        private TextMeshProUGUI soundText;

        // 音量滑条颜色
        private Color activeSliderBgColor;
        private Color inactiveSliderBgColor;
        private Color activeSliderColor;
        private Color inactiveSliderColor;

        private Resolution[] resolutions;
        private int currentResolutionIndex;
        private WindowModeEnum currentWindowMode = WindowModeEnum.Fullscreen;
        private FrameRateEnum currentFrameRate = FrameRateEnum.FPS60;

        // 音频设置
        private float musicVolume = 1.0f;
        private float soundVolume = 1.0f;
        private bool isMusicMuted = false;
        private bool isSoundMuted = false;

        /// <summary>
        /// 当前选中的Toggle类型
        /// </summary>
        private WindowModeEnum curToggleType = WindowModeEnum.Borderless;

        private void Awake()
        {
            // 在Awake中找到所有UI元素
            resolutionDropdown = transform.Find("ResolutionDropdown").GetComponent<TMP_Dropdown>();
            frameRateDropdown = transform.Find("FrameRateDropdown").GetComponent<TMP_Dropdown>();

            toggleFullscreen = transform.Find("Toggles/ToggleFullscreen").GetComponent<Toggle>();
            toggleWindowed = transform.Find("Toggles/ToggleWindowed").GetComponent<Toggle>();
            toggleBorderless = transform.Find("Toggles/ToggleBorderless").GetComponent<Toggle>();

            // 初始化音频相关UI控件
            musicSlider = transform.Find("MusicSettings/MusicSlider").GetComponent<Slider>();
            soundSlider = transform.Find("SoundSettings/SoundSlider").GetComponent<Slider>();
            musicToggle = transform.Find("MusicSettings/MusicToggle").GetComponent<Toggle>();
            soundToggle = transform.Find("SoundSettings/SoundToggle").GetComponent<Toggle>();
            musicText = transform.Find("MusicSettings/MusicTmp").GetComponent<TextMeshProUGUI>();
            soundText = transform.Find("SoundSettings/SoundTmp").GetComponent<TextMeshProUGUI>();

            applyButton = transform.Find("ApplyButton").GetComponent<Button>();
            cancelButton = transform.Find("CancelButton").GetComponent<Button>();

            // 初始化颜色
            ColorUtility.TryParseHtmlString("#F3F3F3", out activeSliderBgColor);
            ColorUtility.TryParseHtmlString("#a1a3a6", out inactiveSliderBgColor);
            ColorUtility.TryParseHtmlString("#6C8CC4", out activeSliderColor);
            ColorUtility.TryParseHtmlString("#bfc5cc", out inactiveSliderColor);

            // 添加事件监听
            applyButton.onClick.AddListener(ApplySettings);
            cancelButton.onClick.AddListener(OnCancelButtonClicked);

            // 绑定Toggle事件
            BindToggleToHandler(toggleFullscreen,
                (isOn) => { HandleWindowModeToggleChange(toggleFullscreen, isOn, WindowModeEnum.Fullscreen); });
            BindToggleToHandler(toggleWindowed,
                (isOn) => { HandleWindowModeToggleChange(toggleWindowed, isOn, WindowModeEnum.Windowed); });
            BindToggleToHandler(toggleBorderless,
                (isOn) => { HandleWindowModeToggleChange(toggleBorderless, isOn, WindowModeEnum.Borderless); });

            // 绑定音频相关控件事件
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            musicToggle.onValueChanged.AddListener(OnMusicMuteChanged);
            soundToggle.onValueChanged.AddListener(OnSoundMuteChanged);
        }

        private void Start()
        {
            InitializeResolutionOptions();
            InitializeFrameRateOptions();
            InitializeAudioSettings();
            LoadSavedSettings();

            // 确保启动时应用已保存的帧率设置
            ApplyFrameRateSetting();
        }

        private void InitializeResolutionOptions()
        {
            // 获取可用分辨率
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();

            List<string> options = new();

            // 填充分辨率选项
            for (int i = 0; i < resolutions.Length; i++)
            {
                // 使用refreshRateRatio代替过时的refreshRate属性
                string option =
                    $"{resolutions[i].width} x {resolutions[i].height} @{resolutions[i].refreshRateRatio.value:F0}Hz";
                options.Add(option);

                // 记录当前分辨率索引
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                    currentResolutionIndex = i;
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            // 设置当前全屏模式
            toggleFullscreen.isOn = Screen.fullScreen;

            // 添加分辨率下拉框的值变更监听
            resolutionDropdown.onValueChanged.AddListener((int value) =>
            {
                AudioManager.Instance.PlaySound(AudioSoundEnum.Button_Click);
            });
        }

        private void InitializeFrameRateOptions()
        {
            frameRateDropdown.ClearOptions();

            List<string> frameRateOptions = new List<string>
            {
                "30 FPS",
                "60 FPS",
                "120 FPS",
                "无限制"
            };

            frameRateDropdown.AddOptions(frameRateOptions);

            // 设置当前帧率选项
            int currentFps = Application.targetFrameRate;
            int dropdownValue = 1; // 默认60fps

            if (currentFps == 30) dropdownValue = 0;
            else if (currentFps == 60) dropdownValue = 1;
            else if (currentFps == 120) dropdownValue = 2;
            else if (currentFps == -1 || currentFps > 120) dropdownValue = 3;

            frameRateDropdown.value = dropdownValue;
            frameRateDropdown.RefreshShownValue();

            // 监听帧率下拉框变化
            frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
        }

        /// <summary>
        /// 初始化音频设置
        /// </summary>
        private void InitializeAudioSettings()
        {
            // 从PlayerPrefs直接读取音频设置
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            soundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
            isMusicMuted = PlayerPrefs.GetInt("MusicMute", 0) == 1;
            isSoundMuted = PlayerPrefs.GetInt("SoundMute", 0) == 1;

            // 设置滑块初始值
            musicSlider.value = musicVolume;
            soundSlider.value = soundVolume;

            // 设置静音开关初始状态
            musicToggle.isOn = !isMusicMuted;
            soundToggle.isOn = !isSoundMuted;

            // 更新UI显示
            UpdateMusicUI(isMusicMuted, musicVolume);
            UpdateSoundUI(isSoundMuted, soundVolume);
        }

        private void OnFrameRateChanged(int index)
        {
            // 点击音效
            AudioManager.Instance.PlaySound(AudioSoundEnum.Button_Click);
            switch (index)
            {
                case 0:
                    currentFrameRate = FrameRateEnum.FPS30;
                    break;
                case 1:
                    currentFrameRate = FrameRateEnum.FPS60;
                    break;
                case 2:
                    currentFrameRate = FrameRateEnum.FPS120;
                    break;
                case 3:
                    currentFrameRate = FrameRateEnum.Unlimited;
                    break;
            }
        }

        /// <summary>
        /// 处理音乐音量变化
        /// </summary>
        private void OnMusicVolumeChanged(float value)
        {
            musicVolume = value;
            AudioManager.Instance.SetMusicVolume(value);
            UpdateMusicUI(isMusicMuted, value);
        }

        /// <summary>
        /// 处理音效音量变化
        /// </summary>
        private void OnSoundVolumeChanged(float value)
        {
            soundVolume = value;
            AudioManager.Instance.SetSoundVolume(value);
            UpdateSoundUI(isSoundMuted, value);
        }

        /// <summary>
        /// 处理音乐静音状态变化
        /// </summary>
        private void OnMusicMuteChanged(bool isOn)
        {
            isMusicMuted = !isOn;
            AudioManager.Instance.SetMusicMute(isMusicMuted);
            UpdateMusicUI(isMusicMuted, musicVolume);

            // 更新Toggle的视觉效果
            UpdateToggleVisual(musicToggle, isOn);
        }

        /// <summary>
        /// 处理音效静音状态变化
        /// </summary>
        private void OnSoundMuteChanged(bool isOn)
        {
            isSoundMuted = !isOn;
            AudioManager.Instance.SetSoundMute(isSoundMuted);
            UpdateSoundUI(isSoundMuted, soundVolume);

            // ���放一个音效让用户能够听到变化
            if (isOn && soundVolume > 0)
            {
                AudioManager.Instance.PlaySound(AudioSoundEnum.Button_Click);
            }

            // 更新Toggle的视觉效果
            UpdateToggleVisual(soundToggle, isOn);
        }

        /// <summary>
        /// 更新音乐UI显示
        /// </summary>
        private void UpdateMusicUI(bool isMuted, float volume)
        {
            // 更新文本显示
            int volumePercent = Mathf.RoundToInt(volume * 100);
            musicText.text = $"音乐\n{volumePercent}%";

            // 更新滑块颜色
            UpdateSliderColors(musicSlider, !isMuted);
        }

        /// <summary>
        /// 更新音效UI显示
        /// </summary>
        private void UpdateSoundUI(bool isMuted, float volume)
        {
            // 更新文本显示
            int volumePercent = Mathf.RoundToInt(volume * 100);
            soundText.text = $"音效\n{volumePercent}%";

            // 更新滑块颜色
            UpdateSliderColors(soundSlider, !isMuted);
        }

        /// <summary>
        /// 更新滑块颜色
        /// </summary>
        private void UpdateSliderColors(Slider slider, bool isActive)
        {
            // 获取滑块的背景和填充图片
            Image background = slider.transform.Find("Background").GetComponent<Image>();
            Image fill = slider.transform.Find("Fill Area/Fill").GetComponent<Image>();
            Image handle = slider.transform.Find("Handle Slide Area/Handle").GetComponent<Image>();

            // 设置颜色
            background.color = isActive ? activeSliderBgColor : inactiveSliderBgColor;
            fill.color = isActive ? activeSliderColor : inactiveSliderColor;
            handle.color = isActive ? activeSliderColor : inactiveSliderColor;
        }

        /// <summary>
        /// 更新Toggle的视觉效果
        /// </summary>
        /// <param name="toggle">需要更新的Toggle</param>
        /// <param name="isOn">Toggle是否开启</param>
        private void UpdateToggleVisual(Toggle toggle, bool isOn)
        {
            // 获取Toggle的背景图片
            Image background = toggle.transform.GetChild(0).GetComponent<Image>();
            // 获取Toggle的Checkmark
            RectTransform checkmark = toggle.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
            Image checkmarkImage = checkmark.GetComponent<Image>();

            // 确保Checkmark始终可见，不受Toggle状态影响
            checkmarkImage.enabled = true;

            // 根据状态更新Checkmark的位置
            // 未禁音时Checkmark的x位置为49，禁音时为2
            Vector2 position = checkmark.anchoredPosition;
            position.x = isOn ? 49 : 2;
            checkmark.anchoredPosition = position;

            // 根��状态改变背景图片的颜色
            // 注意：只改变背景图片颜色，不改变Checkmark的颜色
            if (isOn)
            {
                background.color = activeSliderColor;
            }
            else
            {
                background.color = inactiveSliderColor;
            }
        }

        /// <summary>
        /// 应用帧率设置并禁用垂直同步
        /// </summary>
        private void ApplyFrameRateSetting()
        {
            // 禁用垂直同步，确保帧率设置能生效
            QualitySettings.vSyncCount = 0;

            // 应用帧率设置
            Application.targetFrameRate = (int)currentFrameRate;

            Debug.Log($"已设置帧率为: {(int)currentFrameRate}");
        }

        private void ApplySettings()
        {
            // 点击音效
            AudioManager.Instance.PlaySound(AudioSoundEnum.Button_Click);
            // 应用分辨率设置
            Resolution resolution = resolutions[resolutionDropdown.value];

            // 应用窗口模式设置
            FullScreenMode screenMode = FullScreenMode.Windowed;

            switch (currentWindowMode)
            {
                case WindowModeEnum.Fullscreen:
                    screenMode = FullScreenMode.ExclusiveFullScreen;
                    break;
                case WindowModeEnum.Windowed:
                    screenMode = FullScreenMode.Windowed;
                    break;
                case WindowModeEnum.Borderless:
                    screenMode = FullScreenMode.FullScreenWindow;
                    break;
            }

            // 设置分辨率和窗口模式
            Screen.SetResolution(resolution.width, resolution.height, screenMode);

            // 应用帧率设置
            ApplyFrameRateSetting();

            // 应用音频设置
            AudioManager.Instance.SetMusicVolume(musicVolume);
            AudioManager.Instance.SetSoundVolume(soundVolume);
            AudioManager.Instance.SetMusicMute(isMusicMuted);
            AudioManager.Instance.SetSoundMute(isSoundMuted);

            // 保存设置
            PlayerPrefs.SetInt("ResolutionWidth", resolution.width);
            PlayerPrefs.SetInt("ResolutionHeight", resolution.height);
            PlayerPrefs.SetInt("WindowMode", (int)currentWindowMode);
            PlayerPrefs.SetInt("FrameRate", (int)currentFrameRate);
            PlayerPrefs.Save();

            // 应用设置后销毁物体
            Destroy(gameObject);
        }

        private void LoadSavedSettings()
        {
            // 加载分辨率设置
            if (PlayerPrefs.HasKey("ResolutionWidth") && PlayerPrefs.HasKey("ResolutionHeight"))
            {
                int width = PlayerPrefs.GetInt("ResolutionWidth");
                int height = PlayerPrefs.GetInt("ResolutionHeight");

                // 更新分辨率下拉框
                for (int i = 0; i < resolutions.Length; i++)
                {
                    if (resolutions[i].width == width && resolutions[i].height == height)
                    {
                        resolutionDropdown.value = i;
                        currentResolutionIndex = i;
                        break;
                    }
                }
            }

            // 加载窗口模式设置
            if (PlayerPrefs.HasKey("WindowMode"))
            {
                int windowModeValue = PlayerPrefs.GetInt("WindowMode");
                currentWindowMode = (WindowModeEnum)windowModeValue;

                // 设置对应的Toggle
                switch (currentWindowMode)
                {
                    case WindowModeEnum.Fullscreen:
                        toggleFullscreen.isOn = true;
                        break;
                    case WindowModeEnum.Windowed:
                        toggleWindowed.isOn = true;
                        break;
                    case WindowModeEnum.Borderless:
                        toggleBorderless.isOn = true;
                        break;
                }

                curToggleType = currentWindowMode;
            }
            else
            {
                // 如果没有保存过窗口模式，根��当前全屏状态设置默认值
                if (Screen.fullScreen)
                {
                    if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen)
                    {
                        toggleFullscreen.isOn = true;
                        currentWindowMode = WindowModeEnum.Fullscreen;
                    }
                    else if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow)
                    {
                        toggleBorderless.isOn = true;
                        currentWindowMode = WindowModeEnum.Borderless;
                    }
                }
                else
                {
                    toggleWindowed.isOn = true;
                    currentWindowMode = WindowModeEnum.Windowed;
                }

                curToggleType = currentWindowMode;
            }

            // 加载帧率设置
            if (PlayerPrefs.HasKey("FrameRate"))
            {
                int frameRateValue = PlayerPrefs.GetInt("FrameRate");
                currentFrameRate = (FrameRateEnum)frameRateValue;

                // 设置下拉框值
                int dropdownValue = 1; // 默认60fps

                if (frameRateValue == 30) dropdownValue = 0;
                else if (frameRateValue == 60) dropdownValue = 1;
                else if (frameRateValue == 120) dropdownValue = 2;
                else if (frameRateValue == -1) dropdownValue = 3;

                frameRateDropdown.value = dropdownValue;
            }
        }

        private void OnCancelButtonClicked()
        {
            // 点击音效
            AudioManager.Instance.PlaySound(AudioSoundEnum.Button_Back);
            gameObject.SetActive(false);
        }

        private void BindToggleToHandler(Toggle toggle, System.Action<bool> handler)
        {
            toggle.onValueChanged.AddListener((isOn) => handler(isOn));
        }

        /// <summary>
        /// 处理窗口模式Toggle切换
        /// </summary>
        private void HandleWindowModeToggleChange(Toggle toggle, bool isOn, WindowModeEnum mode)
        {
            #region 标签按钮显示相��

            var tmpLabel = toggle.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            var maskTrans = toggle.transform.Find("Mask");
            UIUtil.SetTextHexColor(tmpLabel, isOn ? "#153573" : "#6E80c4");
            maskTrans.gameObject.SetActive(isOn);

            #endregion

            if (isOn && curToggleType != mode)
            {
                // 点击音效
                AudioManager.Instance.PlaySound(AudioSoundEnum.Button_Click);
                curToggleType = mode;
                currentWindowMode = mode;
            }
        }
    }
}