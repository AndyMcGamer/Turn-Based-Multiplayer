using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Player
{
    public readonly string Name;
    public readonly byte Id;
    public int Ping;

    private readonly PlayerManager _playerManager;
    protected Player(string name, PlayerManager playerManager, byte id)
    {
        Id = id;
        Name = name;
        _playerManager = playerManager;
    }
}
public class ClientPlayer : Player
{
    private readonly Client _client;

    public ClientPlayer(Client client, PlayerManager manager, string name, byte id) : base(name, manager, id)
    {
        _client = client;
    }
}

public class RemotePlayer : Player
{
    private readonly PlayerManager _playerManager;
    public RemotePlayer(PlayerManager manager, string name, PlayerJoinedPacket pjPacket) : base(name, manager, pjPacket.Id)
    {
        
    }
}

public class PlayerManager
{
    private readonly Dictionary<byte, Player> _players;
    private List<byte> _playerIds;
    private readonly Client _client;
    private ClientPlayer _clientPlayer;

    public ClientPlayer OurPlayer => _clientPlayer;
    public int Count { get => _players.Count; }

    public Player this[int index]
    {
        get => (index < _playerIds.Count) ? _players.TryGetValue(_playerIds[index], out var player) ? player : null : null; 
    }

    public PlayerManager(Client client)
    {
        _client = client;
        _players = new Dictionary<byte, Player>();
        _playerIds = new List<byte>();
    }

    public Player GetById(byte id)
    {
        return _players.TryGetValue(id, out var player) ? player : null;
    }

    public Player RemovePlayer(byte id)
    {
        if (_players.TryGetValue(id, out var player))
        {
            _players.Remove(id);
            _playerIds.Remove(id);
        }

        return player;
    }

    public void AddClientPlayer(ClientPlayer player)
    {
        _clientPlayer = player;
        _players.Add(player.Id, player);
        _playerIds.Add(player.Id);
    }

    public void AddPlayer(RemotePlayer player)
    {
        _players.Add(player.Id, player);
        _playerIds.Add(player.Id);
    }

    public IEnumerator<Player> GetEnumerator()
    {
        foreach (var item in _players.Values)
        {
            yield return item;
        }
    }


    public void Clear()
    {
        _players.Clear();
    }
}
