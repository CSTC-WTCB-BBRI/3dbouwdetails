using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using UnityEngine.Networking;

namespace UnityEngine.Reflect
{
    public class ChangeMaterial : MonoBehaviour
    {
        //public Toggle toggle; //Initially 'on' toggle, defines if material replacement can happen
        public Text showText; //Empty text file, shows name of selected object
        public GameObject selectedObject; //Leave empty, gets populated

        Material newMaterialCopy;
        public Image newMaterialCopyImage; //Just create a new empty Image, this is to show current material? It's bugged I think

        //public Text floatName; //Empty text
        public Camera mainCam; //This has to be linked to the main camera of the player
        //public float[] floatNameOffset; //[x, y, z] offset of image selection name, can be [0,0,0]
        public float[] floatImgOffset; //[x,y,z] offset of image selection images, can be [0,0,0]
        private Vector3 hitPoint;

        public List<Texture2D> texPoss;
        public List<Material> matPoss;
        List<Image> materialImages;
        public List<string> texPaths;

        float timeClick;

        GameObject root;// = GameObject.Find("Root");

        public int mortarWidth = 4; //Mortar width, in pixels, standard = 4
        public Color mortarColor; //Color of the mortar

        public Dropdown mortarSizeDrop; //Empty dropdown, gets populated

        public GameObject replacementTest; //Gameobject to be used to replace objects, currently unused

        public bool functionReplaceCalled; //Exists only to give to FaceMerging script

        public Material testMat;

        Web webScript;

        bool newSelected = false;

        int curCorner = 0;
        Vector3 curCornerLoc;

        public GameObject UIhide;

        // Start is called before the first frame update
        void Start() //Initializes time, root, the images for the right click menu...
        {
            //Added by Arnaud, 05/05/21 because FindAllObjects-->FindAll-->transformList is null otherwise
            FindAllObjects findAllObjectsScript = GameObject.Find("Root").GetComponent<FindAllObjects>();
            findAllObjectsScript.ClearLists();
            webScript = GameObject.Find("Root").GetComponent<Web>();
            //Added by Arnaud, 05/05/21 because it is null otherwise and thus materialImages.Add(tempImg) crashes
            materialImages = new List<Image>();
            texPoss = new List<Texture2D>();

            timeClick = Time.time;
            for (int i = 1; i < 300; i++)
            {
               //Image tempImg = Instantiate(newMaterialCopyImage, newMaterialCopyImage.transform.parent);
               //materialImages.Add(tempImg);
            }
            root = GameObject.Find("Root");
            //newMaterialCopy = newMaterial;
            if (root != null)
            {
                Debug.Log("success");
            }
            else
            {
                Debug.Log("root is null");
            }
            functionReplaceCalled = false; //For external use, to see if the material replace function is used

            UIhide = GameObject.Find("UI Root");
        }

