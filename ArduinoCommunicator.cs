using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class ArduinoCommunicator : MonoBehaviour
{
    private SerialPort serialPort;
    private List<ArduinoValue> values = new List<ArduinoValue>();
    public string portname;
    public int baudrate;
    private Thread serialThread;
    private bool keepReading = true; // Flag to control the thread loop

    private readonly object dataLock = new object(); // Lock for thread-safe data access

    void Start()
    {
        // Initialize and open the serial port
        serialPort = new SerialPort(portname, baudrate); // Replace "COM3" with your port
        serialPort.ReadTimeout = 1000;             // Set read timeout to 1000 milliseconds
        serialPort.WriteTimeout = 1000;            // Set write timeout to 1000 milliseconds

        try
        {
            serialPort.Open();
            Debug.Log("Serial port opened successfully.");

            // Start a new thread to read serial data
            serialThread = new Thread(ReadSerialDataInThread);
            serialThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error opening serial port: " + e.Message);
        }
    }

    private void ReadSerialDataInThread()
    {
        while (keepReading)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    // Read a line of data from the serial port
                    string data = serialPort.ReadLine();
                    string[] itemPairs = data.Split('<');
                   // Debug.Log(itemPairs);
                    List<ArduinoValue> items = new List<ArduinoValue>();
                    foreach (string pair in itemPairs)
                    {
                        if (!string.IsNullOrEmpty(pair)) // Ensure the pair is not empty
                        {
                            // Use the helper function to convert the pair into an Item
                            ArduinoValue item = ParseItem(pair);
                            items.Add(item);
                        }
                    }

                    // Lock access to shared data and then add new or amended items
                    lock (dataLock)
                    {   
                        AddOrUpdateItems(items);
                    }
                }
                catch (System.TimeoutException)
                {
                    // Handle timeout - can ignore it if no data
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error reading from serial port: " + e.Message);
                }
            }
            Thread.Sleep(10); // Small delay to reduce CPU usage
        }
    }


    // Function to parse a string and return an Item
    ArduinoValue ParseItem(string pair)
    {
        string[] parts = pair.Split('>');
        if (parts.Length == 2)
        {
            int id = int.Parse(parts[0]);
            int value = int.Parse(parts[1]);
            
            return new ArduinoValue(id, value);
        }
        Debug.LogWarning($"Invalid item format: {pair}");
        return new ArduinoValue(-1, -1);
    }

    public void SendValue(int value)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine(value.ToString()); // Send the value as a string with newline
                Debug.Log("Sent value: " + value);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error writing to serial port: " + e.Message);
            }
        }
    }

   public  ArduinoValue? GetItemById(int id)
    {
        lock (values)
        {
            // Find the item with the matching ID
            int index = values.FindIndex(item => item.id == id);

            if (index != -1)
            {
                return values[index];
            }
            return null; // Item not found
        }
    }

    public bool IsConnected()
    {
        return serialPort != null && serialPort.IsOpen;
    }

void AddOrUpdateItems(List<ArduinoValue> newItems)
    {
        foreach (var newItem in newItems)
        {
            // Check if an item with the same ID already exists
            int index = values.FindIndex(item => item.id == newItem.id);

            if (index != -1)
            {
                // Overwrite the existing item
                values[index] = newItem;
               // Debug.Log($"Updated Item with ID {newItem.id}: {newItem}");
            }
            else
            {
                // Add the new item to the list
                values.Add(newItem);
              //  Debug.Log($"Added New Item: {newItem}");
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Stop the serial thread when the application quits
        keepReading = false;

        // Close the serial port
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.Close();
                Debug.Log("Serial port closed.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error closing serial port: " + e.Message);
            }
        }

        // Wait for the thread to finish
        if (serialThread != null && serialThread.IsAlive)
        {
            serialThread.Join(); // Ensure the thread completes
        }
    }
}



public struct ArduinoValue
{
    public int id { get; set; }
    public int value { get; set; }

    public ArduinoValue(int id, int value)
    {
        this.id = id;
        this.value = value;
    }

    public override string ToString()
    {
        return $"ID: {id}, Value: {value}";
    }

public int GetValue()
    {
        return value;
    }
}