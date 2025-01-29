using Microsoft.AspNetCore.SignalR.Client;

namespace Client;

public class HubController
{
    public HubConnection Connection { get; set; }

    public HubController()
    {
        Connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:54321/gameHub")
            .Build();
        _ = StartConnection();
    }

    private async Task StartConnection() => await Connection.StartAsync();
}