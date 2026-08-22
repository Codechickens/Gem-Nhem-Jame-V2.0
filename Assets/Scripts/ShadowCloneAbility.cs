using UnityEngine;
using UnityEngine.InputSystem;

public class ShadowCloneAbility : MonoBehaviour
{
    [Header("Kage Bunshin Settings")]
    public GameObject clonePrefab;
    public int numberOfClones = 7;
    public float spawnRadius = 3.5f;
    public float cooldownTime = 20f;

    private float currentCooldown = 0f;

    void Update()
    {
        if (currentCooldown > 0) currentCooldown -= Time.unscaledDeltaTime;

        bool isCPressed = Input.GetKeyDown(KeyCode.C) || 
                          (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame);

        if (isCPressed && currentCooldown <= 0)
        {
            SpawnClones();
            currentCooldown = cooldownTime;
        }
    }

    void SpawnClones()
    {
        Debug.Log("TAJUU KAGE BUNSHIN NO JUTSU!");
        int cloneHP = 50; 

        for (int i = 0; i < numberOfClones; i++)
        {
            Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
            GameObject clone = Instantiate(clonePrefab, randomPos, transform.rotation);
            
            CloneBehavior cloneScript = clone.GetComponent<CloneBehavior>();
            if (cloneScript != null) cloneScript.Initialize(cloneHP);
        }
    }
}