using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using Unity.Reflect.Viewer.UI;
using UnityEngine.UIElements;
using System;
using System.Text.RegularExpressions;
using UnityEditor;

namespace UnityEngine.Reflect
{
    public class MyEventArgs : EventArgs
    {
        public MyEventArgs(params object[] args)
        {
            Args = args;
        }

        public object[] Args { get; set; }
    }

    public class FindAllObjects : MonoBehaviour  //Finds objects and makes them ready for editing and stuff?
    {
        // Event handler
        public event EventHandler<MyEventArgs> ListOfSurfacesSet;

        Transform[] transformArr;
        List<Transform> transformList; //List of all the transforms of all objects imported by Reflect
        List<GameObject> objList; //List of all objects imported
        List<Metadata> metaList; //List of all metadata
        List<string> phases; //List of all phasing info
        int numPhases = 0; //Number of phases
        public Dropdown dropDownPhases; //An EMPTY dropdown menu, gets populated with the detected phases

        public UI.Slider slider; //EMPTY slider to go between phases
        public Text sliderVal; //Name of current phase shown
        public UI.Toggle prevToggle; //To select if to show phase alone or include previous phases as well, toggles between off and on

        public InputField sortBy; //EMPTY inputfield, gets populated automatically
        public Dropdown sortByDrop; //Empty dropdown, auto populated
        public Dropdown showOnly; //Empty dropdown

        public static FreeFlyCamera freefly_cam_script; 

        /// <summary>
        /// Dictionary that holds for each room its name, and it validity status (as a bool).
        /// </summary>
        public Dictionary<string, bool> roomValidities;

        List<string> keyList;
        List<string> keyList2;
        GameObject root; //GameObject under which all imported gameobjects are stored

        private bool buildingLoaded = false;
        public List<Vector3> roomCenters { get; private set; }
        public List<string> roomNames { get; private set; }
        public List<GameObject> roomPlaceHolders { get; private set; }

        /// <summary>
        /// The list of surfaces with "A" Comments parameter, for each room. "A" parameter means these are tilable surfaces, included in price by default.
        /// </summary>
        public Dictionary<string, List<int>> surfacesAPerRoom { get; private set; }

        /// <summary>
        /// The list of surfaces with "0" Comments parameter, for each room. "0" parameter means these are tilable surfaces, but not included in price by default.
        /// </summary>
        public Dictionary<string, List<int>> surfaces0PerRoom { get; private set; }

        public Dictionary<int, bool> surfacesValidities { get; private set; }

        GameObject minimapImage;
        GameObject minimapZoom;
        public List<Light> lights;
        public UI.Slider lightIntensity;
        public UI.Slider lightTemperature;
        List<float> roomVolumeList;
        public Light sunLight;
        public UI.Slider sunLightIntensity;
        public UI.Slider sunLightTemperature;

        // Start is called before the first frame update
        void Start()
        {
            surfacesAPerRoom = new Dictionary<string, List<int>>();
            surfaces0PerRoom = new Dictionary<string, List<int>>();
            roomValidities = new Dictionary<string, bool>();
            surfacesValidities = new Dictionary<int, bool>();

            slider.minValue = 1;
            slider.maxValue = 1;
            slider.value = 1;

            UIStateManager.stateChanged += UIStateManager_stateChanged; // Listening to UI state change in order to know when the building is loaded.

            minimapImage = GameObject.Find("MinimapImage");
            minimapZoom = GameObject.Find("MinimapZoom");
            minimapImage.SetActive(false);
            minimapZoom.SetActive(false);
            lights = new List<Light>();
            sunLight.useColorTemperature = true;
        }

