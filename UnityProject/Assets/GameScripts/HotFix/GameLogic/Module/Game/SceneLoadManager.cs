using UnityEngine;

namespace GameLogic.Game
{
    public static class SceneLoadManager
    {
        static GameObject s_scenceParent;

        public static GameObject ScenceParent
        {
            get
            {
                if (s_scenceParent == null)
                {
                    s_scenceParent = GameObject.Find("Scene");
                    if (s_scenceParent == null)
                        s_scenceParent = new GameObject("Scene");
                }
                return s_scenceParent;
            }
        }

        public static void LoadScence(string sceneName)
        {
        }

        public static void CleanScene()
        {
        }
    }
}
