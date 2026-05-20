using System;
using UnityEngine;

namespace ThirdPersonController
{
    public class TimerService : MonoSingleton<TimerService>
    {
        public TickTimer tickTimer { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            tickTimer = new TickTimer();
        }

        private void Update()
        {
            tickTimer.UpdateTime();
        }

        private void OnDestroy()
        {
            tickTimer.ResetTimer();
        }

        public int AddTimer(int time, Action taskCB, Action cancelCB = null, int count = 1)
        {
            return tickTimer.AddTimer(time, taskCB, cancelCB, count);
        }

        public void RemoveTimer(int tid)
        {
            tickTimer.DeleteTimer(tid);
        }
    }
}
