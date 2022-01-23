using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Globalization;

public class Client : MonoBehaviour, INetEventListener
{
    #region NetVars
    private NetManager _netManager;
    private NetDataWriter _writer;
    private NetPacketProcessor _packetProcessor;

    private NetPeer _server;

    #endregion

    #region ClientVars
    private string _userName;
    private int _ping;
    private PlayerManager _playerManager;
    private string ipAddress;
    #endregion

    #region NetEvents
    public void OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey(ipAddress);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.Log("[C] NetworkError: " + socketError);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        _ping = latency;
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        byte packetType = reader.GetByte();
        if (packetType >= NetworkGeneral.PacketTypesCount)
            return;
        PacketType pt = (PacketType)packetType;
        switch (pt)
        {
            case PacketType.Serialized:
                _packetProcessor.ReadAllPackets(reader);
                break;
            default:
                Debug.Log("Unhandled packet: " + pt);
                break;
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log("[C] Connected to server: " + peer.EndPoint);
        _server = peer;

        SendPacket(new JoinPacket { UserName = _userName }, DeliveryMethod.ReliableOrdered);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("[C] Disconnected from server: " + disconnectInfo.Reason);
        StopClient();
    }
    #endregion

    #region Client Connection
    public void StartClient()
    {
        _playerManager = new PlayerManager(this);
        _writer = new NetDataWriter();
        _packetProcessor = new NetPacketProcessor();
        _packetProcessor.SubscribeReusable<PlayerJoinedPacket>(OnPlayerJoined);
        _packetProcessor.SubscribeReusable<JoinAcceptPacket>(OnJoinAccept);
        _packetProcessor.SubscribeReusable<PlayerLeftPacket>(OnPlayerLeft);
        _netManager = new NetManager(this)
        {
            AutoRecycle = true,
            IPv6Enabled = IPv6Mode.Disabled,
            UnconnectedMessagesEnabled = true,
            UpdateTime = 15
        };
        _netManager.Start();
    }


    public void Connect(string endPoint, string key)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length != 2)
        {
            Debug.Log("Invalid endpoint format");
            return;
        }
        IPAddress ip;
        if (!IPAddress.TryParse(ep[0], out ip))
        {
            Debug.Log("Invalid ip-adress");
            return;
        }
        int port;
        if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
        {
            Debug.Log("Invalid port");
            return;
        }

        _netManager.Connect(new IPEndPoint(ip, port), key);
    }
    public void Connect(string endPoint)
    {
        string[] ep = endPoint.Split(':');
        if (ep.Length != 2)
        {
            Debug.Log("Invalid endpoint format");
            return;
        }
        IPAddress ip;
        if (!IPAddress.TryParse(ep[0], out ip))
        {
            Debug.Log("Invalid ip-adress");
            return;
        }
        int port;
        if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
        {
            Debug.Log("Invalid port");
            return;
        }

        _netManager.Connect(new IPEndPoint(ip, port), "");
        
    }
    #endregion

    private void Awake()
    {
        
        IPHostEntry Host = default(IPHostEntry);
        string Hostname = null;
        Hostname = Environment.MachineName;
        Host = Dns.GetHostEntry(Hostname);
        foreach (IPAddress IP in Host.AddressList)
        {
            if (IP.AddressFamily == AddressFamily.InterNetwork)
            {
                ipAddress = Convert.ToString(IP);
            }
        }
    }

    private void FixedUpdate()
    {
        if(_netManager != null)
        {
            _netManager.PollEvents();
        }
        
    }

    #region Packet Processing
    private void OnPlayerJoined(PlayerJoinedPacket packet)
    {
        Debug.Log($"[C] Player joined: {packet.UserName}");
        var remotePlayer = new RemotePlayer(_playerManager, packet.UserName, packet);
        _playerManager.AddPlayer(remotePlayer);
    }
    private void OnJoinAccept(JoinAcceptPacket packet)
    {
        Debug.Log("[C] Join accept. Received player id: " + packet.Id);
        var clientPlayer = new ClientPlayer(this, _playerManager, _userName, packet.Id);
        _playerManager.AddClientPlayer(clientPlayer);

        EventManager.InvokeLoadConfirmed();
    }
    private void OnPlayerLeft(PlayerLeftPacket packet)
    {
        var player = _playerManager.RemovePlayer(packet.Id);
        if (player != null)
            Debug.Log($"[C] Player Left: {player.Name}");
    }
    #endregion

    #region Send Functions
    public void SendPacketSerializable<T>(PacketType type, T packet, DeliveryMethod deliveryMethod) where T : INetSerializable
    {
        if (_server == null)
            return;
        _writer.Reset();
        _writer.Put((byte)type);
        packet.Serialize(_writer);
        _server.Send(_writer, deliveryMethod);
    }

    public void SendPacket<T>(T packet, DeliveryMethod deliveryMethod) where T : class, new()
    {
        if (_server == null)
            return;
        _writer.Reset();
        _writer.Put((byte)PacketType.Serialized);
        _packetProcessor.Write(_writer, packet);
        _server.Send(_writer, deliveryMethod);
    }
    #endregion

    private void StopClient()
    {
        _server = null;
        _playerManager = null;
        _writer = null;
        _packetProcessor = null;
        if(_netManager != null)
        {
            _netManager.Stop();
            _netManager = null;
        }
        
    }

    private void OnDestroy()
    {
        StopClient();
    }
}
