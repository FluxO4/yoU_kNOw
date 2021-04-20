using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    [SyncVar]
    public bool myturn = false;
    [SyncVar]
    public int myID = -1;
    [SyncVar]
    public int numCards = 0;

    //Buffers
    /*[SyncVar]
    public int needToDrawBuffer = 0;
    [SyncVar]
    public int cardOnTableBuffer = -1;*/
    
    /*[SyncVar]
    public bool TurnPassable = true;
    [SyncVar]
    public bool AutoAddCards = false;
    [SyncVar]
    public bool AnyOfJoinInMayMatch = false;*/

    public Image myNameTag;
    public Text myNameText;

    [SyncVar]
    public string playerName;
    [SyncVar]
    public bool isReady;

    int passDraw = 0;
    int skip = 0;
    int reverse = 0;
    int cardsDrawn = 0;


    //public bool hasFinished = false;

    bool selectionEnabled = true;

    bool passAvailable = false;

    [SerializeField]
    Transform _hand;
    Vector3 ogHandPosition;
    public Transform hand
    {
        get
        {
            return _hand;
        }
        set
        {
            _hand = value;
            ogHandPosition = _hand.position;
        }
    }

    List<Card> myCards = new List<Card>();
    List<Card> launchList = new List<Card>();

    //AT the start, we send our presence to the server along with our name
    private void Start()
    {
        if (!isLocalPlayer)
            return;

        CmdReady(UIController.current.playerNameField.text);
    }

    [Command]
    void CmdReady(string playername)
    {
        if (string.IsNullOrEmpty(playername) || playername == "RandomName")
        {
            //playerName = "PLAYER " + Random.Range(1, 99);
            playerName = Sceneobjects.current.randomNames[Random.Range(0,Sceneobjects.current.randomNames.Length)];
        }
        else
        {
            playerName = playername;
        }

        isReady = true;
    }

    [Command]
    void CmdIncrementNeedToDraw(int amount)
    {
        Gameplay.current.incrementNeedToDraw(amount);
    }

    [Command]
    void CmdSetNeedToDraw(int amount)
    {
        Gameplay.current.setNeedToDraw(amount);
    }

    [Command]
    void CmdSetCardOnTable(int value)
    {
        Gameplay.current.setCardOnTable(value);
    }


    //Main functions
    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (!myturn)
        {
            return;
        }

        if (!selectionEnabled)
        {
            return;
        }

        /*if (!Gameplay.current.GameInProgress)
        {
            return;
        }*/


        //Raycast to see if we selected a card or clicked on the deck
        if (Input.GetMouseButtonUp(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            {
                if (hit.transform.tag == "card")
                {
                    Card cardproperties = hit.transform.GetComponent<Card>();

                    //If current card is selected
                    if (cardproperties.selected)
                    {
                        PlayCards();
                    }
                    else
                    //Select hit card
                    {
                        if(myCards.Contains(cardproperties))
                        {
                            SelectCard(cardproperties);
                        }
                    }
                }
                else
                if (hit.transform.tag == "Deck" && (Gameplay.current.needToDrawBuffer > cardsDrawn || !passAvailable || Gameplay.current.settings[6]))
                {
                    if (!Gameplay.current.settings[7])
                    {
                        if (checkIfAnyMoveIsLegal())
                        {
                            goto skipthis;
                        }
                    }
                    DeselectAllCards();
                    AddCard(getRandomCard);
                    passAvailable = true;
                    //Gameplay.current.cards.RemoveAt(0);
                    //CmdIncrementNeedToDraw(0);
                    
                    


                    if (Gameplay.current.needToDrawBuffer > cardsDrawn)
                    {
                        //Gameplay.current.needToDraw += -1;
                        cardsDrawn += 1;

                        if (Gameplay.current.settings[1])
                        {
                            if (Gameplay.current.needToDrawBuffer == cardsDrawn)
                            {
                                Pass();
                            }
                        }


                    }



                    //CmdUpdatePlayerNames();
                    CmdAutoSetMyUIName();

                    CmdPlaceNCardsForAllNonLocals(myCards.Count);

                skipthis:;
                    /*SetNeedToDrawText();
                    selectedCards.Clear();

                    CmdUpdateHandCards();*/
                }
            }
            else
            {
                launchList.Clear();
                DeselectAllCards();
            }
        }

        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            hand.position += -hand.right * 0.5f;
        }

        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            hand.position += hand.right * 0.5f;
        }
    }

    public void ResetAllFlags()
    {
        passDraw = 0;
        skip = 0;
        reverse = 0;
        //selectionEnabled = true;
        //passAvailable = false;
    }


    [ClientRpc]
    public void RpcGameStartSetup()
    {
        ResetAllFlags();
        RemoveAllCards();
        myCards.Clear();

        if (isLocalPlayer)
        {
            for (int i = 0; i < Sceneobjects.current.numStartCards; i++)
            {
                AddCard(getRandomCard);
            }
        }
        else
        {
            PlaceNCardsS(Sceneobjects.current.numStartCards);
        }
    }

    public void SendReadyToServer(string playername)
    {
        if (!isLocalPlayer)
            return;

        CmdReady(playername);
    }

    //Hand actions

    void AddCard(int cardValue)
    {
        GameObject tcard = Instantiate(Sceneobjects.current.card, Sceneobjects.current.deck.transform.position, Quaternion.Euler(0, 0, -10));
        tcard.transform.position = Sceneobjects.current.deck.transform.position;
        tcard.transform.SetParent(hand);
        numCards = numCards + 1;
        Card tcardP = tcard.GetComponent<Card>();
        myCards.Add(tcardP);

        //tcardP.resetCard();
        tcardP.Value = cardValue;
        tcard.name = tcardP.Value.ToString();

        RefreshCardPositions();

        //tcardP.offsetIfInHand = tcard.transform.localPosition.x;
    }

    void RemoveCard(Card theCard, bool refreshImmediate = true)
    {
        numCards = numCards - 1;
        theCard.transform.SetParent(null);
        Destroy(theCard.transform.gameObject);
        //theCard.swipeAwayAndDestroy();
        myCards.Remove(theCard);
        if(refreshImmediate)
        RefreshCardPositions();
    }

    public int getRandomCard
    {
        get
        {
            //int temp = Random.Range(0, 59);
            return Gameplay.current.getRandomCard;
        }
    }

    void RemoveAllCards()
    {
        /*for (int ii = 0; ii < hand.childCount; ii++)
        {
            Destroy(hand.GetChild(ii).gameObject);
        }*/
        foreach(Transform child in hand.transform)
        {
            Destroy(child.gameObject);
        }

        numCards = 0;
        myCards.Clear();
    }

    void RefreshCardPositions(float customOffset = 1f)
    {
        int angle = 180;
        if (isLocalPlayer)
        {
            angle = 0;
        }
        numCards = myCards.Count;
        DeselectAllCards();
        for (int ii = 0; ii < myCards.Count; ii++)
        {
            //Vector3 pos = hand.position - hand.right * (customOffset * (ii - (myCards.Count) * 0.5f));
            float xpos = (customOffset * (ii - (myCards.Count - 1) * 0.5f));
            //myCards[ii].transform.position = pos;
            //myCards[ii].transform.Rotate(hand.right, -20);
            //myCards[ii].transform.localRotation = Quaternion.Euler(0, 0, -10 + angle);
            if (isLocalPlayer)
            {
                if (myCards[ii].transform.localPosition.z < 0.5f)
                {
                    myCards[ii].smoothMove(new Vector3(xpos, 0, 0), new Vector3(0, 0, -10 + angle));
                }
                else
                {
                    myCards[ii].smoothMove(new Vector3(xpos, 0, 0), new Vector3(0, 0, -10 + angle), true);
                }
            }
            else
            {
                myCards[ii].smoothMove(new Vector3(xpos, 0, 0), new Vector3(0, 0, -10 + angle));
            }
            
            myCards[ii].offsetIfInHand = xpos;

        }
    }

    void PlaceNCards(int number)
    {
        //RemoveAllCards();
        if (number > myCards.Count)
        {
            for (int i = 0; i < number - myCards.Count; i++)
            {
                GameObject tcard = Instantiate(Sceneobjects.current.card, Sceneobjects.current.deck.transform.position + Vector3.up, Quaternion.Euler(0, 0, -190));
                tcard.transform.SetParent(hand);
                Card tcardP = tcard.GetComponent<Card>();
                myCards.Add(tcardP);

                /*tcardP.Value = getRandomCard;
                tcardP.offsetIfInHand = tcard.transform.localPosition.x;
                tcard.name = tcardP.Value.ToString();*/
            }
        }
        else if(number < myCards.Count)
        {
            for (int i = 0; i < myCards.Count - number; i++)
            {
                RemoveCard(myCards[0], false);
            }
        }
        numCards = number;
        RefreshCardPositions(0.4f);
    }

    void PlaceNCardsS(int number)
    {
        RemoveAllCards();

        for (int i = 0; i < number; i++)
        {
            GameObject tcard = Instantiate(Sceneobjects.current.card, Sceneobjects.current.deck.transform.position + Vector3.up, Quaternion.Euler(0, 0, -190));
            tcard.transform.SetParent(hand);
            Card tcardP = tcard.GetComponent<Card>();
            myCards.Add(tcardP);

            /*tcardP.Value = getRandomCard;
            tcardP.offsetIfInHand = tcard.transform.localPosition.x;
            tcard.name = tcardP.Value.ToString();*/
        }

        numCards = number;
        RefreshCardPositions(0.4f);
    }



    //Card actions
    void PlayCards()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        bool moveLegal = false;

        if (Gameplay.current.settings[4])
        {
            for (int i = 0; i < launchList.Count; i++)
            {
                if (checkMoveLegality(launchList[i]))
                {
                    moveLegal = true;
                    break;
                }
            }
        }
        else
        {
            if (checkMoveLegality(launchList[0]))
            {
                moveLegal = true;
            }
        }

        if (moveLegal)
        {
            ResetAllFlags();
            StartCoroutine(LaunchCards());
            //passAvailable = true;
            myturn = false;
        }
        else
        {
            for (int i = 0; i < launchList.Count; i++)
            {
                launchList[i].shakeCard();
            }
        }
    }

    void LaunchFirstCardInLaunchList()
    {
        Card cardToLaunch = launchList[0];
        launchList.RemoveAt(0);

        Vector3 pos = hand.position - hand.forward + hand.right * (cardToLaunch.offsetIfInHand);
        pos = new Vector3(pos.x, 5, pos.z);

        int cardvalue = cardToLaunch.cardNumber;


        GameObject tcard = Instantiate(Sceneobjects.current.cardR, pos, Quaternion.Euler(0, 0, 0));
        Gameplay.current.tableCardsR.Add(tcard);
        Card tcardP = tcard.GetComponent<Card>();
        tcardP.Value = cardToLaunch.Value;
        tcard.name = cardToLaunch.Value.ToString();
        //Gameplay.current.cardOnTable = cardToLaunch.Value;
        CmdSetCardOnTable(cardToLaunch.Value);
        Rigidbody tcardR = tcard.GetComponent<Rigidbody>();
        tcardR.velocity = -new Vector3(tcardR.position.x, 0, tcardR.position.z) * Time.fixedDeltaTime * Gameplay.current.CardSpeedMultiplier;

        if (cardvalue == 10)
        {
            passDraw += 2;
        }
        else
        if (cardvalue == 11)
        {
            skip += 1;
        }
        else
        if (cardvalue == 12)
        {
            reverse += 1;
        }
        else
        if (cardvalue == 13)
        {
            passDraw += 4;
        }

        CmdLaunchNonLocalCard(cardToLaunch.Value, cardToLaunch.offsetIfInHand, myCards.Count - 1);

        RemoveCard(cardToLaunch);

        if(isLocalPlayer)
        Gameplay.current.CmdClearBottomCardsOnTable();

    }

    public void selectColour(int colour)
    {
        CmdSetCardOnTable(Gameplay.current.getCardValue(colour, Gameplay.current.getCardNumber(Gameplay.current.cardOnTableBuffer)));
        colourSelected = true;
    }

    bool colourSelected = false;
    IEnumerator LaunchCards()
    {
        passDraw = 0;
        selectionEnabled = false;
        int safetyCounter = 0;
        ResetAllFlags();
        while (launchList.Count > 0 || safetyCounter > 20)
        {
            safetyCounter += 1;
            LaunchFirstCardInLaunchList();

            yield return new WaitForSeconds(0.5f);
        }
        launchList.Clear();
        colourSelected = false;
        if (Gameplay.current.getCardNumber(Gameplay.current.cardOnTableBuffer) >= 13)
        {
            UIController.current.SelectColourBox.SetActive(true);
            int counter = 0;
            while(!colourSelected){
                counter++;
                yield return new WaitForSeconds(0.1f);
                if(counter > 100)
                {
                    if (!colourSelected)
                    {
                        Gameplay.current.selectWildColour(Random.Range(0,4));
                    }
                }
            }
        }
        //yield return new WaitForSeconds(1.0f);
        yield return new WaitForEndOfFrame();

        myturn = false;
        Sceneobjects.current.WildColourIndicator.color = Sceneobjects.current.loadingIndicatorColour;

        if (myCards.Count <= 0)
        {
            CmdPassTurnToNext(passDraw, skip, reverse, cardsDrawn, true, playerName);
        }
        else
        {
            CmdPassTurnToNext(passDraw, skip, reverse, cardsDrawn, false, playerName);
        }
        colourSelected = false;
        //selectionEnabled = true;

        //ResetAllFlags();
    }

    [Command]
    void CmdLaunchNonLocalCard(int value, float offset, int nnumcards)
    {
        RpcLaunchNonLocalCard(value, offset, nnumcards);
    }

    [ClientRpc]
    void RpcLaunchNonLocalCard(int value, float offset, int nnumcards)
    {
        if (isLocalPlayer)
        {
            return;
        }

        Vector3 pos = hand.position - hand.forward + hand.right * (offset);
        pos = new Vector3(pos.x, 5, pos.z);

        GameObject tcard = Instantiate(Sceneobjects.current.cardR, pos, Quaternion.Euler(0, 0, 0));
        Gameplay.current.tableCardsR.Add(tcard);
        Card tcardP = tcard.GetComponent<Card>();
        tcardP.Value = value;
        tcard.name = tcardP.Value.ToString();
        CmdSetCardOnTable(value);
        Rigidbody tcardR = tcard.GetComponent<Rigidbody>();
        tcardR.velocity = -new Vector3(tcardR.position.x, 0, tcardR.position.z) * Time.fixedDeltaTime * Gameplay.current.CardSpeedMultiplier;

        PlaceNCards(nnumcards);
    }

    [Command]
    void CmdPlaceNCardsForAllNonLocals(int num)
    {
        RpcPlaceNCardsForAllNonLocals(num);
    }

    [ClientRpc]
    void RpcPlaceNCardsForAllNonLocals(int num)
    {
        if (isLocalPlayer)
        {
            return;
        }
        PlaceNCards(num);
    }


    void SelectCard(Card cardToSelect)
    {
        if (cardToSelect.selected)
        {
            return;
        }

        cardToSelect.selectCard();
        if(!launchList.Contains(cardToSelect))
            launchList.Add(cardToSelect);

        if (!Gameplay.current.settings[2])
        {
            DeselectAllCards();
            PlayCards();
            return;
        }

        for (int i = 0; i < myCards.Count; i++)
        {
            if(myCards[i] != cardToSelect){
                if ((myCards[i].cardNumber >= 13 && !Gameplay.current.settings[3])|| !CheckIfSameValue(cardToSelect, myCards[i]))
                {
                    myCards[i].resetCard();
                    if (launchList.Contains(myCards[i]))
                    {
                        launchList.Remove(myCards[i]);
                    }
                }
            }
        }


    }

    void DeselectAllCards()
    {
        for (int i = 0; i < myCards.Count; i++)
        {
            myCards[i].resetCard();
        }
    }

    //Card-value relationships
    bool CheckIfSameValue(Card myCard, Card cardToCheck)
    {
        if(myCard.cardNumber == cardToCheck.cardNumber)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool CheckIfSameColour(Card myCard, Card cardToCheck)
    {
        if (myCard.colour == cardToCheck.colour)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //Turn actions

    bool checkMoveLegality(Card cardToCheck)
    {

        int lcolour = Gameplay.current.getCardColour(Gameplay.current.cardOnTableBuffer);
        int lvalue = Gameplay.current.getCardNumber(Gameplay.current.cardOnTableBuffer);

        int tcolour = (int)cardToCheck.colour;
        int tvalue = cardToCheck.cardNumber;

        CmdIncrementNeedToDraw(0);
        if (Gameplay.current.needToDrawBuffer > cardsDrawn)
        {
            if(tvalue == 10 || tvalue == 13)
            {
                if(tvalue == lvalue)
                {
                    return true;
                }
            }
        }else
        {
            if(tvalue >= 13)
            {
                return true;
            }
            if(tvalue == lvalue || tcolour == lcolour)
            {
                return true;
            }
        }

        return false;
    }

    public bool checkIfAnyMoveIsLegal()
    {
        for(int i = 0; i < myCards.Count; i++)
        {
            if (checkMoveLegality(myCards[i]))
            {
                return true;
            }
        }
        return false;
    }

    public bool checkIfIHaveDrawCards()
    {
        for (int i = 0; i < myCards.Count; i++)
        {
            if (myCards[0].cardNumber == 10 || myCards[0].cardNumber == 13)
            {
                return true;
            }
        }
        return false;
    }


    public void Pass()
    {
        if (!isLocalPlayer)
            return;
        Gameplay.current.CmdCheckTurn();

        if (!Gameplay.current.settings[5])
            return;
        if (Gameplay.current.needToDrawBuffer <= cardsDrawn && passAvailable)
        {
            CmdSetNeedToDraw(0);
            CmdPlaceNCardsForAllNonLocals(myCards.Count);
            CmdPassTurnToNext(passDraw, skip, reverse, cardsDrawn, false, playerName);
            ResetAllFlags();
            selectionEnabled = false;
        }
    }

    [Command]
    public void CmdPassTurnToNext(int _passDraw, int _skip, int _reverse, int _cardsDrawn, bool Iwin, string myname)
    {
        Gameplay.current.PassTurnToNextPlayer(_passDraw, _skip, _reverse, _cardsDrawn, Iwin, myname);
    }


    [ClientRpc]
    public void RpcTurnStart(bool isitmyturn, int tempNeedToDraw)
    {
        Gameplay.current.needToDrawBuffer = tempNeedToDraw;

        if (isitmyturn)
        {
            myturn = true;
        }
        else
        {
            myturn = false;
        }

        if (isLocalPlayer)
        {
            for (int i = 0; i < myCards.Count; i++)
            {
                myCards[i].resetCard();
            }

            launchList.Clear();

            if (Gameplay.current.settings[0])
            {
                if (myturn)
                {
                    if (Gameplay.current.needToDrawBuffer > 0)
                    {
                        if ( !Gameplay.current.settings[10]  || !checkIfIHaveDrawCards() )
                        {
                            for (int i = 0; i < Gameplay.current.needToDrawBuffer; i++)
                            {
                                AddCard(getRandomCard);
                            }
                            CmdSetNeedToDraw(0);
                            CmdPlaceNCardsForAllNonLocals(numCards);
                        }
                    }
                }
            }

            selectionEnabled = true;
        }
        else
        {
            PlaceNCards(numCards);
        }

        cardsDrawn = 0;
        CmdIncrementNeedToDraw(0);
        passAvailable = false;
        AutoSetMyUIName();
    }

    [ClientRpc]
    public void RpcSetMyTurn(bool isitmyturn)
    {
        if (isitmyturn)
        {
            myturn = true;
            selectionEnabled = true;
        }
        else
        {
            myturn = false;
        }
    }

    //UI actions
    //[Command]
    public void CmdUpdatePlayerNames()
    {
        Gameplay.current.CmdUpdatePlayerNames();
    }

    [Command]
    public void CmdAutoSetMyUIName()
    {
        RpcAutoSetMyUIName();
    }


    [ClientRpc]
    public void RpcAutoSetMyUIName()
    {
        AutoSetMyUIName();
    }

    public void AutoSetMyUIName() {
        if (myturn)
        {
            if (Gameplay.current.needToDrawBuffer > cardsDrawn)
            {
                myNameText.text = playerName + " (+" + (Gameplay.current.needToDrawBuffer - cardsDrawn).ToString() + ")";

            }
            else
            {
                myNameText.text = playerName;

            }
            myNameTag.color = Sceneobjects.current.selectedNameColour;
        }
        else
        {
            myNameText.text = playerName;
            myNameTag.color = Sceneobjects.current.unselectedNameColour;
        }
    }
}


