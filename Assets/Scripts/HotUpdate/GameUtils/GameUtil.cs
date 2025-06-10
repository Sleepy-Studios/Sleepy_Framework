namespace HotUpdate.GameUtils
{
    public static class GameUtil
    {
        public static void TriggerEvent(EventName eventName)
        {
            EventManager.Instance.TriggerEvent(eventName);
        }
    }
}