using UnityEngine;

/// <summary>
/// 背景音乐管理器：根据游戏状态播放不同风格的背景音乐
/// 支持：启动/菜单、游戏中、暂停、游戏结束 四种状态
/// 所有音乐程序化生成，无需外部音频文件
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    // 两个 AudioSource 用于交叉淡入淡出
    private AudioSource sourceA;
    private AudioSource sourceB;
    private bool isSourceA = true;

    // 音乐片段
    private AudioClip menuMusic;
    private AudioClip gameplayMusic;
    private AudioClip pauseMusic;
    private AudioClip gameOverMusic;

    // 当前状态
    private GameState currentState = GameState.Menu;
    private bool isPaused = false;

    // 淡入淡出参数
    private float fadeDuration = 1.0f;
    private float fadeTimer;
    private bool isFading;
    private AudioSource fadeOutSource;
    private AudioSource fadeInSource;
    private float fadeInTarget;

    // 音量由 SettingsManager 统一管理
    private float masterVolume => SettingsManager.Volume;

    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>
    /// 自动创建 MusicManager 实例（如果不存在）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
            Debug.Log("[MusicManager] AutoCreate - 自动创建 MusicManager");
            var go = new GameObject("MusicManager");
            go.AddComponent<MusicManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 创建两个 AudioSource 用于交叉淡入淡出
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();
        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;
        sourceA.loop = true;
        sourceB.loop = true;

        // 生成所有音乐片段
        menuMusic = GenerateMenuMusic();
        gameplayMusic = GenerateGameplayMusic();
        pauseMusic = GeneratePauseMusic();
        gameOverMusic = GenerateGameOverMusic();

        Debug.Log("[MusicManager] Awake - 初始化完成");
    }

    private void Start()
    {
        Debug.Log("[MusicManager] Start - 开始");

        // 立即尝试订阅事件
        SubscribeEvents();

        // 如果订阅失败，延迟重试
        if (PauseMenu.Instance == null)
        {
            Debug.Log("[MusicManager] PauseMenu.Instance 为 null，延迟重试");
            Invoke(nameof(SubscribeEvents), 0.5f);
        }

        // 默认播放菜单音乐
        PlayMusic(GameState.Menu);
    }

    private void SubscribeEvents()
    {
        // 监听游戏事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver; // 先取消订阅，避免重复
            GameManager.Instance.OnGameOver += HandleGameOver;
            Debug.Log("[MusicManager] SubscribeEvents - 订阅 OnGameOver");
        }

        // 监听暂停事件
        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.OnPauseChanged -= HandlePauseChanged; // 先取消订阅，避免重复
            PauseMenu.Instance.OnPauseChanged += HandlePauseChanged;
            Debug.Log("[MusicManager] SubscribeEvents - 订阅 OnPauseChanged");
        }
        else
        {
            Debug.LogWarning("[MusicManager] SubscribeEvents - PauseMenu.Instance 为 null");
        }
    }

    private void Update()
    {
        if (isFading)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float t = fadeTimer / fadeDuration;

            if (t >= 1f)
            {
                t = 1f;
                isFading = false;
            }

            // 淡出
            if (fadeOutSource != null)
            {
                fadeOutSource.volume = Mathf.Lerp(fadeOutSource.volume, 0f, t);
                if (t >= 1f) fadeOutSource.Stop();
            }

            // 淡入
            if (fadeInSource != null)
            {
                fadeInSource.volume = Mathf.Lerp(fadeInSource.volume, fadeInTarget, t);
            }
        }
    }

    /// <summary>
    /// 切换到指定状态的音乐
    /// </summary>
    public void PlayMusic(GameState state)
    {
        if (currentState == state && !isFading) return;

        currentState = state;
        AudioClip targetClip = GetClipForState(state);

        // 音量完全跟随 SettingsManager.Volume
        float targetVolume = masterVolume;

        if (targetClip == null)
        {
            Debug.LogError("[MusicManager] targetClip is null!");
            return;
        }

        // 交叉淡入淡出
        AudioSource currentSource = isSourceA ? sourceA : sourceB;
        AudioSource nextSource = isSourceA ? sourceB : sourceA;

        if (currentSource.isPlaying)
        {
            // 开始淡入淡出
            fadeOutSource = currentSource;
            fadeInSource = nextSource;
            fadeInTarget = targetVolume;
            fadeTimer = 0f;
            isFading = true;

            nextSource.clip = targetClip;
            nextSource.volume = 0f;
            nextSource.Play();
        }
        else
        {
            // 直接播放
            currentSource.clip = targetClip;
            currentSource.volume = targetVolume;
            currentSource.Play();
        }

        isSourceA = !isSourceA;
        Debug.Log($"[MusicManager] PlayMusic - {state}, volume: {targetVolume}");
    }

    /// <summary>
    /// 获取状态对应的音乐片段
    /// </summary>
    private AudioClip GetClipForState(GameState state)
    {
        switch (state)
        {
            case GameState.Menu: return menuMusic;
            case GameState.Playing: return gameplayMusic;
            case GameState.Paused: return pauseMusic;
            case GameState.GameOver: return gameOverMusic;
            default: return gameplayMusic;
        }
    }

    /// <summary>
    /// 处理暂停状态变化
    /// </summary>
    private void HandlePauseChanged(bool paused)
    {
        Debug.Log($"[MusicManager] HandlePauseChanged - paused: {paused}");
        isPaused = paused;
        if (paused)
            PlayMusic(GameState.Paused);
        else
            PlayMusic(GameState.Playing);
    }

    /// <summary>
    /// 处理游戏结束
    /// </summary>
    private void HandleGameOver()
    {
        Debug.Log("[MusicManager] HandleGameOver - 游戏结束");
        PlayMusic(GameState.GameOver);
    }

    /// <summary>
    /// 通知游戏开始（由 GameManager 调用）
    /// </summary>
    public void NotifyGameStarted()
    {
        Debug.Log("[MusicManager] NotifyGameStarted - 游戏开始");
        PlayMusic(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.OnPauseChanged -= HandlePauseChanged;
        }
    }

    // ============================================
    // 程序化音乐生成
    // ============================================

    /// <summary>
    /// 生成菜单音乐：舒缓的太空氛围，带有神秘感
    /// </summary>
    private AudioClip GenerateMenuMusic()
    {
        int sampleRate = 44100;
        float duration = 30f; // 30秒循环
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;

            // 基础低音 pad
            float pad = Mathf.Sin(2f * Mathf.PI * 60f * t / sampleRate * i) * 0.5f;
            pad += Mathf.Sin(2f * Mathf.PI * 90f * t / sampleRate * i) * 0.3f;

            // 缓慢的音高变化
            float freqMod = Mathf.Sin(2f * Mathf.PI * 0.1f * t) * 20f;
            pad += Mathf.Sin(2f * Mathf.PI * (120f + freqMod) * t / sampleRate * i) * 0.25f;

            // 高频泛音点缀
            float sparkle = Mathf.Sin(2f * Mathf.PI * 800f * t / sampleRate * i) * 0.1f;
            sparkle *= Mathf.Sin(2f * Mathf.PI * 0.5f * t); // 缓慢闪烁

            // 整体包络：缓慢淡入淡出
            float envelope = Mathf.Sin(Mathf.PI * t);

            data[i] = (pad + sparkle) * envelope;
        }

        var clip = AudioClip.Create("MenuMusic", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// 生成游戏音乐：紧张刺激的战斗节奏
    /// </summary>
    private AudioClip GenerateGameplayMusic()
    {
        int sampleRate = 44100;
        float duration = 20f; // 20秒循环
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        // 节奏参数
        float bpm = 140f;
        float beatLength = 60f / bpm;
        int samplesPerBeat = Mathf.CeilToInt(sampleRate * beatLength);

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float beatT = (i % samplesPerBeat) / (float)samplesPerBeat;

            // 低音鼓点（4拍子）
            float kick = 0f;
            int currentBeat = (i / samplesPerBeat) % 4;
            if (currentBeat == 0 || currentBeat == 2)
            {
                float kickFreq = Mathf.Lerp(150f, 50f, beatT);
                kick = Mathf.Sin(2f * Mathf.PI * kickFreq * t / sampleRate * i);
                kick *= Mathf.Exp(-beatT * 8f) * 0.4f;
            }

            // 高频 hi-hat（每拍）
            float hihat = 0f;
            float noise = (float)(new System.Random(i).NextDouble() * 2.0 - 1.0);
            hihat = noise * Mathf.Exp(-beatT * 15f) * 0.1f;

            // 合成器 bass line
            float bassFreq = 80f;
            if (currentBeat == 1) bassFreq = 100f;
            if (currentBeat == 3) bassFreq = 60f;
            float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t / sampleRate * i) * 0.2f;

            // 紧张的 pad
            float tension = Mathf.Sin(2f * Mathf.PI * 200f * t / sampleRate * i) * 0.1f;
            tension *= Mathf.Sin(2f * Mathf.PI * 0.5f * t); // 脉冲效果

            data[i] = kick + hihat + bass + tension;
        }

        var clip = AudioClip.Create("GameplayMusic", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// 生成暂停音乐：空灵放松的氛围
    /// </summary>
    private AudioClip GeneratePauseMusic()
    {
        int sampleRate = 44100;
        float duration = 25f; // 25秒循环
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;

            // 深沉的 drone
            float drone = Mathf.Sin(2f * Mathf.PI * 40f * t / sampleRate * i) * 0.5f;
            drone += Mathf.Sin(2f * Mathf.PI * 60f * t / sampleRate * i) * 0.3f;

            // 缓慢移动的和声
            float harmony1 = Mathf.Sin(2f * Mathf.PI * 150f * t / sampleRate * i) * 0.2f;
            float harmony2 = Mathf.Sin(2f * Mathf.PI * 225f * t / sampleRate * i) * 0.15f;

            // 空间感的高频
            float space = Mathf.Sin(2f * Mathf.PI * 600f * t / sampleRate * i) * 0.1f;
            space *= Mathf.Sin(2f * Mathf.PI * 0.2f * t) * 0.5f + 0.5f;

            // 整体包络
            float envelope = Mathf.Sin(Mathf.PI * t) * 0.7f;

            data[i] = (drone + harmony1 + harmony2 + space) * envelope;
        }

        var clip = AudioClip.Create("PauseMusic", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// 生成游戏结束音乐：悲壮的尾声
    /// </summary>
    private AudioClip GenerateGameOverMusic()
    {
        int sampleRate = 44100;
        float duration = 8f; // 8秒，不循环
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;

            // 下降的音调
            float freq = Mathf.Lerp(300f, 80f, t);
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t / sampleRate * i) * 0.7f;

            // 低沉的 rumble
            float rumble = Mathf.Sin(2f * Mathf.PI * 40f * t / sampleRate * i) * 0.5f;

            // 噪声尾音
            float noise = 0f;
            if (t > 0.5f)
            {
                float noiseAmount = (t - 0.5f) * 2f;
                noise = (float)(new System.Random(i).NextDouble() * 2.0 - 1.0) * noiseAmount * 0.2f;
            }

            // 衰减包络
            float envelope = Mathf.Exp(-t * 0.5f);

            data[i] = (tone * 0.5f + rumble + noise) * envelope;
        }

        var clip = AudioClip.Create("GameOverMusic", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}