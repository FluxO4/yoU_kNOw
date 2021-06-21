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
    //public List<Player> players;
    public PlayerList players = new PlayerList();
    /*List<int> activePlayers;
    int[] playerScores = new int[4];*/
    public Player turnPlayer;

    public class PlayerList
    {
        public List<Player> theList = new List<Player>();

        public void ResetALL()
        {
            _turn = 0;
            numInactivePlayers = 0;
            for(int i = 0; i < Count; i++)
            {
                theList[i].active = true;
            }
        }

        public int Count
        {
            get
            {
                return theList.Count;
            }
        }

        public void Add(Player toAdd)
        {
            theList.Add(toAdd);
        }

        public void Clear()
        {
            theList.Clear();
        }

        public bool Contains(Player toContain)
        {
            return theList.Contains(toContain);
        }

        public int IndexOf(Player toIndex)
        {
            return theList.IndexOf(toIndex);
        }

        int _turn = 0;
        int turn
        {
            get
            {
                return _turn;
            }
            set
            {
                int t = value;
                while (t >= Count)
                {
                    t = t - Count;
                }
                while (t < 0)
                {
                    t = t + Count;
                }
                _turn = t;
            }
        }

        int numInactivePlayers = 0;
        public int numActivePlayers
        {
            get
            {
                return Count - numInactivePlayers;
            }
            set
            {
                numInactivePlayers = Count - value;
            }
        }

        public Player randomTurnPlayer()
        {
            if (Count <= 0)
            {
                return null;
            }
            turn = Random.Range(0, this.Count - 1);
            return this[turn];
        }

        public Player nextTurnPlayer(int _skip, int _turnDirection)
        {
            if (Count <= 0)
            {
                return null;
            }
            //turn = (turn + (1 + _skip) * _turnDirection);
            for (int i = 0; i <= _skip; i++)
            {
                turn = turn + _turnDirection;
                int safetyCounter = 0;
                while (!this[turn].active && safetyCounter < 50)
                {
                    safetyCounter++;
                    turn = turn + _turnDirection;
                }
            }

            return this[turn];
        }

        public void deactivatePlayer(Player thePlayer)
        {
            if (this.Count <= 0)
            {
                return;
            }
            for (int i = 0; i < this.Count; i++)
            {
                if (this[i] == thePlayer)
                {
                    this[i].active = false;
                    if (numActivePlayers >= 0)
                    {
                        numActivePlayers--;
                    }
                }
            }
        }

        public void RemovePlayer(Player playerToRemove)
        {
            if (playerToRemove.active)
            {
                if (numActivePlayers >= 0)
                {
                    numActivePlayers--;
                }
            }
            playerToRemove.active = false;
            theList.Remove(playerToRemove);
        }

        public void RemovePlayer(int playerToRemove)
        {
            if (this[playerToRemove].active)
            {
                if (numActivePlayers >= 0)
                {
                    numActivePlayers--;
                }
            }
            theList.RemoveAt(playerToRemove);
        }

        public Player randomActivePlayer()
        {
            List<Player> temp = new List<Player>();
            for (int i = 0; i < Count; i++)
            {
                if (this[i].active)
                {
                    temp.Add(this[i]);
                }
            }

            return temp[Random.Range(0, temp.Count)];

        }

        public List<int> playerScores()
        {
            List<int> temp = new List<int>();
            for (int i = 0; i < Count; i++)
            {
                temp.Add(this[i].score);
            }
            return temp;
        }

        public List<Player> activePlayers()
        {
            
            {
                List<Player> temp = new List<Player>();
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].active)
                    {
                        temp.Add(this[i]);
                    }
                }
                return temp;
            }
        }

        public Player this[int key]
        {
            get => theList[key];
            set => theList[key] = value;
        }

        public PlayerList()
        {
        }
    }


    //public Player 

    void CheckPlayers()
    {
        List<Player> tempPlayers = new List<Player>();

        foreach (KeyValuePair<uint, NetworkIdentity> kvp in NetworkIdentity.spawned)
        {
            Player comp = kvp.Value.GetComponent<Player>();

            //Add if new
            if (comp != null)
            {
                tempPlayers.Add(comp);
            }
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (!tempPlayers.Contains(players[i]))
            {
                players.RemovePlayer(i);
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


    #region Settings Variables and Methods

    //Settings
    public List<bool> settings = new List<bool>();

    // ------ SETTINGS SET ------
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

    [Command(requiresAuthority = false)]
    public void CmdSettingsChangeEvent()
    {
        Debug.Log("Cmd Settings Change Event called");
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
        Debug.Log("RPC Settings Change Event called");

        UIController.current.settingsChangeConsequence(tempSettings, tempProbabilities);
    }


    public bool settingsUpdated = false;

    //------------End of settings
    #endregion





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

    //turn
    int server_drawCards = 0;
    int server_passCard = 0;

    int winOrderCounter = 0;

    [SyncVar]
    public int turnDirection = 1; //1 is clockwise



    private void Update()
    {
        if (NetworkManager.singleton.isNetworkActive)
        {
            if (!isServer)
            {
                if (!settingsUpdated)
                {
                    CmdSettingsChangeEvent();
                    settingsUpdated = true;
                }
            }

        }
        else
        {
            if (GameInProgress)
            {
                UIController.current.DisconnectedBox.SetActive(true);
                GameInProgress = false;

                //Clean up
                //GameInProgress = false;
                settingsUpdated = false;
                LocalPlayer = null;
                players.Clear();
                //cardOnTable = -1;
                //turn = 0;
                //needToDraw = 0;
                turnDirection = 1;

            }

            Debug.Log("Disconnected I think");
        }
    }

    IEnumerator playerChecks()
    {
        for (; ; )
        {
            if (NetworkManager.singleton.isNetworkActive)
            {
                if (!GameInProgress)
                    CheckPlayers();


                if (LocalPlayer == null)
                {
                    FindLocalPlayer();
                }
            }



            yield return new WaitForSeconds(1);
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





    //Initialisation methods

    private void Start()
    {
        Physics.gravity = new Vector3(0, -20, 0);
        settingsUpdated = false;
        LocalPlayer = null;
        //players = new PlayerList();
        players.Clear();
        //turn = 0;
        turnDirection = 1;

        StartCoroutine(playerChecks());
    }



    #region Game Start Methods

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

        CmdStartGame(_settings, temp);
    }

    int firstcard = -1;

    [Command(requiresAuthority = false)]
    public void CmdStartGame(List<bool> _settings, List<int> randomCardFrequencies)
    {

        RpcSetGameUI();

        players.ResetALL();
        //turnTimeOutCounter = 0;

        turnDirection = 1;

        GameInProgress = true;

        winOrderCounter = 0;

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

        //turn = Random.Range(0, activePlayers.Count);
        turnPlayer = players.randomTurnPlayer();
        //firstWinnerSetInClients = false;


        firstWinnerSet = false;

        for (int i = 0; i < players.Count; i++)
        {
            players[i].serverCards.Clear();
            players[i].RpcGameStartSetup();
            players[i].active = true;
            for (int ii = 0; ii < Sceneobjects.current.numStartCards; ii++)
            {
                players[i].serverCards.Add(getRandomCard);
            }
            players[i].RpcSetCards(players[i].serverCards, -1);

            if (players[i] == turnPlayer)
            {
                players[i].RpcTurnStart(true, 0, firstcard);
            }
            else
            {
                players[i].RpcTurnStart(false, 0, firstcard);
            }
        }


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

    }

    [ClientRpc]
    public void RpcSetPlayerHands()
    {

        int myIndex = players.IndexOf(LocalPlayer);

        if (players.Count == 1)
        {
            LocalPlayer.hand = Sceneobjects.current.Hands[0];
        }
        else
        if (players.Count == 2)
        {
            for (int i = 0; i < 2; i++)
            {
                if (i == myIndex)
                {
                    players[i].hand = Sceneobjects.current.Hands[0];
                }
                else
                {
                    players[i].hand = Sceneobjects.current.Hands[2];
                }
            }
        }
        else
        if (players.Count == 3)
        {
            for (int i = 0; i < 3; i++)
            {
                if (i == myIndex)
                {
                    players[i].hand = Sceneobjects.current.Hands[0];
                }
                else
                if (i == (myIndex + 1) % 3)
                {
                    players[i].hand = Sceneobjects.current.Hands[1];
                }
                else
                if (i == (myIndex + 2) % 3)
                {
                    players[i].hand = Sceneobjects.current.Hands[3];
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
                }
                else
                if (i == (myIndex + 1) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[1];
                }
                else
                if (i == (myIndex + 2) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[2];
                }
                else
                if (i == (myIndex + 3) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[3];
                }
            }
        }

        for (int i = 0; i < players.Count; i++)
        {
            players[i].hand.swapButton.SetActive(true);
            players[i].hand.resetAll();
        }

    }

    #endregion



    //Turn methods

    [Server]
    public void PassTurnToNextPlayer(int drawCards = 0, int skip = 0, int reverse = 0, int passCard = -1, int swapTarget = -1, bool passed = false, bool iwin = false, List<int> cards = null)
    {
        server_drawCards = drawCards;
        server_passCard = passCard;

        /*turnPlayer.serverCards.Clear();

        for(int i = 0; i < cards.Count; i++)
        {
            turnPlayer.serverCards.Add(cards[i]);
        }*/

        turnPlayer.serverCards = new List<int>(cards);

        Debug.Log(reverse);
        
        if(reverse % 2 == 1)
        {
            turnDirection = -turnDirection;
        }

        /*for (int iii = 0; iii < reverse; i++)
        {
            turnDirection = -turnDirection;
        }*/

        Color temp = Sceneobjects.current.CardColours[getCardColour(passCard)];
        RpcSetColourIndicator(temp, turnDirection);

        if (skip > 0)
        {
            if (settings[12])
            {
                if (players.numActivePlayers == 2 || players.Count == 2)
                {
                    skip = 1;
                }
            }
        }

        if (reverse > 0)
        {
            if (settings[13])
            {
                if (players.numActivePlayers == 2 || players.Count == 2)
                {
                    skip = 1;
                    
                }
            }
        }

        if (iwin)
        {
            //Player turnPlayer = players[activePlayers[turn]];
            Player winner = turnPlayer;
            players.deactivatePlayer(winner);
            winner.active = false;

            /*int winner = activePlayers[prevturn];
            activePlayers.Remove(winner);*/
            int remaining = players.Count - players.numActivePlayers;
            //players[winner].hasFinished = true;
            winOrderCounter++;
            winner.winOrder = winOrderCounter;


            if (!firstWinnerSet)
            {
                firstWinner = winner.playerName;
                //players[winner].hand.firstPlaceIcon.SetActive(true);
                firstWinnerSet = true;
            }

            if (!settings[22])
            {
                winner.score += players.Count - players.numActivePlayers;
            }
            else
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] != winner && players[i].active)
                    {
                        int tempscore = 0;
                        for (int ii = 0; ii < players[i].serverCards.Count; ii++)
                        {
                            int cardnum = getCardNumber(players[i].serverCards[ii]);
                            if (cardnum < 10)
                            {
                                tempscore += cardnum;
                            }
                            else if (cardnum < 13)
                            {
                                tempscore += 20;
                            }
                            else
                            {
                                tempscore += 50;
                            }
                        }
                        winner.score += tempscore;
                    }
                }
            }
            if (remaining <= 1 || !settings[15])
            {
                GameInProgress = false;
                RpcEndGame(firstWinner, players.playerScores());
            }
        }

        if (!GameInProgress)
        {
            return;
        }

        if (settings[17] && !passed && getCardNumber(passCard) == 0)
        {
            //StartCoroutine(swapCardsAround());
            List<Player> tempActivePlayers = players.activePlayers();
            List<List<int>> tttempCards = new List<List<int>>();

            for (int i = 0; i < tempActivePlayers.Count; i++)
            {
                tttempCards.Add(new List<int>(tempActivePlayers[i].serverCards));
            }

            for (int i = 0; i < tempActivePlayers.Count; i++)
            {
                int ti = normaliseForIndex(i - turnDirection, tempActivePlayers.Count);
                tempActivePlayers[i].RpcSetCards(tttempCards[ti], tempActivePlayers[ti].myID);
            }

        }
        else if (settings[16] && getCardNumber(passCard) == 7 && players.numActivePlayers > 1 && !passed)
        {
            if (swapTarget == -1)
            {
                swapTarget = players.randomActivePlayer().myID;
            }

            //StartCoroutine(swapCards(turnPlayer.myID, swapTarget));
            List<int> tempCards = new List<int>(players[swapTarget].serverCards);

            players[swapTarget].RpcSetCards(turnPlayer.serverCards, turnPlayer.myID);

            turnPlayer.RpcSetCards(tempCards, swapTarget);
            
        }

        {
            nextTurn(skip);
        }
    }


    public void nextTurn(int skips)
    {
        turnPlayer = players.nextTurnPlayer(skips, turnDirection);
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == turnPlayer)
            {
                players[i].RpcTurnStart(true, server_drawCards, server_passCard);
            }
            else
            {
                players[i].RpcTurnStart(false, server_drawCards, server_passCard);
            }
        }

        /*StopCoroutine(turnTimeOut());
        turnTimeOutCounter++;
        StartCoroutine(turnTimeOut());*/
    }

    public void selectSwap(Hand playerHand)
    {
        UIController.current.SelectSwapBox.SetActive(false);
        int theTarget = -1;
        if (playerHand != null)
        {
            theTarget = playerHand.myPlayer.myID;
        }
        LocalPlayer.selectSwap(theTarget);
        return;
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

    /*[SyncVar]
    public bool firstWinnerSetInClients = false;*/


    [ClientRpc]
    public void RpcEndGame(string _firstwinner, List<int> _playerScores)
    {



        UIController.current.winText.text = _firstwinner + " is victorious!";

        /*for (int i = 0; i < UIController.current.playerWinStatList.Count; i++)
        {


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
        UIController.current.WinScreen.SetActive(true);*/

        for (int i = 0; i < players.Count; i++)
        {
            for (int ii = 0; ii < _playerScores.Count; ii++)
            {
                if (players[i].myID == ii)
                {
                    players[i].hand.winText.text = players[i].playerName + ": " + _playerScores[ii].ToString();
                }
            }
        }

        UIController.current.WinScreen.SetActive(true);


    }

    [ClientRpc]
    public void RpcSetColourIndicator(Color t, int td)
    {
        Sceneobjects.current.WildColourIndicator.color = t;
        Sceneobjects.current.turnDirectionIndicator.localEulerAngles = new Vector3(90 - 90 * td, 0, 0);
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
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].myID == theid)
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


        return t % 15;
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


