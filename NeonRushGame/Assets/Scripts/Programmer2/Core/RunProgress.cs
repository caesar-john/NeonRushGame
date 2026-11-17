using UnityEngine;

// Arah lari berbeda antar map: map lama memakai sumbu Z, map desainer memakai
// sumbu X. Skor dan kesulitan harus tetap memakai sumbu gerak yang benar.
public static class RunProgress
{
    private const float AxisLockDisplacement = 1f;

    // -1 = belum ditentukan, 0 = sumbu X, 2 = sumbu Z.
    private static int axis = -1;
    private static Vector3 origin;

    public static void SetAxis(int value)
    {
        axis = value;
    }

    public static void Reset(Vector3 startPosition)
    {
        origin = startPosition;
    }

    public static float Measure(Transform player)
    {
        if (player == null) return 0f;

        Vector3 delta = player.position - origin;
        if (axis < 0)
        {
            float horizontal = Mathf.Abs(delta.x);
            float vertical = Mathf.Abs(delta.z);
            if (horizontal < AxisLockDisplacement && vertical < AxisLockDisplacement) return 0f;
            axis = horizontal >= vertical ? 0 : 2;
        }

        return Mathf.Abs(axis == 0 ? delta.x : delta.z);
    }
}
