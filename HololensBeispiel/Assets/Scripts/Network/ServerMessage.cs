using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IMLD.MixedReality.Network;

public class Server : MonoBehaviour
{
    private List<string> logMessages = new List<string>();

    void Start()
    {
        NetworkServer.Instance.RegisterMessageHandler(MessageContainer.MessageType.JSON_POSITION_ARRAY, HandlePositionDictionaryMessage);
    }

    private void HandlePositionDictionaryMessage(MessageContainer container)
    {
        // Verarbeite die empfangenen Positionsdaten hier
        MessagePositionDictionary message = MessagePositionDictionary.Unpack(container);
        if (message != null)
        {
            foreach (var item in message.Data)
            {
                string positionInfo = "Received position for object " + item.Key + ": ";
                foreach (var entry in item.Value)
                {
                    positionInfo += entry.Key + ": " + entry.Value + ", ";
                }
                logMessages.Add(positionInfo);
            }
        }
    }

    void Update()
    {
        foreach (var message in logMessages)
        {
            Debug.Log(message);
        }
        logMessages.Clear(); // Löschen Sie die Liste nach dem Anzeigen der Nachrichten
    }
}
