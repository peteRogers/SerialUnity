using UnityEngine;

public class SimpleDataReader : MonoBehaviour
{
  private ArduinoCommunicator arduinoCommunicator;
  void Start()
  {
    arduinoCommunicator = GameObject.FindFirstObjectByType<ArduinoCommunicator>();
  }

  void Update()
  {
    ArduinoValue? v = arduinoCommunicator.GetItemById(0);
    if (v != null)
    {
      // Debug.Log($"Data from Arduino: {v.Value.GetValue()}");
      float f = v.Value.GetValue() / 100.0f;
      // transform.Translate(f, 0, 0);
      transform.position = new Vector3(f, transform.position.y, transform.position.z);
    }
  }
}
