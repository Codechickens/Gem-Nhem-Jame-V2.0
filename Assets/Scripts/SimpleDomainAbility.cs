using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleDomainAbility : AbilityBase
{
    [Header("Simple Domain (Giản Lĩnh Vực)")]
    public float domainRadius = 0.5f;
    public string enemyBulletTag = "EnemyBullets";
    public LayerMask bulletLayer;

    [Header("Duration & Cooldown")]
    public float maxActiveDuration = 3f;
    public float cooldownTime = 5f;
    public float counterAttackCooldown = 0.2f;
    private float currentActiveTimer = 0f;
    private float currentCooldownTimer = 0f;
    private float currentCounterTimer = 0f;
    private bool isDomainActive = false;
    public static bool isSimpleDomainActive = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        if (currentCooldownTimer > 0)
            currentCooldownTimer -= Time.unscaledDeltaTime;
        if (currentCounterTimer > 0) currentCounterTimer -= Time.unscaledDeltaTime;
        if (isDomainActive)
            {
                currentActiveTimer -= Time.unscaledDeltaTime;

                if (currentActiveTimer <= 0)
                {
                    DeactivateDomain();
                    Debug.Log("Giản Lĩnh Vực tan biến!");
                }
            }
    }

    public override void OnButtonDown()
    {
        if (currentCooldownTimer <= 0 && !isDomainActive)
        {
            isDomainActive = true;
            isSimpleDomainActive = true;
            currentActiveTimer = maxActiveDuration;
            Debug.Log("GIẢN LĨNH VỰC: Kích hoạt Tân Âm Lưu!");
        }
    }

    public override void OnButtonHeld()
    {
        if (isDomainActive)
        {
            TriggerSimpleDomain();
        }
    }

    public override void OnButtonUp()
    {
        if (isDomainActive)
        {
            DeactivateDomain();
        }
    }
    void TriggerSimpleDomain()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, domainRadius, bulletLayer);
        bool hasCounterAttackedThisFrame = false; 

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag(enemyBulletTag))
            {
                BulletMovement bulletScript = col.GetComponent<BulletMovement>();
                
                if (bulletScript != null)
                {
                    bulletScript.InterceptBySimpleDomain();

                    if (!hasCounterAttackedThisFrame && playerMovement != null)
                    {
                        Vector2 direction = (col.transform.position - transform.position).normalized;
                        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                        playerMovement.transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);

                        playerMovement.OnClick();
                        hasCounterAttackedThisFrame = true; 
                        currentCounterTimer = counterAttackCooldown;
                    }
                }
            }
        }
    }

    void DeactivateDomain()
    {
        isDomainActive = false;
        isSimpleDomainActive = false;
        currentCooldownTimer = cooldownTime;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, domainRadius);
    }
}