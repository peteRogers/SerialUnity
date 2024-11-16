# SerialUnity 
### A Simple way to read values from the serial port in Unity 6.
#### Tagging data with an ID creates an efficient way of sending multiple pieces of data out from a serial device to Unity which can then be accessed by ID in any gameObject. 

##### Below is an example of data being sent from arduino, it sends three pieces of data with the ID of 0, 1, 2. The character '>' splits the ID and the value and the '<' character splits the data up into id and value pairs - so that more than one ID and Value can be sent at a time. It is important write this in the correct way:
```Arduino
Serial.println("0>1000<1>2000<2>105<);
```
##### Once you have added the ArduinoCommunicator script to an empty GameObject in your scene and configured the serial port name and baud rate you just need to add this code to get the data in any GameObjects script making sure you have the correct ID set in the dataID variable:
```csharp
private ArduinoCommunicator arduinoCommunicator;
private int dataID = 1; //change this value for the correct ID
  void Start()
  {
    arduinoCommunicator = GameObject.FindFirstObjectByType<ArduinoCommunicator>();
  }

  void Update()
  {
    ArduinoValue? v = arduinoCommunicator.GetItemById(dataID);
    if (v != null)
    {
      // Debug.Log($"Data from Arduino: {v.Value.GetValue()}");
      float myValue = v.Value.GetValue() / 100.0f;
      //DO SOMETHING e.g. 
      transform.position = new Vector3(myValue, transform.position.y, transform.position.z);
    }
  }
```
##### You can also send data out on Serial, but right now it is just a string that is sent without any labelling or anything clever. 
