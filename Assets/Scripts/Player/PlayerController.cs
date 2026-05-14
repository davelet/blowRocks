using UnityEngine;
using System.Collections;

/// <summary>
/// Controls the player ship: movement and rotation toward the mouse cursor.
/// Top-down 2D space shooter.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Shooting")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireRate = 0.3f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invincibleDuration = 2f;       // 无敌持续时间(秒)
    [SerializeField] private float invincibleBlinkRate = 0.1f;    // 闪烁频率(秒)

    private int currentHealth;
    private float nextFireTime;
    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool isInvincible;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => isInvincible;

    public System.Action<int, int> OnHealthChanged;
    public System.Action OnPlayerDied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleShooting();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 direction = new Vector2(h, v).normalized;
        rb.velocity = direction * moveSpeed;

        // Wrap around screen edges
        Vector3 pos = transform.position;
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(pos);

        if (viewportPos.x < 0) pos.x = mainCamera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;
        if (viewportPos.x > 1) pos.x = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        if (viewportPos.y < 0) pos.y = mainCamera.ViewportToWorldPoint(new Vector3(0, 1, 0)).y;
        if (viewportPos.y > 1) pos.y = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0)).y;

        transform.position = pos;
    }

    private void HandleRotation()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void HandleShooting()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            VFX.Instance?.MuzzleFlash(firePoint.position, transform.eulerAngles.z * Mathf.Deg2Rad);
            SFX.Instance?.PlayLaser();
        }
    }

    public void TakeDamage(int damage = 1)
    {
        if (isInvincible) return;

        VFX.Instance?.Explosion(transform.position, new Color(1f, 0.3f, 0.2f), 1.5f, 25);
        SFX.Instance?.PlayHit();
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnPlayerDied?.Invoke();
        // GameManager handles game over logic
        gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        StartCoroutine(InvincibilityCoroutine());
    }

    /// <summary>
    /// 无敌帧：禁用碰撞 + 闪烁效果，持续 invincibleDuration 秒
    /// </summary>
    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        if (col != null) col.enabled = false;

        float elapsed = 0f;
        while (elapsed < invincibleDuration)
        {
            // 闪烁：交替显示/隐藏
            if (spriteRenderer != null)
                spriteRenderer.enabled = !spriteRenderer.enabled;

            yield return new WaitForSeconds(invincibleBlinkRate);
            elapsed += invincibleBlinkRate;
        }

        // 恢复正常
        isInvincible = false;
        if (col != null) col.enabled = true;
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }
}
