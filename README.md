
# JOIN RISING DEVELOPERS COMMUNITY 🚀

[![Discord](https://img.shields.io/discord/843927072789317376?color=blue&label=Join%20Community&logo=discord&style=for-the-badge)](https://discord.gg/cwdh7kb3Z)


# BinollaApiDotNet(Unofficial)

 BinollaApiDotNet is a C# API designed to facilitate communication and trading on binolla.com via WebSocket connections. This library enables developers to directly open orders and execute trades programmatically, providing a seamless interface for interacting with the platform.



## 🌟Features

- **🔗 Connect to Binolla**: Connect to the Binolla platform using a WebSocket connection.
- **🕵️ Stealth Login**: Your activities appear just like they would in your logged-in browser session, ensuring safety and reducing detection.
- **💰 Get Balance**: Retrieve the current balance of the connected account.
- **🔄 Change Balance Type**: Change the account balance type between demo and real.
- **📊 Get Payment**: Retrieve the current open assets and their payouts.
- **🛒 Buy**: Open a new order on the platform.
- **🏆 Check Win**: Check the win amount of a previously opened order.
- 

## 📦 Installation
To install BinollaApiDotNet, simply clone the repository and add the project to your solution. The library is built using .NET Standard 8.0, so it can be used in any .NET project that supports this version.



## Getting Started Guide

This api doesnot support Login Via Email And Password.

You need to Login your Account in Browser and Get Network Session String

### Example Auth Token
```json
 42["authorization",{"isDemo":true,"token":"h4ZUUsfGs1yOQ9Rhowyb3AZHeNKKSkRrwWfV6wLN"}]
```

After Getting Auth Token, Clone This Repository and Add BinollaApiDotNet Project to your Solution.

```bash
git clone https://github.com/muneebanwar33/BinollaApiDotNet.git
cd BinollaApiDotNet
git checkout -b main
```

### Example Login
```csharp
string ssid = "42["authorization",{"isDemo":true,"token":"h4ZUUsfGs1yOQ9Rhowyb3AZHeNKKSkRrwWfV6wLN"}]";

var binollaApi = new BinollaApi(ssid);
bool okLogin = binollaApi.Connect();
if (okLogin)
{
    Console.WriteLine("Login Success");
}
else
{
    Console.WriteLine("Login Failed");
}

//To Get Balance
var balance = binollaApi.GetBalance();
Console.WriteLine("Balance: " + balance);

//to change Account Balance Type (Demo or Real)
var newBl = binollaApi.ChangeBalanceType("REAL");
Console.WriteLine("New Balance: " + newBl);

//Please Note when u Login Default Account Type will be Selected To demo

//To Get Current Open Assets And Thier payouts
Dictionary<string, double> payment = binollaApi.GetPayment();
foreach (var item in payment)
{
    Console.WriteLine(item.Key + " : " + item.Value);
}

//Buy and Check Win Saample

var response= api.Buy("EURUSD", "call", 100, 60);
if (response.IsSuccessFull)
{
    Console.WriteLine("Buy Success");
    var id = response.Message;
    var win = api.CheckWin(id); //this will pool Untill Trade is Finished
     if(win.IsSuccess){
        Console.WriteLine("Win Amount: " + win.WinAmount);
     }
     else{
        Console.WriteLine("Check Win Failed Error : " + win.Message);
     }
    
    
}
else
{
    Console.WriteLine("Buy Failed");
}

```



# Author Info   

```json
{
  "Name": "MUNEEB ANWAR",
  "Email": "email@muneebanwar.com"
}
```
For Any Query or Help Contact Me Discord: muneebanwar
```
