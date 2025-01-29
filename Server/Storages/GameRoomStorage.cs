using System.Collections.Concurrent;

namespace Server.Storages;

public class GameRoomStorage
{
    public static ConcurrentDictionary<string, GameRoom> Rooms = new ConcurrentDictionary<string, GameRoom>();
}