namespace BinollaApiDotNet;



public class Program
{
    static void Main(string[] args)
    {
        string ssid = "ssid";
        var api = new BinollaApiClient(ssid);
        api.Connect();
        double balance = api.GetBalance();
        var response= api.Buy("EURUSD", "call", 100, 60);
        var id = api.CheckWin("A");

        Console.WriteLine($"Balance: {balance}");
    }
}