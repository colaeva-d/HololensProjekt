using System;
using System.Net;
using System.Text;

namespace IMLD.MixedReality.Network
{
    public class MessageContainer
    {
        /// <summary>
        /// Enum of all possible message types. Register new messages here. Depending on 
        /// </summary>
        public enum MessageType
        {
            BINARY_UINT,        // a binary message containing a single UInt32
            JSON_DICTIONARY,     // a json message containing a dictionary of key-value pairs (string, float)
            JSON_INT,
        }

        /// <summary>
        /// The sender ip/port of this message
        /// </summary>
        public IPEndPoint Sender;

        /// <summary>
        /// The type of the message. Add any new message types to MessageType enum.
        /// </summary>
        public MessageType Type;

        /// <summary>
        /// The payload of this message as a byte array. The actual data format is specified by the message type.
        /// </summary>
        public byte[] Payload;

        /// <summary>
        /// Constructor for string payloads
        /// </summary>
        /// <param name="type">The message type</param>
        /// <param name="payload">The payload as a string</param>
        public MessageContainer(MessageType type, string payload)
        {
            Type = type;
            Payload = Encoding.UTF8.GetBytes(payload);
        }

        /// <summary>
        /// Constructor for byte array payloads
        /// </summary>
        /// <param name="type">The message type</param>
        /// <param name="payload">The payload as a byte array</param>
        public MessageContainer(MessageType type, byte[] payload)
        {
            Type = type;
            Payload = payload;
        }

        /// <summary>
        /// A static method that deserializes a network message into a message container.
        /// </summary>
        /// <param name="sender">The ip and port of the sender</param>
        /// <param name="payload">The payload of the message</param>
        /// <param name="messageType">The unparsed message type</param>
        /// <returns>A message container with the correct type and payload added</returns>
        public static MessageContainer Deserialize(IPEndPoint sender, byte[] payload, byte messageType)
        {
            MessageType Type = (MessageType)messageType;
            var Message = new MessageContainer(Type, payload);
            Message.Sender = sender;
            return Message;
        }

        /// <summary>
        /// Serializes this message container into a byte array.
        /// </summary>
        /// <returns>A byte array that contains the header and the payload. The total length is payload.Length + 5.</returns>
        public byte[] Serialize()
        {
            byte[] Envelope = new byte[Payload.Length + 5];
            Array.Copy(BitConverter.GetBytes(Payload.Length), Envelope, 4);
            Envelope[4] = (byte)Type;
            Array.Copy(Payload, 0, Envelope, 5, Payload.Length);
            return Envelope;
        }

    }
}