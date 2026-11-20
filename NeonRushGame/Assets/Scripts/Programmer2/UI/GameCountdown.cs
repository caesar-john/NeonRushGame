
using System.Collections;
using TMPro;
using UnityEngine;

public class GameCountdown : MonoBehaviour
{
    [Header("UI Countdown")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TMP_Text countdownText;

    [Header("Countdown Settings")]
    [SerializeField] private int countdownDuration = 3;

    [Header("Countdown Audio")]
    [SerializeField] private AudioSource countdownAudio;
    [SerializeField] private AudioClip countdownBeep;
    [SerializeField] private AudioClip goSound;

    private void Start()
    {
        StartCoroutine(StartCountdown());
    }

    private IEnumerator StartCountdown()
    {
        // Menghentikan gameplay sementara
        Time.timeScale = 0f;

        // Menampilkan panel countdown
        countdownPanel.SetActive(true);

        // Countdown 3, 2, 1
        for (int i = countdownDuration; i > 0; i--)
        {
            countdownText.text = i.ToString();

            // Memutar suara pada setiap angka
            if (countdownAudio != null && countdownBeep != null)
            {
                countdownAudio.PlayOneShot(countdownBeep);
            }

            // Jeda satu detik sebelum angka berikutnya
            yield return new WaitForSecondsRealtime(1f);
        }

        // Menampilkan GO!
        countdownText.text = "GO!";

        // Memutar suara GO sampai selesai
        if (countdownAudio != null && goSound != null)
        {
            countdownAudio.PlayOneShot(goSound);
            yield return new WaitForSecondsRealtime(goSound.length);
        }
        else
        {
            yield return new WaitForSecondsRealtime(1f);
        }

        // Memulai gameplay kembali
        Time.timeScale = 1f;

        // Menyembunyikan panel countdown
        countdownPanel.SetActive(false);
    }
}
