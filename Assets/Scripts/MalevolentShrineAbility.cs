using System.Collections; // BẮT BUỘC phải có dòng này để dùng Coroutine
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MalevolentShrineAbility : MonoBehaviour
{
    [Header("Protective Shield")]
    public float domainRadius = 6f;
    public float maxActiveDuration = 5f;
    public float cooldownTime = 15f;
    
    [Header("Damage & Slow Config")]
    public float tickRate = 0.05f; 
    public float damagePerTick = 0.25f; 
    public float domainDragForce = 15f;

    [Header("Visual Effects & Animation")]
    public GameObject domainVisual; 
    public GameObject slashEffectPrefab; 
    [Tooltip("Thời gian bành chướng")]
    public float expandDuration = 0.5f; 
    [Tooltip("Độ rung màn hình")]
    public float shakeMagnitude = 0.3f;

    private float currentActiveTimer = 0f;
    private float currentCooldownTimer = 0f;
    private float currentTickTimer = 0f;
    private bool isDomainActive = false;

    private Dictionary<Collider2D, float> damageAccumulator = new Dictionary<Collider2D, float>();
    private Dictionary<Rigidbody2D, float> originalDrags = new Dictionary<Rigidbody2D, float>();

    void Start()
    {
        if (domainVisual != null) domainVisual.SetActive(false);
    }

    void Update()
    {
        if (currentCooldownTimer > 0) currentCooldownTimer -= Time.unscaledDeltaTime;

        bool isVPressed = Input.GetKeyDown(KeyCode.V) || 
                          (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame);

        if (isVPressed && currentCooldownTimer <= 0 && !isDomainActive)
        {
            isDomainActive = true;
            currentActiveTimer = maxActiveDuration;
            currentTickTimer = 0f; 
            
            if (domainVisual != null) 
            {
                StartCoroutine(ExpandDomainVisual());
            }
            StartCoroutine(ScreenShake(expandDuration, shakeMagnitude));
            
            Debug.Log("Protective Shield");
        }

        if (isDomainActive)
        {
            currentActiveTimer -= Time.unscaledDeltaTime;
            currentTickTimer -= Time.unscaledDeltaTime;

            if (currentTickTimer <= 0)
            {
                ExecuteDomainLogic();
                SpawnSlashEffects(); 
                currentTickTimer = tickRate; 
            }

            if (currentActiveTimer <= 0)
            {
                DeactivateDomain();
            }
        }
    }

    IEnumerator ExpandDomainVisual()
    {
        domainVisual.SetActive(true);
        SpriteRenderer sr = domainVisual.GetComponent<SpriteRenderer>();
        
        Color finalColor = sr.color;
        float targetAlpha = finalColor.a;
        
        domainVisual.transform.localScale = Vector3.zero;
        finalColor.a = 0f;
        sr.color = finalColor;

        float elapsedTime = 0f;
        Vector3 targetScale = new Vector3(domainRadius * 2, domainRadius * 2, 1f);

        while (elapsedTime < expandDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = elapsedTime / expandDuration;
            
            domainVisual.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            finalColor.a = Mathf.Lerp(0f, targetAlpha, t);
            sr.color = finalColor;
            
            yield return null; 
        }

        domainVisual.transform.localScale = targetScale;
        finalColor.a = targetAlpha;
        sr.color = finalColor;
    }

    IEnumerator ScreenShake(float duration, float magnitude)
    {
        Vector3 originalPos = Camera.main.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            Camera.main.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Camera.main.transform.localPosition = originalPos; 
    }


    void SpawnSlashEffects()
    {
        if (slashEffectPrefab == null) return;
        int slashesThisTick = Random.Range(50, 60);
        for (int i = 0; i < slashesThisTick; i++)
        {
            Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle * domainRadius;
            Quaternion randomRot = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            GameObject slash = Instantiate(slashEffectPrefab, randomPos, randomRot);
            Destroy(slash, 0.04f);
        }
    }

    void ExecuteDomainLogic()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, domainRadius);
        HashSet<Rigidbody2D> currentEnemiesInDomain = new HashSet<Rigidbody2D>();

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Enemies") || col.CompareTag("PushableEnemy"))
            {
                if (!damageAccumulator.ContainsKey(col)) damageAccumulator[col] = 0f;
                damageAccumulator[col] += damagePerTick;

                if (damageAccumulator[col] >= 1f)
                {
                    int actualDamage = Mathf.FloorToInt(damageAccumulator[col]);
                    if (col.TryGetComponent(out IDamageable enemy)) enemy.TakeDamage(actualDamage);
                    damageAccumulator[col] -= actualDamage; 
                }

                Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    currentEnemiesInDomain.Add(rb);
                    if (!originalDrags.ContainsKey(rb))
                    {
                        originalDrags[rb] = rb.linearDamping;
                        rb.linearDamping += domainDragForce; 
                    }
                }
            }
        }

        List<Rigidbody2D> escapedEnemies = new List<Rigidbody2D>();
        foreach (var rb in originalDrags.Keys)
        {
            if (rb == null || !currentEnemiesInDomain.Contains(rb)) escapedEnemies.Add(rb);
        }

        foreach (var rb in escapedEnemies)
        {
            if (rb != null) rb.linearDamping = originalDrags[rb];
            originalDrags.Remove(rb);
        }
    }

    void DeactivateDomain()
    {
        isDomainActive = false;
        currentCooldownTimer = cooldownTime;
        
        if (domainVisual != null) domainVisual.SetActive(false);
        
        foreach (var kvp in originalDrags)
        {
            if (kvp.Key != null) kvp.Key.linearDamping = kvp.Value;
        }
        originalDrags.Clear();
        damageAccumulator.Clear();
    }
}