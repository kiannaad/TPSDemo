# AI Code Reviewer MVP 设计与使用说明

## Scope

本文记录当前仓库内第一版 AI Code Reviewer 的设计、实现过程、关键细节和使用方式。

当前版本不是完整 GitHub App，而是 GitHub Actions 版最小闭环。它支持两种使用方式：

- **直接 push 检查**：push 后自动获取本次 `before...after` diff，结果显示在 GitHub Actions，严重问题让 workflow 失败。
- **PR 审查**：如果以后使用 PR，也会获取 PR diff，并把结果写成 PR Review。

当前用户偏好是直接 push 后检查，所以日常使用以 push 模式为主。

新增文件：

- `.github/workflows/ai-code-review.yml`
- `.ai-review.yml`
- `tools/ai_code_reviewer.py`
- `.gitignore`

## Architecture

### Push 模式主链路

```text
git push
  -> GitHub Actions push event
  -> tools/ai_code_reviewer.py
  -> GitHub Compare API 获取 before...after diff
  -> path ignore 过滤
  -> Rule Review Engine
  -> AI Review Engine，可选
  -> Finding 去重与汇总
  -> Decision Engine
  -> Actions Summary + workflow status
```

### PR 模式主链路

```text
Pull Request opened / synchronized / reopened / ready_for_review
  -> GitHub Actions pull_request event
  -> tools/ai_code_reviewer.py
  -> GitHub PR Files API 获取 PR diff
  -> path ignore 过滤
  -> Rule Review Engine
  -> AI Review Engine，可选
  -> Finding 去重与汇总
  -> Decision Engine
  -> GitHub PR Review
```

### 模块职责

#### GitHub Actions 入口

文件：`.github/workflows/ai-code-review.yml`

职责：

- 监听任意分支 push。
- 监听 PR 事件：
  - `opened`
  - `synchronize`
  - `reopened`
  - `ready_for_review`
- 支持 `workflow_dispatch` 手动输入 PR number 触发。
- 声明 GitHub 权限：
  - `contents: read`
  - `pull-requests: write`
  - `issues: write`
- 注入运行环境变量：
  - `GITHUB_TOKEN`
  - `AI_REVIEW_API_KEY`
  - `AI_REVIEW_BASE_URL`
  - `AI_REVIEW_MODEL`
  - `OPENAI_API_KEY`，旧配置兼容兜底
  - `PR_NUMBER`

#### 配置文件

文件：`.ai-review.yml`

职责：

- 定义阻塞策略。
- 定义 diff 长度限制。
- 定义项目上下文文档。
- 定义忽略路径。
- 定义哪些路径变更需要测试或验证说明。
- 定义输出语言和审查严格程度。

当前配置是一个小型 YAML 子集，脚本内置轻量解析器，不依赖第三方 Python 包。

#### Reviewer 脚本

文件：`tools/ai_code_reviewer.py`

职责：

- 判断当前运行模式：`push`、`pull_request` 或手动 PR。
- push 模式下，读取事件里的 `before` 和 `after` sha。
- PR 模式下，读取 PR number。
- 调 GitHub REST API 拉取 changed files。
- 过滤不需要 review 的路径。
- 执行确定性规则审查。
- 如果配置了 `AI_REVIEW_API_KEY` 或 `OPENAI_API_KEY`，读取项目文档上下文并调用 OpenAI-compatible API 做语义审查。
- 合并并去重 findings。
- 生成 Markdown Review 内容。
- push 模式写入 Actions Summary 并打印日志。
- PR 模式通过 GitHub PR Review API 写回结果。
- 根据严重程度决定是否让 workflow 失败。

## Implementation Process

### 1. 先选 GitHub Actions MVP

原始完整形态可以是 GitHub App：

```text
Webhook Receiver -> Queue -> Diff Fetcher -> AI Review -> GitHub Reporter
```

第一版为了快速跑通，改成 GitHub Actions：

```text
push event -> workflow -> Python script -> Actions Summary / workflow status
pull_request event -> workflow -> Python script -> PR Review
```

原因：

- 当前仓库已经接入 GitHub，Actions 可以直接使用。
- 不需要先部署 Webhook 服务。
- 不需要数据库、队列和公网地址。
- 能先验证“diff 审查 + 严重问题失败检查”是否有价值。

### 2. 用 Python 标准库实现

