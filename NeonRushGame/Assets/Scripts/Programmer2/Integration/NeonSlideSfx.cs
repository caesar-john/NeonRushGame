using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Bagian P2 untuk efek suara geser. Mekanik slide tetap milik P1: skrip ini
// hanya membaca status slide yang dibuka oleh skrip P1 (properti/field bool
// bernama slid* atau parameter Animator "Slide"), lalu memutar SfxId.Slide.
// Tidak ada input, collider, atau gerakan yang diubah di sini.
[AddComponentMenu("Neon Rush/Integration/Slide Sfx")]
public class NeonSlideSfx : MonoBehaviour
{
    [Tooltip("Nama properti/field bool di skrip gerak P1 yang menandakan sedang geser.")]
    [SerializeField] private string[] stateNames = { "IsSliding", "isSliding", "Sliding", "sliding", "Slide" };

    private const string AnimatorParameter = "Slide";

    private readonly List<Func<bool>> readers = new List<Func<bool>>();
    private Animator animator;
    private bool animatorHasSlide;
    private bool sliding;

    private void Awake()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this) continue;

            Type type = behaviour.GetType();
            if (!type.Name.StartsWith("Player", StringComparison.Ordinal)) continue;

            for (int n = 0; n < stateNames.Length; n++)
            {
                Func<bool> reader = CreateReader(behaviour, type, stateNames[n]);
                if (reader != null)
                {
                    readers.Add(reader);
                    break;
                }
            }
        }

        animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Bool &&
                    parameters[i].name == AnimatorParameter)
                {
                    animatorHasSlide = true;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        bool now = IsSliding();

        if (now && !sliding) AudioManager.PlaySfx(SfxId.Slide);
        sliding = now;
    }

    private bool IsSliding()
    {
        for (int i = 0; i < readers.Count; i++)
        {
            if (readers[i]()) return true;
        }

        return animatorHasSlide && animator.GetBool(AnimatorParameter);
    }

    private static Func<bool> CreateReader(MonoBehaviour behaviour, Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        PropertyInfo property = type.GetProperty(name, flags);
        if (property != null && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0)
            return () => (bool)property.GetValue(behaviour);

        FieldInfo field = type.GetField(name, flags);
        if (field != null && field.FieldType == typeof(bool))
            return () => (bool)field.GetValue(behaviour);

        return null;
    }
}
