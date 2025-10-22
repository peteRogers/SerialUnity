using UnityEngine;

public class ReallySimpleReader : MonoBehaviour
{
    public ArduinoCommunicator arduinoCommunicator; // drag it in if not found automatically
    public int idToRead = 0;                        // which Arduino value ID to show
    public float currentValue;                      // visible in Inspector

    void Start()
    {
        // Try to find the communicator automatically if not set
        if (arduinoCommunicator == null)
        {
            arduinoCommunicator = GameObject.FindFirstObjectByType<ArduinoCommunicator>();
        }

        if (arduinoCommunicator == null)
        {
            Debug.LogError("❌ ArduinoCommunicator not found in scene!");
        }
    }

    void Update()
    {
        if (arduinoCommunicator == null) return;

        ArduinoValue? value = arduinoCommunicator.GetItemById(idToRead);
        if (value != null)
        {
            currentValue = value.Value.GetValue();
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 250, 30), $"Arduino Value (ID {idToRead}): {currentValue}");
    }
}