        private void Update()
        {
            for(int i = 0; i < lights.Count; i++)
            {
                lights[i].colorTemperature = lightTemperature.value;
                lights[i].intensity = lightIntensity.value*roomVolumeList[i]/10f;
            }
            RenderSettings.ambientIntensity = sunLightIntensity.value;
            sunLight.intensity = sunLightIntensity.value * 2f;
            sunLight.colorTemperature = sunLightTemperature.value;

            if (RenderSettings.skybox.HasProperty("_Tint"))
            {
                RenderSettings.skybox.SetColor("_Tint", Mathf.CorrelatedColorTemperatureToRGB(sunLightTemperature.value));
            }
            else if (RenderSettings.skybox.HasProperty("_SkyTint"))
            {
                RenderSettings.skybox.SetColor("_SkyTint", Mathf.CorrelatedColorTemperatureToRGB(sunLightTemperature.value));
            }

            if (sunLightIntensity.value <= 0.4f)
            {
                RenderSettings.ambientLight = Mathf.CorrelatedColorTemperatureToRGB( sunLightTemperature.value * sunLightIntensity.value*2.5f) * sunLightIntensity.value*2.5f;
                if (RenderSettings.skybox.HasProperty("_Tint"))
                {
                    RenderSettings.skybox.SetColor("_Tint", Mathf.CorrelatedColorTemperatureToRGB(sunLightTemperature.value) * sunLightIntensity.value*2.5f);
                }
                else if (RenderSettings.skybox.HasProperty("_SkyTint"))
                {
                    RenderSettings.skybox.SetColor("_SkyTint", Mathf.CorrelatedColorTemperatureToRGB(sunLightTemperature.value*2.5f) * sunLightIntensity.value*2.5f);
                }
            }
            else
            {
                RenderSettings.ambientLight = Mathf.CorrelatedColorTemperatureToRGB(sunLightTemperature.value);
            }
        }

        private void UIStateManager_stateChanged(UIStateData obj)
        {
            if (obj.progressData.totalCount > 0 && obj.progressData.currentProgress == obj.progressData.totalCount)    // Then the building is fully loaded
            {
                if (!buildingLoaded)
                {
                    ListOfSurfacesSet += RoomScriptableObject.InitNewRoomTable;
                    var web = GameObject.Find("Root").GetComponent<Web>();
                    var mh = GameObject.Find("Root").GetComponent<MenusHandler>();
                    //web.DBAccessError += mh.ShowInfoMessage;
                    ExploitPLaceHolders();
                    ExploitRooms();
                    FindAll("Wall");
                    
                    buildingLoaded = true;

                    minimapImage.SetActive(true);
                    minimapZoom.SetActive(true);
                }
            }
        }

        /// <summary>
        /// Hides all the rooms renderers and set them onto a dedicated layer.
        /// </summary>
        void ExploitRooms()
        {
            Transform[] mytransformArr = GameObject.FindObjectsOfType(typeof(Transform)) as Transform[];
            foreach (Transform tr in mytransformArr)
            {
                GameObject go = tr.gameObject;
                var meta = go.GetComponent<Metadata>();
                var mr = go.GetComponent<MeshRenderer>();

                if (meta != null && mr != null && meta.GetParameter("Comments") == "BIMEXPOROOM")
                {
                    mr.enabled = false;
                    go.layer = 29;  // Assigning to 'Rooms' layer
                }
            }
        }

