using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [Header("Pushback & Speed")]
    [SerializeField] float moveSpeed = 100f;
    [SerializeField] float knockbackForce = 15f;
    [Header("Effects")]
    [SerializeField] GameObject sparkEffect; // Biến chứa Prefab tia lửa
    Rigidbody2D rb;

    // --- CÁC BIẾN QUẢN LÝ TỐC ĐỘ VÀ GIẢN LĨNH VỰC ---
    private float currentSpeed;
    private bool isIntercepted = false; // Đánh dấu đạn đã bị chém chưa

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = moveSpeed; // Gán tốc độ ban đầu
    }

    void Update()
    {
        // Di chuyển đạn bằng thời gian thực (unscaledDeltaTime) để xuyên qua Time Stop
        transform.localPosition += transform.up * Time.unscaledDeltaTime * currentSpeed;
    }

    // --- HÀM BỊ GỌI KHI LỌT VÀO GIẢN LĨNH VỰC ---
    public void InterceptBySimpleDomain()
    {
        // Nếu đạn đã bị chém rồi thì bỏ qua để không chém đè lên nhau
        if (isIntercepted) return; 
        
        isIntercepted = true;

        // 1. Ép giảm tốc độ đi 50% ngay lập tức để tạo cảm giác "bị khựng lại"
        currentSpeed = moveSpeed * 0.5f;

        // 2. Hẹn giờ tiêu hủy đạn (0.15 giây) 
        Destroy(gameObject, 0.15f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Chạm tường hoặc biên thì hủy đạn ngay
        if (other.CompareTag("Walls") || other.CompareTag("Borders")){
            Destroy(gameObject);
            return;
        }

        // 2. Xử lý va chạm của ĐẠN PLAYER (Đảm bảo khớp cả 2 trường hợp có s hoặc không có s)
        if (gameObject.CompareTag("Bullets") || gameObject.CompareTag("Bullet"))
        {
            if (other.CompareTag("Enemies")){
                Destroy(gameObject);
            }
            else if (other.CompareTag("EnemyBullets")){
                if (sparkEffect != null)
                {
                    Instantiate(sparkEffect, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
                Destroy(other.gameObject); // Đạn mình chạm đạn địch thì cả 2 cùng nổ
            }
            else if (other.CompareTag("PushableEnemy")){
                Rigidbody2D targetRb = other.GetComponent<Rigidbody2D>();
                if (targetRb != null){
                    Vector2 pushDirection = transform.up;
                    targetRb.AddForce(pushDirection * knockbackForce, ForceMode2D.Impulse);
                }
                Destroy(gameObject);
            }
        }
        // 3. Xử lý va chạm của ĐẠN ĐỊCH
        else if (gameObject.CompareTag("EnemyBullets"))
        {
            PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
            if (player != null){
                // Nếu trúng vỏ ngoài của Player
                if (other.CompareTag("Player") && !player.isShimmering && !player.isDashing){
                    player.TakeDamage(10);
                    Destroy(gameObject);
                }
                // Nếu trúng lõi Core
                else if (other.CompareTag("Core") && player.isShimmering){
                    player.TakeDamage(20);
                    Destroy(gameObject);
                    Debug.Log("My core is hit! GAHH!!");
                }
            }
        }
    }
}