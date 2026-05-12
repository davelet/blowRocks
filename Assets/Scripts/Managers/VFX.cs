using UnityEngine;

/// <summary>
/// Visual effects: explosions, engine trails, screen shake.
/// Singleton - call VFX.Instance.Explosion(pos, color, size) etc.
/// </summary>
public class VFX : MonoBehaviour
{
    public static VFX Instance { get; private set; }

    [SerializeField] private Material particleMaterial;

    private struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
        public float life;
        public float maxLife;
        public float size;
        public Color color;
    }

    private Particle[] particles;
    private const int MAX_PARTICLES = 500;
    private int activeCount;

    private void Awake()
    {
        Instance = this;
        particles = new Particle[MAX_PARTICLES];
        activeCount = 0;

        // Create default particle material
        if (particleMaterial == null)
        {
            particleMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }

    public void Explosion(Vector2 position, Color color, float size = 1f, int count = 15)
    {
        for (int i = 0; i < count && activeCount < MAX_PARTICLES; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            float speed = Random.Range(2f, 6f) * size;

            particles[activeCount] = new Particle
            {
                position = position,
                velocity = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed),
                life = Random.Range(0.4f, 0.9f),
                maxLife = Random.Range(0.4f, 0.9f),
                size = Random.Range(0.05f, 0.2f) * size,
                color = color
            };
            activeCount++;
        }
    }

    public void MuzzleFlash(Vector2 position, float angle)
    {
        for (int i = 0; i < 4 && activeCount < MAX_PARTICLES; i++)
        {
            float a = angle + Random.Range(-0.3f, 0.3f);
            float speed = Random.Range(3f, 6f);

            particles[activeCount] = new Particle
            {
                position = position,
                velocity = new Vector2(Mathf.Cos(a) * speed, Mathf.Sin(a) * speed),
                life = 0.15f,
                maxLife = 0.15f,
                size = Random.Range(0.04f, 0.1f),
                color = new Color(1f, 0.9f, 0.4f)
            };
            activeCount++;
        }
    }

    private void Update()
    {
        // Update particles
        int writeIdx = 0;
        for (int i = 0; i < activeCount; i++)
        {
            ref Particle p = ref particles[i];
            p.position += p.velocity * Time.deltaTime;
            p.velocity *= 0.96f;
            p.life -= Time.deltaTime;

            if (p.life > 0)
            {
                if (writeIdx != i) particles[writeIdx] = p;
                writeIdx++;
            }
        }
        activeCount = writeIdx;
    }

    private void OnRenderObject()
    {
        if (activeCount == 0) return;

        particleMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.QUADS);

        for (int i = 0; i < activeCount; i++)
        {
            ref Particle p = ref particles[i];
            float alpha = Mathf.Clamp01(p.life / p.maxLife);
            Color c = p.color;
            c.a = alpha;
            GL.Color(c);

            float s = p.size * alpha; // shrink over time
            GL.Vertex3(p.position.x - s, p.position.y - s, 0);
            GL.Vertex3(p.position.x + s, p.position.y - s, 0);
            GL.Vertex3(p.position.x + s, p.position.y + s, 0);
            GL.Vertex3(p.position.x - s, p.position.y + s, 0);
        }

        GL.End();
        GL.PopMatrix();
    }
}
