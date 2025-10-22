using UnityEngine;

public class JumpExample : MonoBehaviour
{
 float jumpForce = 10f;
float threshold = 0.1f;
 float jumpCooldown = 0.03f;

  [SerializeField] private float groundYThreshold = 0f;   // Consider grounded when transform.position.y <= this
  [SerializeField] private float groundTolerance = 0.05f;  // Cushion to avoid flicker

  private float _prevValue = 0f; // to detect rising edge over threshold
  private float lastJumpTime = -Mathf.Infinity;

  private ArduinoCommunicator arduinoCommunicator;
  private Rigidbody rb;

  void Start()
  {
    arduinoCommunicator = GameObject.FindFirstObjectByType<ArduinoCommunicator>();
    rb = GetComponent<Rigidbody>();
    if (rb == null)
    {
      Debug.LogError("Rigidbody component missing from this GameObject.");
    }
    if (Mathf.Approximately(groundYThreshold, 0f))
    {
      groundYThreshold = transform.position.y;
    }
  }

  void FixedUpdate()
  {
    ArduinoValue? v = arduinoCommunicator.GetItemById(0);
    if (v != null && rb != null)
    {
      float value = v.Value.GetValue();
      
      bool isGrounded = transform.position.y <= (groundYThreshold + groundTolerance);

      Debug.Log($"isGrounded: {isGrounded}, value: {value}");

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

  

      _prevValue = value;
    }
  }
}
