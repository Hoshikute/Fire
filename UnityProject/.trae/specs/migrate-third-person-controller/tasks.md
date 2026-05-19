# Tasks
- [x] Task 1: 盘点源工程依赖与迁移策略
  - [x] 列出源工程使用的 Unity Package（InputSystem/Cinemachine/URP/第三方库等）与版本/配置点
  - [x] 对比本项目现状，确定需要安装、替换或禁用的依赖项
  - [x] 输出资源落盘映射（AssetArt vs AssetRaw）与命名/目录迁移规则（不引入新的体系）

- [x] Task 2: 导入并重构资源目录（AssetArt + AssetRaw）
  - [x] 将源工程 Assets 导入到本项目临时目录（保留 .meta，确保 GUID 稳定）
  - [x] 按映射规则迁移到 `Assets/AssetArt/ThirdPersonController/...` 与 `Assets/AssetRaw/ThirdPersonController/...`
  - [x] 修复引用断裂（材质/贴图/AnimatorController/Prefab/Timeline 等）
  - [x] 确认资源可被现有资源收集/打包工具识别（如 AssetBundleCollector 配置需要补充则补充）

- [x] Task 3: 新增 HotFix 角色模块并接入控制器
  - [x] 在 `Assets/GameScripts/HotFix/GameLogic/Module` 下创建 Character/Player 模块骨架（遵循现有模块风格）
  - [x] 将第三人称控制器接入模块，并消除与主工程/第三方脚本的耦合点（必要时做适配层）
  - [x] 若源工程包含 Editor 脚本，迁移到本项目 `Assets/Editor` 体系下或剔除（按实际需要）

- [x] Task 4: 输入系统适配（复用现有 InputActions）
  - [x] 分析源工程输入 ActionMap 与绑定
  - [x] 将所需 ActionMap 合并/追加到项目现有 InputActions（`Assets/TEngine/Extension/InputModule`）或通过代码方式扩展（以不冲突为前提）
  - [x] 验证输入在编辑器与运行时均可驱动控制器逻辑

- [x] Task 5: 提供最小验证场景与运行验证
  - [x] 创建/迁入一个最小验证场景（角色 + 相机 + 地面 + 必要光照），用于验证移动/转向/镜头/基础动作
  - [x] 将场景纳入可运行入口（按现有启动/流程体系接入，不绕过框架直接进场）
  - [x] 运行验证并修复遗留问题（Missing Script、报错、资源加载方式不符合规范等）

# Task Dependencies
- Task 2 depends on Task 1
- Task 3 depends on Task 2
- Task 4 depends on Task 3
- Task 5 depends on Task 2, Task 3, Task 4
