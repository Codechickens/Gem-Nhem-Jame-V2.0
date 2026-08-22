using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class SquareEnemy : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] GameObject bullet;
    [SerializeField] Transform[] spawnPoints;

    [Header ("Combat Stats")]
    [SerializeField] float spareDistance = 2f;
    [SerializeField] float rotationSpeed;
    [SerializeField] float health = 300f;
    [SerializeField] float timeBetweenFiring = 0.5f;
    float delaySeconds = 1f;

    NavMeshAgent agent;
    Camera mainCam;
    float fireTimer;
    float delayTimer;
    bool isPreparingToFire = false;

    void Awake(){
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;
        Vector3 viewPos = mainCam.WorldToViewportPoint(transform.position);
        bool isVisibleOnCamera = viewPos.x >= 0 && viewPos.x <= 1 && viewPos.y >=0 && viewPos.y <=1;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (isVisibleOnCamera && distanceToPlayer <= spareDistance){
            EngagePlayer();
        }
        else{
            ChasePlayer();
        }
    }

    void EngagePlayer(){
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        if (!isPreparingToFire){
            isPreparingToFire = true;
            delayTimer = delaySeconds;
        }
        if (delayTimer > 0){
            delayTimer -= Time.deltaTime;
            return;
        }
        transform.Rotate(0,0, rotationSpeed);
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0){
            foreach (Transform sp in spawnPoints){
                Instantiate(bullet, sp.position, sp.rotation);
            }
            fireTimer = timeBetweenFiring;
        }
    }

    void ChasePlayer(){
        agent.isStopped = false;
        agent.SetDestination(player.position);
        transform.rotation = Quaternion.identity;
        isPreparingToFire = false;
        fireTimer = 0;
    }

    void OnTriggerEnter2D (Collider2D other){
        if (other.CompareTag("Bullets") || other.CompareTag("Bullet")){
            TakeDamage(10);
        }
       if (other.gameObject.CompareTag("Player"))
    {
        // 1. Tìm script PlayerMovement trên đối tượng bị va chạm
        PlayerMovement playerScript = other.GetComponent<PlayerMovement>();
        
        // 2. Nếu tìm thấy script đó, tiến hành trừ máu
        if (playerScript != null)
        {
            playerScript.TakeDamage(10, transform); 
            // Lưu ý: Đảm bảo hàm TakeDamage trong file PlayerMovement của bạn 
            // thực sự có nhận 2 tham số là (int damage, Transform enemyTransform)
        }
    }
    }

    public void TakeDamage(float amount){
        health -= amount;
        Debug.Log($"GAH! I'm hit. I only have {health} left!");
        if (health <= 0){
            gameObject.SetActive(false);
        }
    }
}
