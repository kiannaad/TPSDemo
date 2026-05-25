# 自研角色控制器前必须掌握的 Unity 物理知识清单

## 1. 结论

如果目标是自己实现一个可控、可调、能处理坡面和贴地的角色控制器，那么有必要学习 Unity 物理系统，但不需要先把整套 PhysX 话题全部学完。

当前真正必须掌握的重点，不是刚体受力细节，而是：

- 碰撞查询
- 接触结果理解
- 碰撞层过滤
- 查询时序
- 胶囊体几何
- 基本穿插修正

一句话说，就是：

**要先学会如何用 Unity 物理系统“看懂环境”，再决定如何自己写角色运动规则。**

## 2. 这份清单服务什么目标

这份清单服务的目标非常具体：

- 不依赖 `CharacterController`
- 不把 `Rigidbody` 当主控制器
- 自己写出一个 kinematic 风格的角色 motor
- 用 Unity 现成的物理查询能力做地面探测、坡面判断、墙面滑动和穿插修正

所以这里的重点不是“让物理引擎替你移动角色”，而是“借 Unity 物理系统获得环境信息”。

## 3. 必须掌握的知识分层

### 3.1 第一层：必须先掌握

这是你现在立刻要补的内容。

#### 1. Collider 基础

至少要搞清楚：

- `BoxCollider`
- `SphereCollider`
- `CapsuleCollider`
- `MeshCollider`
- Trigger 和非 Trigger 的区别

为什么重要：

角色控制器首先是在和场景几何交互，不知道碰撞体类型，很难判断地形行为为什么异常。

#### 2. Raycast / SphereCast / CapsuleCast

至少要会：

- 往地面打一条射线
- 用球或胶囊去扫前方
- 读取 `RaycastHit.point`
- 读取 `RaycastHit.normal`
- 读取 `RaycastHit.distance`

为什么重要：

这是地面检测、墙面检测、坡度分析的基本工具。

#### 3. Overlap 检测

至少要了解：

- `CheckSphere`
- `OverlapSphere`
- `OverlapCapsule`

为什么重要：

cast 更适合“沿路径查询”，overlap 更适合“当前位置是否已经嵌入或靠近某物体”。

#### 4. 法线和坡度角

必须会：

- 理解 `hit.normal`
- 用法线和世界上方向计算坡度角
- 根据角度区分 walkable slope 和 wall

为什么重要：

角色控制器很多问题都不是“有没有碰撞”，而是“碰到的表面是不是可走地面”。

#### 5. LayerMask 和 QueryTriggerInteraction

至少要会控制：

- 地面查询打到哪些层
- 忽略哪些层
- 是否命中 Trigger

为什么重要：

如果查询层控制不好，地面检测、子弹检测、相机遮挡都会出错。

#### 6. Update / FixedUpdate / deltaTime

至少要真正搞清楚：

- 输入通常在哪收
- 查询通常在哪做
- 位移通常在哪更新
- `Time.deltaTime` 和 `Time.fixedDeltaTime` 的区别

为什么重要：

角色控制器最终不只是几何问题，也是时序问题。时序没理清，移动会抖、相机会漂、输入会发粘。

### 3.2 第二层：开始写 motor 时必须掌握

#### 7. ComputePenetration

至少要知道：

- 它是做什么的
- 什么时候适合用
- 如何拿到推出方向和距离

为什么重要：

自研角色控制器经常需要做 depenetration。光会 cast 还不够，还要能把已经嵌进去的胶囊体推出去。

#### 8. ClosestPoint

为什么重要：

有助于做最近点分析、辅助调试和某些轻量接触修正。

#### 9. PhysicsScene

至少知道：

- 你默认在当前物理场景查询
- 查询 API 实际上都运行在 PhysicsScene 上

为什么重要：

现在不一定马上要用自定义 PhysicsScene，但知道它的边界有助于理解查询到底依赖什么环境。

#### 10. Rigidbody 的职责边界

至少要理解：

- 什么时候需要刚体
- 什么时候只需要 Collider
- 静态场景物体为什么通常不需要刚体
- 为什么角色 motor 不应该默认依赖 Rigidbody 力驱动

为什么重要：

这样你才不会被 Unity Inspector 里一堆刚体参数带偏。

### 3.3 第三层：后续优化阶段再掌握

下面这些有价值，但不是你现在写第一版 motor 的前置条件：

