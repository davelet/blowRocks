using UnityEngine;

/// <summary>
/// Moves the bullet forward and destroys it when off-screen.
/// </summary>
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        // Bullet flies in the direction the ship was facing
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.velocity = transform.up * speed;

        // Auto-destroy after a few seconds
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Asteroid"))
        {
            // Asteroid script handles the split/destroy logic
            other.GetComponent<Asteroid>()?.OnHit();
            Destroy(gameObject);
        }
    }
}
