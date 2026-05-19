# EGamePlay战斗框架入门

## 1. 这套战斗框架是什么

`EGamePlay` 在当前项目里是一套独立的战斗运行时内核，不是单纯挂在场景上的几个 `MonoBehaviour` 脚本。

你可以先把它理解成下面这套结构：

1. `TEngine` 负责模块管理、热更新入口、UI 和基础运行环境。
2. `BattleModule` 负责把战斗系统初始化并挂进项目主框架。
3. `EGamePlay` 负责真正的战斗逻辑，包括单位、技能、Buff、执行体、伤害结算。
4. Unity 场景里的 `GameObject`、特效、动画更多承担表现职责。

一句话概括：

`EGamePlay = 用 Entity/Component + Ability/Trigger/Effect 组织起来的战斗规则内核`

---

## 2. 在当前项目里的接入位置

### 2.1 模块入口

战斗模块入口在：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/BattleModule.cs`
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/IBattleModule.cs`

热更新主入口会显式初始化战斗模块：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/GameApp.cs`

模块访问统一走：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/GameModule.cs`

### 2.2 初始化时做了什么

`BattleModule.EnsureInitialized()` 主要做了这几件事：

1. 设置 `SynchronizationContext`
2. 创建 `ECSNode`
3. 创建 `TimerManager`
4. 创建全局战斗上下文 `CombatContext`
5. 读取 `Configs.prefab` 上的 `ReferenceCollector`
6. 挂载 `ConfigManageComponent`，把战斗配置加载进运行时

所以 `BattleModule` 可以理解成：

`EGamePlay` 在本项目里的宿主模块

---

## 3. 初学者先建立的核心认知

学习这套框架时，建议先记住下面 7 句话：

1. `CombatContext` 代表一场战斗
2. `CombatEntity` 代表一个战斗单位
3. `Ability` 统一表示技能、Buff、被动等能力
4. `Trigger` 决定什么时候触发
5. `Effect` 决定触发后产生什么结果
6. `AbilityExecution` 代表一次技能执行过程
7. 伤害、治疗、加 Buff 等结算最终都会落到具体 `Action`

---

## 4. 整体分层怎么理解

对初学者来说，最适合按下面 5 层理解：

### 4.1 模块层

负责把战斗系统接入项目主框架。

关键类：

- `BattleModule`
- `IBattleModule`

### 4.2 上下文层

负责管理“当前这一场战斗”的全局数据与流程。

关键类：

- `CombatContext`

### 4.3 单位层

负责描述“角色/怪物在战斗系统里的运行时实体”。

关键类：

- `CombatEntity`

### 4.4 能力层

负责统一表示技能、Buff、被动等能力。

关键类：

- `Ability`
- `SkillComponent`
- `StatusComponent`

### 4.5 执行层

负责处理技能释放后的表现流程与结算过程。

关键类：

- `SpellComponent`
- `SpellAction`
- `AbilityExecution`
- `DamageAction`
- `AddStatusAction`

---

## 5. CombatContext：一场战斗的根

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/CombatContext.cs`

### 5.1 它的职责

`CombatContext` 是战斗世界的根实体，主要负责：

1. 保存战场中的单位
2. 区分英雄阵营和敌方阵营
3. 处理死亡事件
4. 组织战斗流程

### 5.2 当前项目里的实现特点

当前仓库里的 `CombatContext` 明显保留了回合制组织方式，例如：

- `HeroEntities`
- `EnemyEntities`
- `RoundActions`
- `StartCombat()`
- `RefreshRoundActions()`

这说明：

`EGamePlay` 本身是通用战斗内核，但当前项目中的这一份实现，至少包含一套回合制战斗流程组织方式。

### 5.3 初学者要注意

`CombatContext` 不是 Unity 场景，不是 UI，也不是普通管理器脚本。

它更像：

`战斗规则运行时的根节点`

---

## 6. CombatEntity：战斗单位

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/_CombatEntity/CombatEntity.cs`

### 6.1 它是什么

`CombatEntity` 是角色或怪物在战斗系统里的运行时实体。

场景里也许有一个英雄模型，但在 `EGamePlay` 内核里，真正参与战斗逻辑的是它对应的 `CombatEntity`。

### 6.2 它初始化时挂了什么

`CombatEntity.Awake()` 里会挂很多组件，比较关键的是：

1. `AttributeComponent`
2. `BehaviourPointComponent`
3. `AbilityComponent`
4. `StatusComponent`
5. `SkillComponent`
6. `SpellComponent`
7. `MotionComponent`
8. `HealthPointComponent`

