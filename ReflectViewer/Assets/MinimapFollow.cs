using System.Collections;
using System.Collections.Generic;
using Unity.Reflect.Viewer.UI;
using UnityEngine.UIElements;
using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    // Start is called before the first frame update
    public Camera mainCam;
    public Camera minimapCam;
    public GameObject minimapCanvas;
    public UnityEngine.UI.Slider minimapSlider;
    Transform curFollow;
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        GameObject fps = GameObject.Find("FPSController(Clone)");
        minimapCam.fieldOfView = minimapSlider.value;
        if (fps != null)
        {
            curFollow = fps.transform;
        }
        else
        {
            curFollow = mainCam.transform;
        }
        minimapCam.transform.position = new Vector3(curFollow.position.x, curFollow.position.y+100, curFollow.position.z);
        //minimapCanvas.transform.position = new Vector3(curFollow.position.x, curFollow.position.y-1, curFollow.position.z);
    }
}