        // Update is called once per frame
        void Update() //Show and move the possible tile choices on screen and check for inputs
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (UIhide.activeSelf)
                {
                    UIhide.SetActive(false);
                }
                else
                {
                    UIhide.SetActive(true);
                }
            }
            if (true) //if material replacement can happen
            {
                if (newSelected && selectedObject != null)
                {
                    matPoss = CreateUINew(selectedObject, 1);
                    newSelected = false;
                    curCorner = 0;
                }
                if (Input.GetMouseButtonDown(1)) //right click, this is done for timing reasons
                {
                    timeClick = Time.time;
                }
                if ((Input.GetMouseButtonUp(1) && Time.time - timeClick < 0.3f) || (Input.touchCount > 2 && Input.touches[2].phase == TouchPhase.Began)) //checks for a fast right click
                {

                    newSelected = true;

                    selectedObject = ClickObjects();
                    showText.text = selectedObject.name;
                    //newMaterialCopy = new Material(selectedObject.GetComponent<Renderer>().material);
                    //newMaterialCopy.shader = Shader.Find("Unlit/Texture");
                    //newMaterialCopyImage.material = newMaterialCopy;

                    //ChangeMaterialClick(testMat, selectedObject);
                }
                if (Input.GetKey(KeyCode.X))
                {
                    curCorner = -1;
                    selectedObject = ClickObjects();
                    if (selectedObject.GetComponent<Renderer>().material.shader == Shader.Find("Shader Graphs/TileShader2"))
                    {
                        selectedObject.GetComponent<Renderer>().material.SetVector("_Corner1", curCornerLoc);
                    }
                }
                else if (Input.GetKey(KeyCode.C))
                {
                    curCorner = 1;
                    selectedObject = ClickObjects();
                    if (selectedObject.GetComponent<Renderer>().material.shader == Shader.Find("Shader Graphs/TileShader2"))
                    {
                        selectedObject.GetComponent<Renderer>().material.SetVector("_Corner2", curCornerLoc);
                    }
                }
                if (Input.GetKey(KeyCode.V))
                {
                    curCorner = -1;
                    selectedObject = ClickObjects();
                    if (selectedObject.GetComponent<Renderer>().material.shader == Shader.Find("Shader Graphs/TileShader2"))
                    {
                        selectedObject.GetComponent<Renderer>().material.SetVector("_Corner1_2", curCornerLoc);
                    }
                }
                else if (Input.GetKey(KeyCode.B))
                {
                    curCorner = 1;
                    selectedObject = ClickObjects();
                    if (selectedObject.GetComponent<Renderer>().material.shader == Shader.Find("Shader Graphs/TileShader2"))
                    {
                        selectedObject.GetComponent<Renderer>().material.SetVector("_Corner2_2", curCornerLoc);
                    }
                }
                if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.RightControl))
                {
                    ChangeMaterialClick(testMat, selectedObject);
                }
                if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl)) //right click and ctrl
                {
                    selectedObject = ClickObjects();
                }
                if ((Input.touchCount > 2 && Input.touches[2].phase == TouchPhase.Began)) //triple touch
                {
                    selectedObject = ClickObjects();
                    Debug.Log(selectedObject.name);
                    showText.text = selectedObject.name;// + " with cost: " + selectedCostString;
                    //newMaterialCopy = new Material(selectedObject.GetComponent<Renderer>().material);
                    //newMaterialCopy.shader = Shader.Find("Unlit/Texture");
                    //newMaterialCopyImage.material = newMaterialCopy;
                }
                if (Input.GetKeyDown("e"))
                {
                    ToggleLight();
                }
                if (Input.GetKeyDown("r"))
                {
                    ToggleAllLight();
                }
                /*
                // ---------- OBSOLETE --------------------------
                // THE FOLLOWING HAS BEEN MOVED TO ContextualMenu (A.C. 18/08/21)
                if(selectedObject != null && selectedObject.GetComponent<Renderer>().material.shader == Shader.Find("Shader Graphs/testshaderlit")){
                    if (Input.GetKey("left"))
                    {
                        //ChangeMaterialClick(testMat, selectedObject);
                        var rotOld = selectedObject.GetComponent<Renderer>().material.GetFloat("_Rotation");
                        selectedObject.GetComponent<Renderer>().material.SetFloat("_Rotation", rotOld + 1f);
                    }
                    else if (Input.GetKeyDown("up"))
                    {
                        selectedObject.GetComponent<Renderer>().material.SetFloat("_Rotation", 0f);
                    }
                    else if (Input.GetKeyDown("right"))
                    {
                        selectedObject.GetComponent<Renderer>().material.SetFloat("_Rotation", 45f);
                    }
                    else if (Input.GetKeyDown("down"))
                    {
                        selectedObject.GetComponent<Renderer>().material.SetFloat("_Rotation", 90f);
                    }
                }
                */
            }
        }

        public GameObject ClickObjects() //Returns the gameobject that is clicked
        {
            if (selectedObject != null)
            {
                HighlightObject(selectedObject, false);
            }
            Ray ray;
            GameObject target = null;
            if (Input.touchCount > 2 && Input.touches[2].phase == TouchPhase.Began)
            {
                ray = Camera.main.ScreenPointToRay(Input.touches[2].position); //touch
            }
            else
            {
                ray = Camera.main.ScreenPointToRay(Input.mousePosition); //Mouse
            }
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) // you can also only accept hits to some layer and put your selectable units in this layer
            {
                if (hit.transform != null && hit.transform.IsChildOf(root.transform))
                {
                    target = hit.transform.gameObject;
                    curCornerLoc = hit.point + (ray.GetPoint(0.01f)-ray.GetPoint(0.001f))*curCorner;
                }
            }
            HighlightObject(target, true);
            return target;
        }

        public List<Material> CreateUINew(GameObject go, int draw) //This function selects the possible materials for the selected object 'go' and gives it back in a list, draws the UI if 'draw' >= 1
        {
            Vector3 imOffset = new Vector3(floatImgOffset[0], floatImgOffset[1], floatImgOffset[2]); //Defines offsets for tile selection menu
            var meta = go.GetComponent<Metadata>();//Metadata of go
            //List<Material> matPoss = new List<Material>();
            if (go.name.Contains("Wall") || meta.GetParameter("Category").Contains("Wall")) //If it's a wall, show wall material options
            {
                matPoss.AddRange(Resources.LoadAll("Materials/Wall", typeof(Material)).Cast<Material>().ToList());
                //texPossible.AddRange(Resources.LoadAll("Materials/Tiles", typeof(Texture)).Cast<Texture>().ToList());
            }
            if (go.name.Contains("Floor") || meta.GetParameter("Category").Contains("Floor"))
            {
                matPoss.AddRange(Resources.LoadAll("Materials/Floor", typeof(Material)).Cast<Material>().ToList());
            }
            if (go.name.Contains("Window") || meta.GetParameter("Category").Contains("Window"))
            {
                matPoss.AddRange(Resources.LoadAll("Materials/Window", typeof(Material)).Cast<Material>().ToList());
            }
            if (go.name.Contains("Ceiling") || meta.GetParameter("Category").Contains("Ceiling"))
            {
                matPoss.AddRange(Resources.LoadAll("Materials/Wall", typeof(Material)).Cast<Material>().ToList());
            }
            float[] mortarWidthArray = { 0.01f, 0.03f, 0.1f };//Possible choices of mortar widths in meters, defined hear because it was fast and easy...

            texPaths = webScript.PullTexturesForSurface(go);
            matPoss.Clear();
            texPoss.Clear();
            foreach(string st in texPaths)
            {
                var texture = LoadTextureFromDisk(st);
                texPoss.Add(texture);
            }
            foreach (Texture tex in texPoss) //Generate a tile material for every possible texture that doesn't have one yet
            {
                Material tempMat = new Material(Shader.Find("Shader Graphs/TileShader2"));
                tempMat.mainTexture = tex;
                matPoss.Add(tempMat);
            }



            if (draw >= 1 && false)//Draws the materials on screen
            {
                for (int i = 0; i < materialImages.Count()+5; i++)
                {
                    materialImages[i].transform.position = new Vector3(0f, -10000f, 0f);
                }
                if (matPoss.Count() >= 1)
                {
                    for (int i = 0; i < this.matPoss.Count(); i++)// Material mat in matPossible)
                    {
                        Material mat = new Material(this.matPoss[i]);
                        Material mat3D = new Material(this.matPoss[i]);
                        Image tempImg = materialImages[i];
                        int maxSqrt = Mathf.FloorToInt(Mathf.Sqrt(this.matPoss.Count()));
                        mat.shader = Shader.Find("UI/Default");
                        tempImg.material = mat;
                        RectTransform tempRect = (RectTransform)tempImg.transform;
                        tempImg.transform.position = mainCam.WorldToScreenPoint(hitPoint) + imOffset + new Vector3(0f + Mathf.Floor(i / maxSqrt) * (tempRect.rect.width + 40f), -(tempRect.rect.height + 40f) * (i - Mathf.Floor(i / maxSqrt) * maxSqrt), 0f);
                        tempImg.GetComponent<Button>().onClick.AddListener(() => ChangeMaterialClick(mat3D, selectedObject));
                        materialImages[i] = tempImg;
                    }
                }
            }
            return matPoss;
        }

        public void UnHighlightObjects(List<GameObject> go_list)
        {
            foreach (GameObject go in go_list)
            {
                HighlightObject(go, false);
            }
        }

        /// <summary>
        /// Override. Apply an outline around object, given a specific color.
        /// </summary>
        /// <param name="obj">The GameObject to be highlighted</param>
        /// <param name="on">Wheter to highlight or un-highlight the surface</param>
        /// <param name="col">The color of the outline</param>
        public void HighlightObject(GameObject obj, bool on, Color col)
        {
            OutlineUI outline;
            if (!on)
            {
                DestroyImmediate(obj.GetComponent<OutlineUI>());
            }
            if (on && obj.GetComponent<OutlineUI>() == null)
            {
                if (obj.GetComponent<OutlineUI>() == null)
                {
                    //outline = obj.AddComponent<OutlineUI>();
                }
                else
                {
                    //outline = obj.GetComponent<OutlineUI>();
                }
                //outline.OutlineMode = OutlineUI.Mode.OutlineAll;
                //outline.OutlineColor = col;
                //outline.OutlineWidth = 5f;
            }
        }

        /// <summary>
        /// Apply an outline around object
        /// </summary>
        /// <param name="obj">The GameObject to be highlighted</param>
        /// <param name="on">Wheter to highlight or un-highlight the surface</param>
        public void HighlightObject(GameObject obj, bool on)
        {
            OutlineUI outline;
            if (!on)
            {
                DestroyImmediate(obj.GetComponent<OutlineUI>());
            }
            if (on && obj.GetComponent<OutlineUI>() == null)
            {
                if (obj.GetComponent<OutlineUI>() == null)
                {
                    outline = obj.AddComponent<OutlineUI>();
                }
                else
                {
                    outline = obj.GetComponent<OutlineUI>();
                }
                outline.OutlineMode = OutlineUI.Mode.OutlineAll;
                outline.OutlineColor = Color.cyan;
                outline.OutlineWidth = 5f;
            }
        }
        public void ChangeObjectEmission(GameObject obj, Color col)
        {
            Material newMaterialCopy = new Material(obj.GetComponent<Renderer>().material);
            if (newMaterialCopy.GetColor("_EmissionColor").Equals(col))
            {
                newMaterialCopy.SetColor("_EmissionColor", Color.black);
            }
            else
            {
                newMaterialCopy.SetColor("_EmissionColor", col);
            }
            obj.GetComponent<MeshRenderer>().material = newMaterialCopy;
        }

        public void ChangeMaterialClick(Material mat, GameObject go) //Changes materials (all of them) of selectedObject to mat
        {
            functionReplaceCalled = true;
            //TEST
            if (matPoss.Count >= 1)
            {
                mat = matPoss[0];
            }
            //TEST
            
            HighlightObject(go, true);
            selectedObject = go;

            // AC - 19/05/21 - Here bring up my menu offering the possibility of choosing the material to apply.
            //var menuHandler = GameObject.Find("Root").GetComponent<MenusHandler>();
            //menuHandler.ActivateTilesChoiceMenu(); // This menu pops up, gets populated, and then the user can choose. After the choice, the tile gets applied and this choice is saved in DB.
        }

        public void ReplaceObject() //Replaces the selectedObject with replacementTest, matches size as well
        {
            GameObject go = selectedObject;
            float sizeX = go.GetComponent<Renderer>().bounds.size.x;
            float sizeY = go.GetComponent<Renderer>().bounds.size.y;
            float sizeZ = go.GetComponent<Renderer>().bounds.size.z;
            Vector3 loc = go.GetComponent<Renderer>().bounds.center;
            Debug.Log(sizeX.ToString() + " " + sizeY.ToString() + " " + sizeZ.ToString());

            GameObject replGo = (GameObject) Instantiate(replacementTest, root.transform);
            replGo.transform.position = loc;// go.transform.position;
            //replGo.transform.rotation = go.transform.rotation;
            replGo.transform.localScale = new Vector3(sizeX, sizeY, sizeZ);
            //replGo.transform.position += new Vector3(0f, sizeY/2, 0f);
            Destroy(go);
            functionReplaceCalled = true;
        }

        public void ToggleLight() //Toggles the light in selectedObject, if there is any
        {
            GameObject go = selectedObject;
            var meta = go.GetComponent<Metadata>();
            if (meta.GetParameter("Category").Contains("Light"))
            {
                foreach (Transform child in go.transform)
                {
                    Light light = child.gameObject.GetComponent(typeof(Light)) as Light;
                    if (light.enabled)
                    {
                        light.enabled = false;
                    }
                    else
                    {
                        light.enabled = true;
                    }
                }
            }
        }

        public void ToggleAllLight() //Finds all lights in the scene and toggles them on/off
        {
            GameObject root = GameObject.Find("Root");
            Transform[] transList = root.GetComponentsInChildren<Transform>();
            foreach (Transform allObj in transList)
            {
                GameObject go = allObj.gameObject;
                Debug.Log(go.name);
                var meta = go.GetComponent<Metadata>();
                if (meta != null && meta.GetParameter("Category").Contains("Light"))
                {
                    foreach (Transform child in go.transform)
                    {
                        Light light = child.gameObject.GetComponent(typeof(Light)) as Light;
                        if (light.enabled)
                        {
                            light.enabled = false;
                        }
                        else
                        {
                            light.enabled = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Given a file path, this function returns it as a 2D Texture.
        /// </summary>
        /// <param name="FilePath">The full path of the file to look for.</param>
        /// <returns>A Texture2D of the file.</returns>
        public Texture2D LoadTextureFromDisk(string FilePath)
        {
            // Load a PNG or JPG file from disk to a Texture2D
            // Returns null if load fails
            Texture2D Tex2D;
            byte[] FileData;


            //Get the 1st image within directory
            string picture = FilePath;

            if (File.Exists(picture))
            {
                //Debug.Log("File exists!");
                FileData = File.ReadAllBytes(picture);
                Tex2D = new Texture2D(2, 2);                // Create new "empty" texture
                if (Tex2D.LoadImage(FileData))              // Load the imagedata into the texture (size is set automatically)
                    return Tex2D;                           // If data = readable -> return texture
            }
            Debug.Log("File doesn't exist!");
            return null;                                    // Return null if load failed
        }
    }
}
