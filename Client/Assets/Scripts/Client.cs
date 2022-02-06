using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

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
        EventManager.InvokeGoToMain();
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

    public void ConnectToHost(string endPoint)
    {
        StartClient();
        Connect(endPoint);
    }

    private void Connect(string endPoint, string key)
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
    private void Connect(string endPoint)
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

    public void Disconnect()
    {
        StopClient();
    }


    public void HostGame()
    {
        //Start Client
        StartClient();
        //Process.Start Server Application
        EventManager.InvokeChangeLoad();
        //StartServerApp();
        try
        {
            StartCoroutine(StartServerCoroutine());
        }
        catch
        {
            Debug.Log("Missing Crucial Files");
            EventManager.InvokeGoToMain();
            StopClient();
        }
    }

    private IEnumerator StartServerCoroutine()
    {
        yield return null;
        System.Diagnostics.ProcessStartInfo serverStartInfo = new System.Diagnostics.ProcessStartInfo();
        serverStartInfo.FileName = CustomSearcher.FindFile("TurnBasedServer.exe");
        //Send info about client (IP Address and Port)
        string serverInfoArgs = "";
        serverInfoArgs += ipAddress + " ";
        serverInfoArgs += _netManager.LocalPort + " ";
        serverStartInfo.Arguments = serverInfoArgs;
        System.Diagnostics.Process.Start(serverStartInfo);
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
        _userName = "hello" + UnityEngine.Random.Range(0,100);
    }

    private void FixedUpdate()
    {
        if(_netManager != null)
        {
            _netManager.PollEvents();
        }
        
    }

    #region ClientVar Get/Set
    public void SetUsername(string name)
    {
        _userName = name;
    }

    public string GetUsername()
    {
        return _userName;
    }

    public PlayerManager GetPlayerManager()
    {
        return _playerManager;
    }
    #endregion

    #region Packet Processing
    private void OnPlayerJoined(PlayerJoinedPacket packet)
    {
        Debug.Log($"[C] Player joined: {packet.UserName}");
        var remotePlayer = new RemotePlayer(_playerManager, packet.UserName, packet);
        _playerManager.AddPlayer(remotePlayer);
        EventManager.InvokePlayerListUpdate();
    }
    private void OnJoinAccept(JoinAcceptPacket packet)
    {
        Debug.Log("[C] Join accept. Received player id: " + packet.Id);
        var clientPlayer = new ClientPlayer(this, _playerManager, _userName, packet.Id);
        _playerManager.AddClientPlayer(clientPlayer);

        EventManager.InvokeLoadConfirmed();
        EventManager.InvokePlayerListUpdate();
    }
    private void OnPlayerLeft(PlayerLeftPacket packet)
    {
        var player = _playerManager.RemovePlayer(packet.Id);
        EventManager.InvokePlayerListUpdate();
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
public class CustomSearcher
{
    public static List<string> GetDirectories(string path, string searchPattern = "*",
        SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (searchOption == SearchOption.TopDirectoryOnly)
            return Directory.GetDirectories(path, searchPattern).ToList();

        var directories = new List<string>(GetDirectories(path, searchPattern));

        for (var i = 0; i < directories.Count; i++)
            directories.AddRange(GetDirectories(directories[i], searchPattern));

        return directories;
    }

    private static List<string> GetDirectories(string path, string searchPattern)
    {
        try
        {
            return Directory.EnumerateDirectories(path, searchPattern).ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }
    public static string FindFile(string fileName)
    {
        string[] drives = Environment.GetLogicalDrives();
        List<string[]> findedFiles = new List<string[]>();
        foreach (string dr in drives)
        {
            Debug.Log($"Start looking in {dr}");
            System.IO.DriveInfo di = new System.IO.DriveInfo(dr);
            if (!di.IsReady)
            {
                Debug.Log($"The drive {di.Name} could not be read");
                continue;
            }
            DirectoryInfo rootDir = di.RootDirectory;
            var findedFiletmp = Directory.GetFiles(rootDir.Name, fileName, SearchOption.TopDirectoryOnly);
            if (findedFiletmp.Length > 0)
            {
                findedFiles.Add(findedFiletmp);
                Debug.Log("Finded file.Continue search?(Y/N)");
                
                
                
                break;
                
            }
            var subDirectories = Directory.GetDirectories(rootDir.Name);
            bool breaked = false;
            foreach (var subDirectory in subDirectories)
            {
                try
                {
                    var findedFiletmp1 = Directory.GetFiles(subDirectory, fileName, SearchOption.AllDirectories);
                    if (findedFiletmp1.Length > 0)
                    {
                        findedFiles.Add(findedFiletmp1);
                        Debug.Log("Finded file.Continue search?(Y/N)");
                        
                        breaked = true;
                        break;
                        
                    }
                }
                catch (Exception exc)
                {
                    Debug.Log(exc.Message);
                }
            }
            Debug.Log($"Finished looking in {dr}");
            if (breaked)
                break;
        }
        return findedFiles[0][0];
    }
}
