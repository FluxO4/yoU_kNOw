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
    public List<int> finishedPlayers;

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
    public int[] cardFrequencies = {1,2,2,2,2,2,2,2,2,2,2,2,2,1,1};

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
            int col = Random.Range(0,4);
            int num = Random.Range(0,cardFrequenciesSum+1);
            int cumulative = 0;
            for(int i = 0; i < 15; i++)
            {
                cumulative += cardFrequencies[i];
                if (num <= cumulative)
                {
                    num = i;
                    break;
                }
            }

            return getCardValue(col,num);
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

    int tempNumPlayers = 0;


    //Game options




    //Main methods
    private void Update()
    {
        if (NetworkManager.singleton.isNetworkActive)
        {
            GameReadyCheck();


            if(LocalPlayer == null)
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
            GameInProgress = false;
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

    [Command(ignoreAuthority = true)]
    public void CmdUpdatePlayerNames()
    {
        for(int i = 0; i < players.Count; i++)
        {
            players[i].CmdAutoSetMyUIName();
        }
    }

    [Command(ignoreAuthority = true)]
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


    [Command(ignoreAuthority = true)]
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

    void GameReadyCheck()
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
    }
    void FindLocalPlayer()
    {
        //Check to see if the player is loaded in yet
        if (ClientScene.localPlayer == null)
            return;

        LocalPlayer = ClientScene.localPlayer.GetComponent<Player>();
        //ReadyButtonHandler();
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
        CmdStartGame(_settings, temp);
    }

    int firstcard = -1;

    [Command(ignoreAuthority = true)]
    public void CmdStartGame(List<bool> _settings, List<int> randomCardFrequencies)
    {
        /*if (GameInProgress)
        {
            return;
        }*/
        RpcSetGameUI();

        //turn = Random.Range(0,players.Count-1);

        turnDirection = 1;

        GameInProgress = true;

        RpcSetPlayerHands();

        SetCardFrequencies(randomCardFrequencies);
        RpcSetCardFrequencies(randomCardFrequencies);
        SetSettings(_settings);
        RpcSetSettings(_settings);



        int _firstcard = getCardValue(Random.Range(0,4), Random.Range(0,10));
        firstcard = _firstcard;
        //RpcSetCardOnTableBuffer(cardOnTable);
        RpcDropFirstCard(firstcard);
        RpcSetColourIndicator(Sceneobjects.current.CardColours[getCardColour(firstcard)], turnDirection);

        turn = Random.Range(0, players.Count);
        //turn = (turn + (1 + Random.Range(0, 10) * turnDirection));

       /* int n = players.Count;
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
        }*/
        //Debug.Log("TURN IS " + turn);

        foreach (Player t in players)
        {
            t.myturn = false;

            //t.cardOnTableBuffer = cardOnTable;
            t.RpcGameStartSetup();

            //Debug.Log("Autoaddcards settings is " + settings[0]);
        }

        for (int i = 0; i < players.Count; i++)
        {
            if (i == turn)
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
        

        for(int i = 0; i < tableCardsR.Count; i++)
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
        if (players.Count == 1)
        {
            LocalPlayer.hand = Sceneobjects.current.Hands[0];
            LocalPlayer.myNameTag = Sceneobjects.current.Names[0];
            LocalPlayer.myNameText = Sceneobjects.current.NameTexts[0];
            LocalPlayer.myNameText.text = LocalPlayer.playerName;
        }
        else
        if (players.Count == 2)
        {
            Sceneobjects.current.Names[1].gameObject.SetActive(false);
            Sceneobjects.current.Names[3].gameObject.SetActive(false);

            for (int i = 0; i < 2; i++)
            {
                if (i == myIndex)
                {
                    players[i].hand = Sceneobjects.current.Hands[0];
                    players[i].myNameTag = Sceneobjects.current.Names[0];
                    players[i].myNameText = Sceneobjects.current.NameTexts[0];
                    players[i].myNameText.text = players[i].playerName;
                }
                else
                {
                    players[i].hand = Sceneobjects.current.Hands[2];
                    players[i].myNameTag = Sceneobjects.current.Names[2];
                    players[i].myNameText = Sceneobjects.current.NameTexts[2];
                    players[i].myNameText.text = players[i].playerName;

                }
            }
        }
        else
        if (players.Count == 3)
        {
            Sceneobjects.current.Names[2].gameObject.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                if (i == myIndex)
                {
                    players[i].hand = Sceneobjects.current.Hands[0];
                    players[i].myNameTag = Sceneobjects.current.Names[0];
                    players[i].myNameText = Sceneobjects.current.NameTexts[0];
                    players[i].myNameText.text = players[i].playerName;
                }
                else
                if (i == (myIndex + 1) % 3)
                {
                    players[i].hand = Sceneobjects.current.Hands[1];
                    players[i].myNameTag = Sceneobjects.current.Names[1];
                    players[i].myNameText = Sceneobjects.current.NameTexts[1];
                    players[i].myNameText.text = players[i].playerName;
                }
                else
                if (i == (myIndex + 2) % 3)
                {
                    players[i].hand = Sceneobjects.current.Hands[3];
                    players[i].myNameTag = Sceneobjects.current.Names[3];
                    players[i].myNameText = Sceneobjects.current.NameTexts[3];
                    players[i].myNameText.text = players[i].playerName;
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
                    players[i].myNameTag = Sceneobjects.current.Names[0];
                    players[i].myNameText = Sceneobjects.current.NameTexts[0];
                    players[i].myNameText.text = players[i].playerName;
                }
                else
                if (i == (myIndex + 1) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[1];
                    players[i].myNameTag = Sceneobjects.current.Names[1];
                    players[i].myNameText = Sceneobjects.current.NameTexts[1];
                    players[i].myNameText.text = players[i].playerName;
                }
                else
                if (i == (myIndex + 2) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[2];
                    players[i].myNameTag = Sceneobjects.current.Names[2];
                    players[i].myNameText = Sceneobjects.current.NameTexts[2];
                    players[i].myNameText.text = players[i].playerName;
                }
                else
                if (i == (myIndex + 3) % 4)
                {
                    players[i].hand = Sceneobjects.current.Hands[3];
                    players[i].myNameTag = Sceneobjects.current.Names[3];
                    players[i].myNameText = Sceneobjects.current.NameTexts[3];
                    players[i].myNameText.text = players[i].playerName;
                }
            }
        }

    }

    //Turn methods
    [Server]
    public void PassTurnToNextPlayer(int drawCards = 0, int skip = 0, int reverse = 0, int passCard = -1, bool Iwin = false)
    {


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
                if (players.Count == 2)
                {
                    skip = 1;
                }
            }
        }
        int prevturn = turn;

        turn = (turn + (1 + skip) * turnDirection);

        if (Iwin)
        {
            RpcEndGame(prevturn);
            //return;
        }

        if (!GameInProgress)
        {
            return;
        }



        /*if (settings[1])
        {
            if (drawCards > 0)
            {
                turn += 1;
            }
        }*/

        int n = players.Count;
        if (turn < 0)
        {
            while(turn < 0)
            {
                turn += n;
            }
        }

        if (turn >= n)
        {
            while (turn >=n)
            {
                turn -= n;
            }
        }

        for(int i = 0; i < players.Count; i++)
        {
            //players[i].needToDrawBuffer = needToDraw;
            if(i == turn)
            {
                players[i].RpcTurnStart(true, drawCards, passCard);
            }
            else
            {
                players[i].RpcTurnStart(false, drawCards, passCard);
            }
        }
        //CmdUpdatePlayerNames();

    }

    [Command]
    public void CmdCheckTurn()
    {
        for(int i = 0; i < players.Count; i++)
        {
            if (i == turn)
            {
                players[i].RpcSetMyTurn(true);
            }
            else
            {
                players[i].RpcSetMyTurn(false);
            }
        }
    }


    [ClientRpc]
    public void RpcEndGame(int turnid)
    {
        
        
        GameInProgress = false;


        UIController.current.winText.text = players[turn].playerName + " is victorious!";
        players[turn].wins += 1;
        for(int i = 0; i < UIController.current.playerWinStatList.Count; i++)
        {
            if (i < players.Count)
            {
                UIController.current.playerWinStatList[i].text = players[i].playerName + ": " + players[i].wins.ToString();
            }
            else
            {

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

    [Command(ignoreAuthority = true)]
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