        void ExploitPLaceHolders()
        {
            roomCenters = new List<Vector3>();
            Transform[] mytransformArr = GameObject.FindObjectsOfType(typeof(Transform)) as Transform[];
            roomNames = new List<string>();
            roomPlaceHolders = new List<GameObject>();
            Regex rx = new Regex(@" \[[0-9]*\]$");
            roomVolumeList = new List<float>();
            foreach (Transform tr in mytransformArr)
            {
                GameObject go = tr.gameObject;
                var meta = go.GetComponent<Metadata>();
                var mr = go.GetComponent<MeshRenderer>();
                if (meta != null && mr != null && meta.GetParameter("Comments") == "BIMEXPOPH")
                {
                    roomCenters.Add(mr.bounds.center);
                    mr.enabled = false;
                    string roomName = rx.Split(meta.GetParameter("Mark"))[0];
                    roomNames.Add(roomName);
                    roomPlaceHolders.Add(go);
                    BuildingInfo.roomPlaceHolders.Add(go);
                    // also create reflection probes here later


                    // Use scriptableObjects to store info for each room
                    // Instantiate a new RoomScriptableObject
                    //var rso = Instantiate(Resources.Load<RoomScriptableObject>("RoomScriptableObject"));

                    // Add this RoomScriptableObject the each placeholder's PlaceHolderInfo component
                    //go.AddComponent<PlaceHolderInfo>();
                    //PlaceHolderInfo phInfo = go.GetComponent<PlaceHolderInfo>();
                    //phInfo.info = rso;

                    // Add info into the RoomScriptableObject
                    //phInfo.info.roomName = roomName;
                    try
                    {
                        roomVolumeList.Add(float.Parse(meta.GetParameter("BimExpoVolume").Split(' ')[0]));
                    }
                    catch (Exception)
                    {
                        roomVolumeList.Add(1.0f);
                    }
                }
                if (meta != null && mr != null && meta.GetParameter("Category").Contains("Wall")) //Put all walls on minimap layer
                {
                    go.layer = 20;
                }
            }
            // Fill Menu
            var sms = GameObject.Find("SlidingMenu").GetComponent<SlidingMenu>();
            sms.PopulateMenu(roomNames);
            var web = GameObject.Find("Root").GetComponent<Web>();
            StartCoroutine(web.CreateRoomValidationTable(roomNames));

            // Generate text to be shown in camera
            GameObject minimapCanvas = GameObject.Find("MinimapCanvas");
            minimapCanvas.layer = 21;
            for (int i = 0; i < roomNames.Count; i++)
            {
                //GameObject textHolder = new GameObject();
                roomPlaceHolders[i].transform.parent = minimapCanvas.transform;
                roomPlaceHolders[i].layer = 21;
                roomPlaceHolders[i].transform.Rotate(90, 0, 0);
                roomPlaceHolders[i].transform.position = roomCenters[i];
                roomPlaceHolders[i].transform.localScale -= new Vector3((float)( roomPlaceHolders[i].transform.localScale.x - 1), (float)(roomPlaceHolders[i].transform.localScale.y - 1), (float)(roomPlaceHolders[i].transform.localScale.z - 1));
                GameObject lightHolder = new GameObject();
                Light curLight = lightHolder.AddComponent<Light>();
                lightHolder.transform.position = roomCenters[i] + new Vector3(0f, 0.6f, 0f); // location of room place holders + 0.3m higher
                curLight.useColorTemperature = true;
                curLight.colorTemperature = lightTemperature.value;
                curLight.intensity = lightIntensity.value;
                curLight.type = LightType.Spot;
                curLight.spotAngle = 180;
                curLight.transform.Rotate(90, 0, 0);
                lights.Add(curLight);

                Text roomText = roomPlaceHolders[i].AddComponent<Text>();
                roomText.text = roomNames[i];
                roomText.color = Color.white;
                roomText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
                roomText.alignment = TextAnchor.MiddleCenter;

                roomText.fontSize = 14;
            }
        }

