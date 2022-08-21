using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class GameManager : MonoBehaviour
{

    // ================  DISCORD VARIBLES =================
    public Discord.Discord discord;
    public Discord.ActivityManager activityManager;
    public Discord.LobbyManager lobbyManager;
    //public Discord.NetworkManager networkManager;

    Discord.Lobby lobby;
    Discord.Activity activity;

    public Text chat;
    public Text info;
    public GameObject debugMenu;
    [Space(200)]

    bool joinedNetwork = false;

    // =================== GAME VARIBLES =================

    public GameObject topMenu;
    public GameObject joinScreen;
    public GameObject joiningScreen;

    public GameObject gameScreen;

    public GameObject colorPicker;


    // add twitch crowd controll?
    // create twitch bot program, detect if someone ask how to play/join, give them a Unnus discord invite

    public enum gameStates { menu, hosting, clienting }
    public gameStates state = gameStates.hosting;

    public int playerTurnIndex;
    public bool myTurn = false;

    //public enum subStates { waiting, turn, drawing }
    //public subStates subState = subStates.waiting;

    // lobby creation varibles

    public List<Text> playerLists;

    public InputField maxPlayersIF;
    public InputField numberOfDecksIF;
    public InputField turnTimerIF;
    public InputField startingCardsIF;

    public Toggle stackingTog;
    public Toggle sevenOTog;
    public Toggle JumpInTog;
    public Toggle forcePlayTog;
    public Toggle drawToMatchTog;
    public Toggle allowDrawingTog;

    public Toggle lockLobbyTog;

    // game rules
    public int maxPlayers = 1;
    public bool lobbyLocked; //??

    public int numberOfDecks = 1;
    public int startingCards = 7;
    public int turnTimer = 10;

    public bool stacking;
    public bool sevenO;
    public bool jumpIn;
    public bool forcePlay;
    public bool drawToMatch;
    public bool allowDrawing;

    // active in-game varibles
    public bool joinedLobby;

    public bool directionOfPlay;
    public Card discard;

    public List<Card> deck;
    public List<Card> discardedDeck;
    public List<Card> oldDeck;

    public List<GameObject> hand;
    public GameObject handCardPrefab;
    public GameObject handParent;

    public Dictionary<long, int> usersCardCount = new Dictionary<long, int>();

    public TextMeshProUGUI activePlayerList;

    Card.Colors color;
    Card.Contents content;

    public List<Sprite> wildSprites;
    public List<Sprite> blueSprites;
    public List<Sprite> greenSprites;
    public List<Sprite> redSprites;
    public List<Sprite> yellowSprites;


    // Start is called before the first frame update
    void Start()
    {
        //var file = new StreamReader("Assets/discord data"); // hide client id from github 
        //var ApplicationID = System.Convert.ToInt64(file.ReadLine());
        discord = new Discord.Discord(974458702398636092, (System.UInt64)Discord.CreateFlags.Default);
        //file.Close();

        activityManager = discord.GetActivityManager();
        lobbyManager = discord.GetLobbyManager();
        //networkManager = discord.GetNetworkManager();
        // We can create a helper method to easily connect to the networking layer of the lobby


        // Let's say we got a game invite from Rich Presence
        activityManager.OnActivityJoin += OnActivityJoin;
        //networkManager.OnMessage += NetworkingTheEasyWay_OnMessage;
        lobbyManager.OnNetworkMessage += OnNetworkMessage; // NetworkingTheEasyWay_OnMessage;

        //lobbyManager.OnMemberConnect += OnMemberConnect;
        lobbyManager.OnMemberUpdate += OnMemberConnect;



    }



    // Update is called once per frame
    void Update()
    {
        discord.RunCallbacks();


        if (Input.GetKeyDown(KeyCode.Backslash)) { debugMenu.SetActive(!debugMenu.activeSelf); } //show hide debug menu

        if ((Input.GetKey(KeyCode.RightAlt) || (Input.GetKey(KeyCode.LeftAlt))) && Input.GetKeyDown(KeyCode.Return))
        { //enter/exit full screen
            var maxSize = Screen.resolutions[Screen.resolutions.Length - 1];
            chat.text += maxSize.ToString();
            if (Screen.fullScreenMode != FullScreenMode.FullScreenWindow)
            {
                Screen.SetResolution(maxSize.width, maxSize.height, FullScreenMode.FullScreenWindow);
            } else { Screen.SetResolution(maxSize.width * 3 / 4, maxSize.height * 3 / 4, FullScreenMode.Windowed); }
        }


        if (lobby.Id != 0)
        {
            info.text = lobby.Id.ToString() + "\n";
            foreach (var user in lobbyManager.GetMemberUsers(lobby.Id))
            {
                info.text += user.Username + ":" + user.Id + "\n";
            }

            if (!joinedNetwork)
            {  //join the lobby's network, after joining a lobby, MUST BE DONE AT LEAST 1 FRAME AFTER JOINING LOBBY, so it's here
                InitNetworking(lobby.Id);
                joinedNetwork = true;
            }
        }

        if (gameScreen.activeInHierarchy)
        {
            UpdateActivePlayerList();
        }

   
        if (hand.Count > 1)  // this only needs to happen when the player's hand gets updated
        { //handle the rendering of player's hand
            for (int i = 0; i < hand.Count; i++)
            {
                //y = -180 normal height = 120 max_x = 200
                if (hand.Count < 10)
                {
                    hand[i].GetComponent<Card>().wantedAnchoredPosition = new Vector2(Remap(i, 0, hand.Count - 1, -200, 200), -180);
                    hand[i].GetComponent<Card>().wantedEulerAngles = new Vector3(0, 0, Remap(i, 0, hand.Count - 1, 10, -10));
                }
                else { hand[i].GetComponent<Card>().wantedAnchoredPosition = new Vector2(Remap(i, 0, hand.Count - 1, -400, 400), -180);
                    hand[i].GetComponent<Card>().wantedEulerAngles = new Vector3(0, 0, Remap(i, 0, hand.Count - 1, 20, -20));
                }

            }
        }
        else if (hand.Count == 1) {
            hand[0].GetComponent<Card>().wantedAnchoredPosition = new Vector2(0, -180);
            hand[0].GetComponent<Card>().wantedEulerAngles = new Vector3(0, 0, 0);
        }



    }
    private void LateUpdate()
    {
        lobbyManager.FlushNetwork();
    }

    //======================= NETWORK FUNCTIONS =============================== 

    private void OnNetworkMessage(long lobbyId, long userId, byte channelId, byte[] data)
    { //handle all of the NetMsgs

        chat.text += "msg:" + System.Text.Encoding.UTF8.GetString(data) + "\n";

        var inText = System.Text.Encoding.UTF8.GetString(data); // convert in data back to str, this is network expensive, but an easy way to handle this 

        if (inText.StartsWith("start game")) // host -> client
        {  // when a client recives the msg from host to start game
            joiningScreen.SetActive(false);
            topMenu.SetActive(false);
            gameScreen.SetActive(true);

            inText = inText.Remove(0, "start game".Length);
            var vals = inText.Split("-".ToCharArray());

            for (int ui = 0; ui < lobbyManager.MemberCount(lobby.Id); ui++)  //set every player's local card count to 0
            {
                var userId_ = lobbyManager.GetMemberUserId(lobby.Id, ui);
                usersCardCount[userId_] = 0;
                chat.text += userId_.ToString() + "\n";
            }

            /////// run a seprate function for both client and host when starting a game???? maybe, but host won't have to update rule settings
            //for (int i = 0; i < System.Int32.Parse(vals[0]); i++)
            // { // draw starting cards
            //     requstCard();               
            // }        

        }

        if (inText == "requst card")  //client -> host + minor client
        { //client asking for a card
            usersCardCount[userId]++;
            //pull card from random postion in deck
            if (state == gameStates.hosting)
            {
                if (deck.Count == 0)
                {
                    foreach (var card in discardedDeck)
                    {
                        deck.Add(card);
                    }
                    discardedDeck.Clear();
                }

                var i = Random.Range(0, deck.Count);
                Card drawenC = deck[i];
                deck.RemoveAt(i);

                lobbyManager.SendNetworkMessage(lobby.Id, userId, 0, System.Text.Encoding.UTF8.GetBytes("drawnen = " + drawenC.ToSending()));
                chat.text += "sent requsted card\n";
            }

        }
        if (inText.StartsWith("drawnen = ")) //host -> client
        { //host returning drawen card
            inText = inText.Remove(0, "drawnen = ".Length);
            var vals = inText.Split(":".ToCharArray());
            Card drawenC = new Card();
            drawenC.color = (Card.Colors)System.Enum.Parse(typeof(Card.Colors), vals[0].ToString());// drawenC.color.vals[0] ;
            drawenC.content = (Card.Contents)System.Enum.Parse(typeof(Card.Contents), vals[1].ToString());
            DrawCard(drawenC);
            chat.text += "recevied requsted card:" + drawenC.ToString() + "\n";
        }

        if (inText.StartsWith("play card = ")) //any -> all
        {
            usersCardCount[userId]--;
            inText = inText.Remove(0, "play card = ".Length);
            var vals = inText.Split(":".ToCharArray());
            // Card playedC = new Card();
            discard.color = (Card.Colors)System.Enum.Parse(typeof(Card.Colors), vals[0].ToString());
            discard.content = (Card.Contents)System.Enum.Parse(typeof(Card.Contents), vals[1].ToString());
            discard.img.sprite = GetCardSprite(discard);
            //discard.content = card.content;
            //discard.color = card.color;

            if (state == gameStates.hosting) {
                Card dc = new Card();
                dc.color = discard.color;
                dc.content = discard.content;
                discardedDeck.Add(dc);
            }
        }
        if (inText.StartsWith("selected color ="))
        { //when anyone uses the color picker after playing a wild
            inText = inText.Remove(0, "selected color =".Length);
            discard.color = (Card.Colors)System.Enum.Parse(typeof(Card.Colors), inText);
            discard.img.sprite = GetCardSprite(discard);
        }

        // }
    }
    private void OnActivityJoin(string secret)
    { //clicking on the join button on an invite
        if (state == gameStates.menu)
        {
            myTurn = false;
            //var lobbyManager = discord.GetLobbyManager();
            chat.text += "OnActivityJoin:" + secret + "\n";
            lobbyManager.ConnectLobbyWithActivitySecret(secret, (Discord.Result result, ref Discord.Lobby tempLobby) =>
            {
                lobby = tempLobby;
                chat.text += "ConnectLobbyWithActivitySecret:" + result.ToString() + " id:" + lobby.Id.ToString() + "\n";
                if (result == Discord.Result.Ok)
                {
                    activity = new Discord.Activity
                    {
                        State = "Lobby Joiner",
                        Details = "123",
                        Secrets = {
                            Match = "matchSecret",  //do i need this?
                            Join = lobbyManager.GetLobbyActivitySecret(lobby.Id)
                        },
                        Party = {
                            Id = lobby.Id.ToString(),
                            Size = {CurrentSize = 2, MaxSize = 10 } //i think discord ingonores currentSize now
                        }
                    };
                    activityManager.UpdateActivity(activity, (result => { chat.text += "OnActivityJoin-UpdateActivity" + result.ToString() + "\n"; }));
                    // Connect to networking
                    InitNetworking(lobby.Id);
                    state = gameStates.clienting;

                    topMenu.SetActive(false);
                    joinScreen.SetActive(false);
                    joiningScreen.SetActive(true);
                    joinedLobby = true;

                }
            });
        }
    }

    private void OnMemberConnect(long lobbyId, long userId)
    { //when someone else joins the lobby
        chat.text += "OnMemberConnect:" + lobbyId.ToString() + "|||" + userId.ToString() + (lobbyId == lobby.Id).ToString() + "\n";
        if (lobbyId == lobby.Id)
        {
            playerLists[0].text = "";
            foreach (var user in lobbyManager.GetMemberUsers(lobbyId))
            {
                playerLists[0].text += user.Username + "\n";
            }
            foreach (var PL in playerLists)
            {
                PL.text = playerLists[0].text;
            }
        }
    }

    private void OnMemberUpdate(long lobbyId, long userId)
    {

    }

    public void CreateLobby()
    { //clicking the create lobby button, just do basic setup for a lobby, and set state to hosting
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
            newTxn.SetCapacity(8);
            lobbyManager.UpdateLobby(lobby.Id, newTxn, (result) =>
            {
                chat.text += "lobby updated:" + result.ToString() + "\n";
            });

            activity = new Discord.Activity
            {
                State = "Lobby owner",
                Details = "Version: alpha dev",
                Secrets = {
                Match = "matchSecret",
                Join = lobbyManager.GetLobbyActivitySecret(lobby.Id)},

                Party = {
                Id = lobby.Id.ToString(),
                Size = {CurrentSize = 1, MaxSize = 8 } }

            };
            activityManager.UpdateActivity(activity, (result => { chat.text += "CreateLobby-UpdateActivity" + result.ToString() + "\n"; }));
            state = gameStates.hosting;
        });
    }

    public void LobbyCreationScreenUpdate()
    {
        maxPlayers = System.Convert.ToInt32(maxPlayersIF.text);

        numberOfDecks = System.Convert.ToInt32(numberOfDecksIF.text);
        turnTimer = System.Convert.ToInt32(turnTimerIF.text);
        startingCards = System.Convert.ToInt32(startingCardsIF.text);

        lobbyLocked = lockLobbyTog.isOn;

        stacking = stackingTog.isOn;
        sevenO = sevenOTog.isOn;
        jumpIn = JumpInTog.isOn;
        forcePlay = forcePlayTog.isOn;
        drawToMatch = drawToMatchTog.isOn;
        allowDrawing = allowDrawingTog.isOn;

        activity = new Discord.Activity
        {
            State = "Lobby owner",
            Details = lobbyLocked.ToString(),
            Secrets = {
                Match = "matchSecret",
                Join = lobbyManager.GetLobbyActivitySecret(lobby.Id)},

            Party = {
                Id = lobby.Id.ToString(),
                Size = {CurrentSize = 1, MaxSize = maxPlayers }}         // curent players

        };
        var txn = lobbyManager.GetLobbyUpdateTransaction(lobby.Id);
        txn.SetCapacity(System.Convert.ToUInt16(maxPlayers));
        txn.SetLocked(lobbyLocked);   // trying to join a locked lobby results in LobbyFull
        lobbyManager.UpdateLobby(lobby.Id, txn, (result => { chat.text += "LobbyCreationScreenUpdate-UpdateLobby" + result.ToString() + "\n"; }));

        activityManager.UpdateActivity(activity, (result => { chat.text += "LobbyCreationScreenUpdate-UpdateActivity" + result.ToString() + "\n"; }));
    }

    public void SendLobbyNetworkMessage() // leggacy i think
    { // Say hello!
        for (int i = 0; i < lobbyManager.MemberCount(lobby.Id); i++)
        {
            var userId = lobbyManager.GetMemberUserId(lobby.Id, i);
            chat.text += "SendNetworkMessage to:" + userId.ToString() + " in lobby:" + lobby.Id.ToString() + "\n";
            lobbyManager.SendNetworkMessage(lobby.Id, userId, 0, System.Text.Encoding.UTF8.GetBytes("Hello!"));
        }
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
    public void quitSafely()
    { // try to leave the net and lobby, don't think it works 100%
        lobbyManager.DisconnectNetwork(lobby.Id);
        lobbyManager.DisconnectLobby(lobby.Id, (result) => { Debug.Log(result); });
        activityManager.ClearActivity((result) => { Debug.Log(result); });
        Debug.Log("quit");
    }

    //========================= MIX FUNCTIONS ===============================
    public void StartGame()
    { // host function, when moving from game/lobby setup to the game proper
      // tell all clents the game is starting and the **********ADD THE RULES TO NETMSG***************
      // create deck of cards and play starting card
        myTurn = true;
        for (int ui = 0; ui < lobbyManager.MemberCount(lobby.Id); ui++)
        {
            var userId = lobbyManager.GetMemberUserId(lobby.Id, ui);
            chat.text += "SendNetworkMessage to:" + userId.ToString() + " in lobby:" + lobby.Id.ToString() + "\n";
            lobbyManager.SendNetworkMessage(lobby.Id, userId, 0, System.Text.Encoding.UTF8.GetBytes("start game-"+ startingCards.ToString()));

            usersCardCount[userId] = 0; // don't update here, as this int[] will update as clients draw cards, when recived start game NetMsg
            //^ set every player's local card count to 0
            chat.text += userId.ToString() + "\n";
        }

        //create deck of cards
        int cId = 0;                 // old bad code, needs to be updated useing Enum.Parse
        for (int i = 0; i < numberOfDecks; i++)
        {
            for (int colorId = 0; colorId < 5; colorId++) //create deck of cards
            {
                switch (colorId) //convert color id to card.color
                {
                    case 0:
                        color = Card.Colors.wild;
                        //Debug.Log("wild");
                        break;
                    case 1:
                        color = Card.Colors.blue;
                        break;
                    case 2:
                        color = Card.Colors.green;
                        break;
                    case 3:
                        color = Card.Colors.red;
                        break;
                    case 4:
                        color = Card.Colors.yellow;
                        break;
                }

                if (colorId > 0)
                {

                    for (int contentId = 0; contentId < 25; contentId++)
                    {
                        switch (contentId)
                        {
                            case 0:
                                content = Card.Contents.zero;
                                break;
                            case 1:
                                content = Card.Contents.one;
                                break;
                            case 2:
                                content = Card.Contents.one;
                                break;
                            case 3:
                                content = Card.Contents.two;
                                break;

                            case 4:
                                content = Card.Contents.two;
                                break;

                            case 5:
                                content = Card.Contents.three;
                                break;

                            case 6:
                                content = Card.Contents.three;
                                break;

                            case 7:
                                content = Card.Contents.four;
                                break;

                            case 8:
                                content = Card.Contents.four;
                                break;

                            case 9:
                                content = Card.Contents.five;
                                break;

                            case 10:
                                content = Card.Contents.five;
                                break;

                            case 11:
                                content = Card.Contents.six;
                                break;

                            case 12:
                                content = Card.Contents.six;
                                break;

                            case 13:
                                content = Card.Contents.seven;
                                break;

                            case 14:
                                content = Card.Contents.seven;
                                break;

                            case 15:
                                content = Card.Contents.eight;
                                break;

                            case 16:
                                content = Card.Contents.eight;
                                break;

                            case 17:
                                content = Card.Contents.nine;
                                break;

                            case 18:
                                content = Card.Contents.nine;
                                break;

                            case 19:
                                content = Card.Contents.skip;
                                break;

                            case 20:
                                content = Card.Contents.skip;
                                break;

                            case 21:
                                content = Card.Contents.reverse;
                                break;

                            case 22:
                                content = Card.Contents.reverse;
                                break;

                            case 23:
                                content = Card.Contents.drawTwo;
                                break;

                            case 24:
                                content = Card.Contents.drawTwo;
                                break;
                        }

                        deck.Add(new Card());
                        deck[cId].color = color;
                        deck[cId].content = content;
                       // Debug.Log(deck[cId].ToString());
                        cId++;
                    }
                }
                else
                {
                    for (int j = 0; j < 4; j++)
                    {
                        deck.Add(new Card());
                        deck[cId].color = color;
                        deck[cId].content = Card.Contents.wild;

                      //  Debug.Log(deck[cId].ToString());
                        cId++;
                    }
                    for (int l = 0; l < 4; l++)
                    {
                        deck.Add(new Card());
                        deck[cId].color = color;
                        deck[cId].content = Card.Contents.wildDrawFour;

                    //    Debug.Log(deck[cId].ToString());
                        cId++;
                    }
                }

            }
        }


        // draw starting discard
        var r = Random.Range(0, deck.Count);
        Card drawenC = deck[r];
        deck.RemoveAt(r);
        discard.color = drawenC.color;
        discard.content = drawenC.content;
        //discard.img.sprite = GetCardSprite(discard);

        foreach (var user in lobbyManager.GetMemberUsers(lobby.Id))
        {
        }
    }
    /// <summary>
    /// update the list of all players, the number of cards in thier hand, and whoose turn it is
    /// TODO: add players' PFP
    /// </summary>
    public void UpdateActivePlayerList()
    { 
        var newText = "";

        var i = -1;
        foreach (var user in lobbyManager.GetMemberUsers(lobby.Id))
        {
            i++;
            if (playerTurnIndex == i)
            {
                newText += "<color=yellow>►";
            }
            else { newText += "<color=white> "; }

            if (user.Id == discord.GetUserManager().GetCurrentUser().Id)
            {
                newText += "<b> ";
            }

            chat.text += user.Id.ToString() + "\n";
            newText += user.Username + ": " + usersCardCount[user.Id].ToString() + "\n";  // can't pull card count... on clinet??????


            if (user.Id == discord.GetUserManager().GetCurrentUser().Id)
            {
                newText += "</b> ";
            }
        }
        activePlayerList.SetText(newText);
        Debug.Log(newText);
    }
    /// <summary>
    /// end your turn and send it to the next player based on dir of play
    /// </summary>
    public void PassTurn() {
        myTurn = false;
      //  lobbyManager.GetMemberUserId(lobby.Id, self)
    }


    //========================= GAME FUNCTIONS ===============================

    /// <summary>
    /// basicly for when a player(Host or Client) wants to draw a card, 
    /// ie. clicking on the deck, or previuos player played a +2
    /// </summary>
    public void requstCard()
    {
        for (int i = 0; i < lobbyManager.MemberCount(lobby.Id); i++)
        {
            var userId = lobbyManager.GetMemberUserId(lobby.Id, i);
            chat.text += "SendNetworkMessage to:" + userId.ToString() + " in lobby:" + lobby.Id.ToString() + "\n";
            lobbyManager.SendNetworkMessage(lobby.Id, userId, 0, System.Text.Encoding.UTF8.GetBytes("requst card"));
        }

        switch (state)
        {
            case gameStates.hosting:
                if (deck.Count == 0)
                {
                    foreach (var card in discardedDeck)
                    {
                        deck.Add(card);  // make sure to set wilds back to wild
                    }
                    discardedDeck.Clear();
                }
                var i = Random.Range(0, deck.Count);
                Card drawenC = deck[i];
                deck.RemoveAt(i);
                DrawCard(drawenC);
                break;
            case gameStates.clienting:
                chat.text += "requsted card\n";
                break;
            default:
                break;
        }
    }

    public void DrawCard(Card drawenC)
    {
        chat.text += "drawing\n";

        GameObject newCard = Instantiate(handCardPrefab, handParent.transform);
        Card newCardC = newCard.GetComponent<Card>();

        newCardC.content = drawenC.content;
        newCardC.color = drawenC.color;

        //hand.Insert(newCard);

        chat.text += "draw card:" + newCard.GetComponent<Card>().ToString() + "\n";

        //first sort new card into hand
        var i = 0;
        var highestPos = 0;
        foreach (var handCardGO in hand)
        {
            i++;
            var HandCardC = handCardGO.GetComponent<Card>();
            var cardVal = (HandCardC.content.GetHashCode() + 1) + ((HandCardC.color.GetHashCode() * 15));
            var newCardVal = (newCardC.content.GetHashCode() + 1) + ((newCardC.color.GetHashCode() * 15));
            if (newCardVal >= cardVal)
            {
                highestPos = i;
            }
            else { break; }
        }

        hand.Insert(highestPos, newCard);
        usersCardCount[discord.GetUserManager().GetCurrentUser().Id]++;


    }

    public void PlayCard(GameObject cardObj)  // need to add visual code
    {
        if (hand.Exists(o => o == cardObj))
        {
            Card card = cardObj.GetComponent<Card>();
            if (card.color == discard.color || card.content == discard.content || card.color == Card.Colors.wild)
            {
                if (state == gameStates.hosting)
                {
                    Card dc = new Card();
                    dc.color = discard.color;
                    dc.content = discard.content;
                    discardedDeck.Add(dc);
                    //hand.Remove(cardObj);
                }
                hand.RemoveAt(hand.IndexOf(cardObj));
                usersCardCount[discord.GetUserManager().GetCurrentUser().Id]--;

                card.wantedAnchoredPosition = discard.wantedAnchoredPosition;
                card.wantedEulerAngles = Vector3.zero;
                card.discarding = true;
                var cb = cardObj.gameObject.GetComponent<Button>().colors;
                cb.normalColor = Color.white;
                cardObj.gameObject.GetComponent<Button>().colors = cb;
                //Destroy(cardObj);
                discard.content = card.content;
                discard.color = card.color;
                //discard.img.sprite = GetCardSprite(discard);

                if (card.content == Card.Contents.wild || card.content == Card.Contents.wildDrawFour)
                {
                    colorPicker.SetActive(true);
                }

                //send card to other players
                for (int i = 0; i < lobbyManager.MemberCount(lobby.Id); i++)
                {
                    var userId = lobbyManager.GetMemberUserId(lobby.Id, i);
                    chat.text += "SendNetworkMessage to:" + userId.ToString() + " in lobby:" + lobby.Id.ToString() + "\n";
                    lobbyManager.SendNetworkMessage(lobby.Id, userId, 0, System.Text.Encoding.UTF8.GetBytes("play card = " + card.ToString()));
                }
            }
        }
    }

    public void SelectColor(int color)
    {
        colorPicker.SetActive(false);

        if (color == 1) { discard.color = Card.Colors.blue; }
        if (color == 2) { discard.color = Card.Colors.green; }
        if (color == 3) { discard.color = Card.Colors.red; }
        if (color == 4) { discard.color = Card.Colors.yellow; }

        discard.img.sprite = GetCardSprite(discard);
        for (int i = 0; i < lobbyManager.MemberCount(lobby.Id); i++)
        {
            var userId = lobbyManager.GetMemberUserId(lobby.Id, i);
            chat.text += "SendNetworkMessage to:" + userId.ToString() + " in lobby:" + lobby.Id.ToString() + "\n";
            lobbyManager.SendNetworkMessage(lobby.Id, userId, 0, System.Text.Encoding.UTF8.GetBytes("selected color =" + color.ToString()));
        }
    }

    public void Shuffle()
    {
        foreach (var i in deck)
        {
            oldDeck.Add(i);
        }

        for (int i = 0; i < deck.Count; i++)
        {
            int r = Random.Range(0, oldDeck.Count);

            deck[i] = oldDeck[r];
            oldDeck.RemoveAt(r);
        }

    }

    public Sprite GetCardSprite(Card card)
    {
        chat.text += card.content.GetHashCode().ToString();
        switch (card.color)
        {
            case Card.Colors.wild:
                return wildSprites[card.content.GetHashCode()];
            case Card.Colors.blue:
                return blueSprites[card.content.GetHashCode()];
            case Card.Colors.green:
                return greenSprites[card.content.GetHashCode()];
            case Card.Colors.red:
                return redSprites[card.content.GetHashCode()];
            case Card.Colors.yellow:
                return yellowSprites[card.content.GetHashCode()];
            default:
                return wildSprites[card.content.GetHashCode()];
        }
    }


    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public void OpenURL(string url)
    {
        Application.OpenURL(url);
    }


}
