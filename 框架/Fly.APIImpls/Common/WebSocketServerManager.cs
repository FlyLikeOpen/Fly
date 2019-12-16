using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fly.Framework.Common;
using Fly.APIs.Common;

namespace Fly.APIImpls.Common
{
    public class WebSocketServerManager : IWebSocketServerManager
    {
        private WebSocketServer MMServer;
        public WebSocketServerManager()
        {
            MMServer = new WebSocketServer();
        }

        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            MMServer.Dispose();
            GC.SuppressFinalize(this);
        }

        ~WebSocketServerManager()
        {
            Close();
        }

        public virtual void Start(int serverPort)
        {
            MMServer.ServerPort = serverPort;
            Start();
        }
        public void AddNewConnectionEvent(Action<object, EventArgs> handler)
        {
            MMServer.NewConnection += new NewConnectionEventHandler(handler);
        }
        public void AddDataReceivedEvent(Action<object, string, EventArgs> handler)
        {
            MMServer.DataReceived += new DataReceivedEventHandler(handler);
        }
        public void AddDisconnectedEvent(Action<object, EventArgs> handler)
        {
            MMServer.Disconnected += new DisconnectedEventHandler(handler);
        }
        public virtual void Start()
        {
            MMServer.NewConnection += new NewConnectionEventHandler(WSServer_NewConnection);
            MMServer.Disconnected += new DisconnectedEventHandler(WSServer_Disconnected);
            MMServer.DataReceived += new DataReceivedEventHandler(WSServer_DataReceived);
            MMServer.StartServer();
        }
        public virtual void WSServer_Disconnected(object sender, EventArgs e)
        {
        }
        public virtual void WSServer_NewConnection(object sender, EventArgs e)
        {
        }
        public virtual void WSServer_DataReceived(object sender, string message, EventArgs e)
        {
        }
        public void Send(string routeKey, string message)
        {
            MMServer.Send(routeKey, message);
        }
        public void Send(string message)
        {
            MMServer.Send(message);
        }
    }
    public delegate void NewConnectionEventHandler(object sender, EventArgs e);
    public delegate void DataReceivedEventHandler(object sender, string message, EventArgs e);
    public delegate void DisconnectedEventHandler(object sender, EventArgs e);

    internal class WebSocketServer : IDisposable
    {
        private bool alreadyDisposed;
        private Socket listener;
        private int connectionsQueueLength;

        private List<SocketConnection> connectionSocketList = new List<SocketConnection>();
        private object _lock = new object();

        internal int ServerPort { get; set; }

        internal event NewConnectionEventHandler NewConnection;
        internal event DataReceivedEventHandler DataReceived;
        internal event DisconnectedEventHandler Disconnected;

        private void Initialize()
        {
            alreadyDisposed = false;
            connectionsQueueLength = 500;
        }

        internal WebSocketServer()
        {
            ServerPort = 17173;
            Initialize();
        }

        internal WebSocketServer(int serverPort)
        {
            ServerPort = serverPort;
            Initialize();
        }


        ~WebSocketServer()
        {
            Close();
        }


        public void Dispose()
        {
            Close();
        }

        private void Close()
        {
            if (!alreadyDisposed)
            {
                alreadyDisposed = true;
                if (listener != null) listener.Close();
                var len = connectionSocketList.Count;
                //for (int i = 0;i < connectionSocketList.Count; i++)
                //{
                //    connectionSocketList[connectionSocketList.Count - 1 - i].Close();
                //}
                lock (_lock)
                {
                    foreach (SocketConnection item in connectionSocketList)
                    {
                        item.ConnectionSocket.Close(); ;
                    }
                }
                connectionSocketList.Clear();
                GC.SuppressFinalize(this);
            }
        }

        internal static IPAddress GetLocalmachineIPAddress()
        {
            string strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);

            foreach (IPAddress ip in ipEntry.AddressList)
            {
                //IPV4
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }

            return ipEntry.AddressList[0];
        }

        internal void StartServer()
        {
            var char1 = Convert.ToChar(65533);

            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            listener.Bind(new IPEndPoint(GetLocalmachineIPAddress(), ServerPort));

            listener.Listen(connectionsQueueLength);

            while (true)
            {
                //listener.BeginAccept(new AsyncCallback(callback), listener);
                Socket sc = listener.Accept();

                if (sc != null)
                {
                    Thread.Sleep(200);
                    SocketConnection socketConn = new SocketConnection();
                    socketConn.ConnectionSocket = sc;
                    socketConn.NewConnection += new NewConnectionEventHandler(SocketConn_NewConnection);
                    socketConn.DataReceived += new DataReceivedEventHandler(SocketConn_DataReceived);
                    socketConn.Disconnected += new DisconnectedEventHandler(SocketConn_Disconnected);
                    socketConn.TrySocketAction(r => r.BeginReceive(socketConn.ReceivedDataBuffer,
                                                             0, socketConn.ReceivedDataBuffer.Length,
                                                             0, new AsyncCallback(socketConn.ManageHandshake),
                                                             socketConn.ConnectionSocket.Available));
                    lock (_lock)
                    {
                        connectionSocketList.Add(socketConn);
                    }

                }
            }
        }

        private void SocketConn_Disconnected(Object sender, EventArgs e)
        {
            SocketConnection sConn = sender as SocketConnection;
            if (sConn != null)
            {
                Disconnected?.Invoke(sender, EventArgs.Empty);
                sConn.ConnectionSocket.Close();
                lock (_lock)
                {
                    connectionSocketList.Remove(sConn);
                }
            }
        }

        private void SocketConn_DataReceived(Object sender, string message, EventArgs e)
        {
            DataReceived?.Invoke(sender, message, EventArgs.Empty);
        }

        private void SocketConn_NewConnection(Object sender, EventArgs e)
        {
            NewConnection?.Invoke(sender, EventArgs.Empty);
        }

        private void Send(string message, IEnumerable<SocketConnection> connectionSocketList)
        {
            lock (_lock)
            {
                foreach (SocketConnection item in connectionSocketList)
                {
                    item.Send(message);
                }
            }
        }
        internal void Send(string routeKey, string message)
        {
            this.Send(message, this.connectionSocketList.Where(r => r.RouteKey == routeKey));
        }
        internal void Send(string message)
        {
            this.Send(message, this.connectionSocketList);
        }
    }

    public class SocketConnection: ISocketConnection
    {
        internal string RouteKey { get; private set; }
        internal bool IsDataMasked { get; set; }

        internal Socket ConnectionSocket;

        private int maxBufferSize;
        private string handshake;
        private string new_Handshake;

        protected internal byte[] ReceivedDataBuffer;
        private byte[] firstByte;
        private byte[] lastByte;
        private byte[] serverKey1;
        private byte[] serverKey2;


        public event NewConnectionEventHandler NewConnection;
        public event DataReceivedEventHandler DataReceived;
        public event DisconnectedEventHandler Disconnected;

        internal SocketConnection()
        {
            maxBufferSize = 1024 * 100;
            ReceivedDataBuffer = new byte[maxBufferSize];
            firstByte = new byte[maxBufferSize];
            lastByte = new byte[maxBufferSize];
            firstByte[0] = 0x00;
            lastByte[0] = 0xFF;

            handshake = "HTTP/1.1 101 Web Socket Protocol Handshake" + Environment.NewLine;
            handshake += "Upgrade: WebSocket" + Environment.NewLine;
            handshake += "Connection: Upgrade" + Environment.NewLine;
            handshake += "Sec-WebSocket-Origin: " + "{0}" + Environment.NewLine;
            handshake += string.Format("Sec-WebSocket-Location: " + "ws://{0}" + Environment.NewLine, WebSocketServer.GetLocalmachineIPAddress());
            handshake += Environment.NewLine;

            new_Handshake = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine;
            new_Handshake += "Upgrade: WebSocket" + Environment.NewLine;
            new_Handshake += "Connection: Upgrade" + Environment.NewLine;
            new_Handshake += "Sec-WebSocket-Accept: {0}" + Environment.NewLine;
            new_Handshake += Environment.NewLine;
        }
        //public void Close()
        //{
        //    Disconnected?.Invoke(this, EventArgs.Empty);
        //}
        public string GetRouteKey()
        {
            return this.RouteKey;
        }
        public void Send(string message)
        {
            if (!ConnectionSocket.Connected) return;
            try
            {
                if (IsDataMasked)
                {
                    DataFrame dr = new DataFrame(message);
                    ConnectionSocket.Send(dr.GetBytes());
                }
                else
                {
                    ConnectionSocket.Send(firstByte);
                    ConnectionSocket.Send(Encoding.UTF8.GetBytes(message));
                    ConnectionSocket.Send(lastByte);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        private void Read(IAsyncResult status)
        {
            if (!ConnectionSocket.Connected) return;
            string messageReceived = string.Empty;
            DataFrame dr = new DataFrame(ReceivedDataBuffer);

            try
            {
                if (!this.IsDataMasked)
                {
                    // Web Socket protocol: messages are sent with 0x00 and 0xFF as padding bytes
                    System.Text.UTF8Encoding decoder = new System.Text.UTF8Encoding();
                    int startIndex = 0;
                    int endIndex = 0;

                    // Search for the start byte
                    while (ReceivedDataBuffer[startIndex] == firstByte[0]) startIndex++;
                    // Search for the end byte
                    endIndex = startIndex + 1;
                    while (ReceivedDataBuffer[endIndex] != lastByte[0] && endIndex != maxBufferSize - 1) endIndex++;
                    if (endIndex == maxBufferSize - 1) endIndex = maxBufferSize;

                    // Get the message
                    messageReceived = decoder.GetString(ReceivedDataBuffer, startIndex, endIndex - startIndex);
                }
                else
                {
                    messageReceived = dr.Text;
                }

                if ((messageReceived.Length == maxBufferSize && messageReceived[0] == Convert.ToChar(65533)) ||
                    messageReceived.Length == 0)
                {
                    Disconnected?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    DataReceived?.Invoke(this, messageReceived, EventArgs.Empty);
                    Array.Clear(ReceivedDataBuffer, 0, ReceivedDataBuffer.Length);
                    TrySocketAction(r => r.BeginReceive(ReceivedDataBuffer, 0, ReceivedDataBuffer.Length, 0, new AsyncCallback(Read), null));
                }
            }
            catch (Exception ex)
            {
                if (Disconnected != null)
                    Disconnected(this, EventArgs.Empty);
                Logger.Error(ex);
            }
        }

        private void BuildServerPartialKey(int keyNum, string clientKey)
        {
            string partialServerKey = "";
            byte[] currentKey;
            int spacesNum = 0;
            char[] keyChars = clientKey.ToCharArray();
            foreach (char currentChar in keyChars)
            {
                if (char.IsDigit(currentChar)) partialServerKey += currentChar;
                if (char.IsWhiteSpace(currentChar)) spacesNum++;
            }
            try
            {
                currentKey = BitConverter.GetBytes((int)(Int64.Parse(partialServerKey) / spacesNum));
                if (BitConverter.IsLittleEndian) Array.Reverse(currentKey);

                if (keyNum == 1) serverKey1 = currentKey;
                else serverKey2 = currentKey;
            }
            catch
            {
                if (serverKey1 != null) Array.Clear(serverKey1, 0, serverKey1.Length);
                if (serverKey2 != null) Array.Clear(serverKey2, 0, serverKey2.Length);
            }
        }

        private byte[] BuildServerFullKey(byte[] last8Bytes)
        {
            byte[] concatenatedKeys = new byte[16];
            Array.Copy(serverKey1, 0, concatenatedKeys, 0, 4);
            Array.Copy(serverKey2, 0, concatenatedKeys, 4, 4);
            Array.Copy(last8Bytes, 0, concatenatedKeys, 8, 8);

            // MD5 Hash
            var MD5Service = MD5.Create();
            return MD5Service.ComputeHash(concatenatedKeys);
        }
        private void SetBroadCastRouteKey(string clientHandshakeLine)
        {
            var routeIdPattern = @"RouteKey=([\S]+)";
            var boardCastRouteRegex = Regex.Match(clientHandshakeLine, routeIdPattern);
            Guid broadcastRouteId = Guid.Empty;
            if (boardCastRouteRegex.Success)
            {
                this.RouteKey = boardCastRouteRegex.Groups[1]?.Value;
            }
        }
        internal void ManageHandshake(IAsyncResult status)
        {
            TrySocketAction(r =>
            {
                string header = "Sec-WebSocket-Version:";
                int HandshakeLength = (int)status.AsyncState;
                byte[] last8Bytes = new byte[8];

                var decoder = new UTF8Encoding();
                String rawClientHandshake = decoder.GetString(ReceivedDataBuffer, 0, HandshakeLength);

                Array.Copy(ReceivedDataBuffer, HandshakeLength - 8, last8Bytes, 0, 8);

                //现在使用的是比较新的Websocket协议
                if (rawClientHandshake.IndexOf(header) != -1)
                {
                    this.IsDataMasked = true;
                    string[] rawClientHandshakeLines = rawClientHandshake.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                    string acceptKey = "";
                    foreach (string Line in rawClientHandshakeLines)
                    {
                        if (Line.Contains("Sec-WebSocket-Key:"))
                        {
                            acceptKey = ComputeWebSocketHandshakeSecurityHash09(Line.Substring(Line.IndexOf(":") + 2));
                        }
                        if (Line.Contains("RouteKey="))
                        {
                            SetBroadCastRouteKey(Line);
                        }
                    }

                    new_Handshake = string.Format(new_Handshake, acceptKey);
                    byte[] newHandshakeText = Encoding.UTF8.GetBytes(new_Handshake);
                    ConnectionSocket.BeginSend(newHandshakeText, 0, newHandshakeText.Length, 0, HandshakeFinished, null);
                    return;
                }

                string ClientHandshake = decoder.GetString(ReceivedDataBuffer, 0, HandshakeLength - 8);

                string[] ClientHandshakeLines = ClientHandshake.Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);


                // Welcome the new client
                foreach (string Line in ClientHandshakeLines)
                {
                    if (Line.Contains("RouteKey="))
                    {
                        SetBroadCastRouteKey(Line);
                    }
                    if (Line.Contains("Sec-WebSocket-Key1:"))
                        BuildServerPartialKey(1, Line.Substring(Line.IndexOf(":") + 2));
                    if (Line.Contains("Sec-WebSocket-Key2:"))
                        BuildServerPartialKey(2, Line.Substring(Line.IndexOf(":") + 2));
                    if (Line.Contains("Origin:"))
                        try
                        {
                            handshake = string.Format(handshake, Line.Substring(Line.IndexOf(":") + 2));
                        }
                        catch
                        {
                            handshake = string.Format(handshake, "null");
                        }
                }
                // Build the response for the client
                byte[] HandshakeText = Encoding.UTF8.GetBytes(handshake);
                byte[] serverHandshakeResponse = new byte[HandshakeText.Length + 16];
                byte[] serverKey = BuildServerFullKey(last8Bytes);
                Array.Copy(HandshakeText, serverHandshakeResponse, HandshakeText.Length);
                Array.Copy(serverKey, 0, serverHandshakeResponse, HandshakeText.Length, 16);

                ConnectionSocket.BeginSend(serverHandshakeResponse, 0, HandshakeText.Length + 16, 0, HandshakeFinished, null);
            });
        }

        private string ComputeWebSocketHandshakeSecurityHash09(String secWebSocketKey)
        {
            const string MagicKEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string secWebSocketAccept = String.Empty;
            // 1. Combine the request Sec-WebSocket-Key with magic key.
            string ret = secWebSocketKey + MagicKEY;
            // 2. Compute the SHA1 hash
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] sha1Hash = sha.ComputeHash(Encoding.UTF8.GetBytes(ret));
            // 3. Base64 encode the hash
            secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }

        private void HandshakeFinished(IAsyncResult status)
        {
            TrySocketAction(r =>
            {
                r.EndSend(status);
                r.BeginReceive(ReceivedDataBuffer, 0, ReceivedDataBuffer.Length, 0, new AsyncCallback(Read), null);
                NewConnection?.Invoke(this, EventArgs.Empty);
            });
        }
        internal void TrySocketAction(Action<Socket> action)
        {
            try
            {
                action(ConnectionSocket);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Disconnected?.Invoke(this, EventArgs.Empty);
                ConnectionSocket.Close();
            }
        }
    }
    #region 传输消息数据结构
    internal class DataFrame
    {
        private class DataFrameHeader
        {
            internal bool FIN { get; }
            internal bool RSV1 { get; }
            internal bool RSV2 { get; }
            internal bool RSV3 { get; }
            internal sbyte OpCode { get; }
            internal bool HasMask { get; }
            internal sbyte Length { get; }
            internal DataFrameHeader(byte[] buffer)
            {
                if (buffer.Length < 2)
                    throw new ApplicationException("无效的数据头.");

                //第一个字节
                FIN = (buffer[0] & 0x80) == 0x80;
                RSV1 = (buffer[0] & 0x40) == 0x40;
                RSV2 = (buffer[0] & 0x20) == 0x20;
                RSV3 = (buffer[0] & 0x10) == 0x10;
                OpCode = (sbyte)(buffer[0] & 0x0f);
                //第二个字节
                HasMask = (buffer[1] & 0x80) == 0x80;
                Length = (sbyte)(buffer[1] & 0x7f);

            }
            //发送封装数据
            internal DataFrameHeader(bool fin, bool rsv1, bool rsv2, bool rsv3, sbyte opcode, bool hasmask, int length)
            {
                FIN = fin;
                RSV1 = rsv1;
                RSV2 = rsv2;
                RSV3 = rsv3;
                OpCode = opcode;
                //第二个字节
                HasMask = hasmask;
                Length = (sbyte)length;
            }
            //返回帧头字节
            internal byte[] GetBytes()
            {
                byte[] buffer = new byte[2] { 0, 0 };

                if (FIN) buffer[0] ^= 0x80;
                if (RSV1) buffer[0] ^= 0x40;
                if (RSV2) buffer[0] ^= 0x20;
                if (RSV3) buffer[0] ^= 0x10;

                buffer[0] ^= (byte)OpCode;

                if (HasMask) buffer[1] ^= 0x80;

                buffer[1] ^= (byte)Length;

                return buffer;
            }
        }
        private DataFrameHeader header;
        private byte[] extend = new byte[0];
        private byte[] mask = new byte[0];
        private byte[] content = new byte[0];

        internal DataFrame(byte[] buffer)
        {
            //帧头
            header = new DataFrameHeader(buffer);

            //扩展长度
            if (header.Length == 126)
            {
                extend = new byte[2];
                Buffer.BlockCopy(buffer, 2, extend, 0, 2);
            }
            else if (header.Length == 127)
            {
                extend = new byte[8];
                Buffer.BlockCopy(buffer, 2, extend, 0, 8);
            }

            //是否有掩码
            if (header.HasMask)
            {
                mask = new byte[4];
                Buffer.BlockCopy(buffer, extend.Length + 2, mask, 0, 4);
            }

            //消息体
            if (extend.Length == 0)
            {
                content = new byte[header.Length];
                Buffer.BlockCopy(buffer, extend.Length + mask.Length + 2, content, 0, content.Length);
            }
            else if (extend.Length == 2)
            {
                int contentLength = (int)extend[0] * 256 + (int)extend[1];
                content = new byte[contentLength];
                Buffer.BlockCopy(buffer, extend.Length + mask.Length + 2, content, 0, contentLength > 1024 * 100 ? 1024 * 100 : contentLength);
            }
            else
            {
                long len = 0;
                int n = 1;
                for (int i = 7; i >= 0; i--)
                {
                    len += (int)extend[i] * n;
                    n *= 256;
                }
                content = new byte[len];
                Buffer.BlockCopy(buffer, extend.Length + mask.Length + 2, content, 0, content.Length);
            }

            if (header.HasMask) content = Mask(content, mask);

        }

        internal DataFrame(string content)
        {
            this.content = Encoding.UTF8.GetBytes(content);
            int length = this.content.Length;

            if (length < 126)
            {
                extend = new byte[0];
                header = new DataFrameHeader(true, false, false, false, 1, false, length);
            }
            else if (length < 65536)
            {
                extend = new byte[2];
                header = new DataFrameHeader(true, false, false, false, 1, false, 126);
                extend[0] = (byte)(length / 256);
                extend[1] = (byte)(length % 256);
            }
            else
            {
                extend = new byte[8];
                header = new DataFrameHeader(true, false, false, false, 1, false, 127);

                int left = length;
                int unit = 256;

                for (int i = 7; i > 1; i--)
                {
                    extend[i] = (byte)(left % unit);
                    left = left / unit;

                    if (left == 0)
                        break;
                }
            }
        }

        internal byte[] GetBytes()
        {
            byte[] buffer = new byte[2 + extend.Length + mask.Length + content.Length];
            Buffer.BlockCopy(header.GetBytes(), 0, buffer, 0, 2);
            Buffer.BlockCopy(extend, 0, buffer, 2, extend.Length);
            Buffer.BlockCopy(mask, 0, buffer, 2 + extend.Length, mask.Length);
            Buffer.BlockCopy(content, 0, buffer, 2 + extend.Length + mask.Length, content.Length);
            return buffer;
        }

        internal string Text
        {
            get
            {
                if (header.OpCode != 1)
                    return string.Empty;

                return Encoding.UTF8.GetString(content);
            }
        }

        private byte[] Mask(byte[] data, byte[] mask)
        {
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = (byte)(data[i] ^ mask[i % 4]);
            }

            return data;
        }

    }
    #endregion
}