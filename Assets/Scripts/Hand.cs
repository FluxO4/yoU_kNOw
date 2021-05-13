using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hand : MonoBehaviour
{
    public GameObject swapButton;
    public GameObject firstPlaceIcon;
    public GameObject secondPlaceIcon;
    public GameObject disconnectedIcon;
    public Text playerName;
    public Image playerNameImage;

    public Player myPlayer;

    public Vector3 position
    {
        get
        {
            return transform.position;
        }
        set
        {
            transform.position = value;
        }
    }

    public Vector3 right
    {
        get
        {
            return transform.right;
        }
        set
        {
            transform.right = value;
        }
    }

    public Vector3 forward
    {
        get
        {
            return transform.forward;
        }
        set
        {
            transform.forward = value;
        }
    }

    public void resetAll()
    {
        firstPlaceIcon.SetActive(false);
        secondPlaceIcon.SetActive(false);
        disconnectedIcon.SetActive(false);
    }

    static public implicit operator Transform(Hand hand)
    {
        return hand.transform;
    }
}
