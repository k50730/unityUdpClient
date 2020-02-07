using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //public GameObject cube;
    public NetworkMan netMan;
    // Start is called before the first frame update
    void Start()
    {
        
        InvokeRepeating("SendPos", 1, 0.033f);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            this.transform.Translate(Vector3.forward * Time.deltaTime);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            this.transform.Translate(-(Vector3.forward) * Time.deltaTime);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            this.transform.Rotate(-Vector3.up * Time.deltaTime * 90f);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            this.transform.Rotate(Vector3.up * Time.deltaTime * 90f);
        }

    }

    void SendPos()
    {
        netMan.SendPosition(this.transform);
    }
}
