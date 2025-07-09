using System.Collections.Generic;
using System.Linq;
using Aot.Runtime;
using cfg;
using cfg.Loading;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;

namespace HotUpdate
{
    public partial class CommonLoadingView : MonoBehaviour
    {
        private List<NewLoadingRule> openConfigs;
        private int playerLevel = -1;
        private NewLoadingRule globalConfig;
        private List<LoadingType> globalLoadingEnums;

        private void Awake()
        {
        }

        private void OnEnable()
        {
            InitData();
            InitUI();
        }

        void InitUI()
        {
            // 获取当前指定的Loading类型
            LoadingType currentLoadingType = UIUtil.LoadingType;
            Log.Info("指定Loading类型" + currentLoadingType);
            //重置类型
            UIUtil.LoadingType = LoadingType.Default;
            NewLoadingRule selectedInfo = SelectLoadingInfo(currentLoadingType);
            //添加容错
            if (selectedInfo == null)
            {
                var specialConfigs = openConfigs.Where(info => (info.Type == LoadingType.Default)).ToList();
                selectedInfo = SelectLoadingFromConfigs(specialConfigs);
            }

            SetupLoadingUI(selectedInfo);
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        private void InitData()
        {
            playerLevel = 20;
            openConfigs = GlobalLubanConfig.Tables.TbNewLoadingRule.DataList
                .Where(info => info.IsOpen == 1 && playerLevel >= info.OpenLevel).ToList();
            globalConfig =
                GlobalLubanConfig.Tables.TbNewLoadingRule.DataList.FirstOrDefault(info =>
                    info.Type == LoadingType.Global);
            globalLoadingEnums = globalConfig.IsOpen == 1 ? globalConfig.GlobalLoadingSwitch : null;
        }

        /// <summary>
        /// 选择Loading信息的主要逻辑
        /// </summary>
        private NewLoadingRule SelectLoadingInfo(LoadingType loadingType)
        {
            if (globalLoadingEnums != null && globalLoadingEnums.Contains(loadingType))
            {
                // 计算全局Loading的权重作为百分比概率
                int globalWeight = CalculateWeight(globalConfig);
                if (globalWeight > 0 && UnityEngine.Random.Range(0, 100) < globalWeight)
                {
                    Log.Info("命中全局Loading");
                    return globalConfig;
                }
                else
                {
                    Log.Info("未命中全局Loading，走当前类型随机" + loadingType);
                    var specialConfigs = openConfigs.Where(info => (info.Type == loadingType)).ToList();
                    return SelectLoadingFromConfigs(specialConfigs);
                }
            }
            else
            {
                Log.Info("走当前类型随机" + loadingType);
                var specialConfigs = openConfigs.Where(info => (info.Type == loadingType)).ToList();
                return SelectLoadingFromConfigs(specialConfigs);
            }
        }

        /// <summary>
        /// 从配置列表中根据权重选择Loading
        /// </summary>
        private NewLoadingRule SelectLoadingFromConfigs(List<NewLoadingRule> configs)
        {
            var weightedConfigs = new List<(NewLoadingRule config, int weight)>();
            int totalWeight = 0;

            foreach (var config in configs)
            {
                int weight = CalculateWeight(config);
                if (weight > 0)
                {
                    weightedConfigs.Add((config, weight));
                    totalWeight += weight;
                }
            }

            if (totalWeight > 0)
            {
                int randomValue = UnityEngine.Random.Range(0, totalWeight);
                return SelectFromWeightedList(weightedConfigs, randomValue);
            }

            return null;
        }

        private NewLoadingRule SelectFromWeightedList(List<(NewLoadingRule config, int weight)> weightedConfigs,
            int randomValue)
        {
            int currentWeight = 0;
            foreach (var (config, weight) in weightedConfigs)
            {
                currentWeight += weight;
                if (randomValue < currentWeight)
                {
                    return config;
                }
            }

            return weightedConfigs.Count > 0 ? weightedConfigs[0].config : null;
        }

        private int CalculateWeight(NewLoadingRule info)
        {
            if (info.Weight == null || info.Weight.Count == 0)
                return 0;

            switch (info.WeightType)
            {
                case WeightType.None:
                    // 直接使用第一个权重配置
                    return info.Weight.Count > 0 ? info.Weight[0].Weight : 0;

                case WeightType.ActivityDays:
                    return CalculateActivityDaysWeight(info.Weight);

                case WeightType.PlayerLevel:
                    return CalculatePlayerLevelWeight(info.Weight);

                default:
                    return 0;
            }
        }

        private int CalculateActivityDaysWeight(List<WeightConfig> weights)
        {
            int activityDays = GetActivityDays(); // 需要实现获取活动天数的方法

            // 找到符合条件的权重配置
            WeightConfig matchedWeight = null;
            foreach (var weight in weights)
            {
                if (activityDays >= weight.Condition)
                {
                    if (matchedWeight == null || weight.Condition > matchedWeight.Condition)
                    {
                        matchedWeight = weight;
                    }
                }
            }

            return matchedWeight?.Weight ?? 0;
        }

        private int CalculatePlayerLevelWeight(List<WeightConfig> weights)
        {
            // 找到符合条件的权重配置
            WeightConfig matchedWeight = null;
            foreach (var weight in weights)
            {
                if (playerLevel >= weight.Condition)
                {
                    if (matchedWeight == null || weight.Condition > matchedWeight.Condition)
                    {
                        matchedWeight = weight;
                    }
                }
            }

            return matchedWeight?.Weight ?? 0;
        }

        private int GetActivityDays()
        {
            // Todo : 实现获取活动天数的逻辑
            // 临时返回1
            return 1;
        }

        private void SetupLoadingUI(NewLoadingRule loadingInfo)
        {
            Log.Info("最终命中Loading " + loadingInfo.Type + loadingInfo.Id);
            // 随机选择一张图片
            string imagePath = SelectRandomImage(loadingInfo.ImagePaths);
            if (!string.IsNullOrEmpty(imagePath))
            {
                string fullPath = $"{imagePath}";
                var handle = YooAssets.LoadAssetSync<Sprite>(fullPath);
                Image_bg.sprite = handle.AssetObject as Sprite;
            }

            // 设置标题和副标题
            TextMeshProUGUI_title.text = loadingInfo.Title;
            TextMeshProUGUI_titleDesc.text = loadingInfo.SubTitle;

            // 设置小贴士文案
            TextMeshProUGUI_content.text = loadingInfo.TipDesc;
            RectTransform_gameTips.gameObject.SetActive(!string.IsNullOrEmpty(loadingInfo.TipDesc));
            RectTransform_loadingtitlebg.gameObject.SetActive(!string.IsNullOrEmpty(loadingInfo.Title));

            UpdateTitleDescBgSize();
        }

        private string SelectRandomImage(List<string> imagePaths)
        {
            if (imagePaths == null || imagePaths.Count == 0)
                return "";

            if (imagePaths.Count == 1)
            {
                return imagePaths[0];
            }

            return imagePaths[UnityEngine.Random.Range(0, imagePaths.Count)];
        }

        private Vector2 titleDescSize = new Vector2(0, 0);

        private void UpdateTitleDescBgSize()
        {
            if (TextMeshProUGUI_titleDesc == null || RectTransform_titleDescBg == null)
                return;

            // 强制立即重新构建文本的布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(TextMeshProUGUI_titleDesc.rectTransform);

            // 获取文本的RectTransform
            RectTransform textRectTransform = TextMeshProUGUI_titleDesc.rectTransform;

            // 计算背景尺寸：文本尺寸 + 左右各8像素，上下各3像素的边距
            titleDescSize.x = textRectTransform.sizeDelta.x + 13f;
            titleDescSize.y = textRectTransform.sizeDelta.y + 5f;

            // 设置背景RectTransform的尺寸
            RectTransform_titleDescBg.sizeDelta = titleDescSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform_titleDescBg);
        }

        public void SetProgress(float f, string text)
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            f = Mathf.Min(1f, f);
            Slider_slider.value = f;
            TextMeshProUGUI_process.text = text;
            // 设置文本
            TextMeshProUGUI_process.text = string.Concat(((int)(f * 100)).ToString(), "%");
        }

        protected void OnDisable()
        {
            Slider_slider.value = 0;
        }
    }
}