        public void FindAll(string strInput)
        {
            Dictionary<int, string> tempASurfacesPerRoom = new Dictionary<int, string>();
            Dictionary<int, string> temp0SurfacesPerRoom = new Dictionary<int, string>();
            Dictionary<GameObject, string> tempGOPerRoom = new Dictionary<GameObject, string>();
            List<string> necList;
            if (transformList.Count == 0) //If the elements are not yet detected, then detect them
            {
                Initialize();
                string curPhase;
                foreach (Transform tr in transformList)
                {
                    GameObject go = tr.gameObject;
                    if (go.name == "Main Camera")
                    {
                        freefly_cam_script = go.GetComponent<FreeFlyCamera>();
                    }
                    var meta = go.GetComponent<Metadata>();
                    if (go.transform.IsChildOf(root.transform) && meta != null && meta.GetParameters().Count() >= 1)
                    {
                        objList.Add(go);
                        //Adds collision boxes to all objects except those labeled as door and the placeholders
                        if (!meta.GetParameter("Category").Contains("Door") && meta.GetParameter("Comments") != "BIMEXPOPH")
                        {
                            go.AddComponent<MeshCollider>();
                        }
                        metaList.Add(meta);
                        if (go.name.Contains(strInput)) //Find all elements whose name includes...
                        {
                            //Debug.Log(go.name + "\n");
                        }
                        Dictionary<string, Metadata.Parameter> dict = meta.GetParameters();
                        curPhase = meta.GetParameter(dropDownPhases.captionText.text);
                        if (!phases.Contains(curPhase) && curPhase.Count() >= 1)
                        {
                            phases.Add(curPhase);
                        }

                        // Store in which room is each surface
                        // The room names are stored by ROOMNAME FLOOR [SURFACENUMBER]. Need to get rid of the surface number, because this varies for every face
                        Regex rx = BuildingInfo.rx;

                        if (meta.GetParameter("Mark") != null && meta.GetParameter("Mark") != "" && meta.GetParameter("Comments") == "A")
                        {
                            string roomName = rx.Split(meta.GetParameter("Mark"))[0];
                            tempASurfacesPerRoom.Add(Int32.Parse(meta.GetParameter("Id")), roomName);
                            tempGOPerRoom.Add(go, roomName);
                        }
                        else if (meta.GetParameter("Mark") != null && meta.GetParameter("Mark") != "" && meta.GetParameter("Comments") == "0")
                        {
                            string roomName = rx.Split(meta.GetParameter("Mark"))[0];
                            temp0SurfacesPerRoom.Add(Int32.Parse(meta.GetParameter("Id")), roomName);
                            tempGOPerRoom.Add(go, roomName);
                        }
                    }
                }
                phases.Sort();
                numPhases = phases.Count; //number of phases

                if (numPhases == 0)
                {
                    numPhases = 1;
                }
                slider.maxValue = numPhases;
                slider.value = slider.maxValue;

                //Adds filtering options to sortByDrop
                keyList = new List<string>(metaList[0].GetParameters().Keys);
                necList = new List<string>();
                necList.Add("Materiaaltype");
                necList.Add("Materiaaltype gedetailleerd");
                necList.Add("Merk");
                necList.Add("Kosten");
                necList.Add("Beroepsfilter");
                necList.Add("Vereiste tools");
                keyList = necList;
                Debug.Log(metaList[0].GetParameters().Keys.Count());
                sortByDrop.ClearOptions();
                //sortByDrop.AddOptions(keyList);
                sortByDrop.AddOptions(necList);

                //UpdatePhasesShown(); //AC - 23/06/21: I comment this because it leads to crash, (phases is empty). Not used for the moment, we can fix later.
            }

            int count = 0;
            List<string> roomsDone = new List<string>();
            RoomScriptableObject rso = null;
            while (count < tempASurfacesPerRoom.Count)
            {
                string room = tempASurfacesPerRoom.Values.ElementAt(count);

                List<int> ids = new List<int>();
                if (!roomsDone.Contains(room))
                {
                    roomsDone.Add(room);
                    foreach (var item in tempASurfacesPerRoom)
                    {
                        if (item.Value == room)
                        {
                            ids.Add(item.Key);
                        }
                    }
                    surfacesAPerRoom.Add(room, ids);
                }
                count += 1;
            }

            // Now check "0" surfaces per room
            count = 0;
            roomsDone.Clear();
            rso = null;
            while (count < temp0SurfacesPerRoom.Count)
            {
                string room = temp0SurfacesPerRoom.Values.ElementAt(count);

                List<int> ids = new List<int>();
                if (!roomsDone.Contains(room))
                {
                    roomsDone.Add(room);
                    foreach (var item in temp0SurfacesPerRoom)
                    {
                        if (item.Value == room)
                        {
                            ids.Add(item.Key);
                        }
                    }
                    surfaces0PerRoom.Add(room, ids);
                }
                count += 1;
            }

            // Now check the gameobjects per room
            count = 0;
            roomsDone.Clear();
            rso = null;
            while (count < tempGOPerRoom.Count)
            {
                string room = tempGOPerRoom.Values.ElementAt(count);

                List<GameObject> gos = new List<GameObject>();
                if (!roomsDone.Contains(room))
                {
                    roomsDone.Add(room);
                    foreach (var item in tempGOPerRoom)
                    {
                        if (item.Value == room)
                        {
                            gos.Add(item.Key);
                        }
                    }
                    ListOfSurfacesSet?.Invoke(this, new MyEventArgs(room, gos));
                }
                count += 1;
            }

            // Fill surfacesValidities
            var web = GameObject.Find("Root").GetComponent<Web>();
            StartCoroutine(web.GetAllSurfacesValidities(UpdateSurfacesAndRoomsValiditiesDict));
        }

