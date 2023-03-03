using System.Collections;
using System.Collections.Generic;
using Unity.Reflect.Viewer;
using Unity.Reflect.Viewer.UI;
using UnityEngine;

public class MinimapInteraction : MonoBehaviour
{
    public Camera minimapCam;
    public Camera mainCam;
    public Canvas minimapCanvas;
    public GameObject minimapImage;
    FreeFlyCamera freeFlyCameraScript;
    FirstPersonController firstPersonControllerScript;
    Transform curFollow;
    // Start is called before the first frame update
    void Start()
    {
        freeFlyCameraScript = mainCam.GetComponent<FreeFlyCamera>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ClickDetected()
    {

        GameObject fps = GameObject.Find("FPSController(Clone)");
        if (fps != null)
        {
            curFollow = fps.transform;
            firstPersonControllerScript = fps.GetComponent<FirstPersonController>();
        }
        else
        {
            curFollow = mainCam.transform;
        }

        Vector3 oldPosition = curFollow.position;
        Vector3 worldPosition = Input.mousePosition;
        Vector3 minimapPosition = minimapImage.transform.position;
        var rectTransform = minimapImage.GetComponent<RectTransform>();
        float width = rectTransform.sizeDelta.x;
        float height = rectTransform.sizeDelta.y;
        var relX = (worldPosition[0]- minimapPosition[0]) / width;
        var relY = (worldPosition[1] - minimapPosition[1]) / width;
        float frustumW = 2.0f * 100f * Mathf.Tan(minimapCam.fieldOfView * 0.5f * Mathf.Deg2Rad); //100 is the distance of clipping plane
        Vector3 newPosition = oldPosition + new Vector3(relX * frustumW,0f, relY * frustumW);

        //Debug.Log("Old: "+oldPosition[0].ToString() + " " + oldPosition[1].ToString() + " " + oldPosition[2].ToString());
        //Debug.Log("New: "+newPosition[0].ToString() + " " + newPosition[1].ToString() + " " + newPosition[2].ToString());

        if (fps != null)
        {
            //fps.transform.localPosition = newPosition;
            //firstPersonControllerScript.collisionsEnabled= false;
            //firstPersonControllerScript.GetComponent<CharacterController>().Move(new Vector3(relX * frustumW, 0f, relY * frustumW));
            //firstPersonControllerScript.collisionsEnabled = true;

            firstPersonControllerScript.GetComponent<CharacterController>().enabled = false;
            fps.transform.position = newPosition;
            firstPersonControllerScript.GetComponent<CharacterController>().enabled = true;
        }
        else
        {
            Transform newTrans = curFollow;
            newTrans.position = newPosition;
            freeFlyCameraScript.TransformTo(newTrans);
        }
    }
}
