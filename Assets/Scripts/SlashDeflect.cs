using UnityEngine;

public class SlashDeflect : MonoBehaviour
{
    [Header("Visual Effects")]
    public GameObject sparkEffect; // Chứa hiệu ứng tia lửa

    void OnTriggerEnter2D(Collider2D other)
    {
        // Nhận diện đạn địch
        if (other.CompareTag("EnemyBullets") || other.CompareTag("EnemyBullet"))
        {
            // Bắn tia lửa ngay tại tọa độ của viên đạn
            if (sparkEffect != null)
            {
                Instantiate(sparkEffect, other.transform.position, Quaternion.identity);
            }
            
            // Chém đứt (xóa) viên đạn
            Destroy(other.gameObject);
        }
    }
}