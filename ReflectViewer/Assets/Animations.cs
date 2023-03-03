using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.Reflect;
using Unity.Reflect.Viewer.UI;
using UnityEngine.UI;
using UnityEngine;
using BrainFailProductions.PivotModderRuntime;

public class Animations : MonoBehaviour
{
    GameObject root;
    List<GameObject> allChildren;
    public List<GameObject> animatedObjects;

    public int maxAnimationOrder;
    float startTime;
    float curTime;
    public List<float> timeAnimationDurationList;
    public List<float> timeAnimationWaitingtimeList;
    public List<int> timeAnimationCameraList;
    public List<Vector3> objFinalPosList;
    public List<Vector3> objFinalSizeList;

    public Text descriptionText;
    public Text descriptionTextFR;

    public Slider timeSlider;

    public Camera mainCam;

    public Vector3 cylLoc;
    public Vector3 center;
    public Vector3 targetLookAt;

    public GameObject targetObject;

    GameObject loc1;

    int prevCam;
    int nextCam;
    float camAnimTime;

    public List<int> cameraNumbers;
    public List<float> cameraStart;
    public List<float> cameraEnd;

    public float relTime;

    public List<GameObject> allCams;

    bool paused;
    GameObject tempObj;
    PivotModderRuntime pivotScript;

    public GameObject camAim;
    public string curCam;

    // Start is called before the first frame update
    void Start()
    {
        prevCam = 0;
        nextCam = 0;
        camAnimTime = 2f;
        root = GameObject.Find("Root");
        animatedObjects = new List<GameObject>();
        center = new Vector3(0f, 1f, 0f);
        targetLookAt = new Vector3(0f, 0f, 0f);
        allCams = new List<GameObject>();
        paused = false;
        pivotScript = GameObject.Find("Root").GetComponent<PivotModderRuntime>();
    }