脚本没有依赖第三方包。

使用的标准库包括：

- `urllib.request`: 调 GitHub API 和 OpenAI API。
- `json`: 处理 API 请求与响应。
- `re`: 规则扫描。
- `fnmatch`: 路径忽略匹配。
- `dataclasses`: 表示 review finding 和 review context。
- `pathlib`: 读配置和事件文件。

这样做的好处：

- GitHub Actions 的 Ubuntu runner 可直接运行。
- 不需要 `pip install`。
- CI 启动更快，失败点更少。

### 3. 同时支持 push 和 PR

脚本通过事件判断运行模式：

- 如果有 `PR_NUMBER`，进入手动 PR 模式。
- 如果事件里有 `pull_request`，进入 PR 模式。
- 如果事件是 `push` 或事件里有 `before/after`，进入 push 模式。

push 模式使用：

```text
GET /repos/{owner}/{repo}/compare/{before_sha}...{after_sha}
```

PR 模式使用：

```text
GET /repos/{owner}/{repo}/pulls/{pull_number}
GET /repos/{owner}/{repo}/pulls/{pull_number}/files
```

### 4. 先做确定性规则，再接 AI

当前 review 分两层：

```text
Rule Review Engine
  -> 稳定发现硬问题

AI Review Engine
  -> 结合项目文档分析语法/编译、业务语义、架构、可维护性、审美等问题
```

即使不配置 API key，工具仍然能运行确定性规则，并在 Actions Summary 中输出结果。

### 5. 输出统一成 Finding

规则和 AI 的审查结果都转换成同一个结构：

```text
severity
category
file
line
title
body
suggestion
confidence
source
```

好处：

- 后续可以统一排序、去重、统计。
- 可以统一决定是否失败。
- 后续接 inline comments 或 Check annotations 时更容易复用。

## Key Details

### 触发条件

当前 workflow：

```yaml
on:
  push:
    branches:
      - "**"
  pull_request:
    types: [opened, synchronize, reopened, ready_for_review]
  workflow_dispatch:
    inputs:
      pr_number:
        required: false
```

其中：

- 任意分支 push 后会审查该次 push 的 diff。
- 新 PR 创建时会审查。
- PR 有新 commit push 时会重新审查。
- Draft PR 转为 ready for review 时会审查。
- 手动触发时可以填 PR number。

### 路径过滤

第一版默认忽略 Unity 高噪声目录：

```yaml
paths:
  ignore:
    - "Library/**"
    - "Temp/**"
    - "Logs/**"
    - "obj/**"
    - "UserSettings/**"
    - "Assets/YooAsset/**"
    - "Assets/Character Controller Pro/**"
    - "Assets/KinematicCharacterController/**"
    - "*.generated.cs"
    - "*.meta"
```

原因：

- `Library/`、`Temp/`、`Logs/` 是 Unity 本地生成内容。
- `UserSettings/` 通常是用户本机配置。
- 插件目录第一版不作为主要审查对象。
- `.meta` 噪声较高，第一版先忽略。

### 确定性规则

当前规则包括：

- 疑似密钥、token、password、private key。
- 高风险执行调用，例如 `system`、`popen`、`ShellExecute`、`CreateProcess`、`eval`、`exec`。
- 疑似 SQL 字符串拼接。
- Unity 高频路径风险，例如 `GameObject.Find`、`FindObjectOfType`。
- 单文件超大 diff。
- 高风险路径变更缺少测试或验证说明。该项默认是 `minor`，不再因为普通 gameplay 小改动直接打成 `major`。

### 项目上下文

AI 审查会读取 `.ai-review.yml` 中的 `paths.context_docs`：

```yaml
paths:
  context_docs:
    - "Doc/项目设计/整体规划/three-month-steam-pve-shooter-demo-plan.md"
    - "Doc/项目设计/客户端设计/minimal-3c-client-design.md"
    - "Doc/项目设计/客户端设计/network-sync-design.md"
    - "Doc/项目设计/服务端设计/unity-cpp-steam-design-plan.md"
```

这些文档会作为 prompt 上下文，用于判断代码是否符合当前项目方向，例如：

