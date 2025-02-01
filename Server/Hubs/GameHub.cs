using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Server.Storages;
using Shared;

namespace Server.Hubs;

public class GameHub : Hub
{
    public ConcurrentDictionary<string, GameRoom> Rooms = GameRoomStorage.Rooms;

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        string username = "user";
        if (UsernameStorage.Usernames.FirstOrDefault(p => p == username) is not null)
        {
            username += new Random().Next(1, 9);
            UsernameStorage.Usernames.Add(username);
            await Clients.Caller.SendAsync("NameSet", username);
            return;
        }
        UsernameStorage.Usernames.Add(username);
        await Clients.Caller.SendAsync("NameSet", username);
        await Clients.Caller.SendAsync("GetStatus", "Server is running");
    }

    public async Task GetName()
    {
        string username = "user";
        if (UsernameStorage.Usernames.FirstOrDefault(p => p == username) is not null)
        {
            username += new Random().Next(1, 9);
            UsernameStorage.Usernames.Add(username);
            await Clients.Caller.SendAsync("NameSet", username);
            return;
        }
        UsernameStorage.Usernames.Add(username);
        await Clients.Caller.SendAsync("NameSet", username);
    }

    public async Task Status()
    {
        await Clients.Caller.SendAsync("GetStatus", "Server is running");
    }

    public async Task SetName(string newName)
    {
        if (UsernameStorage.Usernames.FirstOrDefault(p => p == newName) is not null)
        {
            newName += new Random().Next(1, 9);
            UsernameStorage.Usernames.Add(newName);
            await Clients.Caller.SendAsync("NameSet", newName);
            return;
        }
        UsernameStorage.Usernames.Add(newName);
        await Clients.Caller.SendAsync("NameSet", newName);
    }

    public async Task CreateRoom(string username)
    {
        var roomCode = Guid.NewGuid().ToString().Substring(0, 6);
        var room = new GameRoom()
        {
            RoomCode = roomCode,
            State = RoomState.Waiting,
        };
        Rooms[roomCode] = room;
        Rooms[roomCode].AddPlayer(username, Context.ConnectionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        await Clients.Caller.SendAsync("RoomCreated", roomCode);
        await Clients.Groups(roomCode).SendAsync("GetRoomInfo", room.State, room.Players, room.PlayerAssigns, room.Board, room.CurrentPlayer);
    }

    public async Task UpdateGroup(string roomCode ,string username)
    {
        if (UsernameStorage.Usernames.FirstOrDefault(s => s == username) is null)
            return;
        GameRoom room;
        try
        {
            room = Rooms[roomCode];
        }
        catch (Exception e)
        {
            Console.WriteLine("Error finding room: " + e.Message);
            return;
        }
        if(room.Players.FirstOrDefault(s => s.Username == username) is null)
            return;
        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);

    }

    public async Task JoinRoom(string roomCode, string username)
    {
        Console.WriteLine("RoomCode:" +roomCode);
        foreach (var room in Rooms)
        {
            Console.WriteLine(room.Key + "-" + room.Value.RoomCode);
        }
        if (Rooms.ContainsKey(roomCode))
        {
            var room = Rooms[roomCode];
            room.AddPlayer(username, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
            await Clients.Caller.SendAsync("JoinedRoom", roomCode);
            await Clients.Groups(roomCode).SendAsync("GetRoomInfo", room.State, room.Players, room.PlayerAssigns, room.Board, room.CurrentPlayer);
        }
        else
        {
            await Clients.Caller.SendAsync("Error", "Room not found");
        }
    }

    public async Task GetRoomInfo(string roomCode, string username)
    {
        if (!Rooms.TryGetValue(roomCode, out var room))
        {
            await Clients.Caller.SendAsync("ValidEntry", false);
            return;
        }
        if(room.Players.FirstOrDefault(p => p.Username == username) == null)
        {
            await Clients.Caller.SendAsync("ValidEntry", false);
            return;
        }
        await Clients.Caller.SendAsync("GetRoomInfo", room.State, room.Players, room.PlayerAssigns, room.Board, room.CurrentPlayer);
    }

    public async Task StartGame(string roomCode)
    {
        if (!Rooms.TryGetValue(roomCode, out var room))
        {
            await Clients.Caller.SendAsync("ValidEntry", false);
            return;
        }
        var result = room.Start();
        if(result)
            await Clients.Groups(roomCode).SendAsync("GetRoomInfo", room.State, room.Players, room.PlayerAssigns, room.Board, room.CurrentPlayer);

    }

    public async Task SendMove(string roomCode, string username, int field)
    {
        if (!Rooms.TryGetValue(roomCode, out var room))
        {
            await Clients.Caller.SendAsync("ValidEntry", false);
            return;
        }
        var result = Rooms[roomCode].MakeMove(username, field);

        if (result.Result == ResultState.InvalidMove)
            return;
        if(result.Result == ResultState.Win)
            await Clients.Group(roomCode).SendAsync("GameFinished", result.Winner);

        if(result.Result == ResultState.Draw)
            await Clients.Group(roomCode).SendAsync("GameFinished", "Draw");

        await Clients.Groups(roomCode).SendAsync("GetRoomInfo", room.State, room.Players, room.PlayerAssigns, room.Board, room.CurrentPlayer);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // TODO fix this
        //Rooms.Select(s => s.Value).ToList().ForEach(g => g.RemovePlayer(userName, Context.ConnectionId));
        await base.OnDisconnectedAsync(exception);
    }
}