- Physic Material
- Continuous Collision Detection
- Rigidbody 插值
- Joint
- 复合刚体系统
- Physics Debugger 的更深用法

## 4. 你现在最需要做的 8 个 Unity 物理实验

### 实验 1：射线看地面

目标：

- 从角色脚底向下打 `Raycast`
- 打印命中点、法线、距离

完成标准：

- 你能在平地和坡面都读出稳定法线

### 实验 2：球形探地和射线探地对比

目标：

- 同时做 `Raycast` 和 `SphereCast`
- 对比边缘、坡面、台阶附近的命中差异

完成标准：

- 你能解释为什么球形探测比单射线更适合角色接地

### 实验 3：胶囊体前向 sweep

目标：

- 用 `CapsuleCast` 探测角色前方障碍
- 读取碰撞距离和法线

完成标准：

- 你能判断“前面是墙”还是“前面只是一个可走坡面”

### 实验 4：坡度分类

目标：

- 根据命中法线计算坡度角
- 设置 walkable angle 阈值
- 输出当前是 ground 还是 steep slope

完成标准：

- 你能稳定区分可走坡和不可走坡

### 实验 5：LayerMask 过滤

目标：

- 给地面、角色、自定义触发区分层
- 分别测试查询命中结果

完成标准：

- 你能精确控制某个查询打到什么，不打到什么

### 实验 6：Overlap 和嵌入检测

目标：

- 让胶囊体和场景碰撞体有意重叠
- 用 `OverlapCapsule` 或相近方案检测重叠

完成标准：

- 你能知道角色当前是否已经处于不合法位置

### 实验 7：ComputePenetration 推出

目标：

- 对一个已经发生重叠的胶囊体
- 读取推出方向和推出距离

完成标准：

- 你能把角色从穿插状态稳定推出

### 实验 8：Update 和 FixedUpdate 行为对比

目标：

- 同样一套查询和移动逻辑分别放在不同更新时机里测试
- 观察输入感、抖动、相机跟随差异

完成标准：

- 你能说清楚当前角色控制器为什么选择某个时序

## 5. 这份清单对应到自研 motor 的模块关系

你后面写自己的角色控制器时，Unity 物理知识大致会落到这几个模块里：

### `GroundProbe`

会用到：

- `SphereCast`
- `CapsuleCast`
- `RaycastHit.normal`
- LayerMask

### `CollisionSolver`

会用到：

- `CapsuleCast`
- `OverlapCapsule`
- `ComputePenetration`

### `MovementState`

会消费：

- 坡度角
- Grounded 状态
- 命中法线
- 接地距离

### `CharacterMotor`

会决定：

- 查询在哪个时机执行
- 查询结果如何转成 grounded / airborne / wall slide 等状态

## 6. 当前阶段最容易走偏的地方

### 误区 1：先研究 Rigidbody 参数

问题：

会把注意力带到“调现成模拟”上，而不是建立自己的角色运动规则。

### 误区 2：只做 Raycast，不做体积查询

问题：

单点射线很难稳定处理边缘、坡面和台阶。

### 误区 3：只看有没有命中，不看法线

问题：

会把墙和地混在一起，坡面逻辑做不准。

### 误区 4：不区分查询和解算

问题：

会把“看到环境”和“决定怎么动”混成一团，后面脚本很难维护。

### 误区 5：不做可视化

问题：

物理查询不画 debug line / gizmo，很难知道自己为什么判错。

## 7. 推荐的学习顺序

建议顺序如下：

1. Collider 基础
2. Raycast / SphereCast / CapsuleCast
3. RaycastHit 法线和坡度角
4. LayerMask / Trigger 查询过滤
5. Overlap 检测
6. ComputePenetration
7. Update / FixedUpdate 时序对比
8. 再进入自己的 `GroundProbe` 和 `CollisionSolver`

## 8. 完成标准

当你能做到下面这些事时，就说明 Unity 物理前置已经够用了：

- 你能独立实现地面探测
- 你能区分地面、陡坡、墙面
- 你能控制查询层过滤
- 你能解释某次穿插是怎么发生的
- 你能把角色从重叠状态推出去
- 你能说明自己的 motor 为什么选某个 update 时序

## 9. 一句话总结

你现在需要学习的 Unity 物理，不是“怎么把角色完全交给物理引擎”，而是：

**如何把 Unity 物理查询系统当成你的环境感知层，为自研角色控制器提供稳定输入。**
