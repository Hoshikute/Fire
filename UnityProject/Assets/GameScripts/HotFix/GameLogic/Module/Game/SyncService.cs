using UnityEngine;

namespace GameLogic.Game
{
    public static class SyncService
    {
        static RouteRule s_serviceType = RouteRule.Client;
        const float c_syncAheadTime = 0.1f;

        private static float m_CurrentServiceTime;
        private static float m_ping;

        public static float SyncAheadTime
        {
            get { return c_syncAheadTime; }
        }

        public static RouteRule ServiceType
        {
            get { return s_serviceType; }
            set { s_serviceType = value; }
        }

        public static float CurrentServiceTime
        {
            get { return m_CurrentServiceTime; }
        }

        public static float Ping
        {
            get { return m_ping; }
        }

        public static void Init()
        {
            m_CurrentServiceTime = 0;
        }

        public static void Update(float deltaTime)
        {
            m_CurrentServiceTime += deltaTime;
        }

        public static void Dispose()
        {
        }
    }

    public enum RouteRule
    {
        Local,
        Client,
        Server
    }

    public enum SyncScene
    {
        City,
        Fight
    }

    public enum SyncEventEnum
    {
        ServiceChange
    }
}
