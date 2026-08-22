using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.VisualScripting;
using NUnit.Framework;

[RequireComponent(typeof(Rigidbody2D))]

public class PlayerMovement : MonoBehaviour
{

[Header("Dash & Phasing")]
    public float dashSpeed = 50f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashDamage = 50f;
    public float dashHitboxRadius = 1f;
    [SerializeField] LayerMask unphasableLayers;
    [SerializeField] LayerMask enemyLayer;
    [SerializeField] int dashingLayerIndex = 8;
    int originalLayerIndex;
    bool canDash = true;
    public bool isDashing = false;
    HashSet<IDamageable> damagedDuringDash = new HashSet<IDamageable>();

    [Header("Movement & Input")]
    [SerializeField] float speed = 22f;
    float originalSpeed;
    [SerializeField] InputActionReference moveAction;
    [SerializeField] InputActionReference aimAction;
    [SerializeField] InputActionReference shootAction;
    [SerializeField] InputActionReference dashAction;
    [SerializeField] InputActionReference interactAction;
    [SerializeField] InputActionReference slowMovementAction;
    [SerializeField] InputActionReference repairAction;

    [Header("Abilities System")]
    [SerializeField] AbilityManager abilityManager;
    [SerializeField] InputActionReference slot1Action;
    [SerializeField] InputActionReference slot2Action;
    [SerializeField] InputActionReference slot3Action;

    Rigidbody2D rb;
    Vector2 moveInput;
    float targetAngle;
    Camera mainCam;

    [Header("Shooting")]
    [SerializeField] GameObject bullet;
    [SerializeField] float timeBetweenFiring;
    [SerializeField] Transform spawnPoint;
    float fireTimer;

    [Header("Health")]
    int maxHealth = 100;
    int currentHealth;
    [SerializeField] HealthBar healthBar;

    [Header("Healing")]
    int maxHeal = 100;
    int currentHeal;
    [SerializeField] RepairBar repairBar;
    float visualHealDuration = 0.5f;
    Coroutine activeRepairRoutine;

    [Header("Interaction")]
    [SerializeField] GameObject interactionButton;
    [SerializeField] InteractionSlider interactionSlider;
    bool holdInteractable = false;
    bool interactable = false;

    [Header("Slow Movement")]
    SpriteRenderer sprite;
    [SerializeField] float fadeDuration;
    [SerializeField] Color shimmerColor = Color.white;
    public bool isShimmering;
    [SerializeField] GameObject playerCore;
    float fadeTimer = 0;
    [SerializeField] float slowSpeed = 1f;
    Color originalColor;
    bool colorInit = false;

    [Header("Screen Shake")]
    public float recoilForce = 0.5f;
    CinemachineImpulseSource impulseSource;

    [Header("Hit Stop Effect")]
    [SerializeField] float hitStopDuration = 0.05f;
    bool isHitStopping = false;
    public float hitRecoilForce = 0.3f;

    [Header("Damage & I-Frames")]
    float invulnerabilityDuration = 1.5f;
    float knockbackForce = 15f;
    float knockbackDuration = 0.2f;
    Color damageFlashColor = Color.red;
    bool isInvincible = false;
    bool isKnockedBack = false;

    [SerializeField] int flashCount = 6;

