using Shared;

namespace Server.Storages;

public class GameRoom
{
    public string RoomCode { get; set; }
    public RoomState State { get; set; }
    public int PlayersCount => Players.Count;
    public List<User> Players { get; set; }
    public Dictionary<string, PlayerAssign> PlayerAssigns { get; set; }
    public List<FieldEntry> Board { get; set; }
    public string CurrentPlayer { get; set; }

    public GameRoom()
    {
        Players = new List<User>();
        PlayerAssigns = new Dictionary<string, PlayerAssign>();
        Board = new List<FieldEntry>();
        for (int i = 0; i < 9; i++)
            Board.Add(FieldEntry.Empty);
    }

    public void AddPlayer(string userName, string contextId)
    {
        var oldUsername = userName;
        if(PlayersCount >= 2)
            return;
        if (Players.FirstOrDefault(p => p.Username == userName) is not null)
        {
            AddPlayer(userName + new Random().Next(1,9), contextId);
            return;
        }
        Players.Add(new User
        {
            ConnectionId = contextId,
            Username = userName
        });
        UsernameStorage.Usernames.Remove(oldUsername);
        UsernameStorage.Usernames.Add(userName);
        UpdateRoomState();
    }

    public void RemovePlayer(string userName, string contextId)
    {
        try
        {
            Players.Remove(Players.First(s => s.Username == userName));
        } catch(Exception ) {/**/}
        UpdateRoomState();
    }

    public void UpdateRoomState()
    {
        if (PlayersCount == 2)
            State = RoomState.Ready;
        else if(PlayersCount < 2)
            State = RoomState.Waiting;
        else
            State = RoomState.Overloaded;
    }

    public bool Start()
    {
        if (State != RoomState.Ready && State != RoomState.Finished)
            return false;
        State = RoomState.Playing;
        AssignPlayers();
        ResetBoard();
        return true;
    }

    public MoveResult MakeMove(string user, int position)
    {
        if(State != RoomState.Playing)
            return new MoveResult() {Result = ResultState.InvalidMove};
        if (user != CurrentPlayer)
            return new MoveResult() {Result = ResultState.InvalidMove};
        if (Board[position] != FieldEntry.Empty)
            return new MoveResult() {Result = ResultState.InvalidMove};
        Board[position] = Enum.Parse<FieldEntry>(PlayerAssigns[Players.First(p => p.Username == user).Username].ToString());
        CurrentPlayer = Players.First(p => p.Username != user).Username;
        return CheckWin();
    }

    private MoveResult CheckWin()
    {
        foreach (var assign in new [] {"X", "O"})
        {
            // CheckHorizontal
            if((Board[0].ToString() == assign && Board[1].ToString() == assign && Board[2].ToString() == assign)
               || (Board[3].ToString() == assign && Board[4].ToString() == assign && Board[5].ToString() == assign
               || (Board[6].ToString() == assign && Board[7].ToString() == assign && Board[8].ToString() == assign)))
            {
                EndGame();
                return new MoveResult()
                {
                    Result = ResultState.Win,
                    Board = Board,
                    CurrentPlayer = CurrentPlayer,
                    Winner = PlayerAssigns.FirstOrDefault(s => s.Value == Enum.Parse<PlayerAssign>(assign)).Key
                };
            }

            // CheckVertical

            else if((Board[0].ToString() == assign && Board[3].ToString() == assign && Board[6].ToString() == assign)
               || (Board[1].ToString() == assign && Board[4].ToString() == assign && Board[7].ToString() == assign
               || (Board[2].ToString() == assign && Board[5].ToString() == assign && Board[8].ToString() == assign)))
            {
                EndGame();
                return new MoveResult()
                {
                    Result = ResultState.Win,
                    Board = Board,
                    CurrentPlayer = CurrentPlayer,
                    Winner = PlayerAssigns.FirstOrDefault(s => s.Value == Enum.Parse<PlayerAssign>(assign)).Key
                };
            }

            // CheckDiagonal

            else if((Board[0].ToString() == assign && Board[4].ToString() == assign && Board[8].ToString() == assign)
               || (Board[2].ToString() == assign && Board[4].ToString() == assign && Board[6].ToString() == assign))
            {
                EndGame();
                return new MoveResult()
                {
                    Result = ResultState.Win,
                    Board = Board,
                    CurrentPlayer = CurrentPlayer,
                    Winner = PlayerAssigns.FirstOrDefault(s => s.Value == Enum.Parse<PlayerAssign>(assign)).Key
                };
            }

        }

        if (Board.Count > 9)
            Board = Board.Take(9).ToList();
        if (Board.All(s => s != FieldEntry.Empty))
        {
            EndGame();
            return new MoveResult()
            {
                Result = ResultState.Draw,
                Board = Board,
                CurrentPlayer = CurrentPlayer,
            };
        }
        return new MoveResult()
        {
            Result = ResultState.Continue,
            Board = Board,
            CurrentPlayer = CurrentPlayer,
        };
    }

    private void EndGame()
    {
        State = RoomState.Finished;
    }

    private void ResetBoard()
    {
        Board = new List<FieldEntry>();
        for (int i = 0; i < 9; i++)
            Board.Add(FieldEntry.Empty);
    }

    private void AssignPlayers()
    {
        var xPlayer = Players[new Random().Next(0, 2)];
        PlayerAssigns[xPlayer.Username] = PlayerAssign.X;
        PlayerAssigns[Players.First(s => s != xPlayer).Username] = PlayerAssign.O;
        CurrentPlayer = xPlayer.Username;
    }
}