using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sideSpeed = 10f;
    [SerializeField] float laneDistance = 3f; 

    [Header("Track Status")]
 
    [SerializeField] int trackNumber = 1;
    [SerializeField] bool currentMove = false;
    [SerializeField] int moveDirection = 0; 

    private float targetXPos;

    void Update()
    {
        
        if (!currentMove)
        {
           
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                LeftMove();
            }
            
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                RightMove();
            }
        }
    }

    void FixedUpdate()
    {
        
        transform.Translate(Vector3.forward * Time.fixedDeltaTime * moveSpeed, Space.World);

        float currentX = transform.position.x;
        float currentZ = transform.position.z;

      
        if (currentMove && moveDirection == 1)
        {
            transform.Translate(Vector3.left * Time.fixedDeltaTime * sideSpeed, Space.World);

            
            if (transform.position.x <= targetXPos)
            {
                StopMove(targetXPos, currentZ);
            }
        }

        
        if (currentMove && moveDirection == 2)
        {
            transform.Translate(Vector3.right * Time.fixedDeltaTime * sideSpeed, Space.World);
            if (transform.position.x >= targetXPos)
            {
                StopMove(targetXPos, currentZ);
            }
        }
    }

    public void LeftMove()
    {
        if (trackNumber > 0) 
        {
            trackNumber--;
            SetTargetAndMove(1);
        }
    }

    public void RightMove()
    {
        if (trackNumber < 2) 
        {
            trackNumber++;
            SetTargetAndMove(2);
        }
    }

    private void SetTargetAndMove(int direction)
    {
        currentMove = true;
        moveDirection = direction;
        targetXPos = (trackNumber - 1) * laneDistance;
    }

    private void StopMove(float x, float z)
    {
        currentMove = false;
        moveDirection = 0;
        transform.position = new Vector3(x, transform.position.y, z);
    }
}