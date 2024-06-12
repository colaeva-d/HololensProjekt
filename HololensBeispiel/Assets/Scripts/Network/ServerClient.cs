using UnityEngine;
using IMLD.MixedReality.Network;
using System.Collections.Generic;

public class RegisterMessageHandler : MonoBehaviour
{
    private void Start()
    {
        // Register the message handler for JSON_DICTIONARY message type
        NetworkServer.Instance.RegisterMessageHandler(MessageContainer.MessageType.JSON_DICTIONARY, HandleJsonDictionaryMessage);
    }

    private void HandleJsonDictionaryMessage(MessageContainer message)
    {
        // Unpack the message
        MessageJsonDictionary jsonMessage = MessageJsonDictionary.Unpack(message);

        if (jsonMessage != null)
        {
            // Extract the x, y, z positions and rotation from the dictionary
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
                    // Set the position and rotation of the GameObject's transform
                    transform.position = new Vector3(xPosition, yPosition, zPosition);
                    transform.rotation = Quaternion.Euler(xRotation, yRotation, zRotation);

                    Debug.Log("Received position - X: " + xPosition + ", Y: " + yPosition + ", Z: " + zPosition);
                    Debug.Log("Received rotation - RX: " + xRotation + ", RY: " + yRotation + ", RZ: " + zRotation);
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
}
