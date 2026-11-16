
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 10f;
    [SerializeField] private float laneDistance = 3f;

    private Rigidbody rb;
    private int currentLane = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        // Pindah ke kiri
        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentLane = Mathf.Max(0, currentLane - 1);
        }

        // Pindah ke kanan
        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentLane = Mathf.Min(2, currentLane + 1);
        }
    }

    void FixedUpdate()
    {
        Vector3 movement = rb.position;

        // Gerakan maju pada sumbu X negatif
        movement.x -= forwardSpeed * Time.fixedDeltaTime;

        // Perpindahan jalur pada sumbu Z
        movement.z = (currentLane - 1) * laneDistance;

        rb.MovePosition(movement);
    }
}
