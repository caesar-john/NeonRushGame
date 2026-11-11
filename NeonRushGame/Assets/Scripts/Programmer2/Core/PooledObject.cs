using UnityEngine;

[DisallowMultipleComponent]
public class PooledObject : MonoBehaviour
{
    [HideInInspector] public GameObject SourcePrefab;
}
