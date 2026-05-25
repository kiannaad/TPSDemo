# Doc Index

## 项目设计 / 整体规划

- [三个月 Steam 联机第三人称 PVE 射击试玩版计划](./项目设计/整体规划/three-month-steam-pve-shooter-demo-plan.md)
  - Scope: 记录三个月内完成 Steam 联机第三人称 PVE 射击试玩版的范围收敛、阶段目标和验收标准。
  - Format: Decision Record.
  - Date: 2026-05-23.
  - Tags: 项目规划, Steam, 联机, PVE, 第三人称射击, 试玩版.
  - Summary: 将项目目标收敛为 2 人合作线性闯关试玩版，并明确 3C、联机、AI、关卡、Steam 包装的做与不做。

## 项目设计 / 服务端设计

- [Unity + C++ + Steam 最终设计方案](./项目设计/服务端设计/unity-cpp-steam-design-plan.md)
  - Scope: 记录项目服务端与 Steam 联机方向的总体技术方案。
  - Format: Decision Record.
  - Date: 2026-05-22.
  - Tags: 服务端, Steam, C++, HTTP, WebSocket, 权威同步.
  - Summary: 明确客户端 Unity、服务端 C++、控制面 HTTP、实时面长连接、服务端权威和 hitscan 第一版战斗模型。
- [客户端正式开工前的服务端验收标准](./项目设计/服务端设计/client-start-server-readiness-checklist.md)
  - Scope: 记录客户端正式开发前服务端需要达到的最低稳定标准。
  - Format: Checklist.
  - Date: 2026-05-22.
  - Tags: 服务端, 验收标准, 客户端开工, 联机闭环.
  - Summary: 明确登录、房间、心跳、重连、同步和调试链路等最低验收条件。
- [主链路分析与核心概念拆解笔记](./项目设计/服务端设计/main-chain-analysis-notes.md)
  - Scope: 记录服务端主链路分析方法和 Session、Room、Heartbeat 等核心概念。
  - Format: Learning Note.
  - Date: 2026-05-22.
  - Tags: 服务端, 主链路, Session, Room, Heartbeat.
  - Summary: 拆解服务端核心对象的职责、状态和行为边界。

## 项目设计 / 客户端设计

- [最小 3C 客户端设计方案](./项目设计/客户端设计/minimal-3c-client-design.md)
  - Scope: 记录当前阶段客户端 3C 的最小可落地架构。
  - Format: Design Note.
  - Date: 2026-05-22.
  - Tags: 客户端, 3C, PlayerMotor, 动画, 射击.
  - Summary: 定义输入、运动、动画、射击和表现层边界，避免运动、动画和射击逻辑耦合。
- [项目网络同步设计](./项目设计/客户端设计/network-sync-design.md)
  - Scope: 记录项目网络同步的总体模型和客户端/服务端职责划分。
  - Format: Design Note.
  - Date: 2026-05-22.
  - Tags: 客户端, 网络同步, 服务端权威, 快照, 增量同步.
  - Summary: 说明服务端权威、客户端镜像还原、RPC/Notify、SyncObject 和实体同步主链路。

## 项目设计 / 工具设计

- [AI Code Reviewer MVP](./项目设计/工具设计/ai-code-reviewer-mvp.md)
  - Scope: 记录 GitHub Actions 版 AI Code Reviewer 的最小实现闭环。
  - Format: Design Note.
  - Date: 2026-05-23.
  - Tags: GitHub Actions, AI Code Review, PR, Diff, 自动化.
  - Summary: 说明 PR 触发、diff 获取、规则审查、可选 AI 审查、PR Review 写回和严重问题打回策略。

## 知识积累 / 游戏设计

- [独立游戏立项方法论笔记](./知识积累/游戏设计/indie-game-project-initiation-notes.md)
  - Scope: 总结独立游戏立项方法论长文。
  - Format: Learning Note.
  - Date: 2026-05-22.
  - Tags: 独立游戏, 立项, 游戏设计, 项目管理, 风险控制.
  - Summary: 整理独游立项的底层逻辑、玩法核心、背包理论、周期控制、生态位、飞轮效应和长期技术积累方法。

## 知识积累 / 服务端

- [C++ 构建与链接入门笔记](./知识积累/服务端/cpp-build-basics.md)
  - Scope: 记录 C++ 构建、链接和头文件拆分的基础概念。
  - Format: Learning Note.
  - Date: 2026-05-22.
  - Tags: C++, 构建, 链接, 头文件, PowerShell.
  - Summary: 整理预处理、翻译单元、声明/定义、符号、链接和 PowerShell 运行脚本与可执行文件的基础认知。
- [WinSock2 Socket 与 HTTP 最小链路说明](./知识积累/服务端/winsock2-http-demo.md)
  - Scope: 记录 WinSock2 和 HTTP 最小服务端链路。
  - Format: Learning Note.
  - Date: 2026-05-22.
  - Tags: WinSock2, Socket, HTTP, 服务端, 路由.
  - Summary: 整理 socket、bind、listen、accept、recv、send 的职责关系，并补充 HTTP 请求语义、URL、Body、路由、Session、Room 和心跳概念。

## 知识积累 / 客户端

- [自研角色控制器前必须掌握的 Unity 物理知识清单](./知识积累/客户端/unity-physics-prerequisites-for-custom-character-motor.md)
  - Scope: 记录自研 Unity 角色 motor 前需要掌握的物理前置知识。
  - Format: Learning Note.
  - Date: 2026-05-22.
  - Tags: Unity, 物理, Character Motor, 碰撞, 射线检测.
  - Summary: 聚焦 Unity 物理查询、碰撞过滤、法线与坡度、穿插修正和更新时序。

## 知识积累 / 客户端 / 角色控制器

- [角色控制器相关物理学习笔记](./知识积累/客户端/角色控制器/character-controller-physics-study-notes.md)
  - Scope: 记录角色控制器相关物理概念和学习判断。
  - Format: Learning Note.
  - Date: 2026-05-22.
  - Tags: Unity, 角色控制器, 物理, 学习笔记.
  - Summary: 整理角色控制器相关的最小物理认知、约束思维、示例代码阅读价值判断以及推荐学习顺序。
- [自研 3C 物理控制器学习与实现规划](./知识积累/客户端/角色控制器/self-developed-3c-motor-study-plan.md)
  - Scope: 记录自研 3C 物理控制器的学习路径和实现规划。
  - Format: Learning Plan.
  - Date: 2026-05-22.
  - Tags: Unity, 3C, 角色控制器, PlayerMotor, 学习计划.
  - Summary: 规划角色运动控制器该学什么、怎么拆模块、按什么阶段实现，以及如何围绕坡面、贴地、摩擦和碰撞求解建立自己的 3C 物理模型。
