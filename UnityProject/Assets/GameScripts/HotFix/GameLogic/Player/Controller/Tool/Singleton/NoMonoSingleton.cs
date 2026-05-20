using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThirdPersonController
{
    public class NoMonoSingleton<T> where T : new()
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new T();
                }
                return instance;
            }
        }
    }
}
