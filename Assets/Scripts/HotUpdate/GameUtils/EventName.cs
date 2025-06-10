namespace HotUpdate.GameUtils
{
    /// <summary>
    /// 事件枚举
    /// </summary>
    public enum EventName
    {
        ///游戏开始事件
        GameStartEvent,
        ///游戏结束事件
        GameEndEvent,
        ///游戏暂停事件
        GamePauseEvent,
        ///游戏继续事件
        GameResumeEvent,
        ///空间划分事件
        SpaceDivisionEvent,
        ///打开测试门事件
        OpenTestDoor,
        ///复原测试门事件
        CloseTestDoor,
        ///敌人攻击事件
        EnemyAttackEvent,
        ///伤害跳字
        DamagePop,
        ///玩家攻击事件
        PlayerAttackEvent,
        ///获得经验值事件
        GainExperienceEvent,
        ///玩家升级事件
        PlayerLevelUpEvent,
        ///显示升级UI事件
        ShowLevelUpUIEvent,
        ///技能升级事件
        SkillUpgradeEvent,
        ///技能碎片获取事件
        SkillFragmentEvent,
        ///玩家更新血条UI
        PlayerUpdateHealthBarEvent,
        ///存活时间刷新时间
        PlayerLiveTime,
        ///玩家死亡事件
        PlayerDeathEvent,
        ///刷新已存在的技能GridUI
        RefreshSkillGridUIEvent,
    }
}