using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Discord;

/*
    Grab that Client ID from earlier
    Discord.CreateFlags.Default will require Discord to be running for the game to work
    If Discord is not running, it will:
    1. Close your game
    2. Open Discord
    3. Attempt to re-open your game
    Step 3 will fail when running directly from the Unity editor
    Therefore, always keep Discord running during tests, or use Discord.CreateFlags.NoRequireDiscord
*/


public class DiscordController : MonoBehaviour
{
	public Discord.Discord discord;
	public Discord.Activity activity;
	public Discord.LobbyManager LM;
	public Discord.LobbyManager my_lobby;
	public Text chat;

	Discord.ActivityManager activityManager;
	Discord.Lobby lobby;

	Discord.LobbyManager.MemberConnectHandler MemberConnectHandler;

	// Start is called before the first frame update
	void Start()
	{
		discord = new Discord.Discord(974458702398636092, (System.UInt64)Discord.CreateFlags.Default);
		LM = discord.GetLobbyManager();

        discord.GetUserManager().OnCurrentUserUpdate += DiscordController_OnCurrentUserUpdate;
        discord.GetNetworkManager().OnMessage += DiscordController_OnMessage;


		activityManager = discord.GetActivityManager();
		activity = new Discord.Activity
		{
			State = "Still Testing2",
			Details = "Bigger Test",
		};

		Debug.Log(discord.GetNetworkManager().GetPeerId());

		activityManager.OnActivityJoinRequest += (ref Discord.User user) => // move to Start()
		{
			Debug.Log("OnJoinRequest " + user.Username.ToString() + ":" + user.Id.ToString());
			chat.text += "\n" + "OnJoinRequest " + user.Username.ToString() + ":" + user.Id.ToString();

			activityManager.AcceptInvite(user.Id, (Discord.Result result) =>
			{
				Debug.Log("connected to lobby: " + lobby.Id.ToString());
				chat.text += "\n" + "connected to lobby: " + lobby.Id.ToString();
			});
		};
		//Debug.Log("amount: " + LM.LobbyCount().ToString());
		activityManager.OnActivityJoin += secret =>
		{
			Debug.Log("onJoin" + secret);
			Debug.Log("amount: " + LM.LobbyCount().ToString());
			LM.ConnectLobbyWithActivitySecret(secret, (Discord.Result result, ref Discord.Lobby lobbyTEMP) =>
			{
				switch (result)
				{
					case Result.Ok:
						Debug.Log("ok");
						break;
					case Result.ServiceUnavailable:
						break;
					case Result.InvalidVersion:
						break;
					case Result.LockFailed:
						break;
					case Result.InternalError:
						break;
					case Result.InvalidPayload:
						break;
					case Result.InvalidCommand:
						break;
					case Result.InvalidPermissions:
						break;
					case Result.NotFetched:
						break;
					case Result.NotFound:
						Debug.Log("not found lobbby");
						break;
					case Result.Conflict:
						break;
					case Result.InvalidSecret:
						break;
					case Result.InvalidJoinSecret:
						break;
					case Result.NoEligibleActivity:
						break;
					case Result.InvalidInvite:
						break;
					case Result.NotAuthenticated:
						break;
					case Result.InvalidAccessToken:
						break;
					case Result.ApplicationMismatch:
						break;
					case Result.InvalidDataUrl:
						break;
					case Result.InvalidBase64:
						break;
					case Result.NotFiltered:
						break;
					case Result.LobbyFull:
						break;
					case Result.InvalidLobbySecret:
						break;
					case Result.InvalidFilename:
						break;
					case Result.InvalidFileSize:
						break;
					case Result.InvalidEntitlement:
						break;
					case Result.NotInstalled:
						break;
					case Result.NotRunning:
						break;
					case Result.InsufficientBuffer:
						break;
					case Result.PurchaseCanceled:
						break;
					case Result.InvalidGuild:
						break;
					case Result.InvalidEvent:
						break;
					case Result.InvalidChannel:
						break;
					case Result.InvalidOrigin:
						break;
					case Result.RateLimited:
						break;
					case Result.OAuth2Error:
						break;
					case Result.SelectChannelTimeout:
						break;
					case Result.GetGuildTimeout:
						break;
					case Result.SelectVoiceForceRequired:
						break;
					case Result.CaptureShortcutAlreadyListening:
						break;
					case Result.UnauthorizedForAchievement:
						break;
					case Result.InvalidGiftCode:
						break;
					case Result.PurchaseError:
						break;
					case Result.TransactionAborted:
						break;
					default:
						break;
				}
				Debug.Log("connected to lobby: " + lobbyTEMP.Id.ToString());

				chat.text += "\n" + "connected to lobby: " + lobbyTEMP.Id.ToString();
				//LM.ConnectNetwork(lobbyTEMP.Id);
			});
			//activityManager.UpdateActivity(discord, lobby);

		};

		LM.OnLobbyUpdate += (lobbyID) =>
		{
			Debug.Log("lobby successfully updated: " + lobbyID.ToString());
		};

		
	}

