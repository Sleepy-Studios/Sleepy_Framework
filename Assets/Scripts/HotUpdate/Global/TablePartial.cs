namespace HotUpdate
{
      /// <summary>
    /// 全局静态访问类，用于简化配置表的访问
    /// 使用方式：Tables.TbNewLoadingRule.DataList
    /// </summary>
    public static class Tables
    {
        public static cfg.Loading.TbNewLoadingRule TbNewLoadingRule = GlobalLubanConfig.Tables.TbNewLoadingRule;
    }
}