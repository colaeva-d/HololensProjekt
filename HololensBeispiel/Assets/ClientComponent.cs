using System;
using UnityEngine;
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
        // Check if the position has changed
        if (transform.position != lastPosition)
        {
            // Update the last known position
            lastPosition = transform.position;

            // Send the updated X position to the server
            SendPositionData();
        }
    }

    private void SendPositionData()
    {
        // Collect the current X position of the client
        int positionX = Mathf.RoundToInt(transform.position.x);

        // Create a message with the X position information
        MessageJsonInt message = new MessageJsonInt(positionX);

        // Pack the message into a MessageContainer
        MessageContainer container = message.Pack();

        // Send the message to the server
        NetworkServer.Instance.SendToAll(container);
    }
}
