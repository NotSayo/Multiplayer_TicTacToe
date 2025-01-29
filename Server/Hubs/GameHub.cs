using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Server.Storages;

namespace Server.Hubs;

public class GameHub : Hub
{
    public ConcurrentDictionary<string, GameRoom> Rooms = GameRoomStorage.Rooms;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        UsernameStorage.Usernames[Context.ConnectionId] = "User";
    }

    public async Task Status()
    {
        await Clients.Caller.SendAsync("GetStatus", "Server is running", UsernameStorage.Usernames[Context.ConnectionId]);
    }

    public async Task SetName(string newName)
    {
        UsernameStorage.Usernames[Context.ConnectionId] = newName;
        await Clients.Caller.SendAsync("NameSet", newName);
    }

    public async Task CreateRoom()
    {
        var roomCode = Guid.NewGuid().ToString().Substring(0, 6);
        Rooms[roomCode] = new GameRoom()
        {
            RoomCode = roomCode,
            RoomState = "Waiting",
            Players = new List<string>(),
            PlayerScores = new Dictionary<string, int>()
        };
        Rooms[roomCode].AddPlayer(UsernameStorage.Usernames[Context.ConnectionId]);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.Caller.SendAsync("RoomCreated", roomCode);
    }

    public async Task JoinRoom(string roomCode)
    {
        Console.WriteLine("RoomCode:" +roomCode);
        foreach (var room in Rooms)
        {
            Console.WriteLine(room.Key + "-" + room.Value.RoomCode);
        }
        if (Rooms.ContainsKey(roomCode))
        {
            Rooms[roomCode].AddPlayer(UsernameStorage.Usernames[Context.ConnectionId]);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Caller.SendAsync("JoinedRoom", roomCode);
            await Clients.Group(roomCode).SendAsync("ReceivePlayers", Rooms[roomCode].Players);
            await Clients.Group(roomCode).SendAsync("PlayerJoined", Context.ConnectionId);
        }
        else
        {
            Console.WriteLine("test error");
            await Clients.Caller.SendAsync("Error", "Room not found");
        }
    }

    public async Task GetRoomInfo(string roomCode)
    {
        if (!Rooms.TryGetValue(roomCode, out var room))
        {
            await Clients.Caller.SendAsync("ValidEntry", false);
            return;
        }
        var userName = UsernameStorage.Usernames[Context.ConnectionId];
        if(room.Players.FirstOrDefault(p => p == UsernameStorage.Usernames[Context.ConnectionId]) == null)
        {
            await Clients.Caller.SendAsync("ValidEntry", false);
            return;
        }
        await Clients.Caller.SendAsync("GetRoomInfo", room.RoomState, room.Players, room.PlayerScores);
    }

    public async Task SendMove(string roomCode, int x, int y)
    {
        await Clients.Group(roomCode).SendAsync("ReceiveMove", Context.ConnectionId, x, y);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        // foreach (var room in Rooms)
        // {
        //     if (room.Value.Contains(Context.ConnectionId))
        //     {
        //         room.Value.Remove(Context.ConnectionId);
        //         await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Key);
        //         if (room.Value.Count == 0)
        //         {
        //             Rooms.TryRemove(room.Key, out _);
        //         }
        //         else
        //         {
        //             await Clients.Group(room.Key).SendAsync("PlayerLeft", Context.ConnectionId);
        //         }
        //         break;
        //     }
        // }
        // await base.OnDisconnectedAsync(exception);
    }
}