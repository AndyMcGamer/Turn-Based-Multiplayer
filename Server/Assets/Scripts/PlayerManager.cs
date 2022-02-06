using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;

public class ServerPlayer
{
    private readonly PlayerManager _playerManager;
    public readonly NetPeer AssociatedPeer;

    public readonly string Name;
    public readonly byte Id;
    public int Ping;

    public ServerPlayer(PlayerManager playerManager, string name, NetPeer peer)
    {
        Id = (byte)peer.Id;
        Name = name;
        _playerManager = playerManager;
        peer.Tag = this;
        AssociatedPeer = peer;
    }
}

public class PlayerManager
{
    #region Variables
    private readonly Server _server;
    private readonly ServerPlayer[] _players;
    private int _playerCount;

    public int Count { get => _playerCount; }
    #endregion

    public PlayerManager(Server server)
    {
        _server = server;
        _players = new ServerPlayer[Server.MaxPlayers];

    }

    public void AddPlayer(ServerPlayer player)
    {
        for (int i = 0; i < _playerCount; i++)
        {
            if (_players[i].Id == player.Id)
            {
                _players[i] = player;
                return;
            }
        }

        _players[_playerCount] = player;
        _playerCount++;
    }

    public bool RemovePlayer(byte playerId)
    {
        for (int i = 0; i < _playerCount; i++)
        {
            if (_players[i].Id == playerId)
            {
                _playerCount--;
                _players[i] = _players[_playerCount];
                _players[_playerCount] = null;
                return true;
            }
        }
        return false;
    }

    public IEnumerator<ServerPlayer> GetEnumerator()
    {
        int i = 0;
        while (i < _playerCount)
        {
            yield return _players[i];
            i++;
        }
    }
}
