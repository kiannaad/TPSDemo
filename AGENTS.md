# AGENTS

Project-level navigation and generated doc index.

## How To Use Project Docs

- Before changing gameplay, networking, server, or project direction, read the relevant docs under `Doc/` first.
- Treat `Doc/项目设计/整体规划/three-month-steam-pve-shooter-demo-plan.md` as the current project scope guard.
- Treat `Doc/项目设计/服务端设计/unity-cpp-steam-design-plan.md` as the current networking/server direction unless the user explicitly overrides it.
- Treat `Doc/项目设计/客户端设计/minimal-3c-client-design.md` as the current minimum 3C client architecture.
- Treat `Doc/index.md` as the durable full document index.

## Doc Index

### 项目设计 / 整体规划

- [三个月 Steam 联机第三人称 PVE 射击试玩版计划](./Doc/项目设计/整体规划/three-month-steam-pve-shooter-demo-plan.md): 记录三个月内完成 2 人合作线性闯关试玩版的范围收敛、阶段目标、风险控制和验收标准。

### 项目设计 / 服务端设计

- [Unity + C++ + Steam 最终设计方案](./Doc/项目设计/服务端设计/unity-cpp-steam-design-plan.md): 沉淀后续开发的整体方向，包括最小在线闭环、FPS 权威同步闭环、HTTP 与 WebSocket 分层、protobuf 引入时机、阶段推进顺序以及 Postman/curl 调试方法。
- [客户端正式开工前的服务端验收标准](./Doc/项目设计/服务端设计/client-start-server-readiness-checklist.md): 明确服务端需要先稳定到什么程度，客户端才适合正式开始开发，并给出 10 条最低验收标准。
- [主链路分析与核心概念拆解笔记](./Doc/项目设计/服务端设计/main-chain-analysis-notes.md): 整理主链路分析的分层方法，并拆解 Session、Room、Heartbeat 三个常见服务端概念的系统角色、状态和行为。

### 项目设计 / 客户端设计

- [最小 3C 客户端设计方案](./Doc/项目设计/客户端设计/minimal-3c-client-design.md): 沉淀当前阶段客户端的最小 3C 分层设计，包括输入、运动、动画、射击和表现的职责边界，以及第一阶段的落地顺序。
- [项目网络同步设计](./Doc/项目设计/客户端设计/network-sync-design.md): 说明该项目网络同步的核心架构、主链路、服务端与客户端职责划分，以及客户端如何承接服务端权威状态。

### 项目设计 / 工具设计

- [AI Code Reviewer MVP](./Doc/项目设计/工具设计/ai-code-reviewer-mvp.md): 记录 GitHub Actions 版 AI Code Reviewer 的触发方式、配置方法、审查规则、阻塞策略和后续改进方向。

### 知识积累 / 游戏设计

- [独立游戏立项方法论笔记](./Doc/知识积累/游戏设计/indie-game-project-initiation-notes.md): 整理独游立项的底层逻辑、玩法核心、背包理论、周期控制、生态位、飞轮效应和长期技术积累方法。

### 知识积累 / 服务端

- [C++ 构建与链接入门笔记](./Doc/知识积累/服务端/cpp-build-basics.md): 整理 C++ 预处理、翻译单元、声明/定义、符号、链接、头文件拆分以及 PowerShell 运行脚本与可执行文件的基础认知。
- [WinSock2 Socket 与 HTTP 最小链路说明](./Doc/知识积累/服务端/winsock2-http-demo.md): 整理 WinSock2 中 socket、bind、listen、accept、recv、send 的职责关系，并补充 HTTP 请求语义、URL/Body/路由、Session/Room/心跳等概念模型。

### 知识积累 / 客户端

- [自研角色控制器前必须掌握的 Unity 物理知识清单](./Doc/知识积累/客户端/unity-physics-prerequisites-for-custom-character-motor.md): 聚焦 Unity 物理查询、碰撞过滤、法线与坡度、穿插修正和更新时序，明确自研角色 motor 前必须掌握的最小前置知识。

### 知识积累 / 客户端 / 角色控制器

- [角色控制器相关物理学习笔记](./Doc/知识积累/客户端/角色控制器/character-controller-physics-study-notes.md): 整理角色控制器相关的最小物理认知、约束思维、示例代码阅读价值判断以及推荐学习顺序。
- [自研 3C 物理控制器学习与实现规划](./Doc/知识积累/客户端/角色控制器/self-developed-3c-motor-study-plan.md): 规划角色运动控制器该学什么、怎么拆模块、按什么阶段实现，以及如何围绕坡面、贴地、摩擦和碰撞求解建立自己的 3C 物理模型。
