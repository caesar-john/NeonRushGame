
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float forwardSpeed = 10f;
    [SerializeField] private float laneDistance = 3f;

    private Rigidbody rb;
    private int currentLane = 1;
    private bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints |= RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        if (!canMove) return;
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

    public void StopMovement()
    {
        canMove = false;
        rb.linearVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (!canMove) return;
        Vector3 movement = rb.position;

        // Gerakan maju p
        movement.x -= forwardSpeed * Time.fixedDeltaTime;

        //target posisi lane
        float targetZ = (currentLane - 1) * laneDistance;

        // Perpindahan lane secara bertahap
        movement.z = Mathf.MoveTowards(
            rb.position.z,
            targetZ,
            30 * Time.fixedDeltaTime
            );

        rb.MovePosition(movement);
    }
}
