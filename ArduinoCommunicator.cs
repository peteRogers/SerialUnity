using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Text;

public class ArduinoCommunicator : MonoBehaviour
{
    private SerialPort serialPort;
    private readonly List<ArduinoValue> values = new List<ArduinoValue>(16);
    private readonly List<ArduinoValue> parsedItems = new List<ArduinoValue>(16);
    public int baudrate = 9600;
    public string portname;
    private CancellationTokenSource cancellationTokenSource;
    private readonly object dataLock = new object();
    private int linesLogged = 0;
    private const int maxLinesToLog = 5;
    private SynchronizationContext unitySyncContext;

    public event Action<List<ArduinoValue>> OnDataReceived;

    void Start()
    {
        unitySyncContext = SynchronizationContext.Current;
        portname = GetMostRecentPort();

        if (string.IsNullOrEmpty(portname))
        {
            Debug.LogError("No serial ports found! Please connect your Arduino.");
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ConnectAndReadAsync(cancellationTokenSource.Token));
    }

    private string GetMostRecentPort()
    {
        string[] ports = SerialPort.GetPortNames();

        if (ports == null || ports.Length == 0)
            return null;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX
        var usbPorts = ports
            .Where(p => p.Contains("usb") || p.Contains("ttyACM") || p.Contains("ttyUSB"))
            .OrderByDescending(p => p)
            .ToList();

        if (usbPorts.Count > 0)
        {
            Debug.Log("✅ Detected Arduino-like port: " + usbPorts.First());
            return usbPorts.First();
        }

        Debug.LogWarning("No Arduino-like ports found, using last available: " + ports.Last());
        return ports.Last();
#else
        var comPorts = ports
            .Where(p => p.StartsWith("COM"))
            .Select(p => new { Name = p, Number = int.TryParse(p.Substring(3), out int num) ? num : -1 })
            .Where(p => p.Number >= 0)
            .OrderByDescending(p => p.Number)
            .ToList();

        return comPorts.Count > 0 ? comPorts.First().Name : ports.Last();
#endif
    }

    private async Task ConnectAndReadAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (serialPort == null || !serialPort.IsOpen)
            {
                try
                {
                    serialPort?.Dispose();
                    serialPort = new SerialPort(portname, baudrate)
                    {
                        ReadTimeout = 1000,
                        WriteTimeout = 1000,
                        Encoding = Encoding.ASCII
                    };
                    serialPort.Open();
                    Debug.Log("Serial port opened successfully: " + portname);
                    linesLogged = 0;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error opening serial port '{portname}': {e.Message}");
                    await Task.Delay(2000, token).ConfigureAwait(false);
                    continue;
                }
            }

            try
            {
                string line = await ReadLineAsync(serialPort, token).ConfigureAwait(false);
                if (line != null)
                {
                    if (linesLogged < maxLinesToLog)
                    {
                        Debug.Log($"Received line: {line}");
                        linesLogged++;
                    }

                    ParseLine(line, parsedItems);

                    lock (dataLock)
                    {
                        AddOrUpdateItems(parsedItems);
                    }

                    // Dispatch to main thread
                    if (OnDataReceived != null)
                    {
                        List<ArduinoValue> snapshot;
                        lock (dataLock)
                        {
                            snapshot = new List<ArduinoValue>(values);
                        }
                        unitySyncContext.Post(_ => OnDataReceived?.Invoke(snapshot), null);
                    }

                    parsedItems.Clear();
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, exit loop
                break;
            }
            catch (TimeoutException)
            {
                // Ignore timeout, continue reading
            }
            catch (Exception e)
            {
                Debug.LogError("Error reading from serial port: " + e.Message);
                CloseSerialPort();
                await Task.Delay(2000, token).ConfigureAwait(false);
            }
        }
    }

    private static async Task<string> ReadLineAsync(SerialPort port, CancellationToken token)
    {
        return await Task.Run(() =>
        {
            try
            {
                return port.ReadLine();
            }
            catch (TimeoutException)
            {
                return null;
            }
        }, token).ConfigureAwait(false);
    }

    private void ParseLine(string data, List<ArduinoValue> outputList)
    {
        var itemPairs = data.Split('<');
        foreach (var pair in itemPairs)
        {
            if (!string.IsNullOrEmpty(pair))
            {
                ArduinoValue item = ParseItem(pair);
                if (item.id != -1)
                    outputList.Add(item);
            }
        }
    }

    private ArduinoValue ParseItem(string pair)
    {
        var parts = pair.Split('>');
        if (parts.Length == 2 && int.TryParse(parts[0], out int id) && int.TryParse(parts[1], out int value))
        {
            return new ArduinoValue(id, value);
        }
       // Debug.LogWarning($"Invalid item format: {pair}");
        return new ArduinoValue(-1, -1);
    }

    public void SendValue(int value)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine(value.ToString());
                Debug.Log("Sent value: " + value);
            }
            catch (Exception e)
            {
                Debug.LogError("Error writing to serial port: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("Cannot send value, serial port is not open.");
        }
    }

    public ArduinoValue? GetItemById(int id)
    {
        lock (dataLock)
        {
            int index = values.FindIndex(item => item.id == id);
            return index != -1 ? values[index] : (ArduinoValue?)null;
        }
    }

    private void AddOrUpdateItems(List<ArduinoValue> newItems)
    {
        foreach (var newItem in newItems)
        {
            int index = values.FindIndex(item => item.id == newItem.id);
            if (index != -1)
                values[index] = newItem;
            else
                values.Add(newItem);
        }
    }

    private void CloseSerialPort()
    {
        try
        {
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
                serialPort.Dispose();
                serialPort = null;
                Debug.Log("Serial port closed.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error closing serial port: " + e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        cancellationTokenSource?.Cancel();
        CloseSerialPort();
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

    public override string ToString() => $"ID: {id}, Value: {value}";
    public int GetValue() => value;
}