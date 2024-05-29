namespace IMLD.MixedReality.Network
{
    public class MessageBinaryUInt
    {
        /// <summary>
        /// The type of the message. Add any new message types to the MessageContainer.MessageType enum.
        /// </summary>
        public static MessageContainer.MessageType Type = MessageContainer.MessageType.BINARY_UINT;

        /// <summary>
        /// The payload, a 32 bit unsigned integer
        /// </summary>
        public uint Data;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">The payload, a 32 bit unsigned integer</param>
        public MessageBinaryUInt(uint data)
        {
            Data = data;
        }

        /// <summary>
        /// Packs this message into a MessageContainer with a message header.
        /// </summary>
        /// <returns>The new message container</returns>
        public MessageContainer Pack()
        {
            // convert the uint into a byte array
            byte[] Payload = System.BitConverter.GetBytes(Data);
            return new MessageContainer(Type, Payload);
        }

        /// <summary>
        /// A static method that unpacks the message from a message container.
        /// </summary>
        /// <param name="container">The container to unpack</param>
        /// <returns>A new MessageBinaryUInt</returns>
        public static MessageBinaryUInt Unpack(MessageContainer container)
        {
            // check the container type
            if (container.Type != Type)
            {
                return null;
            }

            // convert the byte array of the payload to an uint
            uint Result = System.BitConverter.ToUInt32(container.Payload, 0);
            return new MessageBinaryUInt(Result);
        }
    }
}