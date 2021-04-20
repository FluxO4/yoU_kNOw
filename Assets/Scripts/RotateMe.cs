using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateMe : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float speed = 1.0f;

    // Update is called once per frame
    void Update()
    {
        transform.localEulerAngles += new Vector3(0, speed * Time.deltaTime, 0);
    }
}