- 当前目标是否仍然收敛在 2 人合作线性 PVE 试玩版。
- 客户端 3C 是否遵守输入、运动、动画、射击、表现分层。
- 真实移动是否由运动层决定，动画是否只消费运动结果。
- 射击是否以 hitscan 和相机中心射线为第一版命中真源。
- 联机逻辑是否让服务端或房主承担房间、敌人、伤害、死亡权威。

### AI 审查

如果仓库 Actions Secrets 中配置了 `AI_REVIEW_API_KEY` 或旧版 `OPENAI_API_KEY`，脚本会调用 OpenAI-compatible Chat Completions API。

默认模型：

```text
gpt-4o-mini
```

可通过仓库 Actions Variables 修改：

```text
AI_REVIEW_MODEL
AI_REVIEW_BASE_URL
```

默认 `AI_REVIEW_BASE_URL` 为：

```text
https://api.openai.com/v1
```

脚本会自动把 base url 规范化为 chat completions endpoint：

```text
https://api.openai.com/v1
  -> https://api.openai.com/v1/chat/completions

https://api.siliconflow.cn/v1
  -> https://api.siliconflow.cn/v1/chat/completions
```

AI prompt 要求只审查本次 diff，关注：

- compile
- business
- correctness
- security
- maintainability
- architecture
- performance
- testability
- style

其中 `business` 用来判断代码是否实现了看起来想实现的业务语义，以及是否符合项目文档约束。

如果 AI 调用失败或返回非法 JSON，脚本不会直接失败，而是退回到规则审查结果。

### 阻塞策略

当前默认策略：

```yaml
review:
  fail_on:
    - blocking
  max_major_before_fail: 3
```

也就是说：

- 任意 `blocking` 问题会让 workflow 失败。
- `major` 问题数量大于等于 3 会让 workflow 失败。
- `minor` 和 `nit` 不阻塞。

PR 模式下，失败时还会创建 `REQUEST_CHANGES` review。

### 输出位置

push 模式：

- GitHub Actions job summary。
- workflow 日志。
- workflow 成功或失败状态。
- Summary 会列出 reviewed files 和 ignored files，避免被忽略文件看起来像“空 diff”。
- findings 会按语法/编译、业务语义、正确性、安全、架构、性能、测试、规范等类别分组。

PR 模式：

- PR 页面中的 `AI Code Review` review。
- GitHub Actions job summary。
- workflow 成功或失败状态。

## How To Use

### 1. 确认文件已经进入仓库

需要提交这些文件：

```text
.github/workflows/ai-code-review.yml
.ai-review.yml
tools/ai_code_reviewer.py
.gitignore
Doc/项目设计/工具设计/ai-code-reviewer-mvp.md
```

### 2. 配置 OpenAI Key

进入 GitHub 仓库：

```text
Settings -> Secrets and variables -> Actions -> Secrets
```

推荐新增：

```text
AI_REVIEW_API_KEY
```

不配置也可以运行，但只会执行确定性规则，不会执行 AI 语义审查。

如果你使用 OpenAI 官方 API，也可以继续使用旧名字：

```text
OPENAI_API_KEY
```

但推荐新配置统一使用 `AI_REVIEW_API_KEY`。

### 3. 可选配置模型

进入：

```text
Settings -> Secrets and variables -> Actions -> Variables
```

新增：

```text
AI_REVIEW_MODEL=gpt-4o-mini
```

如果不配置，默认使用 `gpt-4o-mini`。

如果使用 SiliconFlow，在同一页面新增：

```text
AI_REVIEW_BASE_URL=https://api.siliconflow.cn/v1
AI_REVIEW_MODEL=你在 SiliconFlow 上选择的模型名
```

SiliconFlow 的 key 放在 Secrets：

```text
AI_REVIEW_API_KEY=你的 SiliconFlow API Key
```

### 4. 直接 push 后查看结果

日常使用只需要：

```powershell
git add .
git commit -m "Your change"
git push
```

然后进入 GitHub：

```text
Actions -> AI Code Review
```

查看最新 workflow。

如果有严重问题：

- workflow 会失败。
- job summary 里会显示 findings。
- 日志里会打印完整 review 内容。

如果没有严重问题：

- workflow 成功。
- job summary 会显示没有阻塞问题或只有非阻塞建议。

### 5. 用一个测试提交验证

可以在测试分支或临时提交里放一个假的危险代码，例如：

```csharp
public class AiReviewTest
{
    private string password = "dummy-password-12345";
}
```

注意不要放真实密钥。

