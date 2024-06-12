using Newtonsoft.Json;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageJsonPositionArray
    {
        /// <summary>
        /// The type of the message. Add any new message types to the MessageContainer.MessageType enum.
        /// </summary>
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.JSON_POSITION_ARRAY;

        /// <summary>
        /// The payload data, an array of positions (x, y, z).
        /// </summary>
        public float[] Data;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">The payload data, an array of positions (x, y, z)</param>
        public MessageJsonPositionArray(float[] data)
        {
            Data = data;
        }

        /// <summary>
        /// Packs this message into a MessageContainer with a message header.
        /// </summary>
        /// <returns>The new message container</returns>
        public MessageContainer Pack()
        {
            // Convert the payload data into a JSON string
            string Payload = JsonConvert.SerializeObject(Data);
            return new MessageContainer(Type, Payload);
        }

        /// <summary>
        /// A static method that unpacks the message from a message container.
        /// </summary>
        /// <param name="container">The container to unpack</param>
        /// <returns>A new MessageJsonPositionArray</returns>
        public static MessageJsonPositionArray Unpack(MessageContainer container)
        {
            // Check the container type
            if (container.Type != Type)
            {
                return null;
            }

            // Convert the JSON string in the payload to an array of floats
            var Result = JsonConvert.DeserializeObject<float[]>(Encoding.UTF8.GetString(container.Payload));
            return new MessageJsonPositionArray(Result);
        }
    }
}
