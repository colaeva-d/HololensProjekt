using UnityEngine;
using IMLD.MixedReality.Network;

public class NetworkDataHandler : MonoBehaviour
{
    private int data;

    void Start()
    {
        // Register the message handler for JSON_INT type messages
        NetworkServer.Instance.RegisterMessageHandler(MessageContainer.MessageType.JSON_INT, HandleData);
    }

    private void HandleData(MessageContainer container)
    {
        // Unpack the message and retrieve the int data
        var message = MessageJsonInt.Unpack(container);
        data = message?.Data ?? 0;
    }

    void Update()
    {
        // Log the received data
        if(data != 0){
            Debug.Log(data);
        }
    }
}
