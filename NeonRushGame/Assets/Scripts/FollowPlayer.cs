using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 3, -6); 

    
    void LateUpdate()
    {
        if (player != null)
        {
            
            transform.position = player.position + offset;
        }
    }
}