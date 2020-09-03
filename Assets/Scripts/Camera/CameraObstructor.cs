using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraObstructor : MonoBehaviour
{
    public Camera CameraToAnnoy;
    public float Distance;

    // Update is called once per frame
    void Update()
    {
        transform.position = CameraToAnnoy.transform.position + CameraToAnnoy.transform.forward * Distance;
        transform.rotation = CameraToAnnoy.gameObject.transform.rotation;
    }
}
