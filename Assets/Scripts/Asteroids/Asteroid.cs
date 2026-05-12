using UnityEngine;

/// <summary>
/// Asteroid behavior: drifts, rotates, and splits into smaller pieces when hit.
/// </summary>
public class Asteroid : MonoBehaviour
{
    [Header("Split Settings")]
    [SerializeField] private GameObject smallerAsteroidPrefab; // null = smallest size
    [SerializeField] private int splitCount = 2;               // how many smaller pieces

    [Header("Movement")]
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 2f;
    [SerializeField] private float rotationSpeed = 50f;

    [Header("Score")]
    [SerializeField] private int scoreValue = 10;

    private Rigidbody2D rb;
    private GameManager gameManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        // Random drift direction
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float speed = Random.Range(minSpeed, maxSpeed);
        rb.velocity = randomDir * speed;

        // Random spin
        rb.angularVelocity = Random.Range(-rotationSpeed, rotationSpeed);
    }

    /// <summary>
    /// Called by Bullet when it hits this asteroid.
    /// </summary>
    public void OnHit()
    {
        gameManager?.AddScore(scoreValue);

        // Explosion VFX
        float astSize = transform.localScale.x;
        VFX.Instance?.Explosion(transform.position, new Color(1f, 0.7f, 0.3f), astSize, 20);
        SFX.Instance?.PlayExplosion(astSize);

        // Split into smaller asteroids
        if (smallerAsteroidPrefab != null)
        {
            for (int i = 0; i < splitCount; i++)
            {
                Instantiate(smallerAsteroidPrefab, transform.position, Random.rotation);
            }
        }

        // Spawn particle effect here if you have one
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerController>()?.TakeDamage();
            OnHit(); // Destroy the asteroid on contact too
        }
    }
}
