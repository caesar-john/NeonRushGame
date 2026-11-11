using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float forwardSpeed = 8f;
    public float laneDistance = 3f;
    public float laneChangeSpeed = 15f;
    public int targetLane = 1; // 0: Kiri, 1: Tengah, 2: Kanan

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true; // Kunci rotasi agar tidak guling
        }
    }

    void Update()
    {
        // Tangkap input keyboard secara real-time di Update
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (targetLane < 2) targetLane++;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (targetLane > 0) targetLane--;
        }
    }

    void FixedUpdate()
    {
        // 1. Hitung target jalur Z
        float targetZ = (targetLane - 1) * laneDistance;
        Vector3 currentPos = transform.position;

        // 2. Geser Z secara mulus menggunakan Time.fixedDeltaTime
        float newZ = Mathf.Lerp(currentPos.z, targetZ, laneChangeSpeed * Time.fixedDeltaTime);

        // 3. Maju ke arah sumbu -X
        float newX = currentPos.x - (forwardSpeed * Time.fixedDeltaTime);

        // 4. Perbarui posisi sekaligus agar tidak macet di tengah jalan
        transform.position = new Vector3(newX, currentPos.y, newZ);

        // 5. Paksa rotasi tetap tegak lurus sempurna
        transform.rotation = Quaternion.identity;
    }
}