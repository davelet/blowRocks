using UnityEngine;

/// <summary>
/// 背景音乐管理器：根据游戏状态播放不同风格的背景音乐
/// 支持：游戏中、暂停、游戏结束 三种状态
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
    private AudioClip gameplayMusic;
    private AudioClip pauseMusic;
    private AudioClip gameOverMusic;

    private GameState currentState = GameState.None;

    private float fadeDuration = 1.0f;
    private float fadeTimer;
    private bool isFading;
    private AudioSource fadeOutSource;
    private AudioSource fadeInSource;
    private float fadeOutStartVolume;
    private float fadeInTarget;

    // 音量由 SettingsManager 统一管理
    private float masterVolume => SettingsManager.Volume;

    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        None
    }

    /// <summary>
    /// 自动创建 MusicManager 实例（如果不存在）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance == null)
        {
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

        // 延迟到第一帧渲染完成后再生成音乐，优先保证启动速度
        Invoke(nameof(StartGenerateMusic), 0.2f);
    }

    private void StartGenerateMusic()
    {
        StartCoroutine(GenerateMusicAsync());
    }

    private System.Collections.IEnumerator GenerateMusicAsync()
    {
        // 等待主线程调度器初始化完成
        yield return null;
        
        // 完全后台线程生成游戏音乐，不阻塞主线程
        bool musicReady = false;
        GenerateGameplayMusicAsync(clip =>
        {
            gameplayMusic = clip;
            musicReady = true;
        });
        
        // 等待音乐生成完成
        yield return new WaitUntil(() => musicReady);
        
        // 播放音乐
        SubscribeEvents();
        PlayMusic(GameState.Playing);
        
        // 后台低优先级生成其他音乐
        yield return null;
        pauseMusic = GeneratePauseMusic();
        
        yield return null;
        gameOverMusic = GenerateGameOverMusic();
    }

    private void SubscribeEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= HandleGameOver;
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        if (PauseMenu.Instance != null)
        {
            PauseMenu.Instance.OnPauseChanged -= HandlePauseChanged;
            PauseMenu.Instance.OnPauseChanged += HandlePauseChanged;
        }
    }

    private void Update()
    {
        if (isFading)
        {
            fadeTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);

            if (fadeOutSource != null)
            {
                fadeOutSource.volume = Mathf.Lerp(fadeOutStartVolume, 0f, t);
                if (t >= 1f) fadeOutSource.Stop();
            }

            if (fadeInSource != null)
            {
                fadeInSource.volume = Mathf.Lerp(0f, fadeInTarget, t);
            }

            if (t >= 1f)
            {
                isFading = false;
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
        float targetVolume = masterVolume;

        if (targetClip == null) return;

        AudioSource currentSource = isSourceA ? sourceA : sourceB;
        AudioSource nextSource = isSourceA ? sourceB : sourceA;

        nextSource.Stop();

        if (currentSource.isPlaying)
        {
            fadeOutSource = currentSource;
            fadeOutStartVolume = currentSource.volume;
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
            currentSource.Stop();
            currentSource.clip = targetClip;
            currentSource.volume = targetVolume;
            currentSource.Play();
        }

        isSourceA = !isSourceA;
    }

    private AudioClip GetClipForState(GameState state)
    {
        switch (state)
        {
            case GameState.Playing: return gameplayMusic;
            case GameState.Paused: return pauseMusic;
            case GameState.GameOver: return gameOverMusic;
            default: return gameplayMusic;
        }
    }

    private void HandlePauseChanged(bool paused)
    {
        if (paused)
            PlayMusic(GameState.Paused);
        else
            PlayMusic(GameState.Playing);
    }

    private void HandleGameOver()
    {
        PlayMusic(GameState.GameOver);
    }

    public void NotifyGameStarted()
    {
        PlayMusic(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameOver -= HandleGameOver;
        if (PauseMenu.Instance != null)
            PauseMenu.Instance.OnPauseChanged -= HandlePauseChanged;
    }

    // ============================================
    // 程序化音乐生成
    // ============================================

    // 后台线程生成音乐数据，不占用主线程
    private void GenerateGameplayMusicAsync(System.Action<AudioClip> onComplete)
    {
        int sampleRate = 44100;
        float duration = 16f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            float bpm = 128f;
            float beatLength = 60f / bpm;
            int samplesPerBeat = Mathf.CeilToInt(sampleRate * beatLength);
            var rand = new System.Random();

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float beatT = (i % samplesPerBeat) / (float)samplesPerBeat;
                int currentBeat = (i / samplesPerBeat) % 8;
                int beatStartSample = (i / samplesPerBeat) * samplesPerBeat;
                float tBeatSec = (i - beatStartSample) / (float)sampleRate;

                float kick = 0f;
                if (currentBeat == 0 || currentBeat == 4)
                {
                    float kickPhase = 2f * Mathf.PI * (150f * tBeatSec - 50f * tBeatSec * tBeatSec / beatLength);
                    kick = Mathf.Sin(kickPhase) * Mathf.Exp(-beatT * 10f) * 0.5f;
                }

                float snare = 0f;
                if (currentBeat == 2 || currentBeat == 6)
                {
                    float noise = (float)(rand.NextDouble() * 2.0 - 1.0);
                    snare = noise * Mathf.Exp(-beatT * 12f) * 0.3f;
                }

                float hihat = 0f;
                float hhNoise = (float)(rand.NextDouble() * 2.0 - 1.0);
                hihat = hhNoise * Mathf.Exp(-beatT * 20f) * 0.15f;

                float bassFreq = 110f;
                if (currentBeat == 1) bassFreq = 130f;
                if (currentBeat == 3) bassFreq = 146f;
                if (currentBeat == 5) bassFreq = 130f;
                if (currentBeat == 7) bassFreq = 98f;
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * i / sampleRate) * 0.25f;

                float leadFreq = 440f;
                if (currentBeat % 2 == 0) leadFreq = 523f;
                float lead = Mathf.Sin(2f * Mathf.PI * leadFreq * i / sampleRate) * 0.15f;
                lead *= Mathf.Sin(2f * Mathf.PI * 3f * t) * 0.5f + 0.5f;

                data[i] = kick + snare + hihat + bass + lead;
            }

            // 回到主线程创建AudioClip
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                var clip = AudioClip.Create("GameplayMusic", samples, 1, sampleRate, false);
                clip.SetData(data, 0);
                onComplete(clip);
            });
        });
    }

    /// <summary>
    /// 生成暂停音乐：轻快的电子氛围
    /// </summary>
    private AudioClip GeneratePauseMusic()
    {
        int sampleRate = 44100;
        float arpNoteDur = 0.5f;
        float[] arpFreqs = { 523.25f, 659.25f, 783.99f, 659.25f };
        float duration = arpNoteDur * arpFreqs.Length * 2;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        int arpSamples = Mathf.CeilToInt(sampleRate * arpNoteDur);
        int crossfadeLen = Mathf.CeilToInt(sampleRate * 0.01f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;

            int arpIdx = (i / arpSamples) % arpFreqs.Length;
            float arpT = (i % arpSamples) / (float)arpSamples;
            float arpEnv = Mathf.Exp(-arpT * 3f) * 0.6f;
            float arp = Mathf.Sin(2f * Mathf.PI * arpFreqs[arpIdx] * i / sampleRate) * arpEnv;

            float pad1 = Mathf.Sin(2f * Mathf.PI * 261.63f * i / sampleRate) * 0.12f;
            float pad2 = Mathf.Sin(2f * Mathf.PI * 329.63f * i / sampleRate) * 0.10f;
            float pad3 = Mathf.Sin(2f * Mathf.PI * 392.00f * i / sampleRate) * 0.10f;
            float padTrem = Mathf.Sin(2f * Mathf.PI * 1.5f * t) * 0.15f + 0.85f;
            float pad = (pad1 + pad2 + pad3) * padTrem;

            float bass = Mathf.Sin(2f * Mathf.PI * 130.81f * i / sampleRate) * 0.18f;
            float bassSub = Mathf.Sin(2f * Mathf.PI * 65.41f * i / sampleRate) * 0.08f;

            data[i] = arp + pad + bass + bassSub;
        }

        for (int i = 0; i < crossfadeLen; i++)
        {
            float t = (float)i / crossfadeLen;
            data[i] = data[i] * t + data[samples - crossfadeLen + i] * (1f - t);
        }

        var clip = AudioClip.Create("PauseMusic", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// 生成游戏结束音乐
    /// </summary>
    private AudioClip GenerateGameOverMusic()
    {
        int sampleRate = 44100;
        float duration = 6f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        float phase1 = 0f;
        float phase2 = 0f;
        float phase3 = 0f;
        float phaseBass = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;

            float freq = Mathf.Lerp(330f, 110f, t);
            phase1 += 2f * Mathf.PI * freq / sampleRate;
            phase2 += 2f * Mathf.PI * freq * 1.25f / sampleRate;
            phase3 += 2f * Mathf.PI * freq * 1.5f / sampleRate;
            phaseBass += 2f * Mathf.PI * 55f / sampleRate;

            float chord1 = Mathf.Sin(phase1) * 0.4f;
            float chord2 = Mathf.Sin(phase2) * 0.3f;
            float chord3 = Mathf.Sin(phase3) * 0.2f;
            float bass = Mathf.Sin(phaseBass) * 0.3f;

            float envelope = Mathf.Exp(-t * 0.8f);

            data[i] = (chord1 + chord2 + chord3 + bass) * envelope;
        }

        var clip = AudioClip.Create("GameOverMusic", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}