    void Awake(){
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        originalLayerIndex = gameObject.layer;
        sprite = GetComponent<SpriteRenderer>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    
    void Start(){
        fireTimer = timeBetweenFiring;
        currentHealth = maxHealth;
        currentHeal = maxHeal;
        healthBar.SetMaxHealth(maxHealth);
        if (repairBar != null) repairBar.SetMaxRepair(maxHeal);
        interactionButton.SetActive(false);
        interactionSlider.gameObject.SetActive(false);
        playerCore.SetActive(false);
        originalSpeed = speed;
    }

    void Update()
    {
        if (isDashing || isKnockedBack) return;

        moveInput = moveAction.action.ReadValue<Vector2>();
        Vector2 mouseScreenPos = aimAction.action.ReadValue<Vector2>();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
        
        Vector2 dir = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;
        targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        HandleShooting();
        HandleDash();
        HandleInteraction();
        HandleSlowMovement();
        HandleHealing();
        HandleAbilities();

        if (Time.timeScale < 1f) {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 0.2f);
            foreach (Collider2D hit in hitColliders) {
                if (hit.CompareTag("Explosive")) {
                    Debug.Log("Exploded!");
                    Destroy(hit.gameObject);
                }
            }
        }

        if (currentHealth <= 0)
        {
            Time.timeScale = 1f;
            Destroy(gameObject);
            healthBar.gameObject.SetActive(false);
            interactionButton.SetActive(false);
            interactionSlider.gameObject.SetActive(false);
            Debug.Log("GAH! I AM DEAD!");
        }
    }

    void FixedUpdate(){
        if (isDashing) {
            DetechDashHits();
            return;
        }
        if (isKnockedBack)
        {
            rb.MoveRotation(targetAngle);
            return;
        }
        if (Time.timeScale > 0f)
        {
            rb.linearVelocity = (moveInput.normalized * speed) / Time.timeScale;
        }
        else
        {
            rb.linearVelocity = moveInput.normalized * speed;
        }
        rb.MoveRotation(targetAngle);
    }

