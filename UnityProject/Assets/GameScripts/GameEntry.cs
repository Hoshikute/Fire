using TEngine;
using UnityEngine;

public class GameEntry : MonoBehaviour
{
    void Awake()
    {
        // 模块访问统一通过 GameModule 静态类，首次访问时自动初始化
        Settings.ProcedureSetting.StartProcedure().Forget();
        DontDestroyOnLoad(this);
    }
}