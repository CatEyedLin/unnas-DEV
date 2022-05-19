using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkingTheEasyWay : MonoBehaviour
{
    public Discord.Discord discord;
    public Discord.ActivityManager activityManager;
    public Discord.LobbyManager lobbyManager;
    //public Discord.NetworkManager networkManager;

    Discord.Lobby lobby;
    Discord.Activity activity;

    public Text chat;
    public Text info;

    bool joinedNetwork = false;
    // Start is called before the first frame update
    void Start()
    {
        discord = new Discord.Discord(974458702398636092, (System.UInt64)Discord.CreateFlags.Default);

        activityManager = discord.GetActivityManager();
        lobbyManager = discord.GetLobbyManager();
        //networkManager = discord.GetNetworkManager();
        // We can create a helper method to easily connect to the networking layer of the lobby


        // Let's say we got a game invite from Rich Presence
        activityManager.OnActivityJoin += ActivityManager_OnActivityJoin;
        //networkManager.OnMessage += NetworkingTheEasyWay_OnMessage;
        lobbyManager.OnNetworkMessage += LobbyManager_OnNetworkMessage; // NetworkingTheEasyWay_OnMessage;
    }

    private void LobbyManager_OnNetworkMessage(long lobbyId, long userId, byte channelId, byte[] data)
    {
        chat.text += "msg:" + System.Text.Encoding.UTF8.GetString(data) + "\n";
    }

    public void CreateLobby()
    {
        if (lobby.Id != 0)
        {
            lobbyManager.DisconnectNetwork(lobby.Id);
            lobbyManager.DisconnectLobby(lobby.Id, (result) => { Debug.Log(result); });
            activityManager.ClearActivity((result) => { Debug.Log(result); });
        }

        var txn = lobbyManager.GetLobbyCreateTransaction();
        txn.SetCapacity(8);
        txn.SetType(Discord.LobbyType.Public);

        lobbyManager.CreateLobby(txn, (Discord.Result result, ref Discord.Lobby tempLobby) =>
        {
            lobby = tempLobby;
            chat.text += "lobby created:" + result.ToString() + "\n";
            var newTxn = lobbyManager.GetLobbyUpdateTransaction(lobby.Id);
            newTxn.SetCapacity(5);
            lobbyManager.UpdateLobby(lobby.Id, newTxn, (result) =>
            {
                chat.text += "lobby updated:" + result.ToString() + "\n";
            });

            activity = new Discord.Activity
            {
                State = "Lobby owner",
                Details = "123 det",
                Secrets = {
                Match = "matchSecret",
                Join = lobbyManager.GetLobbyActivitySecret(lobby.Id)},

                Party = {
                Id = lobby.Id.ToString(),
                Size = {CurrentSize = 1, MaxSize = 50 } }

            };
            activityManager.UpdateActivity(activity, (result => { chat.text += "CreateLobby-UpdateActivity" + result.ToString() + "\n"; }));
        });
    }

    public void SendLobbyNetworkMessage()
    { // Say hello!
        for (int i = 0; i < lobbyManager.MemberCount(lobby.Id); i++)
        {
            var userId = lobbyManager.GetMemberUserId(lobby.Id, i);
            chat.text += "SendNetworkMessage to:" + userId.ToString() + " in lobby:" + lobby.Id.ToString() + "\n";
            lobbyManager.SendNetworkMessage(lobby.Id, userId, 0, System.Text.Encoding.UTF8.GetBytes("Hello!"));
        }
    }

private void ActivityManager_OnActivityJoin(string secret)
    {
        //var lobbyManager = discord.GetLobbyManager();
        chat.text += "OnActivityJoin:" + secret + "\n";
        lobbyManager.ConnectLobbyWithActivitySecret(secret, (Discord.Result result, ref Discord.Lobby tempLobby) =>
        {
            lobby = tempLobby;
            chat.text += "ConnectLobbyWithActivitySecret:" + result.ToString() + " id:" + lobby.Id.ToString() +  "\n";
            activity = new Discord.Activity
            {
                State = "Lobby Joiner",
                Details = "123 det",
                Secrets = {
                Match = "matchSecret",
                Join = lobbyManager.GetLobbyActivitySecret(lobby.Id)},

                Party = {
                Id = lobby.Id.ToString(),
                Size = {CurrentSize = 2, MaxSize = 10 } }

            };
            activityManager.UpdateActivity(activity, (result => { chat.text += "OnActivityJoin-UpdateActivity" + result.ToString() + "\n"; }));
            // Connect to networking
            InitNetworking(lobby.Id);

        });
    }

    private void NetworkingTheEasyWay_OnMessage(ulong peerId, byte channelId, byte[] data)
    {
        chat.text += "msg:" + data.ToString() + "\n";
    }

    void InitNetworking(System.Int64 lobbyId)
    {
        // First, connect to the lobby network layer
        var lobbyManager = discord.GetLobbyManager();
        lobbyManager.ConnectNetwork(lobbyId);

        // Next, deterministically open our channels
        // Reliable on 0, unreliable on 1
        lobbyManager.OpenNetworkChannel(lobbyId, 0, true);
        lobbyManager.OpenNetworkChannel(lobbyId, 1, false);

        chat.text += "ConnectNetwork:" + lobbyId.ToString() + "\n";
        joinedNetwork = true;

        // We're ready to go!
    }

    // Update is called once per frame
    void Update()
    {
        discord.RunCallbacks();

        if (lobby.Id != 0)
        {
            info.text = lobby.Id.ToString() + "\n";
            foreach (var user in lobbyManager.GetMemberUsers(lobby.Id))
            {
                info.text += user.Username + ":" + user.Id + "\n";
            }

            if (!joinedNetwork)
            {
                InitNetworking(lobby.Id);
                joinedNetwork = true;
            }
        }

    }
    private void LateUpdate()
    {		//discord.GetNetworkManager().Flush();
        lobbyManager.FlushNetwork();
    }

    public void quitSafely()
    {
        lobbyManager.DisconnectNetwork(lobby.Id);
        lobbyManager.DisconnectLobby(lobby.Id, (result) => { Debug.Log(result); });
        activityManager.ClearActivity((result) => { Debug.Log(result); });
        Debug.Log("quit");
    }
}
