using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using System;

public static class NetworkGeneral
{
    public static readonly int PacketTypesCount = Enum.GetValues(typeof(PacketType)).Length;
}

public enum PacketType : byte
{
    Serialized
}

#region Auto-Serialized Packets
public class JoinPacket
{
    public string UserName { get; set; }
}

public class JoinAcceptPacket
{
    public byte Id { get; set; }
}

public class PlayerJoinedPacket
{
    public string UserName { get; set; }
    public byte Id { get; set; }
    public bool NewPlayer { get; set; }
}

public class PlayerLeftPacket
{
    public byte Id { get; set; }
}
#endregion

#region Manually-Serialized Packets
#endregion
