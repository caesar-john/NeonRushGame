using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(8.5f, 5f, 0f);

    void LateUpdate()
    {
        if (player == null) return;

        transform.position = player.position + offset;

        // Kamera menghadap ke arah Player
        transform.LookAt(player.position);
    }
}