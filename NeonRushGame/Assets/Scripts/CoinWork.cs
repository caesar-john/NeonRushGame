
using System.Collections;
using UnityEngine;

public class CoinWork : MonoBehaviour
{
    [Header("Coin Settings")]
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
        if (collectedCoin) return;

        transform.Rotate(
            0,
            rotateSpeed * Time.deltaTime,
            0,
            Space.World
        );
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Objek menyentuh koin: " + other.name);

        // Cek Tag Player pada objek yang menyentuh
        if (other.CompareTag("Player") && !collectedCoin)
        {
            Debug.Log("KOIN BERHASIL DIAMBIL!");

            collectedCoin = true;

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

        gameObject.SetActive(false);
    }
}
