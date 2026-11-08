using System.Collections;
using UnityEngine;

public class CoinWork : MonoBehaviour
{
    // 1. Variabel
    [SerializeField] private bool collectedCoin = false;
    [SerializeField] private float rotateSpeed = 100f;
    [SerializeField] private float floatUpSpeed = 6f;

    private Animator anim;
    private Collider coinCollider;

    // 2. Fungsi bawaan Unity (Start & Update)
    void Start()
    {
        anim = GetComponent<Animator>();
        coinCollider = GetComponent<Collider>();
    }

    void Update()
    {
        transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.World);

        if (collectedCoin)
        {
            transform.Translate(Vector3.up * floatUpSpeed * Time.deltaTime, Space.World);
        }
    }

    // 3. Fungsi Pemicu Tabrakan (Dipanggil Saat Player Menyentuh Koin)
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !collectedCoin)
        {
            collectedCoin = true;

            if (coinCollider != null) coinCollider.enabled = false;

            if (anim != null)
            {
                anim.Play("Shrink");
            }

            // Memanggil IEnumerator DeleteCoin() di bawahnya
            StartCoroutine(DeleteCoin());
        }
    }

    // 4. Coroutine Jeda Penghapusan Koin
    IEnumerator DeleteCoin()
    {
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }
}