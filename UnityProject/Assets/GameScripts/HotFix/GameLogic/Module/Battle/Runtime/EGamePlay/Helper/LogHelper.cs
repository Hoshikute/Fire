using System;

using TEngine;

namespace EGamePlay
{
#if !NOT_UNITY
    public static class Log
    {
        public static void Console(string log)
        {
            TEngine.Log.Debug(log);
        }

        public static void Debug(string log)
        {
            if (log.Contains("OnceWaitTimer")) return;
            if (log.Contains("GameObjectComponent")) return;
            if (log.Contains("EventComponent")) return;
            if (log.Contains("ChildrenComponent")) return;
            UnityEngine.Debug.Log(log);
        }

        public static void Error(string log)
        {
            TEngine.Log.Error(log);
        }

        public static void Error(Exception e)
        {
            TEngine.Log.Error(e);
        }
    }
#else
    public static class Log
    {
        public static void Console(string log)
        {
            TEngine.Log.Debug(log);
        }

        public static void Debug(string log)
        {
            TEngine.Log.Debug(log);
        }

        public static void Error(string log)
        {
            TEngine.Log.Error(log);
        }

        public static void Error(Exception e)
        {
            TEngine.Log.Error(e);
        }
    }
#endif
}
