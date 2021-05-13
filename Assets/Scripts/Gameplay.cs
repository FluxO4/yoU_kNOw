using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class Gameplay : NetworkBehaviour
{
    //Singleton stuff
    static Gameplay _current;
    public static Gameplay current
    {
        get
        {
            if (_current == null)
                Debug.Log(typeof(Gameplay) + " NOT FOUND");

            return _current;
        }
    }

    private void Awake()
    {
        _current = this as Gameplay;
    }


    //Physics variables
    [Range(10, 100)]
    public int CardSpeedMultiplier = 80;
    public int MinimumPlayersForGame = 1;
    public List<GameObject> tableCardsR;

    //UI elements
    public GameObject StartPanel;
    public GameObject GameOverPanel;
    public Text PlayerNameText;
    public Text WinnerNameText;
    public Text NeedToDraw;

    //Base flags
    public bool GameInProgress;
    public bool IsGameOver;


    //Players
    public Player LocalPlayer;
    public List<Player> players;
    public List<int> activePlayers;


    //Settings
    public List<bool> settings = new List<bool>();
    // ------ OLD SETTINGS SET ------
    //0 = Auto add cards if no comeback
    //1 = Turn skips after draw 2 or draw 4
    //2 = Allow stacking same-numbered cards
    //3 = Allow stacking wild cards
    //4 = Any card in stack may match
    //5 = Allow pass button after one card drawn
    //6 = Allow multiple cards to be drawn in a turn
    //7 = Allow drawing card if legal card exists
    //8 = Fix multi-skip in 2 player game
    //9 = Reverse skips in 2 player game
    //10 = Allow draw 2 and draw 4 comebacks

    // ------ NEW SETTINGS SET ------
    //0 = Recommended Special *CHEF'S KISS* rules
    //1 = UNO(TM) common unofficial rules

    //2 = UNO(TM) official *GROAN* rules
    //3 = UNO(TM) official house rules
    //4 = Ahem, lemme choose

    //5 = Allow stacking same-numbered cards
    //6 = Allow stacking wild cards
    //7 = Allow passing draw with draw card stacking
    //8 = Draw cards automatically if no comeback
    //9 = Skip turn after draw 2 or draw 4
    //10 = Allow drawing card if playable card exists
    //11 = Enable pass button after one card drawn
    //12 = Fix multi-skip in 2 player game
    //13 = Reverse skips in 2 player game
    //14 = Any card in stack may match
    //15 = Continue game after players finish
    //16 = Swap hand with someone 7 is played
    //17 = Hands move around one player if 0 is played
    //18 = Must play or pass after one card drawn
    //19 = Must play or pass after 5 cards drawn
    //20 = Must play or pass after 10 cards drawn
    //21 = Allow draw 4 despite playable card
    //22 = UNO score tallying system



    //Random card frequencies
    public int[] cardFrequencies = { 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1 };


    int cardFrequenciesSum = 27;

    [ClientRpc]
    void RpcSetCardFrequencies(List<int> frequencies)
    {
        SetCardFrequencies(frequencies);
    }

    void SetCardFrequencies(List<int> frequencies)
    {
        cardFrequenciesSum = 0;
        for (int i = 0; i < 15; i++)
        {
            cardFrequencies[i] = frequencies[i];
            cardFrequenciesSum += frequencies[i];
        }
    }

    [ClientRpc]
    void RpcSetSettings(List<bool> _settings)
    {
        SetSettings(_settings);
    }

    void SetSettings(List<bool> _settings)
    {
        for (int i = 0; i < _settings.Count; i++)
        {
            settings.Add(_settings[i]);
        }
    }

    //Random card generator
    public int getRandomCard
    {
        get
        {
            int col = Random.Range(0, 4);
            int num = Random.Range(0, cardFrequenciesSum + 1);
            int cumulative = 0;
            for (int i = 0; i < 15; i++)
            {
                cumulative += cardFrequencies[i];
                if (num <= cumulative)
                {
                    num = i;
                    break;
                }
            }

            return getCardValue(col, num);
        }
    }


    //Syncvars
    /* [SyncVar]
     public int cardOnTable = -1;
     public int cardOnTableBuffer = -1;
     [ClientRpc]
     public void RpcSetCardOnTableBuffer(int val)
     {
         cardOnTableBuffer = val;
     }*/

    [SyncVar]
    public int turn = 0;


    //turn
    int server_drawCards = 0;
    int server_passCard = 0;

    /* [SyncVar]
     public int needToDraw = 0; //Whoever's turn is next needs to draw this many cards in order to pass the turn
     public int needToDrawBuffer = 0;
     [ClientRpc]
     public void RpcSetNeedToDrawBuffer(int val)
     {
         needToDrawBuffer = val;
     }*/

    [SyncVar]
    public int turnDirection = 1; //1 is clockwise


    [SyncVar]
    public bool waitingToSwapCardsAround = false;

    [SyncVar]
    public bool waitingToSwapCards = false;
    
    /*int sourcePlayer = -1;
    int destinationPlayer = -1;*/

    public bool settingsUpdated = false;

    //Game options




    //Main methods
    private void Update()
    {
        if (NetworkManager.singleton.isNetworkActive)
        {
            if (!GameInProgress)
                CheckPlayers();

            if (!isServer)
            {
                if (!settingsUpdated)
                {
                    CmdSettingsChangeEvent();
                    settingsUpdated = true;
                }
            }

            if (isServer && GameInProgress)
            {
                /*
                if (settings[17])
                {
                    if (waitingToSwapCardsAround)
                    {
                        bool waitingForPlayerCards = false;

                        for (int i = 0; i < players.Count; i++)
                        {
                            if (players[i].cardShareUpdated == false)
                            {
                                waitingForPlayerCards = true;
                                break;
                            }
                        }


                        if (!waitingForPlayerCards) {
                            for (int i = 0; i < activePlayers.Count; i++)
                            {
                                int ti = normaliseForIndex(i + turnDirection, activePlayers.Count);
                                //TargetSetCards(players[activePlayers[i]].netIdentity.connectionToClient, playerCards[activePlayers[ti]], activePlayers[ti]);
                                players[activePlayers[i]].RpcSetCards(players[activePlayers[ti]].cardShare, activePlayers[ti]);
                            }
                            waitingToSwapCardsAround = false;
                        }
                    }
                }
                if (settings[17])
                {
                    if (waitingToSwapCards)
                    {
                        bool waitingForPlayerCards = false;


                        for (int i = 0; i < players.Count; i++)
                        {
                            if (players[i].cardShareUpdated == false)
                            {
                                waitingForPlayerCards = true;
                                break;
                            }
                        }



                        if (!waitingForPlayerCards)
                        {
                            //TargetSetCards(players[sourcePlayer].netIdentity.connectionToClient, playerCards[destinationPlayer], destinationPlayer);
                            //TargetSetCards(players[destinationPlayer].netIdentity.connectionToClient, playerCards[sourcePlayer], sourcePlayer);

                            players[sourcePlayer].RpcSetCards(players[destinationPlayer].cardShare, destinationPlayer);
                            players[destinationPlayer].RpcSetCards(players[sourcePlayer].cardShare, sourcePlayer);

                            waitingToSwapCards = false;
                        }
                    }
                }
                */
            }


            if (LocalPlayer == null)
            {
                FindLocalPlayer();
            }
        }
        else
        {
            if (GameInProgress)
            {
                UIController.current.DisconnectedBox.SetActive(true);
                GameInProgress = false;
            }
            //Clean up
            //GameInProgress = false;
            settingsUpdated = false;
            LocalPlayer = null;
            players.Clear();
            //cardOnTable = -1;
            turn = 0;
            //needToDraw = 0;
            turnDirection = 1;

            Debug.Log("Disconnected I think");
        }
    }

    //UI methods

    [Command(requiresAuthority = false)]
    public void CmdUpdatePlayerNames()
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].CmdAutoSetMyUIName();
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdatePlayerNamesWithDC(int drawcard)
    {
        for (int i = 0; i < players.Count; i++)
        {
            players[i].RpcAutoSetMyUIName(drawcard);
        }
    }

    public void ReadyButtonHandler()
    {
        LocalPlayer.SendReadyToServer(PlayerNameText.text);
    }

    public void PassButtonHandler()
    {
        if (LocalPlayer.myturn)
        {
            LocalPlayer.Pass();
        }
    }


    [ClientRpc]
    public void RpcSetGameUI()
    {
        UIController.current.setGameUI();
    }


    //[Command(requiresAuthority = false)]
    [Server]
    public void CmdSettingsChangeEvent()
    {
        List<bool> tempSettings = new List<bool>();
        List<int> tempFrequencies = new List<int>();
        for (int i = 0; i < UIController.current.settingsList.Count; i++)
        {
            tempSettings.Add(UIController.current.settingsList[i].isOn);
        }
        for (int i = 0; i < UIController.current.numberFrequencies.Count; i++)
        {
            tempFrequencies.Add((int)UIController.current.numberFrequencies[i].value);
        }

        RpcSettingsChangeEvent(tempSettings, tempFrequencies);
    }

    [ClientRpc]
    public void RpcSettingsChangeEvent(List<bool> tempSettings, List<int> tempProbabilities)
    {
        UIController.current.settingsChangeConsequence(tempSettings, tempProbabilities);
    }


    //Initialisation methods

    private void Start()
    {
        Physics.gravity = new Vector3(0, -20, 0);

    }

    /*void GameReadyCheck()
    {
        if (!GameInProgress)
        {
            foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkIdentity.spawned)
            {
                Player comp = kvp.Value.GetComponent<Player>();

                //Add if new
                if (comp != null && !players.Contains(comp))
                {
                    players.Add(comp);
                    if(isServer)
                        comp.myID = players.Count - 1;
                }
            }

            UIController.current.updatePlayerList();
        }
    }/*/


    void CheckPlayers()
    {
        List<Player> tempPlayers = new List<Player>();
        //tempPlayers = players;
        //players.Clear();

        foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkIdentity.spawned)
        {
            Player comp = kvp.Value.GetComponent<Player>();

            //Add if new
            if (comp != null)
            {
                tempPlayers.Add(comp);
                //players.Add(comp);
            }
        }
        int playerThatLeft = -1;

        for (int i = 0; i < players.Count; i++)
        {
            if (!tempPlayers.Contains(players[i]))
            {
                players.RemoveAt(i);
                playerThatLeft = i;
            }
        }
        

        for (int i = 0; i < tempPlayers.Count; i++)
        {
            if (!players.Contains(tempPlayers[i]))
            {
                players.Add(tempPlayers[i]);
            }
        }

        if (isServer) for (int i = 0; i < players.Count; i++)
            {
                
                players[i].myID = i;
                if (GameInProgress && playerThatLeft >= 0)
                {
                    //activePlayers.Remove(playerThatLeft);
                    activePlayers.Clear();
                    for (int ii = 0; ii < players.Count; ii++)
                    {
                        if (!players[ii].hasFinished)
                        {
                            activePlayers.Add(ii);
                        }
                    }
                    turn = activePlayers[Random.Range(0, activePlayers.Count)];
                }
            }

        UIController.current.updatePlayerList();


    }


    [Command(requiresAuthority = false)]
    public void CmdCheckPlayers()
    {
        CheckPlayers();
    }

    [Command(requiresAuthority = false)]
    public void CmdCheckPlayersAfterDelay()
    {
        StartCoroutine(checkPlayersAfterDelay());
    }


    IEnumerator checkPlayersAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        CheckPlayers();
    }


    void FindLocalPlayer()
    {
        //Check to see if the player is loaded in yet
        if (NetworkClient.localPlayer == null)
            return;

        LocalPlayer = NetworkClient.localPlayer.GetComponent<Player>();
        //ReadyButtonHandler();
    }

    [Command(requiresAuthority = false)]
    public void CmdDisconnectEverybody()
    {
        RpcDisconnectEverybody();
    }

    [ClientRpc]
    public void RpcDisconnectEverybody()
    {
        //UIController.current.HostLeftBox.SetActive(true);
        UIController.current.SafeLeaveGame();
    }

    [Command(requiresAuthority = false)]
    public void CmdDisconnectPlayer(Player playerToDisconnect)
    {
        playerToDisconnect.RpcDisconnect();

    }


    //Game start methods

    public void StartGame()
    {
        List<bool> _settings = new List<bool>();
        int numSettings = UIController.current.settingsList.Count;
        for (int i = 0; i < numSettings; i++)
        {
            _settings.Add(UIController.current.settingsList[i].isOn);
        }
        List<int> temp = new List<int>();
        for (int i = 0; i < 15; i++)
        {
            temp.Add((int)UIController.current.numberFrequencies[i].value);
        }


        /*for (int i = 0; i < players.Count; i++)
        {
            playerCards.Add(new List<int>());
            playerCardsUpdated.Add(true);
        }*/

        //mdSettingsChangeEvent();

        CmdStartGame(_settings, temp);
    }

    int firstcard = -1;

    [Command(requiresAuthority = false)]
    public void CmdStartGame(List<bool> _settings, List<int> randomCardFrequencies)
    {
        /*if (GameInProgress)
        {
            return;
        }*/
        RpcSetGameUI();

        turnTimeOutCounter = 0;

        playerScores.Clear();
        playerScores.Add(0);
        playerScores.Add(0);
        playerScores.Add(0);
        playerScores.Add(0);
        //turn = Random.Range(0,players.Count-1);

        turnDirection = 1;

        GameInProgress = true;

        //finishedPlayers.Clear();
        activePlayers.Clear();
        for (int i = 0; i < players.Count; i++)
        {
            activePlayers.Add(i);
        }

        RpcSetPlayerHands();

        SetCardFrequencies(randomCardFrequencies);
        RpcSetCardFrequencies(randomCardFrequencies);
        SetSettings(_settings);
        RpcSetSettings(_settings);



        int _firstcard = getCardValue(Random.Range(0, 4), Random.Range(0, 10));
        firstcard = _firstcard;
        //RpcSetCardOnTableBuffer(cardOnTable);
        RpcDropFirstCard(firstcard);
        RpcSetColourIndicator(Sceneobjects.current.CardColours[getCardColour(firstcard)], turnDirection);

        turn = Random.Range(0, activePlayers.Count);


        foreach (Player t in players)
        {
            t.myturn = false;

            //t.cardOnTableBuffer = cardOnTable;
            t.RpcGameStartSetup();

            //Debug.Log("Autoaddcards settings is " + settings[0]);
        }

        firstWinnerSet = false;

        for (int i = 0; i < players.Count; i++)
        {
            if (i == activePlayers[turn])
            {
                players[i].RpcTurnStart(true, 0, firstcard);
            }
            else
            {
                players[i].RpcTurnStart(false, 0, firstcard);
            }
        }
        firstWinnerSetInClients = false;

        CmdUpdatePlayerNames();
    }

    [ClientRpc]
    void RpcDropFirstCard(int cardvalue)
    {


        for (int i = 0; i < tableCardsR.Count; i++)
        {
            Destroy(tableCardsR[i]);
        }
        tableCardsR.Clear();


        GameObject tcard = Instantiate(Sceneobjects.current.cardR);
        tcard.transform.position = new Vector3(0, 5, 0);
        Card tcardp = tcard.GetComponent<Card>();
        tcardp.Value = cardvalue;
        tcard.name = tcardp.Value.ToString();
        tableCardsR.Add(tcard);


        //CmdUpdatePlayerNames();
    }

    [ClientRpc]
    public void RpcSetPlayerHands()
    {
        
        int myIndex = players.IndexOf(LocalPlayer);
        /*Sceneobjects.current.SwapButtons[1].gameObject.SetActive(true);
        Sceneobjects.current.SwapButtons[2].gameObject.SetActive(true);
        Sceneobjects.current.SwapButtons[3].gameObject.SetActive(true);*/


        if (players.Count == 1)
        {

            LocalPlayer.hand = Sceneobjects.current.Hands[0];
            /*LocalPlayer.myNameTag = Sceneobjects.current.Names[0];
            LocalPlayer.myNameText = Sceneobjects.current.NameTexts[0];
            LocalPlayer.myNameText.text = LocalPlayer.playerName;*/
        }
        else
        if (players.Count == 2)
        {
            /*Sceneobjects.current.Names[1].gameObject.SetActive(false);
            Sceneobjects.current.Names[3].gameObject.SetActive(false);

            /*Sceneobjects.current.SwapButtons[1].gameObject.SetActive(false);
            Sceneobjects.current.SwapButtons[3].gameObject.SetActive(false);*/

            for (int i = 0; i < 2; i++)
            {
                if (i == myIndex)
                {
                    players[i].hand = Sceneobjects.current.Hands[0];
                   /* players[i].myNameTag = Sceneobjects.current.Names[0];
                    players[i].myNameText = Sceneobjects.current.NameTexts[0];
                    players[i].myNameText.text = players[i].playerName;*/
                }
                else
                {
                    players[i].hand = Sceneobjects.current.Hands[2];
                    /*players[i].myNameTag = Sceneobjects.current.Names[2];
                    players[i].myNameText = Sceneobjects.current.NameTexts[2];
                    players[i].myNameText.text = players[i].playerName;*/
                }
            }
        }
        else
        if (players.Count == 3)
        {
            /*Sceneobjects.current.Names[2].gameObject.SetActive(false);

            /*Sceneobjects.current.SwapButtons[2].gameObject.SetActive(false);*/

            for (int i = 0; i < 3; i++)
            {
                if (i == myIndex)
                {
                    players[i].hand = Sceneobjects.current.Hands[0];
                   /* players[i].myNameTag = Sceneobjects.current.Names[0];
                    players[i].myNameText = Sceneobjects.current.NameTexts[0];
                    players[i].myNameText.text = players[i].playerName;*/
                }
                else
                if (i == (myIndex + 1) % 3)
                {
                    players[i].hand = Sceneobjects.current.Hands[1];
                   /* players[i].myNameTag = Sceneobjects.current.Names[1];
                    players[i].myNameText = Sceneobjects.current.NameTexts[1];
                    players[i].myNameText.text = players[i].playerName;*/
                }
                else
                if (i == (myIndex + 2) % 3)
                {
                    players[i].hand = Sceneobjects.current.Hands[3];
                    /*players[i].myNameTag = Sceneobjects.current.Names[3];
                    players[i].myNameText = Sceneobjects.current.NameTexts[3];
                    players[i].myNameText.text = players[i].playerName;*/
                }
            }

        }
        else
        if (players.Count == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i == myIndex)
                {
                    players[i].hand = Sceneobjects.current.Hands[0];
                    /*players[i].myNameTag = Sceneobjects.current.Names[0];
                    players[i].myNameText = Sceneobjects.current.NameTexts[0];
                    players[i].myNameText.text = players[i].playerName;*/
                }
                else
                if (i == (myIndex + 1) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[1];
                    /*players[i].myNameTag = Sceneobjects.current.Names[1];
                    players[i].myNameText = Sceneobjects.current.NameTexts[1];
                    players[i].myNameText.text = players[i].playerName;*/
                }
                else
                if (i == (myIndex + 2) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[2];
                   /* players[i].myNameTag = Sceneobjects.current.Names[2];
                    players[i].myNameText = Sceneobjects.current.NameTexts[2];
                    players[i].myNameText.text = players[i].playerName;*/
                }
                else
                if (i == (myIndex + 3) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[3];
                    /*players[i].myNameTag = Sceneobjects.current.Names[3];
                    players[i].myNameText = Sceneobjects.current.NameTexts[3];
                    players[i].myNameText.text = players[i].playerName;*/
                }
            }
        }

        for(int i = 0; i < players.Count; i++)
        {
            players[i].hand.swapButton.SetActive(true);
            players[i].hand.resetAll();
        }

    }


    List<int> playerScores = new List<int>();

    //Turn methods
    [Server]
    public void PassTurnToNextPlayer(int drawCards = 0, int skip = 0, int reverse = 0, int passCard = -1, int swapTarget = -1, bool Iwin = false, bool passed = false)
    {
        server_drawCards = drawCards;
        server_passCard = passCard;


        for (int i = 0; i < reverse; i++)
        {
            turnDirection = -turnDirection;
        }

        Color temp = Sceneobjects.current.CardColours[getCardColour(passCard)];
        RpcSetColourIndicator(temp, turnDirection);

        if (skip > 0)
        {
            if (settings[12])
            {
                if (players.Count == 2)
                {
                    skip = 1;
                }
            }
        }

        if (reverse > 0)
        {
            if (settings[13])
            {
                if (activePlayers.Count == 2)
                {
                    skip = 1;
                }
            }
        }
        int prevturn = turn;
        int prevTurnIndex = activePlayers[turn];

        turn = (turn + (1 + skip) * turnDirection);

        if (Iwin)
        {
            //Player turnPlayer = players[activePlayers[turn]];

            int winner = activePlayers[prevturn];
            activePlayers.Remove(winner);
            int remaining = activePlayers.Count;
            players[winner].hasFinished = true;


            if (!firstWinnerSet)
            {
                firstWinner = players[winner].playerName;
                //players[winner].hand.firstPlaceIcon.SetActive(true);
                firstWinnerSet = true;
            }

            if (!settings[22])
            {
                players[winner].wins += remaining;
                playerScores[winner] += remaining;

                if (remaining <= 1 || !settings[15])
                {
                    GameInProgress = false;
                    RpcEndGame(winner, firstWinner, playerScores);
                }
            }
            else
            {
                for (int i = 0; i < activePlayers.Count; i++)
                {
                    players[activePlayers[i]].RpcUpdateCardShare();
                }
                StartCoroutine(setPlayerScores(winner, remaining));
            }

            //turn = activePlayers.IndexOf(turnPlayer.myID);
        }

        if (!GameInProgress)
        {
            return;
        }

        int n = activePlayers.Count;
        if (turn < 0)
        {
            while (turn < 0)
            {
                turn += n;
            }
        }

        if (turn >= n)
        {
            while (turn >= n)
            {
                turn -= n;
            }
        }


        //Player playerWithTurn = players[activePlayers[turn]];


        if (settings[17] && !passed && getCardNumber(passCard) == 0)
        {
            for (int i = 0; i < activePlayers.Count; i++)
            {
                players[activePlayers[i]].RpcUpdateCardShare();
            }
            StartCoroutine(swapCardsAround());
        }
        else if (settings[16] && getCardNumber(passCard) == 7 && activePlayers.Count > 1 && !passed)
        {
            if(swapTarget == -1)
            {
                swapTarget = activePlayers[Random.Range(0, activePlayers.Count)];
            }


            for (int i = 0; i < activePlayers.Count; i++)
            {
                players[activePlayers[i]].RpcUpdateCardShare();
            }
            StartCoroutine(swapCards(prevTurnIndex, swapTarget));
        }
        else
        {
            nextTurn();
        }
    }

    int turnTimeOutCounter = 0;
    IEnumerator turnTimeOut()
    {
        int temp = turnTimeOutCounter;
        yield return new WaitForSeconds(100);
        if(temp == turnTimeOutCounter)
        players[activePlayers[turn]].RpcDisconnect();
    }

    void IncrementTurn(int skip = 0)
    {
        turn = (turn + (1 + skip) * turnDirection);
        int n = activePlayers.Count;
        if (turn < 0)
        {
            while (turn < 0)
            {
                turn += n;
            }
        }

        if (turn >= n)
        {
            while (turn >= n)
            {
                turn -= n;
            }
        }
    }



    public void nextTurn()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (i == activePlayers[turn])
            {
                players[i].RpcTurnStart(true, server_drawCards, server_passCard);
            }
            else
            {
                players[i].RpcTurnStart(false, server_drawCards, server_passCard);
            }
        }

        StopCoroutine(turnTimeOut());
        turnTimeOutCounter++;
        StartCoroutine(turnTimeOut());
    }

    public void selectSwap(Hand playerHand)
    {
        UIController.current.SelectSwapBox.SetActive(false);
        int theTarget = -1;
        if(playerHand != null)
        {
            theTarget = playerHand.myPlayer.myID;
        }
        LocalPlayer.selectSwap(theTarget);
        return;
    }

   /* int sourcePlayer;
    int destinationPlayer;*/


    /*[Command(requiresAuthority = false)]
    void CmdSetSourceAndDestination(int source, int destination)
    {

        /*players[source].RpcUpdateCardShare();
        players[destination].RpcUpdateCardShare();* /
        /*sourcePlayer = source;
        destinationPlayer = destination;* /
        if(destination < 0)
        {
            destination = activePlayers[Random.Range(0, activePlayers.Count)];
        }

        for (int i = 0; i < activePlayers.Count; i++)
        {
            players[activePlayers[i]].RpcUpdateCardShare();
        }
        //waitingToSwapCards = true;
        StartCoroutine(swapCards(source, destination));
    }*/

    IEnumerator setPlayerScores(int winner, int remaining)
    {
        for (int safetycounter = 0; safetycounter < 100; safetycounter++)
        {
            bool waitingForPlayerCards = false;

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].cardShareUpdated == false)
                {
                    waitingForPlayerCards = true;
                    break;
                }
            }


            if (!waitingForPlayerCards)
            {
                for(int i = 0; i < activePlayers.Count; i++)
                {
                    if(players[activePlayers[i]].myID != winner)
                    {
                        int tempscore = 0;
                        for(int ii = 0; ii < players[activePlayers[i]].cardShare.Count; ii++)
                        {
                            if(players[activePlayers[i]].cardShare[ii] < 10)
                            {
                                tempscore += players[activePlayers[i]].cardShare[ii];
                            }else if(players[activePlayers[i]].cardShare[ii] < 13)
                            {
                                tempscore += 20;
                            }
                            else
                            {
                                tempscore += 50;
                            }
                        }
                        //playerScores[activePlayers[i]] = tempscore;
                        playerScores[winner] += tempscore;
                    }
                }
                break;
            }


            yield return new WaitForSeconds(0.1f);
        }

        if (remaining <= 1 || !settings[15])
        {
            GameInProgress = false;
            RpcEndGame(winner, firstWinner, playerScores);
        }

    }

    IEnumerator swapCardsAround()
    {
        waitingToSwapCardsAround = true;
        for (int safetycounter = 0; safetycounter < 30; safetycounter++)
        {
            bool waitingForPlayerCards = false;

            for (int i = 0; i < activePlayers.Count; i++)
            {
                if (players[activePlayers[i]].cardShareUpdated == false)
                {
                    waitingForPlayerCards = true;
                    break;
                }
            }


            if (!waitingForPlayerCards || safetycounter >= 29)
            {
                for (int i = 0; i < activePlayers.Count; i++)
                {
                    int ti = normaliseForIndex(i - turnDirection, activePlayers.Count);
                    players[activePlayers[i]].RpcSetCards(players[activePlayers[ti]].cardShare, activePlayers[ti]);
                }
                break;
            }


            yield return new WaitForSeconds(0.1f);
        }

        waitingToSwapCardsAround = false;
        nextTurn();
    }
    IEnumerator swapCards(int ss, int dd)
    {
        waitingToSwapCards = true;
        for (int safetycounter = 0; safetycounter < 30; safetycounter++)
        {
            if (waitingToSwapCards)
            {
                bool waitingForPlayerCards = false;


                for (int i = 0; i < activePlayers.Count; i++)
                {
                    if (players[activePlayers[i]].cardShareUpdated == false)
                    {
                        waitingForPlayerCards = true;
                        break;
                    }
                }



                if (!waitingForPlayerCards || safetycounter >= 29)
                {
                    players[ss].RpcSetCards(players[dd].cardShare, dd);
                    players[dd].RpcSetCards(players[ss].cardShare, ss);

                    waitingToSwapCards = false;
                    break;
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        waitingToSwapCards = false;
        nextTurn();
    }

    int normaliseForIndex(int index, int count)
    {
        int newIndex = index;
        while (newIndex >= count)
        {
            newIndex -= count;
        }
        while (newIndex < 0)
        {
            newIndex += count;
        }
        return newIndex;
    }
    string firstWinner = "[if you see this, something went wrong]";
    bool firstWinnerSet = false;

    [SyncVar]
    public bool firstWinnerSetInClients = false;


    [ClientRpc]
    public void RpcEndGame(int playerid, string _firstwinner, List<int> _playerScores)
    {
        


        UIController.current.winText.text = _firstwinner + " is victorious!";

        for (int i = 0; i < UIController.current.playerWinStatList.Count; i++)
        {

            /*UIController.current.playerWinStatList[i].text = players[i].playerName + ": " + players[i].wins.ToString();*/
            bool playerfound = false;

            for(int ii = 0; ii < players.Count; ii++){ 
                if(players[ii].myID == i)
                {
                    if(settings[16] && i == playerid)
                    {
                        players[ii].hand.swapButton.SetActive(false);
                    }
                    playerfound = true;
                    UIController.current.playerWinStatList[i].text = players[ii].playerName + ": " + _playerScores[i].ToString();
                }
            }

            

            if(!playerfound){

                UIController.current.playerWinStatList[i].text = "-";
            }
        }
        UIController.current.WinScreen.SetActive(true);
        
        

    }

    [ClientRpc]
    public void RpcSetColourIndicator(Color t, int td)
    {
        Sceneobjects.current.WildColourIndicator.color = t;
        Sceneobjects.current.turnDirectionIndicator.localEulerAngles = new Vector3(90 - 90*td, 0, 0);
    }

    [Command(requiresAuthority = false)]
    public void CmdClearBottomCardsOnTable()
    {
        RpcClearBottomCardsOnTable();
    }

    [ClientRpc]
    public void RpcClearBottomCardsOnTable()
    {
        if (tableCardsR.Count >= 4)
        {

            Destroy(tableCardsR[0]);
            tableCardsR.RemoveAt(0);
            //for(int i = 0; i < 1; i++)
            /*{
                int valueInConcern = cardsOnTable[cardsOnTable.Count - 1];
                cardsOnTable.RemoveAt(cardsOnTable.Count - 1);
                for (int ii = 0; ii < tableCardsR.Count; ii++)
                {
                    if (int.Parse(tableCardsR[ii].name) == valueInConcern)
                    {
                        Destroy(tableCardsR[ii]);
                        tableCardsR.RemoveAt(ii);
                        break;
                    }
                }
            }*/
        }
    }

    //[Command(ignoreAuthority = true)]
    public void selectWildColour(int colour)
    {
        LocalPlayer.selectColour(colour);
        UIController.current.SelectColourBox.SetActive(false);
    }



    public Player getPlayerWithId(int theid)
    {
        for(int i = 0; i < players.Count; i++)
        {
            if(players[i].myID == theid)
            {
                return players[i];
            }
        }
        return null;
    }

    public int getGlobalPlayerId(int theid)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].myID == theid)
            {
                return players[i].myID;
            }
        }
        return -1;
    }

    //Helper methods
    public int getCardNumber(int t)
    {
        int num = 0;

        {
            num = t % 15;
        }
        return num;
    }

    public int getCardColour(int t)
    {
        int colour;

        {
            colour = ((int)(t / 15));
        }
        return colour;
    }

    public int getCardValue(int colour, int number)
    {
        return 15 * colour + number;
    }

}
