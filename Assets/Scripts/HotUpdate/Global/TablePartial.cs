namespace HotUpdate
{
    /// <summary>
    /// 全局静态访问类，用于简化配置表的访问
    /// 使用方式：Tables.TbNewLoadingRule.DataList
    /// </summary>
    public partial class Tables
    {
        public static readonly cfg.Loading.TbNewLoadingRule TbNewLoadingRule = Config.TbNewLoadingRule;
    }
}