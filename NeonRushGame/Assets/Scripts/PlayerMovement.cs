using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody rb;
   
    // Update is called once per frame
    void FixUpdate()
    {
        rb.AddForce(0, 0, 200 * Time.deltaTime) ;
    }
}
