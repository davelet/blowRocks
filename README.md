# blowRocks 🚀💥

## 游戏概要

太空岩石爆破 — 2D 俯视角太空射击游戏

玩家驾驶飞船在太空中飞行，射击并炸碎不断涌来的小行星。岩石被击中后会分裂成更小的碎片。坚持越久，难度越高。

## 核心玩法

- **移动**: WASD / 方向键控制飞船移动
- **瞄准**: 鼠标控制飞船朝向
- **射击**: 鼠标左键发射子弹
- **屏幕边缘穿越**: 飞船飞出一边会从对面出现

## 游戏机制

| 元素 | 说明 |
|------|------|
| 岩石大小 | 大 → 中 → 小，击中后分裂为 2 块更小的 |
| 分数 | 大岩石 10 分，中 25 分，小 50 分 |
| 生命 | 初始 3 条命，碰到岩石扣 1 |
| 难度 | 每波间隔缩短，每波岩石数量递增 |
| Game Over | 生命归零时触发，显示最终分数 |

## 项目结构

```
blowRocks/
├── Assets/
│   ├── Scenes/          # 游戏场景
│   ├── Scripts/
│   │   ├── Player/      # PlayerController.cs
│   │   ├── Asteroids/   # Asteroid.cs, AsteroidSpawner.cs
│   │   ├── Weapons/     # Bullet.cs
│   │   ├── Managers/    # GameManager.cs
│   │   └── UI/          # GameUI.cs
│   ├── Prefabs/         # 预制体（Player, Asteroids, Weapons, UI）
│   ├── Materials/       # 材质
│   ├── Sprites/         # 2D 精灵图
│   ├── Audio/           # 音效和背景音乐
│   └── Animations/      # 动画
├── ProjectSettings/     # Unity 项目设置
└── Packages/            # Unity 包管理
```

## 脚本架构

```
GameManager (单例)
├── 管理游戏状态 (Playing / GameOver)
├── 跟踪分数和生命
├── 触发重生逻辑
│
AsteroidSpawner
├── 在屏幕边缘随机生成岩石
├── 每波增加难度
│
PlayerController
├── WASD 移动
├── 鼠标瞄准
├── 左键射击
├── 屏幕边缘穿越
│
Asteroid
├── 随机漂移 + 旋转
├── 被击中 → 分裂或销毁
├── 接触玩家 → 伤害 + 销毁
│
Bullet
├── 飞行 + 碰撞检测
│
GameUI
├── 显示分数、生命
└── Game Over 面板
```

## 下一步

1. 在 Unity Hub 中创建 2D 项目，指向此目录
2. 设置 Sprite (飞船、岩石、子弹)
3. 创建 Prefab 并挂载脚本
4. 配置碰撞检测 (Collider2D + Rigidbody2D)
5. 搭建 UI (Canvas)
6. 添加音效和粒子效果
7. 调参平衡
