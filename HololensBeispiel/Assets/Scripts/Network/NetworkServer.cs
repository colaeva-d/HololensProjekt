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
    public class NetworkServer : MonoBehaviour
    {
        /// <summary>
        /// The (singleton) instance of this server class.
        /// </summary>
        public static NetworkServer Instance = null;

        /// <summary>
        /// The underlying TCP socket server used by this server class
        /// </summary>
        public ServerTcp Server;

        /// <summary>
        /// A value indicating whether the handling of messages is paused.
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// The port that the server is listening on.
        /// </summary>
        public int Port;

        /// <summary>
        /// A value indicating whether the server should start automatically when this component is initialized.
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
        /// The queue for client connect events. Connections are enqueued in the network thread and dequeued in the game loop.
        /// </summary>
        private readonly ConcurrentQueue<Socket> clientConnectedQueue = new ConcurrentQueue<Socket>();

        /// <summary>
        /// The queue for client disconnect events. Messages are enqueued in the network thread and dequeued in the game loop.
        /// </summary>
        private readonly ConcurrentQueue<Socket> clientDisconnectedQueue = new ConcurrentQueue<Socket>();

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
        /// Starts the server.
        /// </summary>
        /// <param name="port">The port of the server.</param>
        /// <param name="message">The message to announce the server with.</param>
        /// <returns><see langword="true"/> if the server started successfully, <see langword="false"/> otherwise.</returns>
        public bool StartServer(int port)
        {
            this.Port = port;

            // setup server
            Server = new ServerTcp(this.Port);
            Server.ClientConnected += OnClientConnected;
            Server.ClientDisconnected += OnClientDisconnected;
            Server.DataReceived += OnDataReceived;

            // start server
            bool success = Server.Start();
            if (success == false)
            {
                Debug.Log("Failed to start server!");
                return false;
            }

            Debug.Log("Started server!");
            return true;
        }

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendToAll(MessageContainer message)
        {
            byte[] envelope = message.Serialize();
            foreach (var client in Server.Clients)
            {
                if (client.Connected)
                {
                    Server.SendToClient(client, envelope);
                }
            }
        }

        /// <summary>
        /// Sends a message to a specific client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="client">The client to send the message to.</param>
        public void SendToClient(MessageContainer message, Socket client)
        {
            byte[] envelope = message.Serialize();

            Server.SendToClient(client, envelope);
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void StopServer()
        {
            Server?.Stop();
            Server?.Dispose();
            Server = null;
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
                StartServer(Port);
            }
        }

        private void Update()
        {
            MessageContainer message;
            while (!IsPaused && messageQueue.TryDequeue(out message))
            {
                HandleNetworkMessage(message);
            }

            Socket client;
            while (clientConnectedQueue.TryDequeue(out client))
            {
                ClientConnected?.Invoke(this, client);
            }

            while (clientConnectedQueue.TryDequeue(out client))
            {
                ClientDisconnected?.Invoke(this, client);
            }
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

        private void OnClientDisconnected(object sender, Socket socket)
        {
            Debug.Log("Client disconnected");
            clientDisconnectedQueue.Enqueue(socket);
        }

        private void OnClientConnected(object sender, Socket socket)
        {
            Debug.Log("Client connected: " + IPAddress.Parse(((IPEndPoint)socket.RemoteEndPoint).Address.ToString()));
            clientConnectedQueue.Enqueue(socket);
        }

        private void OnDestroy()
        {
            StopServer();
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