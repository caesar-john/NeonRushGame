using System.Collections;
using UnityEngine;

public class CoinWork : MonoBehaviour
{
    [SerializeField] private bool collectedCoin = false;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float floatUpSpeed = 6f;

    private Animator anim;
    private Collider coinCollider;

    void Start()
    {
        anim = GetComponent<Animator>();
        coinCollider = GetComponent<Collider>();
    }

    void Update()
    {
        // Koin HANYA berputar jika BELUM diambil
        if (!collectedCoin)
        {
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.World);
        }
        else
        {
            // Begitu diambil, koin melayang naik ke atas
            transform.Translate(Vector3.up * floatUpSpeed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Cek apakah yang menabrak adalah Player dan koin ini BELUM diambil
        if (other.CompareTag("Player") && !collectedCoin)
        {
            collectedCoin = true;

            // Langsung matikan Collider khusus koin ini saja agar tidak terdeteksi ganda
            if (coinCollider != null)
            {
                coinCollider.enabled = false;
            }

            if (anim != null)
            {
                anim.Play("Shrink");
            }

            StartCoroutine(DeleteCoin());
        }
    }

    IEnumerator DeleteCoin()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false); // Menonaktifkan koin setelah 0.5 detik
    }
}