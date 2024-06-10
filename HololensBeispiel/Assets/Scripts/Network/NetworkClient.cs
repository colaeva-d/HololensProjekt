// ------------------------------------------------------------------------------------
// <copyright file="SimpleNetworkTransport.cs" company="Technische Universität Dresden">
//      Copyright (c) Technische Universität Dresden.
//      Licensed under the MIT License.
// </copyright>
// <author>
//      Wolfgang Büschel
// </author>
// ------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Connectivity;
#endif

namespace IMLD.MixedReality.Network
{
    /// <summary>
    /// A basic network server for Unity.
    /// </summary>
    public class NetworkClient : MonoBehaviour
    {
        /// <summary>
        /// The (singleton) instance of this client class.
        /// </summary>
        public static NetworkClient Instance = null;

        /// <summary>
        /// Gets a value indicating whether the client is connected to a server.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (Client != null && Client.IsOpen)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// The underlying TCP socket server used by this server class
        /// </summary>
        public ClientTcp Client;

        /// <summary>
        /// A value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// The port of the server to conect to.
        /// </summary>
        public int Port;

        /// <summary>
        /// The IP of the server to connect to.
        /// </summary>
        public string IP;

        /// <summary>
        /// A value indicating whether the client should start automatically when this component is initialized.
        /// </summary>
        public bool StartAutomatically;

        /// <summary>
        /// Event that is triggered when a client connects to this server.
        /// </summary>
        public event EventHandler<Socket> ClientConnected;

        /// <summary>
        /// Event that is triggered when a client disconnects from this server.
        /// </summary>
        public event EventHandler<Socket> ClientDisconnected;

        /// <summary>
        /// The length of the message size header.
        /// </summary>
        private const int MESSAGE_SIZE_LENGTH = 4;

        /// <summary>
        /// The length of the message type header.
        /// </summary>
        private const int MESSAGE_TYPE_LENGTH = 1;

        /// <summary>
        /// The total length of the message header.
        /// </summary>
        private const int MESSAGE_HEADER_LENGTH = MESSAGE_SIZE_LENGTH + MESSAGE_TYPE_LENGTH;

        /// <summary>
        /// The queue for network messages. Messages are enqueued in the network thread and dequeued in the game loop.
        /// </summary>
        private readonly ConcurrentQueue<MessageContainer> messageQueue = new ConcurrentQueue<MessageContainer>();

        /// <summary>
        /// A dictionary of the network buffers for the different network connections. 
        /// </summary>
        private readonly Dictionary<IPEndPoint, EndPointState> endPointStates = new Dictionary<IPEndPoint, EndPointState>();

        /// <summary>
        /// A dictionary of the event handlers for the different message types.
        /// </summary>
        //private readonly Dictionary<MessageContainer.MessageType, EventHandler<MessageContainer>> MessageHandlers = new Dictionary<MessageContainer.MessageType, EventHandler<MessageContainer>>();
        private readonly Dictionary<MessageContainer.MessageType, Action<MessageContainer>> MessageHandlers = new Dictionary<MessageContainer.MessageType, Action<MessageContainer>>();

        /// <summary>
        /// A boolean indicating whether the client is currently trying to connect to a server.
        /// </summary>
        private bool isConnecting = false;

        /// <summary>
        /// Pauses the handling of network messages.
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
        }

        /// <summary>
        /// Restarts the handling of network messages.
        /// </summary>
        public void Unpause()
        {
            IsPaused = false;
        }

        /// <summary>
        /// Connects to a server.
        /// Note: If the method return true, this doesn't mean the connection itself was successful, only that the attempt has been started.
        /// Subscribe to the OnConnectedToServer event to be notified if the connection to the server has been successfully established.
        /// </summary>
        /// <param name="ip">The IP address of the server.</param>
        /// <param name="port">The port of the server.</param>
        /// <returns><see langword="true"/> if the connection attempt was started successful, <see langword="false"/> otherwise.</returns>
        public bool ConnectToServer(string ip, int port)
        {
            if (isConnecting)
            {
                return false;
            }

            if (IsConnected)
            {
                Client.Close();
            }

            Client = new ClientTcp(ip, port);
            Debug.Log("Connecting to server at " + ip);
            Client.Connected += OnConnectedToServer;
            Client.DataReceived += OnDataReceived;
            isConnecting = true;
            return Client.Open();
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToServer(MessageContainer message)
        {
            Client.Send(message.Serialize());
        }

        /// <summary>
        /// Registers a new message handler for a given type of message.
        /// </summary>
        /// <param name="messageType">The message type</param>
        /// <param name="messageHandler">The delegate to register</param>
        /// <returns></returns>
        public bool RegisterMessageHandler(MessageContainer.MessageType messageType, Action<MessageContainer> messageHandler)
        {
            try
            {
                MessageHandlers[messageType] = messageHandler;
            }
            catch (Exception exp)
            {
                Debug.LogError("Registering message handler failed! Original error message: " + exp.Message);
                return false;
            }
            return true;
        }

        private void Awake()
        {
            // Singleton pattern implementation
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }

            Instance = this;
        }

        private void Start()
        {
            if (StartAutomatically == true)
            {
                ConnectToServer(IP, Port);
            }
        }

        private void Update()
        {
            MessageContainer message;
            while (!IsPaused && messageQueue.TryDequeue(out message))
            {
                HandleNetworkMessage(message);
            }
        }

        private void OnConnectedToServer(object sender, EventArgs e)
        {
            Debug.Log("Connected to server!");
            isConnecting = false;
        }

