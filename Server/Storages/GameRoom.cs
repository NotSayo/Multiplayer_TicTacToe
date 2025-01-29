namespace Server.Storages;

public class GameRoom
{
    public string RoomCode { get; set; }
    public string RoomState { get; set; }
    public int PlayersCount => Players.Count;
    public List<string> Players { get; set; }
    public Dictionary<string, int> PlayerScores { get; set; }

    public GameRoom()
    {

    }

    public void AddPlayer(string userName)
    {
        if(PlayersCount >= 2)
            return;
        Players.Add(userName);
        UpdateRoomState();
    }

    public void RemovePlayer(string userName)
    {
        Players.Remove(userName);
        PlayerScores.Remove(userName);
        UpdateRoomState();
    }

    public void UpdateRoomState()
    {
        if (PlayersCount == 2)
            RoomState = "Ready";
        else if(PlayersCount < 2)
            RoomState = "Waiting";
        else
            RoomState = "Full";
    }
}