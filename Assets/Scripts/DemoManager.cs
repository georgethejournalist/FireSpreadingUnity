using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void QuitDemo()
    {
        Debug.Log("Quitting");
        Application.Quit(0);
    }
}
