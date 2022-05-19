using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Discord;

public class NewDiscordManager : MonoBehaviour
{
    public Discord.Discord discord;
    Discord.NetworkManager networkManager;

    public InputField inputPeerId;
    public InputField outputPeerId;

    public Text chat;

    // Start is called before the first frame update
    void Start()
    {
        discord = new Discord.Discord(974458702398636092, (System.UInt64)Discord.CreateFlags.Default);
        networkManager = discord.GetNetworkManager();
        networkManager.OnMessage += NetworkManager_OnMessage;
    }

    private void NetworkManager_OnMessage(ulong peerId, byte channelId, byte[] data)
    {
        chat.text += peerId.ToString() + channelId.ToString() + data.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        outputPeerId.text = networkManager.GetPeerId().ToString();

        if (transform.name == "submit")
        {
            ulong vOut = System.Convert.ToUInt64(inputPeerId.text);
            byte chanel = 12;
            networkManager.OpenChannel(vOut, chanel, true);

           // byte.Parse()
            networkManager.SendMessage(vOut, chanel, System.BitConverter.GetBytes(1001));
            transform.name = "waiting";
        }
    }

    private void LateUpdate()
    {
        networkManager.Flush();
    }
}
