using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonController
{
    public interface IState
    {
        void OnEnter();
        void OnUpdate();
        void OnAnimationUpdate();
        void OnExit();
        void OnAnimationEnd();
    
    }
}
