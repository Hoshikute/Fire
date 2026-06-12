
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 玩家数学工具类（TEngine 规范）。
    /// 原 ThirdPersonController.ToolFunction 的迁移版本：
    ///   - 命名空间改为 GameLogic，与项目其余模块一致。
    ///   - 使用 Log.Warning 替代 Debug.LogWarning（对齐 TEngine 日志规范）。
    ///   - 所有方法均为纯函数 / 静态方法，无副作用，可在逻辑层或表现层复用。
    ///
    /// FrameSync 注意：GetJumpInitVelocity / GetDeltaAngle 等方法仍返回 float，
    /// 仅供「表现层」（PlayerViewSystem / CameraController 等）调用。
    /// 逻辑层确定性计算请使用 SyncVector3 整数运算，禁止引入 float。
    /// </summary>
    public static class PlayerMathUtil
    {
        #region UI 颜色工具

        /// <summary>
        /// 用 Vector3 的 RGB 分量设置 Image 的颜色。
        /// </summary>
        /// <param name="img">目标 Image 组件。</param>
        /// <param name="colorVector">包含 RGB 值（0~1）的 Vector3。</param>
        /// <param name="alpha">透明度（自动 Clamp 到 0~1）。</param>
        public static void SetImageColor(Image img, Vector3 colorVector, float alpha)
        {
            if (img == null)
            {
                Log.Warning("[PlayerMathUtil] SetImageColor: Image is null.");
                return;
            }
            img.color = new Color(colorVector.x, colorVector.y, colorVector.z, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// 用 Vector3 的 RGB 分量设置 RawImage 的颜色。
        /// </summary>
        public static void SetRawImageColor(RawImage img, Vector3 colorVector, float alpha)
        {
            if (img == null)
            {
                Log.Warning("[PlayerMathUtil] SetRawImageColor: RawImage is null.");
                return;
            }
            img.color = new Color(colorVector.x, colorVector.y, colorVector.z, Mathf.Clamp01(alpha));
        }

        /// <summary>
        /// 用现有 Color 设置 RawImage 的颜色（仅替换透明度）。
        /// </summary>
        public static void SetRawImageColor(RawImage img, Color color, float alpha)
        {
            if (img == null)
            {
                Log.Warning("[PlayerMathUtil] SetRawImageColor: RawImage is null.");
                return;
            }
            color.a = Mathf.Clamp01(alpha);
            img.color = color;
        }

        #endregion

        #region 角度工具

        /// <summary>
        /// 计算角色正前方与目标欧拉角方向之间的有符号夹角，范围 (-180, 180)。
        /// 正值表示目标在右侧，负值表示目标在左侧。
        /// </summary>
        /// <param name="player">角色 Transform。</param>
        /// <param name="targetAngle">目标欧拉 Y 角（度）。</param>
        public static float GetDeltaAngle(Transform player, float targetAngle)
        {
            Vector3 targetDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            return GetDeltaAngle(player.forward, targetDir);
        }

        /// <summary>
        /// 计算角色正前方与目标方向之间的有符号夹角，范围 (-180, 180)。
        /// Y 分量忽略（投影到水平面）。
        /// </summary>
        public static float GetDeltaAngle(Transform player, Vector3 toDir)
        {
            return GetDeltaAngle(player.forward, toDir);
        }

        /// <summary>
        /// 计算两个向量在水平面（忽略 Y）的有符号夹角，范围 (-180, 180)。
        /// </summary>
        public static float GetDeltaAngle(Vector3 startDir, Vector3 toDir)
        {
            float startAngle = Mathf.Atan2(startDir.x, startDir.z) * Mathf.Rad2Deg;
            float endAngle   = Mathf.Atan2(toDir.x,   toDir.z)   * Mathf.Rad2Deg;
            return Mathf.DeltaAngle(startAngle, endAngle);
        }

        #endregion

        #region 跳跃物理（表现层用）

        /// <summary>
        /// 由跳跃最大高度和重力（负值）反算跳跃初速度。
        /// 公式：v = sqrt(-2 * gravity * height)。
        /// 仅供表现层（CharacterController / Animancer 驱动）使用；
        /// 逻辑层跳跃速度在 PlayerMoveSystem 中用定点整数常量直接定义。
        /// </summary>
        /// <param name="jumpMaxHeight">最大跳跃高度（米，正值）。</param>
        /// <param name="gravity">重力加速度（负值，如 -12）。</param>
        public static float GetJumpInitVelocity(float jumpMaxHeight, float gravity)
        {
            return Mathf.Sqrt(-2f * gravity * jumpMaxHeight);
        }

        #endregion

        #region 定点数辅助（逻辑层桥接）

        /// <summary>
        /// 把表现层的 Vector2 摇杆输入转换为定点归一化方向，供写入 PlayerInputComponent。
        /// 相机朝向修正（cameraForward / cameraRight）也在此完成，保证定点输出确定性。
        /// 结果是水平面上的归一化 SyncVector3（y = 0）。
        /// </summary>
        /// <param name="moveInput">GameModule.Input.Move（Vector2）。</param>
        /// <param name="cameraForward">相机正前方（世界空间，水平分量）。</param>
        /// <param name="cameraRight">相机正右方（世界空间，水平分量）。</param>
        public static SyncVector3 InputToSyncDir(Vector2 moveInput, Vector3 cameraForward, Vector3 cameraRight)
        {
            // 水平面投影，去掉 Y 分量后归一化，确保方向在地面平面内
            Vector3 forward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;
            Vector3 right   = new Vector3(cameraRight.x,   0f, cameraRight.z).normalized;

            Vector3 worldDir = forward * moveInput.y + right * moveInput.x;
            return SyncVector3.FromVector3(worldDir).Normalized();
        }

        #endregion
    }
}
