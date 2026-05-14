using UnityEngine;

/// <summary>
/// Procedural sound effects generator. No audio files needed!
/// Creates laser, explosion, and hit sounds from code.
/// </summary>
public class SFX : MonoBehaviour
{
    public static SFX Instance { get; private set; }

    /// <summary>
    /// 音量由 SettingsManager 统一管理
    /// </summary>
    private float masterVolume => SettingsManager.Volume;

    private AudioSource audioSource;
    private AudioClip laserClip;
    private AudioClip explosionClip;
    private AudioClip hitClip;
    private AudioClip explodeSmallClip;

    private void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Generate all sound clips
        laserClip = GenerateLaser();
        explosionClip = GenerateExplosion(0.8f);
        explodeSmallClip = GenerateExplosion(0.3f);
        hitClip = GenerateHit();
    }

    public void PlayLaser()
    {
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(laserClip, 0.3f * masterVolume);
    }

    public void PlayExplosion(float size = 1f)
    {
        audioSource.pitch = Random.Range(0.7f, 1.0f) / size;
        var clip = size > 0.5f ? explosionClip : explodeSmallClip;
        audioSource.PlayOneShot(clip, Mathf.Clamp01(size * 0.6f) * masterVolume);
    }

    public void PlayHit()
    {
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.PlayOneShot(hitClip, 0.4f * masterVolume);
    }

    // --- Procedural Audio Generation ---

    private AudioClip GenerateLaser()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // Descending frequency sweep
            float freq = Mathf.Lerp(1200f, 400f, t);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t / sampleRate * i);
            // Envelope: quick attack, fast decay
            float envelope = Mathf.Clamp01(1f - t * 3f);
            data[i] = wave * envelope;
        }

        var clip = AudioClip.Create("Laser", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateExplosion(float length)
    {
        int sampleRate = 44100;
        float duration = 0.3f * length + 0.2f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];
        var rng = new System.Random();

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // Noise burst that decays
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            // Low rumble
            float rumble = Mathf.Sin(2f * Mathf.PI * 60f * t) * 0.5f;
            // Envelope
            float envelope = Mathf.Exp(-t * (4f / length));
            data[i] = (noise * 0.7f + rumble * 0.3f) * envelope;
        }

        var clip = AudioClip.Create("Explosion", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private AudioClip GenerateHit()
    {
        int sampleRate = 44100;
        float duration = 0.2f;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float freq = Mathf.Lerp(800f, 200f, t);
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t / sampleRate * i);
            float envelope = Mathf.Exp(-t * 8f);
            data[i] = wave * envelope * 0.8f;
        }

        var clip = AudioClip.Create("Hit", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
