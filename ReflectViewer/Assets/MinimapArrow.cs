using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapArrow : MonoBehaviour
{
    Transform curFollow;
    public Camera mainCam;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        GameObject fps = GameObject.Find("FPSController(Clone)");
        if (fps != null)
        {
            curFollow = fps.transform;
        }
        else
        {
            curFollow = mainCam.transform;
        }
        transform.position = new Vector3(curFollow.position.x, curFollow.position.y, curFollow.position.z);
        transform.rotation = curFollow.transform.rotation;
        //transform.rotation *= Quaternion.Euler(-90, 0, 0);
        transform.rotation = Quaternion.Euler(-90, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
    }
}
