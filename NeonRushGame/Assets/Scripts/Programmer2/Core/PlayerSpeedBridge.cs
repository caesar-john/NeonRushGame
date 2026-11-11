using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-20)]
[AddComponentMenu("Neon Rush/Integration/Player Speed Bridge")]
public class PlayerSpeedBridge : MonoBehaviour
{
    [Tooltip("Komponen gerak milik Programmer 1. Jika kosong, dicari otomatis berdasarkan nama class.")]
    [SerializeField] private MonoBehaviour movementSource;

    [Tooltip("Nama class komponen gerak Programmer 1.")]
    [SerializeField] private string movementTypeName = "PlayerMovement";

    private FieldInfo speedField;
    private float fallbackSpeed = 8f;

    private void Awake()
    {
        if (movementSource == null)
        {
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component != null && component.GetType().Name == movementTypeName)
                {
                    movementSource = component;
                    break;
                }
            }
        }

        if (movementSource == null) return;

        speedField = movementSource.GetType().GetField("forwardSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        if (speedField == null || speedField.FieldType != typeof(float))
        {
            speedField = null;
            return;
        }

        fallbackSpeed = (float)speedField.GetValue(movementSource);
    }

    private void Update()
    {
        if (movementSource == null || speedField == null) return;

        GameManager gameManager = GameManager.Instance;
        float speed = gameManager != null
            ? (gameManager.IsPlaying ? gameManager.Speed : 0f)
            : fallbackSpeed;

        speedField.SetValue(movementSource, speed);
    }
}