        public void UpdateSurfacesAndRoomsValiditiesDict(Dictionary<int, Tuple<bool, string>> surfacesDict)
        {
            surfacesValidities.Clear();
            roomValidities.Clear();
            foreach (KeyValuePair<string, List<int>> item in surfacesAPerRoom)
            {
                string currentRoom = item.Key;
                List<int> surfaces_ids_in_this_room = item.Value;
                bool isRoomValid = true;
                foreach (int id in surfaces_ids_in_this_room)
                {
                    bool isSurfaceValid = surfacesDict[id].Item1;
                    surfacesValidities.Add(id, isSurfaceValid);
                    if (!isSurfaceValid)
                    {
                        isRoomValid = false;
                    }
                }
                roomValidities.Add(currentRoom, isRoomValid);
            }
            Debug.Log("roomValidities: ");
            foreach (KeyValuePair<string, bool> item in roomValidities)
            {
                Debug.Log(item.Key + " , value: " + item.Value.ToString());
            }
        }

        // OBSOLETE
        public void GoToLocation(UIElements.Button button)
        {
            Vector3 loc = roomCenters[roomNames.IndexOf(button.text)];
            GameObject go = roomPlaceHolders[roomNames.IndexOf(button.text)];
            FreeFlyCamera cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<FreeFlyCamera>();
            cam.SetMovePosition(loc, cam.transform.rotation);
            // TO DO: put
        }
        public void GoToLocation(string room)
        {
            Vector3 loc = roomCenters[roomNames.IndexOf(room)];
            GameObject go = roomPlaceHolders[roomNames.IndexOf(room)];
            FreeFlyCamera cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<FreeFlyCamera>();
            cam.SetMovePosition(loc, cam.transform.rotation);
        }


        public void SortCategories() //Gets the categories from the metadata, and makes it possible to filter by them
        { 
            string sortVal = keyList[sortByDrop.value];
            List<string> categories = new List<string>();
            foreach (GameObject go in objList)
            {
                var meta = go.GetComponent<Metadata>();
                string sortValGo = meta.GetParameter(sortVal);
                Debug.Log(sortValGo);
                if (sortValGo.Length >= 1 && !categories.Contains(sortValGo)) //If the category doesn't exist yet, create it
                {
                    categories.Add(sortValGo);
                    //catObj.name = sortValGo;
                }
            }

            showOnly.ClearOptions();
            keyList2 = categories;
            showOnly.AddOptions(keyList2);

            string sortCatsText = "list of " + sortVal + "\n";
            foreach(string str in categories)
            {
                Debug.Log(str);
                sortCatsText+= "\n" + str;
            }
            //sortCats.text = sortCatsText;

        }
        public void showOnlySelected() //Go through all gameobjects and disable those that don't have a specific metadata parameter
        {
            string sortVal = keyList2[showOnly.value];
            string param = keyList[sortByDrop.value];
            foreach (GameObject go in objList)
            {
                var meta = go.GetComponent<Metadata>();
                string sortValGo = meta.GetParameter(param);
                Debug.Log(sortVal + " " + sortValGo);
                
                if (sortVal.Equals(sortValGo))
                {
                    go.SetActive(true);
                }
                else
                {
                    go.SetActive(false);
                }
            }
        }

        public void showAll() //Enable all objects again
        {
            foreach (GameObject go in objList)
            {
                go.SetActive(true);
            }
        }

        public void UpdatePhasesShown() //This shows the currently active phase, and possibly the previous phases as well
        {
            sliderVal.text = phases[(int)slider.value-1].ToString();
            float maxPhase = slider.value;
            foreach(GameObject go in objList)
            {
                var meta = go.GetComponent<Metadata>();
                int phase = phases.IndexOf(meta.GetParameter(dropDownPhases.captionText.text));
                if (prevToggle.isOn)
                {
                    if (phase >= maxPhase)
                    {
                        go.SetActive(false);
                    }
                    else
                    {
                        go.SetActive(true);
                    }
                }
                else
                {
                    if (phase != maxPhase-1)
                    {
                        go.SetActive(false);
                    }
                    else
                    {
                        go.SetActive(true);
                    }
                }
            }
        }

        public void ClearLists() //Reset some lists if importing a new model
        {
            transformList = new List<Transform>();
            objList = new List<GameObject>();
            metaList = new List<Metadata>();
            phases = new List<string>();
        }
        public void Initialize() //Find Root object, list all the transforms in the scene, initialize some lists
        {
            root = GameObject.Find("Root");
            transformArr = FindObjectsOfType(typeof(Transform)) as Transform[];
            transformList = new List<Transform>(transformArr);
            objList = new List<GameObject>();
            metaList = new List<Metadata>();
            phases = new List<string>();
        }


        

    }
}