    void DetechDashHits(){
        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, dashHitboxRadius, enemyLayer);
        bool hitSomethingThisFrame = false;
        foreach (Collider2D hit in hits){
            if (hit.TryGetComponent(out IDamageable enemy)){
                if (!damagedDuringDash.Contains(enemy)){
                    Debug.Log("Enemy is hit by dashing!");
                    enemy.TakeDamage(dashDamage);
                    damagedDuringDash.Add(enemy);
                    hitSomethingThisFrame = true;
                }
            }
        }
        if (hitSomethingThisFrame && !isHitStopping){
            StartCoroutine(HitStopRoutine());
            if (impulseSource != null){
                impulseSource.GenerateImpulseWithVelocity(rb.linearVelocity.normalized * hitRecoilForce);
            }
        }
    }
    void HandleSlowMovement(){
        if (isInvincible) return;
        if (!colorInit){
            originalColor = sprite.color;
            colorInit = true;
        }
        isShimmering = slowMovementAction.action.IsPressed();
        if (isShimmering){
            if (fadeTimer<fadeDuration){
                fadeTimer+=Time.deltaTime;
            }
            float t = fadeTimer/fadeDuration;
            if (!isInvincible) sprite.color = Color.Lerp(originalColor, shimmerColor, t);
            speed = slowSpeed;
            playerCore.SetActive(true);
            // canDash = false;
        } else{
            fadeTimer = 0;
            if (!isInvincible) sprite.color = originalColor;
            playerCore.SetActive(false);
            speed = originalSpeed;
            // canDash = true;
        }
    }

    void HandleShooting(){
        if (SimpleDomainAbility.isSimpleDomainActive) return;
        fireTimer -= Time.unscaledDeltaTime;
        if (shootAction.action.IsPressed() && fireTimer <= 0){
            Instantiate(bullet, spawnPoint.position, transform.rotation);
            if (impulseSource != null){
                impulseSource.GenerateImpulseWithVelocity(-transform.up * recoilForce);
            }
            fireTimer = timeBetweenFiring;
        }
    }

    void HandleDash(){
        if (dashAction.action.triggered && canDash && !isShimmering){
            Vector2 mouseScreenPos = aimAction.action.ReadValue<Vector2>();
            Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
            Vector2 dashDir = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;
            float expectedDashDistance = dashSpeed * dashDuration;
            float actualDashDuration = dashDuration;
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.5f, dashDir, expectedDashDistance, unphasableLayers);
            if (hit.collider != null){
                float safeDistance = Mathf.Max(0, hit.distance - 0.1f);
                actualDashDuration = safeDistance / dashSpeed;
                if (actualDashDuration <= 0) return;
            }
            StartCoroutine(Dash(dashDir, actualDashDuration));
        }
    }

    void HandleInteraction(){
        bool isHoldingInteract = interactAction.action.IsPressed();
        if (isHoldingInteract && holdInteractable && interactionSlider.timer <= interactionSlider.waitTimer){
            interactionSlider.gameObject.SetActive(true);
            interactionSlider.timer += Time.deltaTime;
            interactionSlider.SetSliderValue();
        }
        if (interactionSlider.timer >= interactionSlider.waitTimer){
            interactionSlider.timer = interactionSlider.waitTimer;
            interactionSlider.gameObject.SetActive(false);
            interactionButton.SetActive(false);
            Debug.Log("Breaching Complete!");
            holdInteractable = false;
        }
        else if (interactionSlider.timer > 0 && holdInteractable){
            interactionSlider.timer -= Time.deltaTime;
            interactionSlider.SetSliderValue();
            if (interactionSlider.timer <= 0){
                interactionSlider.timer = 0;
                interactionSlider.gameObject.SetActive(false);
            }
        }
        if (interactAction.action.triggered && interactable){
            Debug.Log("Interacted!");
        }
    } 

    void HandleAbilities()
    {
        if (abilityManager == null) return;
        abilityManager.ProcessInput(0, slot1Action.action.IsPressed());
        abilityManager.ProcessInput(1, slot2Action.action.IsPressed());
        abilityManager.ProcessInput(2, slot3Action.action.IsPressed());
    }
    void HandleHealing()
    {
        int hpLoss = maxHealth - currentHealth;
        if (repairAction.action.triggered && currentHeal > 0)
        {
            Debug.Log("Healing!");
            int healAmount = Mathf.Min(hpLoss, currentHeal);
            int startHP = currentHealth;
            int startRepair = currentHeal;
            currentHealth += healAmount;
            currentHeal -= healAmount;
            if (activeRepairRoutine != null)
            {
                StopCoroutine(activeRepairRoutine);
            }
            activeRepairRoutine = StartCoroutine(VisualRepairRoutine(startHP, currentHealth, startRepair, currentHeal));
        }
    }

    void OnEnable(){
        moveAction.action.Enable();
        aimAction.action.Enable();
        shootAction.action.Enable();
        dashAction.action.Enable();
        interactAction.action.Enable();
        slowMovementAction.action.Enable();
        repairAction.action.Enable();
        slot1Action.action.Enable();
        slot2Action.action.Enable();
        slot3Action.action.Enable();

        moveAction.action.performed += OnMovePerformed;
        moveAction.action.canceled += OnMoveCanceled;
        }

    void OnDisable(){
        moveAction.action.Disable();
        aimAction.action.Disable();
        shootAction.action.Disable();
        dashAction.action.Disable();
        interactAction.action.Disable();
        slowMovementAction.action.Disable();
        repairAction.action.Disable();
        slot1Action.action.Disable();
        slot2Action.action.Disable();
        slot3Action.action.Disable();

        moveAction.action.performed -= OnMovePerformed;
        moveAction.action.canceled -= OnMoveCanceled;
        }
    void OnMovePerformed(InputAction.CallbackContext ctx){
        // Get Vector2 input
        moveInput = ctx.ReadValue<Vector2>();
        }

    void OnMoveCanceled(InputAction.CallbackContext ctx){
            moveInput = Vector2.zero;
    }

    public void OnClick(){
        Instantiate(bullet, spawnPoint.position, transform.rotation);
    }

    private IEnumerator Dash(Vector2 dashDir, float actualDuration)
    {
        damagedDuringDash.Clear();
        canDash = false;
        isDashing = true;
        gameObject.layer = dashingLayerIndex; 

        // Lấy hướng chuột để lướt tới
        Vector2 mouseScreenPos = aimAction.action.ReadValue<Vector2>();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
        // Vector2 dashDir = ((Vector2)mouseWorldPos - (Vector2)transform.position).normalized;

        float startTime = Time.unscaledTime;
        
        while (Time.unscaledTime < startTime + actualDuration)
        {
            if (Time.timeScale < 1f)
            {
                rb.linearVelocity = Vector2.zero;
                transform.position += (Vector3)dashDir * dashSpeed * Time.unscaledDeltaTime;
            }
            else
            {
                rb.linearVelocity = dashDir * dashSpeed;
            }
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        gameObject.layer = originalLayerIndex; 
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    IEnumerator VisualRepairRoutine(int startHP, int targetHP, int startRepair, int targetHeal)
    {
        float elapsed = 0f;
        while (elapsed < visualHealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / visualHealDuration;
            if (healthBar != null)
            {
                healthBar.SetHealth(Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, t)));
            }
            if (repairBar != null)
            {
                repairBar.SetRepair(Mathf.RoundToInt(Mathf.Lerp(startRepair, targetHeal, t)));
            }
            yield return null;
        }
        if (healthBar != null) healthBar.SetHealth(targetHP);
        if (repairBar != null) repairBar.SetRepair(targetHeal);
        activeRepairRoutine = null;
    }
    IEnumerator HitStopRoutine(){
        isHitStopping = true;
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
        isHitStopping = false;
    }

    public void TakeDamage(int damage, Transform damageSource = null){
        if (isInvincible) return;
        if (activeRepairRoutine != null)
        {
            StopCoroutine(activeRepairRoutine);
            activeRepairRoutine = null;
            if (repairBar != null) repairBar.SetRepair(currentHeal);
        }
        Debug.Log("Taking damages! AH!");
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
        if (currentHealth > 0)
        {
            StartCoroutine(DamageSequenceRoutine(damageSource));
        }
    }

    IEnumerator DamageSequenceRoutine(Transform damageSource)
    {
        isInvincible = true;
        isKnockedBack = true;
        if (isShimmering) isShimmering = false;
        if (playerCore != null) playerCore.SetActive(false);
        Vector2 knockbackDir = Vector2.down;
        if (damageSource != null)
        {
            knockbackDir = ((Vector2)transform.position - (Vector2)damageSource.position).normalized;
        }
        rb.linearVelocity = knockbackDir * knockbackForce;
        float flashInterval = invulnerabilityDuration / (flashCount * 2);
        float elapsedTime = 0f;
        for (int i = 0; i < flashCount; i++)
        {
            sprite.color = damageFlashColor;
            yield return new WaitForSeconds(flashInterval);
            elapsedTime += flashInterval;
            if (isKnockedBack && elapsedTime >= knockbackDuration)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero;
            }
            sprite.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
            if (isKnockedBack && elapsedTime >= knockbackDuration)
            {
                isKnockedBack = false;
                rb.linearVelocity = Vector2.zero;
            }
        }
        isKnockedBack = false;
        isInvincible = false;
        sprite.color = originalColor;
    }

    void OnTriggerEnter2D (Collider2D other){
        if (other.CompareTag("DataCenter") && interactionSlider.timer < interactionSlider.waitTimer){
            interactionButton.SetActive(true);
            holdInteractable = true;
        }
        if (other.CompareTag("Interactives")){
            interactionButton.SetActive(true);
            interactable = true;
        }
    }

    void OnTriggerExit2D (Collider2D other){
        if (other.CompareTag("DataCenter")){
            if (interactionButton != null) interactionButton.SetActive(false);
            holdInteractable = false;
            if (interactionSlider.timer < interactionSlider.waitTimer){
                interactionSlider.timer = 0;
            }
            interactionSlider.gameObject.SetActive(false);
        }
        if (other.CompareTag("Interactives")){
            interactionButton.SetActive(false);
            interactable = false;
        }
    }
}