这说明这套框架的设计思路是：

`战斗单位 = 实体 + 组件`

而不是把所有逻辑都堆到一个角色类里。

### 6.3 它自带哪些行动能力

`CombatEntity` 在初始化时还会创建一批“行动能力”，例如：

- `SpellActionAbility`
- `DamageActionAbility`
- `CureActionAbility`
- `AddStatusActionAbility`
- `AttackActionAbility`
- `RoundActionAbility`

这些能力负责“生成具体 Action”，不直接等于技能本体。

这是初学者特别容易混淆的一点：

- `Ability` 更偏“技能/Buff/被动定义”
- `ActionAbility` 更偏“某类行动的执行入口”

---

## 7. AttributeComponent 和 HealthPointComponent：数值基础

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/_CombatEntity/Components/AttributeComponent.cs`
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/_CombatEntity/Attribute/HealthPointComponent.cs`

### 7.1 AttributeComponent 做什么

`AttributeComponent` 管理角色的战斗数值，例如：

- 生命上限
- 当前生命
- 攻击
- 防御
- 暴击率
- 移速

它底层不是直接放 `int` 或 `float` 字段，而是封装成 `FloatNumeric` 进行管理。

### 7.2 HealthPointComponent 做什么

`HealthPointComponent` 专门处理：

- 当前血量
- 血量上限
- 扣血
- 加血
- 死亡判断

比如伤害真正落地时，最终会走：

- `ReceiveDamage()`

死亡判断则走：

- `CheckDead()`

### 7.3 为什么要拆开

因为“属性系统”和“血量结算”虽然相关，但职责不同：

- `AttributeComponent` 更像角色数值仓库
- `HealthPointComponent` 更像生命值操作器

---

