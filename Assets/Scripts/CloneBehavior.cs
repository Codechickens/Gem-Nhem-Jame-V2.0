using UnityEngine;

public class CloneBehavior : MonoBehaviour
{
    [Header("Stats & Combat")]
    public int health;
    public float lifeTime = 10f; 
    public float fireRate = 1f;
    public float detectionRadius = 15f;

    [Header("AI Movement (Kiting)")]
    public float moveSpeed = 15f; 
    [Tooltip("Khoảng cách lý tưởng mà Clone muốn giữ với địch")]
    public float preferredDistance = 5f; 
    [Tooltip("Vùng đệm để Clone không bị giật lùi/tiến liên tục")]
    public float stopBuffer = 1f; 

    [Header("References")]
    public GameObject smokeEffect;
    public GameObject bulletPrefab;
    public Transform spawnPoint;
    
    private float fireTimer;
    private Rigidbody2D rb;
    private float lifeTimer;

    public void Initialize(int hp)
    {
        health = hp;
        rb = GetComponent<Rigidbody2D>();
        lifeTimer = lifeTime;
    }

    void Update()
    {
        fireTimer -= Time.deltaTime;
        Transform target = FindNearestEnemy();

        if (target != null)
        {
            Vector2 directionToEnemy = (target.position - transform.position).normalized;
            float distanceToEnemy = Vector2.Distance(transform.position, target.position);

            float angle = Mathf.Atan2(directionToEnemy.y, directionToEnemy.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);

            if (rb != null)
            {
                if (distanceToEnemy > preferredDistance + stopBuffer)
                {
                    rb.linearVelocity = directionToEnemy * moveSpeed;
                }
                else if (distanceToEnemy < preferredDistance - stopBuffer)
                {
                    rb.linearVelocity = -directionToEnemy * moveSpeed;
                }
                else
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }

            if (fireTimer <= 0)
            {
                Instantiate(bulletPrefab, spawnPoint.position, transform.rotation);
                fireTimer = fireRate;
            }
        }
        else
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0)
        {
            Die();
        }
    }

    Transform FindNearestEnemy()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemies") || col.CompareTag("PushableEnemy") || col.CompareTag("Explosive"))
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = col.transform;
                }
            }
        }
        return nearest;
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0) 
        {
            Die();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("EnemyBullets"))
        {
            TakeDamage(10);
            Destroy(other.gameObject);
        }
    }
    void Die()
    {
        if (smokeEffect != null)
        {
            Instantiate(smokeEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}