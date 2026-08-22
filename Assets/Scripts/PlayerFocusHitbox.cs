using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerFocusHitbox : AbilityBase
{
    [Header("Time Freeze Ability (Skill Z)")]
    [Tooltip("Thời gian hiệu lực của kỹ năng (giây)")]
    public float freezeDuration = 3f;
    
    [Tooltip("Thời gian hồi chiêu (giây)")]
    public float cooldownTime = 10f;
    
    [Tooltip("Mức độ làm chậm (0 = Đứng im hoàn toàn, 1 = Bình thường)")]
    [Range(0f, 1f)]
    public float slowMotionScale = 0.05f; // Chậm gần như đứng im

    // Các biến quản lý hệ thống thời gian
    private float currentFreezeTimer = 0f;
    private float currentCooldownTimer = 0f;
    public static bool isTimeFrozen = false;

    void Update()
    {
        // --- 1. HỆ THỐNG ĐẾM GIỜ (TIMERS) ---
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.unscaledDeltaTime;
        }

        if (isTimeFrozen)
        {
            currentFreezeTimer -= Time.unscaledDeltaTime;
            
            // Khi hết 3 giây đóng băng
            if (currentFreezeTimer <= 0)
            {
                Time.timeScale = 1f; // Trả lại thời gian bình thường
                Time.fixedDeltaTime = 0.02f; // Trả lại tốc độ tính toán vật lý
                isTimeFrozen = false;
                Debug.Log("Hết thời gian đóng băng!");
            }
        }
    }

    public override void OnButtonDown()
    {
        if (currentCooldownTimer <= 0 && !isTimeFrozen)
        {
            ActivateTimeFreeze();
        }
        else
        {
            Debug.Log("ZA WARUDO đang hồi chiêu!");
        }
    }
    // Hàm thực thi kỹ năng ngưng đọng thời gian
    void ActivateTimeFreeze()
    {
        isTimeFrozen = true;
        currentFreezeTimer = freezeDuration; // Bắt đầu đếm 3s
        currentCooldownTimer = cooldownTime; // Bắt đầu đếm hồi chiêu 10s
        
        Time.timeScale = slowMotionScale; // Làm chậm vạn vật
        
        // Ép hệ thống vật lý tính toán chậm lại theo để tránh lỗi giật lag
        Time.fixedDeltaTime = 0.02f * Time.timeScale; 
        
        Debug.Log("ZA WARUDO! Kích hoạt đóng băng 3s. Cooldown 10s.");
    }

    // [BẢO HIỂM LỖI] - Nếu chuyển Scene hoặc thoát game khi đang bị đóng băng, 
    // phải trả lại thời gian về bình thường, nếu không game sẽ bị kẹt vĩnh viễn
    void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isTimeFrozen = false;
    }
}