## 8. Ability：技能、Buff、被动的统一抽象

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/Ability/Ability.cs`

### 8.1 为什么它最重要

`Ability` 是整套框架最核心的抽象之一。

在这套设计里，下面这些东西都可以统一视作 `Ability`：

1. 主动技能
2. Buff
3. 被动能力
4. 条件触发能力

### 8.2 它里面最关键的字段

`Ability` 常见关键字段有：

- `OwnerEntity`
- `Config`
- `ConfigObject`
- `ExecutionObject`
- `CooldownTimer`
- `Spelling`
- `IsBuff`
- `IsSkill`

### 8.3 初始化时做了什么

`Ability.Awake()` 里会做几件关键事：

1. 读取 `AbilityConfigObject`
2. 根据 `Id` 读取表配置 `AbilityConfig`
3. 解析目标阵营
4. 解析目标选择类型
5. 挂载 `AbilityEffectComponent`
6. 挂载 `AbilityTriggerComponent`
7. 尝试加载 `ExecutionObject`

这说明它不是纯数据对象，而是：

`运行时能力实体`

### 8.4 一个很重要的统一思想

框架不是为每种技能写一套专属类，而是先统一成 `Ability`，再通过：

- 配置
- 触发器
- 效果
- 执行体

把差异表达出来。

这就是它“数据驱动”的核心。

---

## 9. SkillComponent、AbilityComponent、StatusComponent 的分工

核心文件：

- `AbilityComponent.cs`
- `SkillComponent.cs`
- `StatusComponent.cs`

### 9.1 AbilityComponent

`AbilityComponent` 是能力总容器。

它负责：

- 挂载 `Ability`
- 维护能力字典
- 移除能力

可以理解为：

`所有能力的总仓库`

### 9.2 SkillComponent

`SkillComponent` 专门管理“技能型 Ability”。

主要负责：

- 挂技能
- 按名称索引技能
- 按 ID 索引技能
- 记录按键绑定

### 9.3 StatusComponent

`StatusComponent` 专门管理“状态/Buff 型 Ability”。

主要负责：

- 附加状态
- 移除 Buff
- 判断某个状态是否存在
- 根据当前所有状态刷新行动限制

它还会汇总状态效果，更新 `CombatEntity.ActionControlType`，例如控制移动禁制。

### 9.4 这三个组件怎么记

可以用一句话记：

- `AbilityComponent` 管总表
- `SkillComponent` 管技能
- `StatusComponent` 管 Buff

---

## 10. 配置系统：表配置 + ScriptableObject 配置

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameProto/Battle/Scripts/AbilityConfig.cs`
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/ConfigObject/AbilityConfigObject.cs`
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/ConfigObject/Other/AbilityManagerObject.cs`
- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Component/ConfigManageComponent.cs`

### 10.1 为什么是两套配置

当前项目里的战斗配置明显是“双层结构”：

1. 表配置
2. 可视化对象配置

### 10.2 表配置负责什么

`AbilityConfig` 里能看到一些基础字段：

- `Id`
- `KeyName`
- `Name`
- `Type`
- `TargetGroup`
- `TargetSelect`
- `Cooldown`
- `CanStack`

这类配置更偏“规则定义”和“基础数值字段”。

### 10.3 AbilityConfigObject 负责什么

`AbilityConfigObject` 是编辑器里可视化配置的战斗对象，主要承载：

- 触发点列表 `TriggerActions`
- 效果列表 `Effects`
- 目标类型
- 施法类型

所以可以理解为：

- `AbilityConfig` 回答“这个能力是什么”
- `AbilityConfigObject` 回答“这个能力具体如何工作”

### 10.4 AbilityManagerObject 负责什么

`AbilityManagerObject` 管理资源目录约定，比如：

- 技能资源目录
- Buff 资源目录
- 执行体资源目录

这使得 `Ability` 和 `Action` 能按统一规则加载资源。

### 10.5 ConfigManageComponent 负责什么

`ConfigManageComponent` 会从 `Configs.prefab` 的 `ReferenceCollector` 中读取配置文本，再初始化对应配置分类。

所以这套战斗系统不是把所有配置写死在代码里，而是：

`运行时按配置驱动行为`

---

## 11. Trigger + Effect：这套框架真正的规则引擎

核心文件：

- `AbilityTriggerComponent.cs`
- `AbilityTrigger.cs`
- `AbilityEffectComponent.cs`

### 11.1 先记一句最重要的话

`Trigger` 决定“什么时候发动”  
`Effect` 决定“发动后做什么”

### 11.2 AbilityEffectComponent 做什么

`AbilityEffectComponent` 持有一个 `Ability` 上配置的所有效果。

每个效果会被包装成 `AbilityEffect` 实体。

常见理解方式：

- 一个技能可以有多个效果
- 例如造成伤害、加 Buff、播放特效、修改属性

### 11.3 AbilityTriggerComponent 做什么

`AbilityTriggerComponent` 持有一个 `Ability` 上配置的所有触发器。

每个触发器会被包装成 `AbilityTrigger` 实体。

### 11.4 AbilityTrigger 怎么工作

`AbilityTrigger.EnableTrigger()` 里支持多种触发模式，例如：

1. `Instant`
2. `Action`
3. `Condition`

也就是：

1. 激活即触发
2. 行为点事件触发
3. 条件/时间状态触发

### 11.5 真正触发后会发生什么

`AbilityTrigger.OnTrigger()` 的逻辑可以概括成：

1. 计算本次触发上下文 `TriggerContext`
2. 确定目标
3. 做状态条件检查
4. 找到本次触发要应用的效果
5. 为每个效果生成 `EffectAssignAction`
6. 执行效果分配

这就是 `EGamePlay` 最核心的扩展机制。

你以后做新技能时，很多时候不是新增一大堆逻辑代码，而是：

`配置新的 Trigger 和 Effect 组合`

---

## 12. SpellComponent：技能释放入口

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/_CombatEntity/Components/SpellComponent.cs`

### 12.1 它负责什么

`SpellComponent` 负责把“我要放一个技能”转换成战斗系统里的施法动作。

常用入口有：

- `SpellWithTarget()`
- `SpellWithPoint()`

### 12.2 施法时做了什么

以 `SpellWithTarget()` 为例，大致流程是：

1. 检查当前是否已有执行中的技能
2. 从 `SpellActionAbility` 生成一个 `SpellAction`
3. 填入技能本体
4. 填入输入目标
5. 计算角色朝向
6. 调用 `SpellSkill()`

### 12.3 初学者要注意

这里真正开始的是“施法动作”，不是直接伤害结算。

也就是说：

`按下技能 -> 先进入 SpellAction -> 再创建 AbilityExecution`

---

## 13. SpellAction：把技能变成一次执行过程

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/Action/Actions/SpellAction.cs`

### 13.1 它是什么

`SpellAction` 是一次施法行为本身。

它保存这次施法的输入数据，例如：

- `SkillAbility`
- `InputTarget`
- `InputPoint`
- `InputDirection`
- `InputRadian`

### 13.2 SpellSkill() 做什么

`SpellSkill()` 是技能执行链的重要起点，它会：

1. 触发施法前行为点
2. 创建 `AbilityExecution`
3. 将 `ExecutionObject` 赋给执行体
4. 加载执行片段
5. 写入技能目标、输入目标、方向、位置
6. 调用 `BeginExecute()`

所以它的本质是：

`把一次施法请求转换成一次可持续更新的技能执行实体`

---

## 14. AbilityExecution：技能执行体

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/AbilityExecute/AbilityExecution.cs`

