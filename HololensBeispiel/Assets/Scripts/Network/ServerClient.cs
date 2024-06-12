using UnityEngine;
using IMLD.MixedReality.Network;
using System.Collections.Generic;

public class RegisterMessageHandler : MonoBehaviour
{

    void Start()
    {
        NetworkServer.Instance.RegisterMessageHandler(MessageContainer.MessageType.JSON_DICTIONARY, HandleJsonDictionaryMessage);
    }

    void Update()
    {
        //SendData();
    }

    private void HandleJsonDictionaryMessage(MessageContainer message)
    {
        MessageJsonDictionary jsonMessage = MessageJsonDictionary.Unpack(message);

        if (jsonMessage != null)
        {
            if (jsonMessage.Data.TryGetValue("x", out string xPositionStr) &&
                jsonMessage.Data.TryGetValue("y", out string yPositionStr) &&
                jsonMessage.Data.TryGetValue("z", out string zPositionStr) &&
                jsonMessage.Data.TryGetValue("rx", out string xRotationStr) &&
                jsonMessage.Data.TryGetValue("ry", out string yRotationStr) &&
                jsonMessage.Data.TryGetValue("rz", out string zRotationStr))
            {
                if (float.TryParse(xPositionStr, out float xPosition) &&
                    float.TryParse(yPositionStr, out float yPosition) &&
                    float.TryParse(zPositionStr, out float zPosition) &&
                    float.TryParse(xRotationStr, out float xRotation) &&
                    float.TryParse(yRotationStr, out float yRotation) &&
                    float.TryParse(zRotationStr, out float zRotation))
                {
                    transform.position = new Vector3(xPosition, yPosition, zPosition);
                    transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);
                }
                else
                {
                    Debug.LogError("Failed to parse positions or rotations.");
                }
            }
            else
            {
                Debug.LogError("One or more positions or rotations not found in the message data.");
            }
        }
        else
        {
            Debug.LogError("Failed to unpack the JSON_DICTIONARY message.");
        }
    }

    private void SendData()
    {
        float positionX = transform.position.x - 0.2f;
        float positionY = transform.position.y;
        float positionZ = transform.position.z;
        float rotationX = transform.rotation.eulerAngles.x;
        float rotationY = transform.rotation.eulerAngles.y;
        float rotationZ = transform.rotation.eulerAngles.z;

        var data = new Dictionary<string, string> {
            { "x", positionX.ToString() },
            { "y", positionY.ToString() },
            { "z", positionZ.ToString() },
            { "rx", rotationX.ToString() },
            { "ry", rotationY.ToString() },
            { "rz", rotationZ.ToString() }
        };

        var message = new MessageJsonDictionary(data);
        MessageContainer container = message.Pack();
        NetworkServer.Instance.SendToAll(container);
    }
}
