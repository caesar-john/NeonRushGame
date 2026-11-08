using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 8f;   // Kecepatan maju Z
    [SerializeField] private float laneDistance = 3f;    // Jarak antar jalur (misal: X = -3, 0, 3)
    [SerializeField] private float laneChangeSpeed = 15f; // Kehalusan pindah jalur

    // 0 = Kiri (-3), 1 = Tengah (0), 2 = Kanan (3)
    [SerializeField] private int targetLane = 1;

    void Update()
    {
        // 1. Deteksi Tombol Tanpa Mengunci CurrentMove
        // Pemain bisa spam tombol A/D secepat mungkin
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (targetLane > 0)
            {
                targetLane--;
            }
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (targetLane < 2)
            {
                targetLane++;
            }
        }
    }

    void FixedUpdate()
    {
        // 2. Hitung target posisi X berdasarkan targetLane
        float targetX = (targetLane - 1) * laneDistance;

        // 3. Posisikan X secara mulus menggunakan Lerp
        Vector3 currentPos = transform.position;
        float newX = Mathf.Lerp(currentPos.x, targetX, laneChangeSpeed * Time.fixedDeltaTime);

        // 4. Pergerakan Maju Otomatis di Sumbu Z
        float newZ = currentPos.z + (forwardSpeed * Time.fixedDeltaTime);

        // 5. Terapkan Posisi Baru
        transform.position = new Vector3(newX, currentPos.y, newZ);
    }
}