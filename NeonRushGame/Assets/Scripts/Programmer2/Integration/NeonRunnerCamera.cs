using UnityEngine;

// Kamera chase untuk map desainer yang berjalan ke sumbu X negatif. Skrip
// FollowPlayer milik rekan tetap dipakai di scene aslinya, tetapi di scene
// "Game" dipasang versi ini agar bingkai kamera pas untuk karakter berukuran
// besar dan perpindahan lajur tidak membuat seluruh dunia "melompat".
[DefaultExecutionOrder(1000)]
[AddComponentMenu("Neon Rush/Integration/Runner Camera")]
public class NeonRunnerCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float backDistance = 18f;
    [SerializeField] private float height = 9f;
    [SerializeField] private float lookAhead = 5f;
    [SerializeField] private float lookHeight = 2.5f;
    [SerializeField] private float heightSharpness = 10f;

    public static NeonRunnerCamera Install(Transform player)
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Camera camera = Camera.main;

        // Kalau ada beberapa scene termuat (mis. sisa scene GameNeonCity),
        // pakai kamera yang satu scene dengan pemain agar tidak direbut.
        if (player != null)
        {
            if (camera == null || camera.gameObject.scene != player.gameObject.scene)
            {
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i] == null) continue;
                    if (cameras[i].gameObject.scene != player.gameObject.scene) continue;
                    camera = cameras[i];
                    break;
                }
            }
        }

        if (camera == null && cameras.Length > 0) camera = cameras[0];

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] == null || cameras[i] == camera) continue;

            FollowPlayer follow = cameras[i].GetComponent<FollowPlayer>();
            if (follow != null) follow.enabled = false;

            NeonCityCameraRig legacy = cameras[i].GetComponent<NeonCityCameraRig>();
            if (legacy != null) legacy.enabled = false;

            // Kamera dari scene lain dimatikan sama sekali supaya tidak
            // menimpa bingkai kamera chase.
            if (player == null || cameras[i].gameObject.scene != player.gameObject.scene)
                cameras[i].enabled = false;
        }

        if (camera == null)
        {
            Debug.LogWarning("[NeonRunnerCamera] Kamera utama tidak ditemukan, kamera bawaan scene dipakai.", null);
            return null;
        }

        if (!camera.gameObject.activeSelf) camera.gameObject.SetActive(true);
        camera.depth = 100f;
        camera.enabled = true;

        NeonRunnerCamera rig = camera.GetComponent<NeonRunnerCamera>();
        if (rig == null) rig = camera.gameObject.AddComponent<NeonRunnerCamera>();

        rig.target = player;
        rig.Snap();

        Debug.Log("[NeonRunnerCamera] Dipasang di kamera '" + camera.name + "' scene '" + camera.gameObject.scene.name +
                  "' (total kamera aktif: " + cameras.Length + "). Target: " + (player != null ? player.name : "null") + ".", camera);
        return rig;
    }

    public void Configure(Transform followTarget)
    {
        target = followTarget;
        Snap();
    }

    public void Snap()
    {
        if (target == null) return;

        transform.position = target.position + new Vector3(backDistance, height, 0f);
        transform.LookAt(LookPoint());
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            GameManager manager = GameManager.Instance;
            if (manager != null) target = manager.Player;
            if (target == null) return;
            Snap();
        }

        Vector3 origin = target.position;

        // Lateral mengikuti persis agar pemain selalu di tengah; hanya tinggi
        // yang dilancarkan supaya lompatan tidak terasa kaku.
        float y = Mathf.Lerp(transform.position.y, origin.y + height, 1f - Mathf.Exp(-heightSharpness * Time.deltaTime));

        transform.position = new Vector3(origin.x + backDistance, y, origin.z);
        transform.LookAt(LookPoint());
    }

    private Vector3 LookPoint()
    {
        return target.position + new Vector3(-lookAhead, lookHeight, 0f);
    }
}
