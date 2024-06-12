using System;
using UnityEngine;
using System.Collections.Generic;
using IMLD.MixedReality.Network;

public class Client : MonoBehaviour
{
    private Vector3 lastPosition;

    void Start()
    {
        // Initialize the last known position
        lastPosition = transform.position;
    }

    void Update()
    {
            // Send the updated X position to the server
            SendData();
    }

    private void SendData()
    {
        // Collect the current position and rotation of the client
        float positionX = transform.position.x + 0.2f;
        float positionY = transform.position.y;
        float positionZ = transform.position.z;
        float rotationX = transform.rotation.eulerAngles.x;
        float rotationY = transform.rotation.eulerAngles.y;
        float rotationZ = transform.rotation.eulerAngles.z;

        // Create a dictionary with the position and rotation information
        var data = new Dictionary<string, string> {
            { "x", positionX.ToString() },
            { "y", positionY.ToString() },
            { "z", positionZ.ToString() },
            { "rx", rotationX.ToString() },
            { "ry", rotationY.ToString() },
            { "rz", rotationZ.ToString() }
        };

        // Create a message with the dictionary
        var message = new MessageJsonDictionary(data);

        // Pack the message into a MessageContainer
        MessageContainer container = message.Pack();

        NetworkClient.Instance.SendToServer(container);
    }
}
