using UnityEngine;

/// <summary>
/// Renders a parallax star field background.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class StarField : MonoBehaviour
{
    [SerializeField] private int starCount = 150;
    [SerializeField] private float spread = 15f;
    [SerializeField] private float twinkleSpeed = 2f;

    private Vector3[] starPositions;
    private float[] starSizes;
    private float[] starBrightness;
    private float[] starPhases;
    private Material starMaterial;

    private void Start()
    {
        starPositions = new Vector3[starCount];
        starSizes = new float[starCount];
        starBrightness = new float[starCount];
        starPhases = new float[starCount];

        // 缓存材质，不再每帧创建
        starMaterial = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i < starCount; i++)
        {
            starPositions[i] = new Vector3(
                Random.Range(-spread, spread),
                Random.Range(-spread, spread),
                5f // behind everything
            );
            starSizes[i] = Random.Range(0.02f, 0.08f);
            starBrightness[i] = Random.Range(0.3f, 1f);
            starPhases[i] = Random.Range(0f, Mathf.PI * 2);
        }
    }

    private void OnRenderObject()
    {
        if (starPositions == null || starMaterial == null) return;

        starMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.QUADS);

        float time = Time.time;

        for (int i = 0; i < starCount; i++)
        {
            float twinkle = Mathf.Sin(time * twinkleSpeed + starPhases[i]) * 0.3f + 0.7f;
            float b = starBrightness[i] * twinkle;
            GL.Color(new Color(b, b, b * 0.95f, 1f));

            float s = starSizes[i];
            Vector3 pos = starPositions[i];
            GL.Vertex3(pos.x - s, pos.y - s, pos.z);
            GL.Vertex3(pos.x + s, pos.y - s, pos.z);
            GL.Vertex3(pos.x + s, pos.y + s, pos.z);
            GL.Vertex3(pos.x - s, pos.y + s, pos.z);
        }

        GL.End();
        GL.PopMatrix();
    }

    private void OnDestroy()
    {
        // 清理材质，防止泄漏
        if (starMaterial != null)
        {
            Destroy(starMaterial);
        }
    }
}