    // Update is called once per frame
    void Update()
    {
        if (animatedObjects.Count > 0)
        {
            MoveCam(cylLoc);
            //mainCam.transform.LookAt(center);
            mainCam.transform.LookAt(targetLookAt);
        }
        if (Input.GetKeyDown("o")) //Initialize
        {
            GameObject.Find("BoundingBoxRoot").SetActive(false);
            animatedObjects = new List<GameObject>();
            allChildren = new List<GameObject>();
            timeAnimationDurationList = new List<float>();
            timeAnimationWaitingtimeList = new List<float>();
            timeAnimationCameraList = new List<int>();
            maxAnimationOrder = 0;
            startTime = -1000f;
            objFinalPosList = new List<Vector3>();
            objFinalSizeList = new List<Vector3>();
            allCams = new List<GameObject>();

            center = new Vector3(0, 0, 0);
            //int rnum = 0;
            //foreach (Vector3 loc in objFinalPosList)
            //{
             //   center += loc;
            //    rnum += 1;
            //}
            //center = center / rnum;

            foreach (Transform child in root.transform)
            {
                child.gameObject.SetActive(true);
                foreach (Transform childchild in child.transform)
                {
                    childchild.gameObject.SetActive(true);
                    if (childchild.gameObject.GetComponent<Collider>() == null)
                    {
                        //BoxCollider bC = childchild.gameObject.AddComponent<BoxCollider>();
                    }
                    allChildren.Add(childchild.gameObject);
                    var meta = childchild.gameObject.GetComponent<Metadata>();
                    try
                    {
                        if (meta != null && meta.GetParameter("AnimationOrder") != null && float.Parse(meta.GetParameter("AnimationOrder")) > 0) //AnimationOrder
                        {
                            animatedObjects.Add(childchild.gameObject);
                            //center += childchild.GetComponent<Collider>().bounds.center;
                            //maxAnimationOrder = Mathf.Max(maxAnimationOrder, float.Parse(meta.GetParameter("AnimationOrder")));
                        }
                    }
                    catch
                    {
                        Debug.Log(meta.GetParameter("AnimationDescription"));
                    }
                }
            }
            //center = center / animatedObjects.Count;

            animatedObjects = animatedObjects.OrderBy(e => float.Parse(e.transform.GetComponent<Metadata>().GetParameter("AnimationOrder"))).ToList();
            Debug.Log("Created animatedObjects");

            foreach (GameObject go in animatedObjects)
            {
                if (go.GetComponent<Collider>() == null)
                {
                    BoxCollider bC = go.AddComponent<BoxCollider>();
                    objFinalPosList.Add(Vector3.zero);
                    objFinalSizeList.Add(Vector3.zero);
                }
                else
                {
                    Vector3 temp = go.GetComponent<Collider>().bounds.center;
                    if (go.GetComponent<MeshFilter>() != null)
                    {
                        objFinalPosList.Add(go.transform.position + temp);
                        PivotModderRuntime.CentralizePivot(go, true); //why does this crash?
                    }
                    else
                    {
                        objFinalPosList.Add(go.transform.position);
                    }
                    objFinalSizeList.Add(go.GetComponent<Collider>().bounds.extents);
                }
            }

            Debug.Log("Created position and size lists");
            for (int i = 0; i < animatedObjects.Count; i++)
            {
                timeAnimationDurationList.Add(0f);
                timeAnimationWaitingtimeList.Add(0f);
                timeAnimationCameraList.Add(0);
            }
            Debug.Log("Initialized Duration/Waiting/Camera lists");
            for (int i = 0; i < animatedObjects.Count; i++)
            {
                var meta = animatedObjects[i].transform.GetComponent<Metadata>();
                timeAnimationDurationList[i] = Mathf.Max(timeAnimationDurationList[i], float.Parse(meta.GetParameter("AnimationDuration")));
                timeAnimationWaitingtimeList[i] = Mathf.Max(timeAnimationWaitingtimeList[i], float.Parse(meta.GetParameter("AnimationWaitingtime")));
                if(i < animatedObjects.Count - 1)
                {
                    if(float.Parse(animatedObjects[i].transform.GetComponent<Metadata>().GetParameter("AnimationOrder")) == float.Parse(animatedObjects[i+1].transform.GetComponent<Metadata>().GetParameter("AnimationOrder")))
                    {
                        timeAnimationWaitingtimeList[i] = 0;
                    }
                }
            }
            Debug.Log("Filled Duration/Waiting lists");
            for (int i = 1; i < timeAnimationDurationList.Count(); i++)
            {
                Debug.Log(i);
                timeAnimationDurationList[i] = timeAnimationDurationList[i];// + timeAnimationDurationList[i - 1];, normally shouldn't change
                timeAnimationWaitingtimeList[i] = timeAnimationWaitingtimeList[i] + timeAnimationWaitingtimeList[i - 1];
            }

            Debug.Log("Adjusted Duration/Waiting lists");
            foreach (Transform child in root.transform)
            {
                foreach (Transform childchild in child.transform)
                {
                    if (childchild.name.Contains("amera") && !childchild.name.Contains("3D"))
                    {
                        if (childchild.gameObject.GetComponent<Collider>() == null)
                        {
                            BoxCollider bC = childchild.gameObject.AddComponent<BoxCollider>();
                        }
                        PivotModderRuntime.CentralizePivot(childchild.gameObject, true);
                        allCams.Add(childchild.gameObject);
                        childchild.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
            //allCams = allCams.OrderBy(e => int.Parse(e.name.Split(' ')[1])).ToList();

            


        }
        if (Input.GetKeyDown("p"))
        {
            startTime = Time.time;
            timeSlider.minValue = 0;
            timeSlider.maxValue = timeAnimationWaitingtimeList[timeAnimationWaitingtimeList.Count()-1]+10;
        }
        if (Input.GetKey("z"))
        {
            startTime += Time.deltaTime;
        }
        if (Input.GetKey("x"))
        {
            if (paused)
            {
                startTime -= Time.deltaTime;
            }
            else
            {
                startTime -= 2*Time.deltaTime;
            }
        }
        if (Input.GetKeyDown("c"))
        {
            if (paused)
            {
                paused = false;
            }
            else
            {
                paused = true;
            }
        }
        if (paused)
        {
            startTime += Time.deltaTime;
        }
        AnimateObjects();// Manual();

    }
    public void AnimateObjects()
    {
        curTime = Time.time;
        relTime = curTime - startTime;
        timeSlider.value = relTime;
        descriptionText.text = " ";
        descriptionTextFR.text = " ";
        //timeSlider.onValueChanged.AddListener();
        cameraNumbers = new List<int>();
        cameraStart = new List<float>();
        cameraEnd = new List<float>();

        center = camAim.transform.position;

        for (int i = 0; i < animatedObjects.Count; i++)
        {
            GameObject go = animatedObjects[i];
            var meta = go.transform.GetComponent<Metadata>();
            Vector3 moveDir = Vector3.zero;
            if (meta.GetParameter("AnimationDirection") != string.Empty && (meta.GetParameter("AnimationDirection") == "bottom" || meta.GetParameter("AnimationDirection") == "1"))
            {
                moveDir = Vector3.up;
            }
            else if (meta.GetParameter("AnimationDirection") != string.Empty && (meta.GetParameter("AnimationDirection") == "top" || meta.GetParameter("AnimationDirection") == "2"))
            {
                moveDir = Vector3.down;
            }
            else if (meta.GetParameter("AnimationDirection") != string.Empty && meta.GetParameter("AnimationDirection") == "south")
            {
                moveDir = Vector3.forward;
            }
            else if (meta.GetParameter("AnimationDirection") != string.Empty && meta.GetParameter("AnimationDirection") == "west")
            {
                moveDir = Vector3.right;
            }
            else if (meta.GetParameter("AnimationDirection") != string.Empty && meta.GetParameter("AnimationDirection") == "north")
            {
                moveDir = Vector3.back;
            }
            else if (meta.GetParameter("AnimationDirection") != string.Empty && meta.GetParameter("AnimationDirection") == "east")
            {
                moveDir = Vector3.left;
            }
            float curObjWaitTime = 0;
            if (i > 0)
            {
                curObjWaitTime = timeAnimationWaitingtimeList[i - 1];
            }
            float curObjAnimTime = timeAnimationDurationList[i];

            if (!go.activeSelf && curObjWaitTime < relTime)
            {
                go.SetActive(true);
            }
            else if (go.activeSelf && curObjWaitTime > relTime)
            {
                go.SetActive(false);
                //Debug.Log("bruh why");
            }

            if (curObjWaitTime < relTime && curObjWaitTime + curObjAnimTime > relTime)
            {
                if (meta.GetParameter("AnimationType") == "slide")
                {
                    go.transform.position = objFinalPosList[i] + moveDir * 10f * Mathf.Pow((relTime - (curObjWaitTime + curObjAnimTime)) / curObjAnimTime, 3f);
                }
                else if (meta.GetParameter("AnimationType") == "roll")
                {
                    //Debug.Log(go.name);
                    go.transform.localScale = Vector3.one + new Vector3(Mathf.Abs(moveDir[0]), Mathf.Abs(moveDir[1]), Mathf.Abs(moveDir[2])) * Mathf.Pow((relTime - (curObjWaitTime + curObjAnimTime)) / curObjAnimTime, 3f); //1,1,0 to 1,1,1; depending on which axis
                    go.transform.position = objFinalPosList[i] + (moveDir.x + moveDir.y + moveDir.z) * Vector3.Scale(objFinalSizeList[i], go.transform.localScale - Vector3.one);
                }
                else if (meta.GetParameter("AnimationType") == "cute")
                {
                    go.transform.localScale = Vector3.one + new Vector3(Mathf.Abs(moveDir[0]), Mathf.Abs(moveDir[1]), Mathf.Abs(moveDir[2])) * Mathf.Pow((relTime - (curObjWaitTime + curObjAnimTime)) / curObjAnimTime, 3f);
                    go.transform.position = objFinalPosList[i] + Vector3.Scale(go.GetComponent<Collider>().bounds.size,moveDir) * Mathf.Min((relTime - (curObjWaitTime + curObjAnimTime)) / curObjAnimTime,1);

                }

                //descriptionText.text = meta.GetParameter("AnimationDescription");
                if (!string.IsNullOrEmpty(meta.GetParameter("AnimationDescriptionNL")))
                {
                    descriptionText.text = meta.GetParameter("AnimationDescriptionNL");
                }
                if (!string.IsNullOrEmpty(meta.GetParameter("AnimationDescriptionFR")))
                {
                    descriptionTextFR.text = meta.GetParameter("AnimationDescriptionFR");
                }

                //center = objFinalPosList[i]; //focus on new object
            }
            if(curObjWaitTime + curObjAnimTime < relTime)
            {
                if (meta.GetParameter("AnimationType") == "slide")
                {
                    go.transform.position = objFinalPosList[i];
                }
                else if (meta.GetParameter("AnimationType") == "roll")
                {
                    go.transform.localScale = Vector3.one;
                }
            }

            // Here camera stuff starts
            string str = meta.GetParameter("AnimationCamera");
            if (str.Length > 1)
            {
                //Debug.Log(str);
                string str1 = str.Split(' ')[1];
                if (int.Parse(str1) != 0)
                {
                    cameraNumbers.Add(int.Parse(str1));

                }
            }
        }

        for (int i = 0; i < cameraNumbers.Count; i++)
        {
            cameraStart.Add(0);
            cameraEnd.Add(0);
        }
        int curCamNum = 0;
        for (int i = 0; i < animatedObjects.Count; i++)
        {
            GameObject go = animatedObjects[i];
            var meta = go.transform.GetComponent<Metadata>();
            string str = meta.GetParameter("AnimationCamera");
            if (str.Length > 1)
            {
                string str1 = str.Split(' ')[1];
                if (int.Parse(str1) != 0)
                {
                    cameraEnd[curCamNum] = timeAnimationWaitingtimeList[i];
                    cameraStart[curCamNum] = cameraEnd[curCamNum] - camAnimTime;
                    curCamNum++;
                }
            }
        }
        if (cameraNumbers.Count > 1 & allCams.Count > 0)
        {
            for (int i = 0; i < cameraNumbers.Count - 1; i++) //Clean up
            {
                var meta = allCams[i].transform.GetComponent<Metadata>();
                if (cameraStart[i + 1] < cameraEnd[i])
                {
                    cameraEnd[i] = cameraStart[i + 1];
                }
                Vector3 direction = new Vector3(0, 0, 0);
                if (meta.GetParameter("Comments").Length > 0)
                {
                    string[] temp = meta.GetParameter("Comments").Substring(1, meta.GetParameter("Comments").Length - 2).Split(',');
                    allCams[i].transform.LookAt(allCams[i].transform.position + new Vector3(float.Parse(temp[0]), float.Parse(temp[2]), float.Parse(temp[1])));
                }
                else
                {
                    allCams[i].transform.LookAt(center);
                }
            }
            if(relTime < cameraStart[1])
            {
                cylLoc = CartToCyl(allCams[allCams.FindIndex(x => x.name.Contains(" 01"))].transform.position);
                curCam = allCams[allCams.FindIndex(x => x.name.Contains(" 01"))].name;

                targetLookAt = CylToCart(cylLoc) + allCams[allCams.FindIndex(x => x.name.Contains(" 01"))].transform.forward;
            }
            for (int i = 0; i < cameraNumbers.Count; i++)
            {
                if (relTime > cameraEnd[i])
                {
                    foreach (GameObject go in allCams)
                    {
                        if (go.name.Contains("camera 0" + cameraNumbers[i].ToString())){
                            cylLoc = CartToCyl(go.transform.position);
                            curCam = go.name;

                            targetLookAt = CylToCart(cylLoc) + go.transform.forward;
                        }
                    }
                }
                else if(relTime > cameraStart[i] && i > 0)
                {
                    Vector3 camTargetLoc = new Vector3();
                    Vector3 camPrevLoc = new Vector3();
                    Vector3 camTargetDir = new Vector3();
                    Vector3 camPrevDir = new Vector3();
                    foreach (GameObject go in allCams)
                    {
                        if (go.name.Contains("amera 0" + cameraNumbers[i].ToString()))
                        {
                            camTargetLoc = CartToCyl(go.transform.position);
                            curCam = go.name;

                            camTargetDir = go.transform.forward;
                        }
                        if (go.name.Contains("amera 0" + cameraNumbers[i-1].ToString()))
                        {
                            camPrevLoc = CartToCyl(go.transform.position);

                            camPrevDir = go.transform.forward;
                        }
                    }
                    float smoothX = (relTime - cameraStart[i]) / (cameraEnd[i] - cameraStart[i]);
                    if (Mathf.Abs(camTargetLoc[1] - camPrevLoc[1]) > Mathf.Abs(camTargetLoc[1] - 2 * Mathf.PI - camPrevLoc[1]))
                    {
                        camTargetLoc[1] -= 2 * Mathf.PI;
                    }
                    else if (Mathf.Abs(camTargetLoc[1] - camPrevLoc[1]) > Mathf.Abs(camTargetLoc[1] + 2 * Mathf.PI - camPrevLoc[1]))
                    {
                        camTargetLoc[1] += 2 * Mathf.PI;
                    }
                    cylLoc = camPrevLoc + (camTargetLoc - camPrevLoc) * (float)(System.Math.Tanh((2*smoothX-1)/(Mathf.Sqrt(smoothX*(1-smoothX))))+1) / 2;
                    targetLookAt = CylToCart(cylLoc) + (camPrevDir + (camTargetDir - camPrevDir) * (float)(System.Math.Tanh((2 * smoothX - 1) / (Mathf.Sqrt(smoothX * (1 - smoothX)))) + 1) / 2);
                }
            }
        }
    }
    public void MoveCam(Vector3 target)
    {
        float radius = target[0];// *(float)3;//make it a bit further
        float angle1 = target[1];
        float angle2 = target[2];
        mainCam.transform.position = new Vector3(radius * Mathf.Cos(angle1) + center.x, angle2 + center.y, radius * Mathf.Sin(angle1) + center.z);
        return;
    }
    public Vector3 CartToCyl(Vector3 targetCoords)
    {
        float x = targetCoords.x - center.x;
        float y = targetCoords.y - center.y;
        float z = targetCoords.z - center.z;
        Vector3 cylCoords = new Vector3(Mathf.Sqrt(x * x + z * z), Mathf.Atan2(z, x), y);
        return cylCoords;
    }
    public Vector3 CylToCart(Vector3 target)
    {
        float radius = target[0];// *(float)3;//make it a bit further
        float angle1 = target[1];
        float angle2 = target[2];
        return new Vector3(radius * Mathf.Cos(angle1) + center.x, angle2 + center.y, radius * Mathf.Sin(angle1) + center.z);
    }

    public void ChangeRelTime()
    {
        startTime = curTime - timeSlider.value;
        relTime = timeSlider.value;
        return;
    }
}
