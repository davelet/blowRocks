=== 背景音乐系统使用说明 ===

已创建文件：
- Assets/Scripts/Managers/MusicManager.cs
- Assets/Scripts/UI/PauseMenu.cs (已添加 Instance 单例)
- Assets/Scripts/Managers/GameManager.cs (已集成 MusicManager)
- Assets/Scripts/Tests/MusicManagerTest.cs (测试脚本)

功能说明：
支持 4 种不同状态的背景音乐：
1. Menu - 启动/菜单：舒缓的太空氛围音乐（30秒循环）
2. Playing - 游戏中：紧张刺激的战斗节奏（20秒循环）
3. Paused - 暂停：空灵放松的氛围（25秒循环）
4. GameOver - 游戏结束：悲壮的尾声（8秒播放一次）

所有音乐都是程序化生成，无需外部音频文件。

重要更新：
1. MusicManager 现在会自动创建，无需手动在 Unity 编辑器中设置！
2. 音量完全跟随设置面板的百分比，不再写死！

设置步骤：

1. 在 Unity 编辑器中，打开 MainScene 场景

2. 直接运行游戏即可！
   - MusicManager 会在运行时自动创建
   - 会自动播放菜单音乐
   - 游戏开始后切换到战斗音乐
   - 按 ESC 暂停，切换到暂停音乐
   - 游戏结束时切换到结束音乐

测试不同音乐状态：

1. 使用测试脚本：
   - 将 MusicManagerTest.cs 挂载到场景中的任意对象上
   - 运行游戏后，按以下键测试：
     * M - 切换到游戏音乐
     * N - 切换到菜单音乐
     * B - 切换到暂停音乐
     * V - 切换到游戏结束音乐

2. 正常游戏流程测试：
   - 运行游戏 → 自动播放菜单音乐（但很快被游戏音乐覆盖）
   - 按 ESC 暂停 → 切换到暂停音乐
   - 再次按 ESC 恢复 → 切换回游戏音乐
   - 游戏结束 → 切换到游戏结束音乐

音量控制：
- 音乐音量完全跟随 SettingsManager.Volume
- 在设置面板中调整音量滑块，音乐会自动调整
- 不再有单独的音乐音量设置

调试步骤：

1. 运行游戏后，查看 Console 窗口的日志输出
   - 应该看到 "[MusicManager] AutoCreate - 自动创建 MusicManager"
   - 应该看到 "[MusicManager] Awake - 初始化完成"
   - 应该看到 "[MusicManager] Start - 开始"
   - 应该看到 "[MusicManager] PlayMusic - Menu, volume: X"
   - 应该看到 "[MusicManager] NotifyGameStarted - 游戏开始"
   - 应该看到 "[MusicManager] PlayMusic - Playing, volume: X"

2. 测试暂停功能：
   - 按 ESC 暂停
   - 应该看到 "[MusicManager] HandlePauseChanged - paused: True"
   - 应该看到 "[MusicManager] PlayMusic - Paused, volume: X"
   - 再次按 ESC 恢复
   - 应该看到 "[MusicManager] HandlePauseChanged - paused: False"
   - 应该看到 "[MusicManager] PlayMusic - Playing, volume: X"

3. 如果看到错误日志：
   - "[MusicManager] targetClip is null!" - 音频生成失败
   - "[MusicManager] SubscribeEvents - PauseMenu.Instance 为 null" - 事件订阅失败

自定义：

1. 修改音乐风格：
   - 打开 MusicManager.cs
   - 修改以下方法中的音频生成参数：
     * GenerateMenuMusic() - 菜单音乐
     * GenerateGameplayMusic() - 游戏音乐
     * GeneratePauseMusic() - 暂停音乐
     * GenerateGameOverMusic() - 游戏结束音乐

2. 调整淡入淡出时间：
   - 在 MusicManager.cs 中修改 fadeDuration 变量（单位：秒）

3. 修改循环时长：
   - 在各个 Generate*Music() 方法中修改 duration 变量

技术细节：

- 使用 RuntimeInitializeOnLoadMethod 自动创建，无需手动设置
- 使用两个 AudioSource 实现交叉淡入淡出，避免音乐切换时的爆音
- 使用 Time.unscaledDeltaTime 确保暂停时淡入淡出仍正常工作
- 音乐音量完全跟随 SettingsManager.Volume
- 所有音乐使用 44100Hz 采样率，单声道

扩展建议：

1. 如果未来添加主菜单场景：
   - 在 MainMenu 脚本中调用 MusicManager.Instance.PlayMusic(MusicManager.GameState.Menu)

2. 如果需要更复杂的音乐系统：
   - 可以添加音乐随机选择功能
   - 可以添加音乐强度随游戏进度变化的功能
   - 可以添加音乐过渡效果（如淡入淡出时间可配置）

3. 如果需要外部音频文件：
   - 将 AudioClip 赋值给对应的变量即可
   - 程序化生成的音乐会自动被覆盖

常见问题：

Q: 音乐没有播放？
A: 查看 Console 日志确认 MusicManager 是否自动创建。如果看到 "AutoCreate" 日志但没有音乐，检查音频生成是否成功。

Q: 音乐切换时有爆音？
A: 检查 fadeDuration 是否设置得太短，建议至少 0.5 秒。

Q: 暂停时音乐还在播放？
A: MusicManager 使用 Time.unscaledDeltaTime，暂停时音乐会继续播放但切换到暂停音乐。

Q: 如何完全禁用背景音乐？
A: 在设置面板中将音量设置为 0。

Q: 音乐音量不受设置面板控制？
A: 确保 SettingsManager 已正确初始化，MusicManager 会自动读取 SettingsManager.Volume。

Q: 音乐音量太小/太大？
A: 在设置面板中调整音量滑块，音乐会自动跟随。

Q: 如何测试音乐切换？
A: 使用 MusicManagerTest.cs 脚本，按 M/N/B/V 键测试不同状态的音乐。

Q: 只有游戏音乐，没有其他状态的音乐？
A: 检查 Console 日志，确认事件订阅是否成功。如果看到 "SubscribeEvents - PauseMenu.Instance 为 null"，说明 PauseMenu 还没有初始化。