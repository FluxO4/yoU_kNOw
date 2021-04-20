using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class Sceneobjects : ControllerBase<Sceneobjects>
{
    public int handPointer = 1;

    public int numStartCards = 7;

    public NetworkManager netManager;

    public Color unselectedNameColour;
    public Color selectedNameColour;

    public List<Color> CardColours;

    public Color loadingIndicatorColour;

    public SpriteRenderer WildColourIndicator;

    public GameObject colorIndicator;


    public List<Transform> Hands;
    public List<Image> Names;
    public List<Text> NameTexts;
    /*public List<Transform> MyCards;
    public List<List<Transform>> opponentsCards;
    public List<Transform> CardsOnTable;*/

    public List<Material> cardMaterials;

    public GameObject deck;
    public Transform turnDirectionIndicator;

    public GameObject card;
    public GameObject cardR;

    public GameObject lastThrownCard;

    public string[] randomNames =
    {
        "Shrek",
        "John Wick",
        "n00bmaster69",
        "uwu",
        "Robin Hood",
        "Felix",
        "I'M BATMAN",
        "doofenshmirtz",
        "James Bond",
        "Kung Fu Master",
        "Player 144",
        "nyaaa",
        "eeeevil dude",
        "werewolf",
        "werecow",
        "moomoo",
        "King Julian",
        "jklolwut",
        "kakarot",
        "vegeta",
        "perfect cell"
    };
    public bool[] recommended_ruless =
{
            false, //recommended rules
            false, //UNO un-official rules
            false, //UNO official rules
            false, //UNO house rules
            false, //Choose
            true, //5 = Allow stacking same-numbered cards
            true, //6 = Allow stacking wild cards
            true, //7 = Allow passing draw with draw card stacking
            false, //8 = Draw cards automatically if no comeback
            false, //9 = Skip turn after draw 2 or draw 4
            true, //10 = Allow drawing card if playable card exists
            true, //11 = Enable pass button after one card drawn
            true, //12 = Fix multi-skip in 2 player game
            true, //13 = Reverse skips in 2 player game
            false, //14 = Any card in stack may match
            true, //15 = Continue game after players finish
            false, //16 = Swap hand with someone 7 is played
            false, //17 = Hands move around one player if 0 is played
            false, //18 = Must play or pass after one card drawn
            true, //19 = Must play or pass after 5 cards drawn
            false, //20 = Must play or pass after 10 cards drawn
            true, //21 = Allow draw 4 despite playable card
            false //22 = UNO official score tallying system
    };


    public bool[] uno_unofficial_ruless ={
        false,true,false,false,false,

            false, //5 = Allow stacking same-numbered cards
            false, //6 = Allow stacking wild cards
            true, //7 = Allow passing draw with draw card stacking
            false, //8 = Draw cards automatically if no comeback
            true, //9 = Skip turn after draw 2 or draw 4
            true, //10 = Allow drawing card if playable card exists
            true, //11 = Enable pass button after one card drawn
            false, //12 = Fix multi-skip in 2 player game
            true, //13 = Reverse skips in 2 player game
            false, //14 = Any card in stack may match
            true, //15 = Continue game after players finish
            false, //16 = Swap hand with someone 7 is played
            false, //17 = Hands move around one player if 0 is played
            true, //18 = Must play or pass after one card drawn
            false, //19 = Must play or pass after 5 cards drawn
            false, //20 = Must play or pass after 10 cards drawn
            true, //21 = Allow draw 4 despite playable card
            false //22 = UNO official score tallying system
    };

    public bool[] uno_official_ruless ={
        false,false,true,false,false,

            false, //5 = Allow stacking same-numbered cards
            false, //6 = Allow stacking wild cards
            false, //7 = Allow passing draw with draw card stacking
            true, //8 = Draw cards automatically if no comeback
            true, //9 = Skip turn after draw 2 or draw 4
            false, //10 = Allow drawing card if playable card exists
            true, //11 = Enable pass button after one card drawn
            false, //12 = Fix multi-skip in 2 player game
            true, //13 = Reverse skips in 2 player game
            false, //14 = Any card in stack may match
            false, //15 = Continue game after players finish
            false, //16 = Swap hand with someone 7 is played
            false, //17 = Hands move around one player if 0 is played
            true, //18 = Must play or pass after one card drawn
            false, //19 = Must play or pass after 5 cards drawn
            false, //20 = Must play or pass after 10 cards drawn
            false, //21 = Allow draw 4 despite playable card
            true //22 = UNO official score tallying system
    };

    public bool[] uno_house_ruless ={
        false,false,false,true,false,

            false, //5 = Allow stacking same-numbered cards
            false, //6 = Allow stacking wild cards
            true, //7 = Allow passing draw with draw card stacking
            true, //8 = Draw cards automatically if no comeback
            false, //9 = Skip turn after draw 2 or draw 4
            false, //10 = Allow drawing card if playable card exists
            false, //11 = Enable pass button after one card drawn
            false, //12 = Fix multi-skip in 2 player game
            true, //13 = Reverse skips in 2 player game
            false, //14 = Any card in stack may match
            false, //15 = Continue game after players finish
            true, //16 = Swap hand with someone 7 is played
            true, //17 = Hands move around one player if 0 is played
            true, //18 = Must play or pass after one card drawn
            false, //19 = Must play or pass after 5 cards drawn
            false, //20 = Must play or pass after 10 cards drawn.
            false, //21 = Allow draw 4 despite playable card
            true //22 = UNO official score tallying system
    };

    public int[] defaultCardProbabilities =
    {
        1,2,2,2,2,2,2,2,2,2,2,2,2,1,1
    };

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