        private void HandleNetworkMessage(MessageContainer message)
        {
            if (MessageHandlers != null)
            {
                Action<MessageContainer> eventHandler;
                if (MessageHandlers.TryGetValue(message.Type, out eventHandler) && eventHandler != null)
                {
                    eventHandler(message);
                }
                else
                {
                    Debug.Log("Unknown message: " + message.Type.ToString() + " with content: " + message.Payload);
                }
            }
        }

        private void OnDataReceived(object sender, IPEndPoint remoteEndPoint, byte[] data)
        {
            int currentByte = 0;
            int dataLength = data.Length;
            EndPointState state;
            try
            {
                if (endPointStates.ContainsKey(remoteEndPoint))
                {
                    state = endPointStates[remoteEndPoint];
                }
                else
                {
                    state = new EndPointState();
                    endPointStates[remoteEndPoint] = state;
                }

                state.CurrentSender = remoteEndPoint;
                while (currentByte < dataLength)
                {
                    int messageSize;

                    // currently still reading a (large) message?
                    if (state.IsMessageIncomplete)
                    {
                        // 1. get size of current message
                        messageSize = state.CurrentMessageBuffer.Length;

                        // 2. read data
                        // decide how much to read: not more than remaining message size, not more than remaining data size
                        int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, data.Length - currentByte);
                        Array.Copy(data, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                        currentByte += lengthToRead; // increase "current byte pointer"
                        state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

                        // 3. decide how to proceed
                        if (state.CurrentMessageBytesRead == messageSize)
                        {
                            // Message is completed
                            state.IsMessageIncomplete = false;
                            messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
                        }
                        else
                        {
                            // We did not read the whole message yet
                            state.IsMessageIncomplete = true;
                        }
                    }
                    else if (state.IsHeaderIncomplete)
                    {
                        // currently still reading a header
                        // decide how much to read: not more than remaining message size, not more than remaining header size
                        int lengthToRead = Math.Min(MESSAGE_HEADER_LENGTH - state.CurrentHeaderBytesRead, dataLength - currentByte);
                        Array.Copy(data, currentByte, state.CurrentHeaderBuffer, state.CurrentHeaderBytesRead, lengthToRead); // read header data into header buffer
                        currentByte += lengthToRead;
                        state.CurrentHeaderBytesRead += lengthToRead;
                        if (state.CurrentHeaderBytesRead == MESSAGE_HEADER_LENGTH)
                        {
                            // Message header is completed
                            // read size of message from header buffer
                            messageSize = BitConverter.ToInt32(state.CurrentHeaderBuffer, 0);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;

                            // read type of next message
                            state.CurrentMessageType = state.CurrentHeaderBuffer[MESSAGE_SIZE_LENGTH];
                            state.IsHeaderIncomplete = false;
                            state.IsMessageIncomplete = true;
                        }
                        else
                        {
                            // We did not read the whole header yet
                            state.IsHeaderIncomplete = true;
                        }
                    }
                    else
                    {
                        // start reading a new message
                        // 1. check if remaining data sufficient to read message header
                        if (currentByte < dataLength - MESSAGE_HEADER_LENGTH)
                        {
                            // 2. read size of next message
                            messageSize = BitConverter.ToInt32(data, currentByte);
                            state.CurrentMessageBuffer = new byte[messageSize];
                            state.CurrentMessageBytesRead = 0;
                            currentByte += MESSAGE_SIZE_LENGTH;

                            // 3. read type of next message
                            state.CurrentMessageType = data[currentByte];
                            currentByte += MESSAGE_TYPE_LENGTH;

                            // 4. read data
                            // decide how much to read: not more than remaining message size, not more than remaining data size
                            int lengthToRead = Math.Min(messageSize - state.CurrentMessageBytesRead, dataLength - currentByte);
                            Array.Copy(data, currentByte, state.CurrentMessageBuffer, state.CurrentMessageBytesRead, lengthToRead); // copy data from data to message buffer
                            currentByte += lengthToRead; // increase "current byte pointer"
                            state.CurrentMessageBytesRead += lengthToRead; // increase amount of message bytes read

                            // 4. decide how to proceed
                            if (state.CurrentMessageBytesRead == messageSize)
                            {
                                // Message is completed
                                state.IsMessageIncomplete = false;
                                messageQueue.Enqueue(MessageContainer.Deserialize(state.CurrentSender, state.CurrentMessageBuffer, state.CurrentMessageType));
                            }
                            else
                            {
                                // We did not read the whole message yet
                                state.IsMessageIncomplete = true;
                            }
                        }
                        else
                        {
                            // not enough data to read complete header for new message
                            state.CurrentHeaderBuffer = new byte[MESSAGE_HEADER_LENGTH]; // create new header data buffer to store a partial message header
                            int lengthToRead = dataLength - currentByte;
                            Array.Copy(data, currentByte, state.CurrentHeaderBuffer, 0, lengthToRead); // read header data into header buffer
                            currentByte += lengthToRead;
                            state.CurrentHeaderBytesRead = lengthToRead;
                            state.IsHeaderIncomplete = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error while parsing network data: " + e.Message);
            }
        }


        private void OnDestroy()
        {
            Client?.Close();
        }

        /// <summary>
        /// Helper class used to store the current state of a network endpoint.
        /// </summary>
        private class EndPointState
        {
            public byte[] CurrentMessageBuffer;
            public int CurrentMessageBytesRead;
            public byte CurrentMessageType;
            public bool IsMessageIncomplete = false;
            public IPEndPoint CurrentSender;
            public bool IsHeaderIncomplete = false;
            public byte[] CurrentHeaderBuffer;
            public int CurrentHeaderBytesRead;
        }
    }


}