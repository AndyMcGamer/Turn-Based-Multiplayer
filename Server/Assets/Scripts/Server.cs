using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;
using System.Net.Sockets;

public class Server : MonoBehaviour, INetEventListener
{
    #region NetVars
    private NetManager _netManager;
    private NetPacketProcessor _packetProcessor;

    public const int MaxPlayers = 4;
    private readonly NetDataWriter _cachedWriter = new NetDataWriter();

    private PlayerManager _playerManager;
    #endregion

    #region ServerVars
    [SerializeField] private string _key = "";
    #endregion

    #region NetEvents
    public void OnConnectionRequest(ConnectionRequest request)
    {
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
        Debug.Log("[S] Player connected: " + peer.EndPoint);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log("[S] Player disconnected: " + disconnectInfo.Reason);

        if (peer.Tag != null)
        {
            byte playerId = (byte)peer.Id;
            if (_playerManager.RemovePlayer(playerId))
            {
                var plp = new PlayerLeftPacket { Id = (byte)peer.Id };
                _netManager.SendToAll(WritePacket(plp), DeliveryMethod.ReliableOrdered);
            }
        }
    }
    #endregion

    #region Server Startup
    private void Awake()
    {
        SetupServer();
    }

    public void StartServer()
    {
        if (_netManager.IsRunning)
            return;
        _netManager.Start();
        Debug.Log(_netManager.LocalPort);
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
        
    }

    private void OnDestroy()
    {
        StopServer();
    }
}
