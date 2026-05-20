---
name: unity-compile
description: Unity 编译工具。使用 coplay-unity MCP 触发 Unity 编译并检查编译结果。触发场景：(1) 修改 C# 脚本后需要编译验证 (2) 检查编译错误 (3) 等待 Unity 编译完成
---

# Unity 编译工具

使用 coplay-unity MCP 工具触发 Unity 编译并检查编译结果。

## 前置条件

1. Unity 编辑器必须正在运行
2. MCP 连接正常（运行 `/mcp` 确认 `coplay-mcp: ✓ Connected`）

## 使用方式

当需要编译 Unity 项目时，调用此 skill：

```
使用 Skill 工具，skill = "unity-compile"
```

## 编译流程

### 1. 触发编译

使用 `refresh_unity` 工具触发 Unity 刷新和编译：

```json
{ "tool": "refresh_unity", "params": {} }
```

### 2. 等待编译完成

Unity 编译通常需要几秒到几十秒，取决于修改的文件数量。编译完成后编辑器会响应。

### 3. 检查编译结果

使用 `read_console` 检查编译错误：

```json
{ "tool": "read_console", "params": { "count": 50, "logLevel": "Error" } }
```

## 编译结果判断

| 控制台输出 | 状态 |
|-----------|------|
| 无错误日志 | ✅ 编译成功 |
| 有 `CSxxxx` 错误 | ❌ 编译失败，需要修复 |
| 有 `Exception` 错误 | ⚠️ 运行时错误 |

## 常见编译错误

| 错误码 | 说明 | 解决方案 |
|--------|------|---------|
| CS0103 | 找不到类型/命名空间 | 检查 using 声明或类型拼写 |
| CS0246 | 找不到类型 | 检查程序集引用 |
| CS0117 | 类型不包含定义 | 检查方法/属性名称 |
| CS0120 | 静态上下文引用非静态成员 | 添加实例引用或改为静态 |
| CS1503 | 参数类型不匹配 | 检查方法参数类型 |

## 完整编译检查示例

```
1. 调用 refresh_unity 触发编译
2. 等待 2-3 秒
3. 调用 read_console logLevel=Error count=30 检查错误
4. 如有错误，根据错误信息修复代码
5. 重复步骤 1-4 直到无错误
```

## 批量操作

如果需要在编译后执行其他操作，使用 `batch_execute`：

```json
{
  "tool": "batch_execute",
  "commands": [
    { "tool": "refresh_unity", "params": {} },
    { "tool": "execute_menu_item", "params": { "menuItem": "HybridCLR/Generate/All" } }
  ],
  "failFast": true
}
```

## 注意事项

1. **编译时间**：首次编译或大量修改可能需要较长时间
2. **错误定位**：控制台错误会显示文件路径和行号
3. **自动编译**：Unity 会在脚本修改后自动编译，`refresh_unity` 用于强制刷新
4. **程序集定义**：检查 `.asmdef` 文件确保正确的程序集引用
