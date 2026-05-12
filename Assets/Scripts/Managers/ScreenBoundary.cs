using UnityEngine;

/// <summary>
/// Keeps track of screen boundaries in world units.
/// Useful for spawning and screen wrapping.
/// Attach to an empty GameObject or use ScreenBoundary.WorldBounds.
/// </summary>
public class ScreenBoundary : MonoBehaviour
{
    public static Rect WorldBounds { get; private set; }

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        CalculateBounds();
    }

    private void LateUpdate()
    {
        // Recalculate every frame in case camera moves/zooms
        CalculateBounds();
    }

    private void CalculateBounds()
    {
        if (mainCamera == null) return;

        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));

        WorldBounds = new Rect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x - bottomLeft.x,
            topRight.y - bottomLeft.y
        );
    }

    /// <summary>
    /// Returns true if the position is outside the visible screen.
    /// </summary>
    public static bool IsOutOfBounds(Vector2 position, float margin = 1f)
    {
        return position.x < WorldBounds.xMin - margin ||
               position.x > WorldBounds.xMax + margin ||
               position.y < WorldBounds.yMin - margin ||
               position.y > WorldBounds.yMax + margin;
    }

    private void OnDrawGizmos()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 bl = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 tr = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        Vector3 center = (bl + tr) / 2f;
        Vector3 size = tr - bl;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(center, size);
    }
}
