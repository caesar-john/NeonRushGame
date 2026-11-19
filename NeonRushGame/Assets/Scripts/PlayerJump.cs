using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    [SerializeField] private float jumpForce = 15;

    /// <summary>
    /// Kekuatan lompat. Bisa diatur dari script lain (mis. adapter scene)
    /// tanpa harus mengubah nilai di Inspector scene.
    /// </summary>
    public float JumpForce
    {
        get { return jumpForce; }
        set { jumpForce = value; }
    }

    private Rigidbody rb;
    private bool isGrounded = false;

    //animator
    private Animator animator;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody tidak ditemukan pada Player!");
        }

        if (animator == null)
        {
            Debug.LogError("Animator tidak ditemukan pada player");
        }

        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            isGrounded = false;

            if (animator != null) animator.SetTrigger("Jump");
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Sinkronisasi kondisi fisik dengan Animator
        if (animator != null)
        {
            animator.SetBool("IsGrounded", isGrounded);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Hanya menganggap permukaan dari bawah sebagai tempat mendarat
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f && rb.linearVelocity.y <= 0.1f)
            {
                isGrounded = true;
                return;
            }
        }
    }
}
