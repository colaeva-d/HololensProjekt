using UnityEngine;
using System.Collections.Generic;
using IMLD.MixedReality.Network;

public class Message : MonoBehaviour
{

    private Vector3 lastPosition;

    void Start()
    {
        // Senden Sie die Nachricht, wenn der Client initialisiert wird
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
        // Erfassen Sie die aktuellen Positionsinformationen des Clients
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // Erstellen Sie eine Nachricht mit den Positionsinformationen
        Dictionary<string, Dictionary<string, float>> positionData = new Dictionary<string, Dictionary<string, float>>();

        Dictionary<string, float> data = new Dictionary<string, float>
        {
            { "PositionX", position.x },
            { "PositionY", position.y },
            { "PositionZ", position.z },
            { "OrientationX", rotation.x },
            { "OrientationY", rotation.y },
            { "OrientationZ", rotation.z },
            { "OrientationW", rotation.w }
        };

        positionData[gameObject.name] = data;

        MessagePositionDictionary message = new MessagePositionDictionary(positionData);
        MessageContainer container = message.Pack();

        // Senden Sie die Nachricht an den Server
        NetworkServer.Instance.SendToAll(container);
    }
}
