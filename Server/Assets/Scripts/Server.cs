using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;
using System;
using System.Globalization;

public class Server : MonoBehaviour, INetEventListener
{
    #region NetVars
    private NetManager _netManager = null;
    private NetPacketProcessor _packetProcessor;

    public const int MaxPlayers = 6;
    private readonly NetDataWriter _cachedWriter = new NetDataWriter();

    private PlayerManager _playerManager;
    #endregion

    #region ServerVars
    [SerializeField] private string _key = "";
    private NetPeer _hostClient;
    private string hostAddress = "";
    private string hostEndpoint = "";

    #endregion

    #region NetEvents
    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (_netManager.ConnectedPeersCount >= MaxPlayers)
        {
            request.Reject();
            Debug.Log(_netManager.ConnectedPeersCount);
            return;
        }
        request.AcceptIfKey(_key);
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.Log("[S] NetworkError: " + socketError);
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        
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
                _packetProcessor.ReadAllPackets(reader, peer);
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
        Debug.Log($"[S] Player connected: {peer.EndPoint} with Id: {peer.Id}");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

        
        byte playerId = (byte)peer.Id;
        if (_playerManager.RemovePlayer(playerId))
        {
            var plp = new PlayerLeftPacket { Id = (byte)peer.Id };
            _netManager.SendToAll(WritePacket(plp), DeliveryMethod.ReliableOrdered);
        }
        

        if(peer == _hostClient)
        {
            // Possibly Later: send msg to all other players to connect to backup
            StopServer();
        }
    }
    #endregion

    #region Server Startup
    private void Awake()
    {
        LoadServerData();
        SetupServer();
    }

    private void LoadServerData()
    {
        string[] serverData = Environment.GetCommandLineArgs();
        foreach (var arg in serverData)
        {
            Debug.Log(arg);
        }
        hostAddress = serverData[1];
        hostEndpoint = hostAddress + ":" + serverData[2];
    }

    private void StartServer()
    {
        if (_netManager.IsRunning)
            return;
        _netManager.Start();

        string[] ep = hostEndpoint.Split(':');
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

        Debug.Log("Server started on port " + _netManager.LocalPort);
        _hostClient = _netManager.Connect(new IPEndPoint(ip, port), hostAddress);
    }

    private void SetupServer()
    {
        _packetProcessor = new NetPacketProcessor();
        _playerManager = new PlayerManager(this);
        _packetProcessor.SubscribeReusable<JoinPacket, NetPeer>(OnJoinReceived);
        
        _netManager = new NetManager(this)
        {
            IPv6Enabled = IPv6Mode.Disabled,
            UnconnectedMessagesEnabled = true,
            UpdateTime = 15,
            AutoRecycle = true
        };
        StartServer();
    }
    #endregion

    #region Packet Writing
    private NetDataWriter WriteSerializable<T>(PacketType type, T packet) where T : struct, INetSerializable
    {
        _cachedWriter.Reset();
        _cachedWriter.Put((byte)type);
        packet.Serialize(_cachedWriter);
        return _cachedWriter;
    }

    private NetDataWriter WritePacket<T>(T packet) where T : class, new()
    {
        _cachedWriter.Reset();
        _cachedWriter.Put((byte)PacketType.Serialized);
        _packetProcessor.Write(_cachedWriter, packet);
        return _cachedWriter;
    }
    #endregion

    #region Packet Handling
    private void OnJoinReceived(JoinPacket joinPacket, NetPeer peer)
    {
        Debug.Log("[S] Join packet received: " + joinPacket.UserName);
        var player = new ServerPlayer(_playerManager, joinPacket.UserName, peer);
        _playerManager.AddPlayer(player);

        //Send join accept
        var ja = new JoinAcceptPacket { Id = player.Id };
        peer.Send(WritePacket(ja), DeliveryMethod.ReliableOrdered);

        //Send to old players info about new player
        var pj = new PlayerJoinedPacket
        {
            UserName = joinPacket.UserName,
            Id = player.Id,
            NewPlayer = true,
        };
        _netManager.SendToAll(WritePacket(pj), DeliveryMethod.ReliableOrdered, peer);

        //Send to new player info about old players
        pj.NewPlayer = false;
        foreach (ServerPlayer otherPlayer in _playerManager)
        {
            if (otherPlayer == player)
                continue;
            pj.UserName = otherPlayer.Name;
            pj.Id = otherPlayer.Id;
            peer.Send(WritePacket(pj), DeliveryMethod.ReliableOrdered);
        }
    }
    
    #endregion

    private void FixedUpdate()
    {
        _netManager.PollEvents();
    }

    private void StopServer()
    {
        if(_netManager != null)
        {
            _netManager.Stop();
        }
        Application.Quit();
        
    }

    private void OnDestroy()
    {
        StopServer();
    }
}
