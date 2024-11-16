# SerialUnity 
### A Simple way to read values from the serial port in Unity 6.
#### Tagging data with an ID creates an efficent way of sending multiple pieces of data out from a serial device to Unity which can then be accessed by ID in any gameObject. 

##### Below is an example of data being sent from arduino, it sends three pieces of data with the ID of 0, 1, 2. The character '>' splits the ID and the value and the '<' character splits the data up into id and value pairs - so that more than one ID and Value can be sent at a time. It is important write this in the correct way:
```Arduino
Serial.println("0>1000<1>2000<2>105<);
```
##### You can also send data out on Serial, but right now it is just a string that is sent without any labelling or anything clever. 


