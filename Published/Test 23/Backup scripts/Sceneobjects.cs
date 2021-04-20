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
        "jklolwut"
    };

    public bool[] defaultSettings =
    {
        false, false, true, false, false, true, true, true, true, false, true
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
