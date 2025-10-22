using UnityEngine;

public class SimpleDataReader : MonoBehaviour
{
 float jumpForce = 10f;
float threshold = 100f;
float groundCheckDistance = 0.1f;
 float jumpCooldown = 0.3f;

  [SerializeField] private LayerMask groundLayerMask = ~0; // default: all layers
  [SerializeField] private float groundExtraDistance = 0.05f; // small cushion beyond collider
  private float _prevValue = 0f; // to detect rising edge over threshold
  private float lastJumpTime = -Mathf.Infinity;

  private ArduinoCommunicator arduinoCommunicator;
  private Rigidbody rb;
  private Collider col;

  void Start()
  {
    arduinoCommunicator = GameObject.FindFirstObjectByType<ArduinoCommunicator>();
    rb = GetComponent<Rigidbody>();
    if (rb == null)
    {
      Debug.LogError("Rigidbody component missing from this GameObject.");
    }
    col = GetComponent<Collider>();
    if (col == null)
    {
      Debug.LogError("Collider component missing from this GameObject. Ground check needs a collider to estimate feet position.");
    }
  }

  void FixedUpdate()
  {
    ArduinoValue? v = arduinoCommunicator.GetItemById(0);
    if (v != null && col != null && rb != null)
    {
      float valueRaw = v.Value.GetValue();
      float value = Mathf.Lerp(_prevValue, valueRaw, 0.3f);

      float sphereRadius = 0.25f;
      Vector3 spherePosition = col.bounds.center + Vector3.down * (col.bounds.extents.y - 0.05f);
      bool isGrounded = Physics.CheckSphere(spherePosition, sphereRadius, groundLayerMask, QueryTriggerInteraction.Ignore);
#if UNITY_EDITOR
      Debug.DrawRay(col.bounds.center, Vector3.down * (col.bounds.extents.y + groundExtraDistance), isGrounded ? Color.green : Color.red, 0f, false);
#endif

      //Debug.Log($"isGrounded: {isGrounded}, value: {value}");

      // Rising-edge trigger: only jump when we cross the threshold this frame, and we are grounded
      bool crossedUp = _prevValue >= threshold && value < threshold;
      if (crossedUp && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
      {
        lastJumpTime = Time.time;

        // Add randomized jump force and rotation using Random.insideUnitSphere
        Vector3 randomSphere = Random.insideUnitSphere;
        Vector3 randomDirection = Vector3.up + new Vector3(randomSphere.x * 0.1f, 0, randomSphere.z * 0.1f);
        float randomMultiplier = Random.Range(0.8f, 1.2f);
        rb.AddForce(randomDirection.normalized * jumpForce * randomMultiplier, ForceMode.Impulse);

        // Add a small random torque for rotation
        Vector3 randomTorque = Random.insideUnitSphere * jumpForce * 0.1f;
        rb.AddTorque(randomTorque, ForceMode.Impulse);
      }

      // Falling-edge trigger
      bool crossedDown = _prevValue > 105f && value < 95f;
      if (crossedDown)
      {
        // Implement any desired behavior for falling-edge trigger here
      }

      _prevValue = value;
    }
  }

  void OnDrawGizmosSelected()
  {
    if (col != null)
    {
      float sphereRadius = col.bounds.size.x * 0.5f;
      Vector3 spherePosition = col.bounds.center + Vector3.down * (col.bounds.extents.y - sphereRadius + groundExtraDistance);
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(spherePosition, sphereRadius);
    }
  }
}
