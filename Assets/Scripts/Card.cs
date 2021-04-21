using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardColor {red, blue, green, yellow};

public class Card : MonoBehaviour
{
    //Values: Red 0-12, Blue 13-25, Green 26-38, Yellow 39-41, 42 = Wild, 43 = Wild D4
    //cardnumbers: 0-9, 10 = draw 2, 11 = skip, 12 = reverse, 13 = draw 4

    //Values: Red 0-14, Blue 15-29, Green 30-44, Yellow 45-59
    //cardnumbers: 0-9, 10 = draw 2, 11 = skip, 12 = reverse, 13 = wild draw 4, 14 = wild

    public int indexIfInList = -1;
    public float offsetIfInHand = 0;
    public Collider myCollider;
    public Transform myModel;
    public void setIndexIfInList(int index)
    {
        indexIfInList = index;
    }

    public void resetIndexIfInList()
    {
        indexIfInList = -1;
    }


    int Cvalue = 0;
    public CardColor colour = CardColor.red;
    public int cardNumber = 0;
    public bool selected = false;

    public void resetCard()
    {
        selected = false;
        StopCoroutine(startShaking());
        shaking = false;
        //transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
        smoothMove(new Vector3(destination.x, destination.y, 0));
    }

    public void selectCard()
    {
        selected = true;
        //transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0.25f);
        smoothMove(new Vector3(destination.x, destination.y, 0.4f));
    }

    public int Value
    {
        get
        {
            return Cvalue;
        }
        set
        {
            int t = value;
            //rawValue = t;
            if(t > 59)
            {
                t = 59;
            }
            else if(t < 0)
            {
                t = 0;
            }

            {
                colour = (CardColor)((int)(t/15));

                cardNumber = t % 15;
            }

            Cvalue = t;

            setCardTextureByValue();
            //Debug.Log("Reached set");
            
        }
    }

    public int getScore
    {
        get
        {
            if(cardNumber <= 9)
            {
                return cardNumber;
            }else if(cardNumber <= 12)
            {
                return 20;
            }
            else
            {
                return 50;
            }
        }
    }

    //public int rawValue = 0;

    Renderer myRenderer;
    Material cardMat;
    public Transform cardFront;

    void setCardTextureByValue()
    {
        {
            if (myRenderer == null)
            {
                myRenderer = cardFront.GetComponent<Renderer>();
            }

            myRenderer.material = Sceneobjects.current.cardMaterials[(int)colour];
            if(cardNumber == 13)
            {
                myRenderer.material = Sceneobjects.current.cardMaterials[5];
            }else
            if (cardNumber == 14)
            {
                myRenderer.material = Sceneobjects.current.cardMaterials[4];
            }else
            {

                float offsetX = 0;
                float offsetY = 0.5f;

                int temp = cardNumber;
                if (temp > 6)
                {
                    offsetY = 0f;
                    temp = temp - 7;
                }
                offsetX = temp / (7 * 1.0f);
                //Debug.Log("SetCARD VALUES");
                myRenderer.material.mainTextureOffset = new Vector2(offsetX, offsetY);
            }
        }

        
    }


    // Start is called before the first frame update
    void Start()
    {
        myRenderer = cardFront.GetComponent<Renderer>();
    }

    bool shaking = false;
    IEnumerator startShaking()
    {
        shaking = true;
        float k = 0;
        Vector3 pos = transform.position;
        for(int i = 0; i < 16; i++)
        {
            transform.position = pos + new Vector3(0,0,0.05f*Mathf.Sin(k));
            yield return new WaitForSeconds(0.03f);
            k += 1f;
        }
        transform.position = pos;

        shaking = false;
    }

    public void shakeCard()
    {
        transform.localPosition = destination;
        if (!shaking)
        {
            StartCoroutine(startShaking());
        }
    }

    public void swipeAwayAndDestroy()
    {
        Destroy(myCollider);
        smoothMove(-transform.forward * 5);
        StartCoroutine(startDestroyTimer());
    }

    IEnumerator startDestroyTimer()
    {
        yield return new WaitForSeconds(1f);
        Destroy(transform.gameObject);
    }

    bool moving = false;
    bool goingUpFirst = false;
    float goUpAmount = 2.0f;

    Vector3 destination = new Vector3(0, 0, 0);
    Vector3 finalEulerAngles = new Vector3(0, 0, 0);

    public void smoothMove(Vector3 _destination, Vector3 _finalLocalEulerAngles, bool goUpFirst = false, float goUpAmount = 2.0f)
    {
        if (goUpFirst)
        {
            destination = _destination + Vector3.up * goUpAmount;
            goingUpFirst = true;
        }
        else
        {
            destination = _destination;
        }
        finalEulerAngles = _finalLocalEulerAngles;
        moving = true;
    }

    public void smoothMove(Vector3 _destination, bool goUpFirst = false, float _goUpAmount = 2.0f)
    {
        if (goUpFirst)
        {
            goUpAmount = _goUpAmount;
            destination = _destination + Vector3.up * _goUpAmount;
            goingUpFirst = true;
        }
        else
        {
            destination = _destination;
        }

        moving = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (moving)
        {
            Vector3 newPos = Vector3.Lerp(transform.localPosition, destination, 10 * Time.deltaTime);

            transform.localPosition = newPos;
            //Vector3 newRot = Vector3.Slerp(transform.localEulerAngles, finalEulerAngles, 0.5f);
            transform.localEulerAngles = finalEulerAngles;
            if((transform.localPosition - destination).sqrMagnitude < 0.001f && Vector3.Dot(transform.localEulerAngles, finalEulerAngles) < 0.01f)
            {
                if (goingUpFirst)
                {
                    destination = new Vector3(destination.x, 0, destination.z);
                    goingUpFirst = false;
                    goUpAmount = 2.0f;
                }
                else
                {
                    moving = false;
                }
            }
        }
    }
}