push 后 Actions 应该失败，并在 summary 中提示疑似敏感凭据。

### 6. 使用 PR 时的行为

如果以后创建 PR，workflow 也会自动运行。

触发场景：

- 打开 PR
- 往 PR 分支 push 新 commit
- 重新打开 PR
- Draft PR 转成 ready for review

运行后，PR 页面会出现一条 `AI Code Review` review。严重问题会 `REQUEST_CHANGES`。

### 7. 调整规则

编辑 `.ai-review.yml`。

例如，如果不希望 3 个 major 自动失败，可以改成：

```yaml
review:
  fail_on:
    - blocking
  max_major_before_fail: 99
```

如果希望 `.meta` 文件也参与审查，移除：

```yaml
- "*.meta"
```

如果希望 gameplay 目录必须有测试，添加：

```yaml
paths:
  require_tests_for:
    - "Assets/Script/**"
    - "Assets/Game/**"
```

## Local Verification

### 语法检查

本地可运行：

```powershell
python -m py_compile tools\ai_code_reviewer.py
```

### 配置解析检查

可以用下面的 PowerShell 片段快速测试配置解析和路径匹配：

```powershell
@'
import sys
from pathlib import Path
sys.path.insert(0, str(Path("tools").resolve()))
import ai_code_reviewer as mod

config = mod.load_config(Path(".ai-review.yml"))
print(config["review"]["max_major_before_fail"])
print(mod.matches_any("Library/foo.asset", config["paths"]["ignore"]))
print(mod.matches_any("Assets/Script/Foo.cs", config["paths"]["require_tests_for"]))
'@ | python -
```

预期：

```text
3
True
True
```

### 本地完整运行限制

脚本完整运行依赖 GitHub Actions 环境变量。

push 模式需要：

- `GITHUB_REPOSITORY`
- `GITHUB_TOKEN`
- `GITHUB_EVENT_PATH`

PR 模式需要：

- `GITHUB_REPOSITORY`
- `GITHUB_TOKEN`
- `GITHUB_EVENT_PATH` 或 `PR_NUMBER`

所以本地默认只建议做语法和配置测试。完整端到端测试更适合通过 GitHub Actions 触发。

## Known Limitations

当前版本是 MVP，有这些限制：

- 不是 GitHub App，没有独立 webhook 服务和队列。
- Push 模式无法写 PR Review，只能看 Actions Summary 和 workflow 状态。
- 不做 job 去重，同一个 head sha 可能重复检查。
- 不做 inline comments。
- 不使用 GitHub Check Run annotations。
- YAML 解析器只支持当前配置所需的小子集。
- AI 只看 diff，不默认读取完整文件上下文。
- 大 diff 会被截断，AI 可能看不到全部内容。
- 对 Unity `.meta`、插件目录和资源改动默认不审查。
- 如果提交只包含 ignored files，AI 会看到空 diff；Summary 会明确列出这些 ignored files。
- 第一次 push 如果 `before` 是全 0，当前版本不会审查完整仓库，只会输出空 diff 结果。

## Future Improvements

后续可以按下面顺序增强：

1. 增加 Check Run annotations，让 push 模式也能有更清楚的行级结果。
2. 增加 head sha 去重，避免重复检查。
3. 增加完整 YAML parser，例如 PyYAML。
4. 增加 Unity/C# 专属规则：
   - `Update()` 中频繁分配
   - `Find` 出现在高频路径
   - 协程未停止
   - 事件订阅未取消
   - `async void` 滥用
5. 增加 C++ 服务端专属规则：
   - mutex 加锁范围过大
   - raw pointer 生命周期风险
   - socket 错误码未处理
   - JSON 字段缺少校验
6. 如果以后愿意恢复 PR 流程，再增加 inline review comments。
7. 从 GitHub Actions MVP 演进为 GitHub App：
   - Webhook Receiver
   - Job Queue
   - Review Worker
   - Review History DB
   - Repository-level policy

## Current Design Principle

这个工具的定位不是“让 AI 替代人合并代码”，而是：

```text
确定性规则兜底
+ AI 做语义审查
+ 统一裁决是否阻塞
+ Actions / PR 给人类可读反馈
```

它应该帮助开发者更早发现明显问题，尤其是安全、正确性、测试缺失和维护性风险。真正是否合并，仍然应该由人做最终判断。
