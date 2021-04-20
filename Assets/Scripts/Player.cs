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
    [SyncVar]
    public int wins = 0;



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
    //int needToDraw = 0;
    int passCard = -1;

    int numCardDrawn = 0;
    int numCardDrawnWithoutObligation = 0;


    //public bool hasFinished = false;

    bool selectionEnabled = true;

    //bool passAvailable = false;

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

    bool handMoved = false;
    Vector3 ogogHandPosition;

    //Main functions
    void Update()
    {
        if (!isLocalPlayer)
            return;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            hand.position += -hand.right * Time.deltaTime;
            if (!handMoved)
            {
                ogogHandPosition = hand.position;
                handMoved = true;
            }
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            hand.position += hand.right * Time.deltaTime;
            if (!handMoved)
            {
                ogogHandPosition = hand.position;
                handMoved = true;
            }
        }

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
                    if (passDraw <= 0 || Gameplay.current.settings[5]) {
                        Card cardproperties = hit.transform.GetComponent<Card>();

                        //If current card is selected
                        if (cardproperties.selected)
                        {
                            PlayCards();
                        }
                        else
                        //Select hit card
                        {
                            if (myCards.Contains(cardproperties))
                            {
                                SelectCard(cardproperties);
                            }
                        }
                    }
                }
                else
                if (hit.transform.tag == "Deck")
                {
                    if(passDraw <= 0 && Gameplay.current.settings[11])
                    if(Gameplay.current.settings[18] || Gameplay.current.settings[19] || Gameplay.current.settings[20])
                    {
                        int drawlimit = 5;
                        if (Gameplay.current.settings[18])
                        {
                            drawlimit = 1;
                        }else
                            if (Gameplay.current.settings[19])
                        {
                            drawlimit = 5;
                        }
                        else
                            if (Gameplay.current.settings[20])
                        {
                            drawlimit = 10;
                        }

                        if(numCardDrawnWithoutObligation >= drawlimit)
                        {
                            return;
                        }
                        
                    }


                    if (!Gameplay.current.settings[10])
                    {
                        if (checkIfAnyMoveIsLegal())
                        {
                            goto skipthis;
                        }
                    }


                    DeselectAllCards();
                    AddCard(getRandomCard);
                    numCardDrawn++;
                    if(numCardDrawn > 0) {
                        if(Gameplay.current.settings[11])
                            if(!UIController.current.passButton.activeSelf)
                                UIController.current.passButton.SetActive(true);
                    }


                    if (passDraw > 0)
                    {
                        //Gameplay.current.needToDraw += -1;
                        passDraw -= 1;
                        if (passDraw < 0) passDraw = 0;

                        if (passDraw <= 0)
                        {
                            if (Gameplay.current.settings[9])
                            {
                                Pass();
                            }
                        }
                    }
                    else
                    {
                        numCardDrawnWithoutObligation++;
                    }



                    //CmdUpdatePlayerNames();
                    Gameplay.current.CmdUpdatePlayerNamesWithDC(passDraw);

                    CmdPlaceNCardsForAllNonLocals(myCards.Count);

                skipthis:;
                    /*SetNeedToDrawText();
                    selectedCards.Clear();

                    CmdUpdateHandCards();*/
                }
                else
                {
                    launchList.Clear();
                    DeselectAllCards();
                }
            }
            else
            {
                launchList.Clear();
                DeselectAllCards();
            }
        }


    }




    [ClientRpc]
    public void RpcGameStartSetup()
    {
        RemoveAllCards();
        myCards.Clear();
        skip = 0;
        reverse = 0;
        numCardDrawn = 0;
        numCardDrawnWithoutObligation = 0;
        if(handMoved)
            hand.position = ogogHandPosition;

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

        if (Gameplay.current.settings[14])
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
            skip = 0;
            reverse = 0;
            StartCoroutine(LaunchCards());

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
        passCard = cardToLaunch.Value;
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
        passCard = Gameplay.current.getCardValue(colour, Gameplay.current.getCardNumber(passCard));
        colourSelected = true;
    }

    bool colourSelected = false;
    IEnumerator LaunchCards()
    {
        selectionEnabled = false;
        int safetyCounter = 0;
        while (launchList.Count > 0 || safetyCounter > 20)
        {
            safetyCounter += 1;
            LaunchFirstCardInLaunchList();

            yield return new WaitForSeconds(0.5f);
        }
        launchList.Clear();
        colourSelected = false;
        if (Gameplay.current.getCardNumber(passCard) >= 13)
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
            CmdPassTurnToNext(passDraw, skip, reverse, passCard, true);
        }
        else
        {
            CmdPassTurnToNext(passDraw, skip, reverse, passCard, false);
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
        passCard = value;
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

        /*if (!Gameplay.current.settings[5])
        {
            DeselectAllCards();
            PlayCards();
            return;
        }*/

        for (int i = 0; i < myCards.Count; i++)
        {
            if(myCards[i] != cardToSelect){
                if ((myCards[i].cardNumber >= 13 && !Gameplay.current.settings[6])|| !CheckIfSameValue(cardToSelect, myCards[i]))
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

        int lcolour = Gameplay.current.getCardColour(passCard);
        int lvalue = Gameplay.current.getCardNumber(passCard);

        int tcolour = (int)cardToCheck.colour;
        int tvalue = cardToCheck.cardNumber;

        if (passDraw > 0)
        {
            if (Gameplay.current.settings[7])
            {
                return false;
            }
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

        if (passDraw <= 0 && numCardDrawn >= 1)
        {
            CmdPlaceNCardsForAllNonLocals(myCards.Count);
            CmdPassTurnToNext(passDraw, skip, reverse, passCard, false);
            selectionEnabled = false;
        }
    }

    [Command]
    public void CmdPassTurnToNext(int _passDraw, int _skip, int _reverse, int _passCard, bool Iwin)
    {
        Gameplay.current.PassTurnToNextPlayer(_passDraw, _skip, _reverse, _passCard, Iwin);
    }


    [ClientRpc]
    public void RpcTurnStart(bool isitmyturn, int tempNeedToDraw, int cardPassed)
    {
        passDraw = tempNeedToDraw;
        passCard = cardPassed;
        skip = 0;
        reverse = 0;
        numCardDrawn = 0;
        numCardDrawnWithoutObligation = 0;

        UIController.current.passButton.SetActive(false);

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

            if (Gameplay.current.settings[8])
            {
                if (myturn)
                {
                    if (passDraw > 0)
                    {
                        if ( !Gameplay.current.settings[7]  || !checkIfIHaveDrawCards() )
                        {
                            for (int i = 0; i < passDraw; i++)
                            {
                                AddCard(getRandomCard);
                            }
                            passDraw = 0;
                            CmdPlaceNCardsForAllNonLocals(numCards);
                        }
                    }
                }
            }

            selectionEnabled = true;
            //passAvailable = false;
            //Debug.Log("passAvailable changed to FALSO");

        }
        else
        {
            PlaceNCards(numCards);
        }

        
        AutoSetMyUIName(passDraw);
        Gameplay.current.CmdUpdatePlayerNames();
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
    [Command(ignoreAuthority = true)]
    public void CmdAutoSetMyUIName()
    {
        RpcAutoSetMyUIName(passDraw);
    }


    [ClientRpc]
    public void RpcAutoSetMyUIName(int needToDraw)
    {
        AutoSetMyUIName(needToDraw);
    }

    public void AutoSetMyUIName(int needToDraw) {
        if (myturn)
        {
            if (needToDraw > 0)
            {
                myNameText.text = playerName + " (+" + (needToDraw).ToString() + ")";

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


