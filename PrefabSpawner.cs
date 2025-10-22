using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject prefab;          // Prefab to spawn
    public int count = 100;            // Number of prefabs to spawn
    public float areaSize = 10f;       // Width/length of the spawn area
    public float spawnHeight = 3f;     // Y-position where objects are spawned

    [Header("Color Settings")]
    public bool randomizeColor = true; // Toggle random colors

    void Start()
    {
        if (prefab == null)
        {
            Debug.LogError("❌ Prefab not assigned in PrefabSpawner!");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            // Random spawn position in a square area
            float x = Random.Range(-areaSize / 2f, areaSize / 2f);
            float z = Random.Range(-areaSize / 2f, areaSize / 2f);
            Vector3 position = new Vector3(x, spawnHeight, z);

            // Spawn prefab
            GameObject obj = Instantiate(prefab, position, Quaternion.identity);
            obj.transform.SetParent(transform);

            // Randomize material color
            if (randomizeColor)
            {
                Renderer rend = obj.GetComponent<Renderer>();
                if (rend != null)
                {
                    // Make a new material instance so each prefab has its own color
                    Material newMat = new Material(rend.sharedMaterial);
                    newMat.color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.8f, 1f); // vivid colors
                    rend.material = newMat;
                }
                else
                {
                    Debug.LogWarning($"Prefab {obj.name} has no Renderer, cannot colorize.");
                }
            }
        }
    }

// #if UNITY_EDITOR
//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.yellow;
//         Gizmos.DrawWireCube(transform.position + Vector3.up * spawnHeight, new Vector3(areaSize, 0.1f, areaSize));
//     }
// #endif
}