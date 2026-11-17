using UnityEngine;

[AddComponentMenu("Neon Rush/Integration/Neon City Camera Rig")]
public class NeonCityCameraRig : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float worldHeight = 11.16f;
    [SerializeField] private float distance = 30f;
    [SerializeField] private float lateralOffset = -2.06f;

    public void Configure(Transform followTarget, float height, float followDistance, float lateral)
    {
        target = followTarget;
        worldHeight = height;
        distance = followDistance;
        lateralOffset = lateral;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            GameManager manager = GameManager.Instance;
            if (manager != null) target = manager.Player;
        }

        if (target == null) return;

        Vector3 position = target.position;
        transform.position = new Vector3(position.x + lateralOffset, worldHeight, position.z - distance);
    }
}