    private void DiscordController_OnMessage(ulong peerId, byte channelId, byte[] data)
    {
		//discord.GetNetworkManager().OpenChannel();
		//discord.GetNetworkManager().SendMessage()

		//   this --->  LM.GetMemberUser(lobby.Id, LM.GetMemberUserId(lobby.Id, I)); in a for loop should give the userID for every player in the looby (still need to make sure people can join lobbys) this should also count as peerId for sendMessage ^ and OpenChannel ^ then with that i should be able to send binary messages and then recevie them with discord.GetNetworkManager().OnMessage += DiscordController_OnMessage; i should also check and make sure that the incoming peerId is the host
		// looks like i will get the userID when they join, so this is mostly irrelavent
		// peer id is gotten by var myPeerId = networkManager.GetPeerId();

	}

	private void DiscordController_OnCurrentUserUpdate()
    {
		Debug.Log (discord.GetUserManager().GetCurrentUser().Avatar);
	}

    // Update is called once per frame
    void Update()
	{
		//LM.SendLobbyMessage(lobby.Id, "ping", (res) => Debug.Log(res));
		if (chat.text.Length > 250)
        {
			chat.text = chat.text.Remove(1,1);
			//chat.text += "remove";
        }

		if (transform.name == "host")
		{
			Host();
			transform.name = "hosting";
		}

		if (transform.name == "hosting")
		{
			LM.OnMemberConnect += LM_OnMemberConnect;
		}


		if (transform.name == "close")
		{
			Close();
			transform.name = "closing";
		}

		if (transform.name != "closed")
		{
			activityManager.UpdateActivity(activity, (res) =>
			{
				if (res == Discord.Result.Ok)
				{
					var pass = 0;
					//Debug.LogWarning("Everything is fine!");
				}
			});
		}

		discord.RunCallbacks();

	}

    private void LateUpdate()
    {
		discord.GetNetworkManager().Flush();
    }

    private void LM_OnMemberConnect(long lobbyId, long userId)
	{
		Debug.Log("OnMemberConnect");
		Debug.Log(lobbyId);
		Debug.Log(userId);
		chat.text += "\n" + "on member connect lobbyId | userId :" + lobbyId.ToString() + userId.ToString();

	}


	void Host()
	{
		var activityManager = discord.GetActivityManager();
		activity = new Discord.Activity
		{
			State = "Hostingfd",
			Details = "Bigger Test"
		};
		activityManager.UpdateActivity(activity, (res) =>
		{
			if (res == Discord.Result.Ok)
			{
				var pass = 0;
				//Debug.LogError("Everything is fine!");
			}
		});

		var txn = LM.GetLobbyCreateTransaction();
		txn.SetCapacity(8);
		txn.SetType(Discord.LobbyType.Public);
		txn.SetMetadata("a", "123");

		LM.CreateLobby(txn, (Discord.Result result, ref Discord.Lobby lobbyTEMP) =>
		{
			lobby = lobbyTEMP;
			Debug.Log("lobby " + lobby.Id.ToString() + " created with secret " + lobby.Secret);
			chat.text += "\n" + "lobby " + lobby.Id.ToString() + " created with secret " + lobby.Secret;

			// We want to update the capacity of the lobby
			// So we get a new transaction for the lobby
		var newTxn = LM.GetLobbyUpdateTransaction(lobby.Id);
			newTxn.SetCapacity(50);
			newTxn.SetType(LobbyType.Public);
			newTxn.SetLocked(false);

			LM.UpdateLobby(lobby.Id, newTxn, (result) =>
			{
				Debug.Log("lobby updated" + lobby.Id.ToString());
			}
			);

			var activitySecret = LM.GetLobbyActivitySecret(lobby.Id);

			activity = new Discord.Activity
			{
				State = "Hosting Testing",
				Details = chat.text,
				Type = ActivityType.Playing,
				Party = { Id = "the only one", Size = { CurrentSize = 1, //lobbyManager.MemberCount(lobby.Id),
						MaxSize = 50 } },

				Secrets = { Join = activitySecret },
				Instance = true,

			};
		});

		//LM.ConnectNetwork(lobby.Id);
		//discord.GetNetworkManager().c

	}

	void Close()
	{
		LM.DeleteLobby(lobby.Id, (result) =>
		{
			if (result == Discord.Result.Ok)
			{
				Debug.Log("Success!");
				transform.name = "closed";
			}
			else
			{
				Debug.Log("Failed");
				transform.name = "close Failed";
			}
		});

		activityManager.ClearActivity((result) =>
		{
			if (result == Discord.Result.Ok)
			{
				Debug.Log("Success!");
				transform.name = "closed";
			}
			else
			{
				Debug.Log("Failed");
				transform.name = "close Failed";
			}
		});
	}
	//void OnApplicationQuit()
   // {
//		discord.GetActivityManager().ClearActivity((result) =>
//		{
//			Debug.Log(result.ToString());
//		});
 //   }
}