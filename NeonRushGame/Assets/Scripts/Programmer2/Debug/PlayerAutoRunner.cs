using UnityEngine;

[AddComponentMenu("Neon Rush/Debug/Player Auto Runner (Testing Only)")]
public class PlayerAutoRunner : MonoBehaviour
{
    [Header("TESTING ONLY - ganti dengan PlayerController milik Programmer 1")]
    [SerializeField] private float laneWidth = 3f;
    [SerializeField] private float laneChangeSpeed = 14f;
    [SerializeField] private float jumpHeight = 2.2f;
    [SerializeField] private float gravity = -32f;

    private int currentLane = 1;
    private float targetX;
    private float verticalVelocity;
    private float groundY;
    private bool grounded = true;

    private void Start()
    {
        groundY = transform.position.y;
        targetX = (currentLane - 1) * laneWidth;
    }

    private void Update()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || !gameManager.IsPlaying) return;

        transform.position += Vector3.forward * (gameManager.Speed * Time.deltaTime);

        float x = Mathf.MoveTowards(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);
        float y = transform.position.y;

        if (grounded && JumpPressed())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            grounded = false;
            AudioManager.PlaySfx(SfxId.Jump);
        }

        if (!grounded)
        {
            verticalVelocity += gravity * Time.deltaTime;
            y += verticalVelocity * Time.deltaTime;

            if (y <= groundY)
            {
                y = groundY;
                verticalVelocity = 0f;
                grounded = true;
            }
        }

        transform.position = new Vector3(x, y, transform.position.z);

        if (LeftPressed()) MoveLane(-1);
        if (RightPressed()) MoveLane(1);
        if (SlidePressed()) AudioManager.PlaySfx(SfxId.Slide);
    }

    private void MoveLane(int direction)
    {
        int next = Mathf.Clamp(currentLane + direction, 0, 2);
        if (next == currentLane) return;

        currentLane = next;
        targetX = (currentLane - 1) * laneWidth;
    }

    private static bool LeftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
#endif
    }

    private static bool RightPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
#endif
    }

    private static bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
    }

    private static bool SlidePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
#endif
    }
}