### 14.1 它是什么

`AbilityExecution` 是一次技能播放过程对应的运行时实体。

这点非常重要。

很多初学者会以为技能只是“调用一个函数”，但在这套框架里，技能释放过程本身被建模成了一个实体。

### 14.2 它承载哪些信息

`AbilityExecution` 里会保存：

- 所属 `Ability`
- 施法者 `OwnerEntity`
- 执行配置 `ExecutionObject`
- 技能目标列表
- 输入目标
- 输入点
- 输入方向
- 输入弧度
- 起始时间

### 14.3 生命周期怎么走

主要流程是：

1. `BeginExecute()`
2. 标记施法占用
3. 启动 `ExecutionClipComponent`
4. 持续更新
5. 时间到后 `EndExecute()`
6. 销毁执行体

### 14.4 为什么这种设计很重要

因为复杂技能往往不是瞬时完成的，它可能包含：

- 前摇动画
- 位移
- 生成碰撞体
- 子弹飞行
- 延迟伤害
- 持续特效

如果没有 `AbilityExecution` 这一层，这些临时状态就只能硬塞回技能或角色本体，代码会很乱。

---

## 15. DamageAction：伤害落地

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/Action/Actions/DamageAction.cs`

### 15.1 它是什么

`DamageAction` 是一次具体的伤害结算动作。

注意：

技能不等于伤害，伤害只是技能可能产生的一种结果。

### 15.2 它怎么计算伤害

当前代码里会根据伤害来源区分不同逻辑：

1. 普攻伤害
2. 技能伤害
3. Buff 伤害

还会处理：

- 暴击
- 多目标衰减
- 行为点前后置通知

### 15.3 真正扣血在哪里

最终会调用：

- `Target.GetComponent<HealthPointComponent>().ReceiveDamage(this)`

所以：

`DamageAction` 负责准备和组织结算  
`HealthPointComponent` 负责真正修改生命值

### 15.4 死亡怎么触发

结算结束后会检查：

- `CheckDead()`

如果死亡，就发布 `EntityDeadEvent`，然后由 `CombatContext` 统一处理清理。

---

## 16. AddStatusAction：Buff 挂载

核心文件：

- `UnityProject/Assets/GameScripts/HotFix/GameLogic/Module/Battle/Runtime/EGamePlay/Combat/Action/Actions/AddStatusAction.cs`

### 16.1 它是什么

`AddStatusAction` 是一次“给目标附加状态/Buff”的动作。

### 16.2 它的主要流程

大致是：

1. 找到目标 Buff 配置
2. 判断是否允许叠加
3. 不可叠加时刷新已有 Buff 持续时间
4. 可附加时通过 `StatusComponent.AttachStatus()` 挂载 Buff
5. 同步等级、参数
6. 必要时添加 `AbilityLifeTimeComponent`
7. 激活 Buff

### 16.3 它说明了什么

它很好地体现了 `EGamePlay` 的统一抽象：

`Buff 不是特殊分支逻辑，而是挂在角色身上的 Ability`

---

## 17. 行为点系统：扩展性的关键

`CombatEntity` 提供了：

- `ListenActionPoint()`
- `TriggerActionPoint()`

这背后是 `BehaviourPointComponent` 在工作。

### 17.1 行为点可以理解成什么

可以理解成：

`战斗内核里的可订阅挂点`

例如：

- 施法前
- 施法后
- 造成伤害前
- 造成伤害后
- 受到伤害前
- 受到伤害后
- 施加状态后
- 受状态后

### 17.2 为什么它很重要

很多扩展玩法都适合挂在这些点位上：

1. 被动技能
2. 反击
3. 护盾
4. 受击触发
5. 吸血
6. 天赋词条
7. 装备特效

所以从架构上看，这套框架的扩展力主要来自：

- 行为点
- 触发器
- 效果

---

## 18. 一条完整的技能链路

如果把一次技能释放串起来，可以先这样理解：

1. 某个 `CombatEntity` 身上已经挂好 `Ability`
2. 通过 `SpellComponent.SpellWithTarget()` 或 `SpellWithPoint()` 发起施法
3. `SpellComponent` 生成 `SpellAction`
4. `SpellAction.SpellSkill()` 创建 `AbilityExecution`
5. `AbilityExecution` 根据 `ExecutionObject` 驱动执行过程
6. 执行过程中的触发器 `Trigger` 被触发
7. 触发器挑选对应 `Effect`
8. 效果分配动作生成具体 `Action`
9. 例如生成 `DamageAction` 或 `AddStatusAction`
10. 这些 Action 最终修改血量、挂 Buff、触发死亡等结果

可以把它记成一句话：

`施法请求 -> 执行体 -> 触发器 -> 效果 -> 具体结算动作`

---

## 19. 对初学者最容易混淆的几个概念

### 19.1 Ability 和 Action 的区别

`Ability` 是能力定义和运行时能力实体。  
`Action` 是一次具体行为或结算动作。

简单理解：

- `Ability` 更像“技能本体”
- `Action` 更像“技能本体触发出来的一次实际行为”

### 19.2 Skill 和 Buff 的区别

在 `EGamePlay` 里，二者底层都可以是 `Ability`。

区别主要体现在：

- 配置类型不同
- 挂载位置不同
- 生命周期不同
- 触发条件不同

### 19.3 Execution 和 Effect 的区别

- `Execution` 更偏表现流程和技能执行过程
- `Effect` 更偏规则效果和实际作用

比如：

- 播动画、生成碰撞体、发射物体，常常属于执行过程
- 扣血、加状态、改属性，常常属于效果

### 19.4 CombatEntity 和 GameObject 的区别

- `GameObject` 是 Unity 表现对象
- `CombatEntity` 是战斗规则对象

不要把这两个概念混成一个。

---

## 20. 建议的学习顺序

如果你是第一次接触这套框架，建议按下面顺序读代码：

### 第一步：看模块入口

- `BattleModule.cs`
- `GameApp.cs`
- `GameModule.cs`

目标：

搞清楚战斗系统是怎么启动的。

### 第二步：看战斗根和单位

- `CombatContext.cs`
- `CombatEntity.cs`

目标：

搞清楚一场战斗和一个单位在运行时分别是什么。

### 第三步：看能力抽象

- `Ability.cs`
- `AbilityComponent.cs`
- `SkillComponent.cs`
- `StatusComponent.cs`

目标：

搞清楚技能和 Buff 为什么能被统一建模。

### 第四步：看施法链

- `SpellComponent.cs`
- `SpellAction.cs`
- `AbilityExecution.cs`

目标：

搞清楚“放技能”到底是怎么变成运行时执行流程的。

### 第五步：看规则触发与结算

- `AbilityTrigger.cs`
- `AbilityEffectComponent.cs`
- `DamageAction.cs`
- `AddStatusAction.cs`

目标：

搞清楚伤害和 Buff 是怎么真正落地的。

---

## 21. 适合初学者的学习方法

建议不要一上来就想“完全读懂全部代码”，而是先带着问题读。

可以先按下面 4 个问题走：

1. 一个角色的技能挂在哪里？
2. 点击释放技能后，第一段代码走到哪里？
3. 技能什么时候决定目标？
4. 伤害最后是在哪一层生效？

如果这 4 个问题都能回答清楚，你对这套框架就已经入门了。

---

## 22. 当前项目里值得注意的一点

从当前代码看，项目已经把 `EGamePlay` 战斗内核接进了 `BattleModule`，也有战斗示例 UI `BattleMainUI`。

但从现有热更入口来看，目前更像是：

- 已经接入战斗框架
- 已经具备战斗核心运行时
- 但业务侧完整战斗流程、场景联动、具体玩法包装可能还需要继续往上层补

所以学习时不要默认：

`项目业务层 = EGamePlay 全功能样板`

更准确的理解应该是：

`项目引入了一套完整战斗内核，并正在以模块化方式接入到现有框架中`

---

## 23. 最后总结

如果只用最短的话总结 `EGamePlay`：

1. 它是一套基于 `Entity/Component` 的战斗运行时框架
2. 它用 `Ability` 统一表示技能、Buff、被动
3. 它用 `Trigger + Effect` 描述战斗规则
4. 它用 `AbilityExecution` 承载技能执行过程
5. 它用 `Action` 完成具体结算

你后面继续深入时，最值得先吃透的是这条主链：

`CombatEntity -> Ability -> SpellAction -> AbilityExecution -> Trigger -> Effect -> DamageAction/AddStatusAction`

只要这条链你能顺着讲出来，说明你已经真正进入这套框架了。
