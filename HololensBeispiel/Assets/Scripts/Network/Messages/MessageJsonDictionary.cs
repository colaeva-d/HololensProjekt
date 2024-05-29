using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageJsonDictionary
    {
        /// <summary>
        /// The type of the message. Add any new message types to the MessageContainer.MessageType enum.
        /// </summary>
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.JSON_DICTIONARY;

        /// <summary>
        /// The payload, a dictionary containing key-value pairs (string, string).
        /// </summary>
        public Dictionary<string, string> Data;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">The payload, a dictionary containing key-value pairs (string, string)</param>
        public MessageJsonDictionary(Dictionary<string, string> data)
        {
            Data = data;
        }

        /// <summary>
        /// Packs this message into a MessageContainer with a message header.
        /// </summary>
        /// <returns>The new message container</returns>
        public MessageContainer Pack()
        {
            // convert the payload data into a json string
            string Payload = JsonConvert.SerializeObject(Data);
            return new MessageContainer(Type, Payload);
        }

        /// <summary>
        /// A static method that unpacks the message from a message container.
        /// </summary>
        /// <param name="container">The container to unpack</param>
        /// <returns>A new MessageJsonDictionary</returns>
        public static MessageJsonDictionary Unpack(MessageContainer container)
        {
            // check the container type
            if (container.Type != Type)
            {
                return null;
            }

            // convert the json string in the payload to a dictionary.
            var Result = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(container.Payload));
            return new MessageJsonDictionary(Result);
        }
    }
}