using System;

namespace HotUpdate
{
    public static class GameUtil
    {
        public static void TriggerEvent(String eventName)
        {
            EventManager.Instance.TriggerEvent(eventName);
        }
    }
}