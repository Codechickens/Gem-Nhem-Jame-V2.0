using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleDomainAbility : MonoBehaviour
{
    [Header("Simple Domain (Giản Lĩnh Vực)")]
    public float domainRadius = 0.5f;
    public string enemyBulletTag = "EnemyBullets";

    [Header("Duration & Cooldown")]
    public float maxActiveDuration = 3f;
    public float cooldownTime = 5f;

    private float currentActiveTimer = 0f;
    private float currentCooldownTimer = 0f;
    private bool isDomainActive = false;
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (currentCooldownTimer > 0)
            currentCooldownTimer -= Time.unscaledDeltaTime;

        bool isXPressed = Input.GetKey(KeyCode.X) || 
                          (Keyboard.current != null && Keyboard.current.xKey.isPressed);

        if (isXPressed && currentCooldownTimer <= 0)
        {
            if (!isDomainActive)
            {
                isDomainActive = true;
                currentActiveTimer = maxActiveDuration;
                Debug.Log("GIẢN LĨNH VỰC: Kích hoạt Tân Âm Lưu!");
            }

            if (isDomainActive)
            {
                TriggerSimpleDomain();
                currentActiveTimer -= Time.unscaledDeltaTime;

                if (currentActiveTimer <= 0)
                {
                    DeactivateDomain();
                    Debug.Log("Giản Lĩnh Vực tan biến!");
                }
            }
        }
        else if (isDomainActive && !isXPressed)
        {
            DeactivateDomain();
        }
    }

    void TriggerSimpleDomain()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, domainRadius);
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
                        transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);

                        playerMovement.OnClick();
                        hasCounterAttackedThisFrame = true; 
                    }
                }
            }
        }
    }

    // Chỉ có ĐÚNG MỘT hàm DeactivateDomain ở đây
    void DeactivateDomain()
    {
        isDomainActive = false;
        currentCooldownTimer = cooldownTime;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, domainRadius);
    }
}