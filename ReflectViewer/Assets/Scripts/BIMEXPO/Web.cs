using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Reflect; //Unity.Reflect has to be added to the asmdef in the current folder
using System.Collections.ObjectModel;
using Unity.Reflect.Viewer.UI;
using SimpleJSON;
using System.Text.RegularExpressions;

public class Web : MonoBehaviour
{
    DateTime sessionDateTime;
    public string sessionSqlFormattedDate { get; private set; }
    
    private bool tablesCreated = false;
    private bool localPreselectionDone = false;
    public bool preselectionDone { get { return localPreselectionDone; } }
    public string texturePath { get; set; }
    private List<string> localSelectedTiles = new List<string>();
    private List<string> localWallSelectedTiles = new List<string>();
    private List<string> localSlabSelectedTiles = new List<string>();
    private List<string> localTileNames = new List<string>();
    private List<string> localWallTileNames = new List<string>();
    private List<string> localSlabTileNames = new List<string>();
    public ReadOnlyCollection<string> preselectedTiles { get { return localSelectedTiles.AsReadOnly(); } } // preselectedTiles can be read but not modified outside this class
    public ReadOnlyCollection<string> wallPreselectedTiles { get { return localWallSelectedTiles.AsReadOnly(); } }
    public ReadOnlyCollection<string> slabPreselectedTiles { get { return localSlabSelectedTiles.AsReadOnly(); } }
    public ReadOnlyCollection<string> allTileNames { get { return localTileNames.AsReadOnly(); } }
    public ReadOnlyCollection<string> wallTileNames { get { return localWallTileNames.AsReadOnly(); } }
    public ReadOnlyCollection<string> slabTileNames { get { return localSlabTileNames.AsReadOnly(); } }
    public event EventHandler<string> DBAccessError;
    event EventHandler<MyEventArgs> NewTileChoice;

    [Header("DATABASE")]
    public string host;
    public string database, username, password, tilesTable;
    [Header("PROJECT DETAILS")]
    public string clientId;
    public string projectId;

    void Start()
    {
        // Session info
        sessionDateTime = DateTime.Now;
        sessionSqlFormattedDate = sessionDateTime.ToString("yyyy-MM-dd HH:mm:ss");

        // !! YOU NEED TO HAVE SET UP A VRITUALHOST NAMED 'bimexpo', pointing to the 'PHP' folder
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string csvDir = Directory.GetParent(currentDir).Parent.Parent.FullName;
        string csvPath = csvDir + "\\DB_Carrelages_Demo.csv";
        csvPath = csvPath.Replace("\\", "/");                   //SQL needs forwards slashes...
        StartCoroutine(CreateTableFromCSV(csvPath, "tptiles"));
        StartCoroutine(CreateUserChoicesTable());

        // Enable the preselection menu now, to be 100% sure the coroutines here above rune BEFORE that
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "PreselectionMenu")
            {
                go.SetActive(true);
            }
        }
        // Listening to UI state change in order to know when the building is loaded.
        // The 'stateChanged' event handler is signaled when the state of the UI has changed (see UIStateManagerAction, line 148).
        // It takes a UIStateData argument. Indeed, the stateChanged event handler is invoked with 'm_UIStateData' as argument.
        UIStateManager.stateChanged += UIStateManager_stateChanged;
        NewTileChoice += RoomScriptableObject.RecordTileChoice;
    }

    /// <summary>
    /// Event listener that checks for the building to be loaded in order to create some tables and perform some startup actions.
    /// </summary>
    /// <param name="obj">The UIStateData object the event handler was invoked with. In this case it is the 'm_UIStateData' field of the UIStateManager class</param>
    private void UIStateManager_stateChanged(UIStateData obj)
    {
        if (obj.progressData.totalCount > 0 && obj.progressData.currentProgress == obj.progressData.totalCount)    // Then the building is fully loaded
        {
            if (!tablesCreated)
            {
                StartCoroutine(createBuildingTable());
                StartCoroutine(SetDefaultMaterialsJSON()); 
                var prlm = GameObject.Find("PerRoomListMenu").GetComponent<PerRoomListMenu>();
                prlm.Initialize();
                tablesCreated = true;
            }
            //StartCoroutine(CreateSurfaceValidationTable());
        }
    }

    /// <summary>
    /// Prepares the user's tile choices table in the DB. 
    /// </summary>
    IEnumerator CreateUserChoicesTable()
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/CreateUserChoicesTable.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// If not yet existing, creates in DB the table that summarizes which rooms are 'validated' or not.
    /// A validated room is a room for which the user has chosen all the tiles.
    /// </summary>
    public IEnumerator CreateRoomValidationTable(List<string> room_names)
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        for (int i = 0; i < room_names.Count; i++)
        {
            form.AddField("rooms[]", room_names[i]);
        }

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/CreateRoomValidationTable.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Creates, if not yet existing, the table that stores the validation status of each surface of the building.
    /// If the surface is not in the table, it means it's not validated.
    /// </summary>
    IEnumerator CreateSurfaceValidationTable()
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/CreateSurfaceValidationTable.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Creates a test DB of (all) the available tiles. This table is created from a csv file.
    /// </summary>
    /// <param name="csvPath">The path to the csv that is to be used to create the table</param>
    /// <param name="tableName">The name to be given to the table</param>
    /// <returns></returns>
    IEnumerator CreateTableFromCSV(string csvPath, string tableName)
    {
        WWWForm form = new WWWForm();
        form.AddField("tableName", tableName);
        form.AddField("csvPath", csvPath);

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/CreateTableFromCSV.php", form))
        {
            // Request and wait for the desired page.
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Creates a table in DB that represents the whole building. The table contains, for each Wall or Floor detected, its Id, Area, and Level ('Base Constraint').
    /// </summary>
    IEnumerator createBuildingTable()
    {
        //yield return new WaitForSeconds(10); // Waits 10s for the model to be loaded before creating the table

        List<string> surfaceIDs = new List<string>();
        List<string> surfaceArea = new List<string>();
        List<string> surfaceLevels = new List<string>();
        List<string> surfaceRooms = new List<string>();
        List<string> surfaceCats = new List<string>();
        Regex rx = new Regex(@" \[[0-9]*\]$");
        foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
        {
            var meta = go.GetComponent<Metadata>();
            if (meta != null)
            {
                if ((go.name.Contains("Wall") || meta.GetParameter("Category").Contains("Wall") || go.name.Contains("Floor") || meta.GetParameter("Category").Contains("Floor"))
                    && (meta.GetParameter("Id").Length > 0))
                {
                    surfaceIDs.Add(meta.GetParameter("Id"));
                    if (meta.GetParameter("BIMexpoArea") != "")
                    {
                        surfaceArea.Add(meta.GetParameter("BIMexpoArea"));
                    }
                    else
                    {
                        surfaceArea.Add(meta.GetParameter("Area"));
                    }
                    surfaceLevels.Add(meta.GetParameter("Base Constraint"));
                    string roomName = rx.Split(meta.GetParameter(RoomScriptableObject.room_parameter))[0];
                    surfaceRooms.Add(roomName);
                    surfaceCats.Add(meta.GetParameter(RoomScriptableObject.surface_type_parameter));
                }
            }
        }

        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        for (int i = 0; i < surfaceIDs.Count; i++)
        {
            form.AddField("ID[]", surfaceIDs[i]);
            form.AddField("Area[]", surfaceArea[i]);
            form.AddField("Level[]", surfaceLevels[i]);
            form.AddField("Room[]", surfaceRooms[i]);
            form.AddField("TileCat[]", surfaceCats[i]);
        }
        
        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/CreateBuildingTable.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Gets the list of the names ('libelles') of all the preselected tiles in the project.
    /// </summary>
    public void RetrievePreselectedTiles(string category = "all")
    {
        //yield return new WaitForSeconds(10); // If this function is called immediately after CreateTableFromCSV, it needs some time for the table to actually be created

        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        form.AddField("category", category);

        string[] phpReturnedList = { };        

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/GetTilePreselection.php", form))
        {
            www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                //Wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string receivedTilesString = www.downloadHandler.text;
                phpReturnedList = receivedTilesString.Split(';');
            }
        }

        bool startRecordingResults = false;

        foreach (string item in phpReturnedList)
        {
            if (startRecordingResults)
            {
                switch (category)
                {
                    case "all":
                        localSelectedTiles.Add(item);
                        break;
                    case "walls":
                        localWallSelectedTiles.Add(item);
                        break;
                    case "slabs":
                        localSlabSelectedTiles.Add(item);
                        break;
                }
                
            }
            if (item.Contains("RETURNS"))
            {
                startRecordingResults = true;
                switch (category)
                {
                    case "all":
                        localSelectedTiles.Clear();
                        break;
                    case "walls":
                        localWallSelectedTiles.Clear();
                        break;
                    case "slabs":
                        localSlabSelectedTiles.Clear();
                        break;
                }
            }
        }
    }

    public string GetTexturePathFromNameM(string name)
    {
        texturePath = null; // So that is is null as long as the DB request is not done.
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        form.AddField("name", name);

        string[] phpReturnedList = { };

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/GetTexturePathFromName.php", form))
        {
            www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                //Wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string receivedTilesString = www.downloadHandler.text;
                phpReturnedList = receivedTilesString.Split(';');
            }
        }

        List<string> texturePaths = new List<string>();
        bool startRecordingResults = false;

        foreach (string item in phpReturnedList)
        {
            if (startRecordingResults)
            {
                texturePaths.Add(item);
            }
            if (item.Contains("RETURNS"))
            {
                startRecordingResults = true;
            }
        }
        return texturePaths[0];
    }

    /// <summary>
    /// Given a tile name ('libelle'), finds the path to its texture, which is located in the table 'chemin_texture' column.
    /// For the moment this path is simply the name of the folder in which the textures are stored for a given tile.
    /// </summary>
    /// <param name="name">The name of the tile (i.e. the 'libelle').</param>
    /// <returns>The path to the texture, as stored in the table.</returns>
    public IEnumerator GetTexturePathFromName(string name)
    {
        texturePath = null; // So that is is null as long as the DB request is not done.
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        form.AddField("name", name);

        string[] phpReturnedList = { };

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/GetTexturePathFromName.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string receivedTilesString = www.downloadHandler.text;
                phpReturnedList = receivedTilesString.Split(';');
            }
        }

        List<string> texturePaths = new List<string>();
        bool startRecordingResults = false;

        foreach (string item in phpReturnedList)
        {
            if (startRecordingResults)
            {
                texturePaths.Add(item);
            }
            if (item.Contains("RETURNS"))
            {
                startRecordingResults = true;
            }
        }
        texturePath = texturePaths[0];
        //yield return null;

        // TO DO: RETURN the path either via a class variable (but then I can't use it right after, because it might tke some time) or via callback??
        // http://codesaying.com/action-callback-in-unity/
        // https://forum.unity.com/threads/how-to-use-coroutines-and-callback-properly-retrieving-an-array-out-of-an-ienumerator.508017/
    }

    /// <summary>
    /// Sends a WebRequest via PHP script to retrieve a list of the names of either all the tiles, or all the wall tiles, or all the slab tiles in the DB.
    /// The class variables localTileNames, localWallTileNames, or localSlabTileNames are updated accordingly.
    /// </summary>
    /// <param name="filter">The (optional) filter to choose between all, walls, or slabs.</param>
    public IEnumerator ListAllTileNamesInDB(string filter = "all")
    {
        WWWForm form = new WWWForm();
        form.AddField("tilesTableName", tilesTable);

        string[] phpReturnedList = { };
        string phpScript = "http://bimexpo/ListAllTilesNamesInDB.php";
        switch (filter)
        {
            case "all":
                localTileNames.Clear();
                break;
            case "walls":
                phpScript = "http://bimexpo/ListAllWallTilesNamesInDB.php";
                localWallTileNames.Clear();
                break;
            case "slabs":
                phpScript = "http://bimexpo/ListAllSlabTilesNamesInDB.php";
                localSlabTileNames.Clear();
                break;
        }

        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            yield return www.SendWebRequest();
            
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string receivedTilesString = www.downloadHandler.text;
                phpReturnedList = receivedTilesString.Split(';');
            }
        }

        bool startRecordingResults = false;

        foreach (string item in phpReturnedList)
        {
            if (startRecordingResults)
            {
                switch (filter)
                {
                    case "all":
                        localTileNames.Add(item);
                        break;
                    case "walls":
                        localWallTileNames.Add(item);
                        break;
                    case "slabs":
                        localSlabTileNames.Add(item);
                        break;
                }
                
            }
            if (item.Contains("RETURNS"))
            {
                startRecordingResults = true;
            }
        }
    }

    /// <summary>
    /// Given a folder path, this function gets the 1st file in the folder and returns it as a 2D Texture.
    /// </summary>
    /// <param name="FolderPath">The full path of the folder to look into.</param>
    /// <returns>A Texture2D of the 1st file found in the folder.</returns>
    public Texture2D LoadTextureFromDiskFolder(string FolderPath)
    {
        // Load a PNG or JPG file from disk to a Texture2D
        // Returns null if load fails
        Texture2D Tex2D;
        byte[] FileData;
        string[] filesInDir;

        //Files in the directory
        filesInDir = Directory.GetFiles(FolderPath);

        //Get the 1st image within directory
        string picture = filesInDir[0];

        if (File.Exists(picture))
        {
            FileData = File.ReadAllBytes(picture);
            Tex2D = new Texture2D(2, 2);                // Create new "empty" texture
            if (Tex2D.LoadImage(FileData))              // Load the imagedata into the texture (size is set automatically)
                return Tex2D;                           // If data = readable -> return texture
        }
        Debug.Log("File doesn't exist!");
        return null;                                    // Return null if load failed
    }

    public IEnumerator ValidatePreSelection()
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", clientId);
        form.AddField("projectId", projectId);
        foreach (string tile in GameObject.Find("PreselectionMenu").GetComponent<PreselectionMenuScript>().selectedTiles)
        {
            form.AddField("preselectedTiles[]", tile);
        }

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/ValidatePreselections.php", form))
        {
            // Request and wait for the desired page.
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
        localPreselectionDone = true;
    }

    /// <summary>
    /// This function is merely a wrapper function for ProduceAmendment.
    /// This way, it can be associated to a click event on the corresponding button.
    /// </summary>
    public void ProduceAmendmentWrapper()
    {
        StartCoroutine(ProduceAmendment());
    }

    /// <summary>
    /// Produces the amendment in an HTML page, and opens the page.
    /// </summary>
    private IEnumerator ProduceAmendment()
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", clientId);
        form.AddField("projectId", projectId);
        form.AddField("session", sessionSqlFormattedDate);

        string phpScript = "http://bimexpo/CreateAmendment.php";
        
        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            yield return www.SendWebRequest();      // I have to do this here, otherwise www.result is still "InProgress" on the next line, and therefore enters the if, although it is a Success!

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                Application.OpenURL("http://bimexpo/amendment.php?clientId=" + clientId + "&projectId=" + projectId + "&session=" + sessionSqlFormattedDate);
            }
        }
    }

    /// <summary>
    /// This method returns a list of the local paths for the textures that are compatible with the surface provided as argument.
    /// In this simple version, the only filtering is done on the type of surface: wall or floor.
    /// Only the preselected tiles are pulled from DB.
    /// </summary>
    /// <param name="surface">The surface for which the compatible textures are requested.</param>
    /// <returns>A List<string> of all the paths to the texture files.</string></returns>
    public List<string> PullTexturesForSurface(GameObject surface)
    {
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string picturesDir = Directory.GetParent(currentDir).Parent.Parent.FullName + "\\pictures_carrelages\\";

        string phpScript = "http://bimexpo/GetCompatibleTexturesFromDB.php";
        List<string> textures = new List<string>();
        var meta = surface.GetComponent<Metadata>();

        WWWForm form = new WWWForm();
        if (meta != null)
        {
            if (surface.name.Contains("Wall") || meta.GetParameter("Category").Contains("Wall"))
            {
                form.AddField("category", "wall");
            }
            else if (surface.name.Contains("Floor") || meta.GetParameter("Category").Contains("Floor"))
            {
                form.AddField("category", "floor");
            }
            else
            {
                Debug.Log("The type of surface is not recognized");
                return textures;
            }

            JSONArray jsonTextures = new JSONArray();
            bool wait = true;
            StartCoroutine(GetJSONResultFromDBCoroutine(phpScript, (jsonResult) =>
            {
                jsonTextures = jsonResult; // Recuperate jsonResult (which is the argument of the callback method, passed in inside GetJSONResultFromDB. So it is jsonArray.)
                wait = false;               // This line is reached only upon callback completion inside GetJSONResultFromDB.
            }, form));

            // Read the JSON result
            for (int i = 0; i < jsonTextures.Count; i++)
            {
                string[] myfiles = Directory.GetFiles(picturesDir + jsonTextures[i].AsObject["chemin_texture"]);
                textures.Add(myfiles[0]);
            }
            return textures;
        }
        else
        {
            Debug.Log("The selected surface has no metadata!");
            return textures;
        }
    }

    /// <summary>
    /// Extracts the tile dimensions in a 2 items list of integers.
    /// The dimensions are extracted from the tile libelle, since it is part of it.
    /// </summary>
    /// <returns>A List containting 2 integers, which are the dimensions of the tile</returns>
    public List<double> GetTileDimensionsFromLibelle(string libelle)
    {
        List<double> dimensions = new List<double>();
        string[] libelleList = libelle.Split(' ');
        string[] dimList = { };
        foreach (string item in libelleList)
        {
            if (item.Contains("/"))
            {
                dimList = item.Split('/');
                break;
            }
        }
        if (dimList == null || dimList.Length != 2)
        {
            throw new Exception("Can't extract dimensions from tile libelle!");
        }
        foreach (string item in dimList)
        {
            double convertedDim;
            if (Double.TryParse(item, out convertedDim))
            {
                dimensions.Add(convertedDim/100.0);
            }
            else
            {
                throw new Exception("Can't extract dimensions from tile libelle!");
            }
        }
        return dimensions;
    }

    public double GetTilePriceFromLibelle(string libelle)
    {
        double price = -1;
        string stringPrice = "";

        WWWForm form = new WWWForm();
        form.AddField("libelle", libelle);
        string phpScript = "http://bimexpo/GetTilePriceFromLibelle.php";
        string[] phpReturnedList = { };

        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                // Just wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string receivedTilesString = www.downloadHandler.text;
                phpReturnedList = receivedTilesString.Split(';');
            }
        }
        bool startRecordingResults = false;
        foreach (string item in phpReturnedList)
        {
            if (startRecordingResults)
            {
                stringPrice = item;
            }
            if (item.Contains("RETURNS"))
            {
                startRecordingResults = true;
            }
        }
        if (stringPrice != "" && stringPrice.Split('€').Length > 0)
        {
            var toto = stringPrice.Split('€');
            var tata = toto[0];
            if (Double.TryParse(stringPrice.Split('€')[0], out price))
            {
                return price;
            }
            else
            {
                throw new Exception("Couldn't extract the tile price from the DB!");
            }
        }
        else
        {
            throw new Exception("Couldn't extract the tile price from the DB!");
        }
    }

    public void saveComment(string comment, GameObject surface)
    {
        // Getting surface ID
        var meta = surface.GetComponent<Metadata>();
        string surfaceID = null;
        if (meta != null)
        {
            surfaceID = meta.GetParameter("Id");
        }
        else
        {
            throw new Exception("No Id parameter found on surface!");
        }

        string phpScript = "http://bimexpo/CreateComment.php";
        WWWForm form = new WWWForm();
        form.AddField("projectId", projectId);
        form.AddField("clientId", clientId);
        form.AddField("comment", comment);
        form.AddField("session", sessionSqlFormattedDate);
        form.AddField("surfaceID", surfaceID);

        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                // Just wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string receivedTilesString = www.downloadHandler.text;
            }
        }
    }

    public void saveScreenshotToDB(string filename, Vector3 position, Quaternion rotation, GameObject targetSurface)
    {
        // Getting surface ID
        var meta = targetSurface.GetComponent<Metadata>();
        string surfaceID = null;
        if (meta != null)
        {
            surfaceID = meta.GetParameter("Id");
        }
        else
        {
            throw new Exception("No Id parameter found on surface!");
        }

        string phpScript = "http://bimexpo/SaveScreenshot.php";
        WWWForm form = new WWWForm();
        form.AddField("projectId", projectId);
        form.AddField("clientId", clientId);
        form.AddField("filename", filename);
        form.AddField("session", sessionSqlFormattedDate);
        form.AddField("surfaceID", surfaceID);
        form.AddField("positionX", position.x.ToString());
        form.AddField("positionY", position.y.ToString());
        form.AddField("positionZ", position.z.ToString());
        form.AddField("rotationX", rotation.x.ToString());
        form.AddField("rotationY", rotation.y.ToString());
        form.AddField("rotationZ", rotation.z.ToString());
        // Should I also use the Quaternion w component of the rotation? Don't care for the moment.


        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                // Just wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Retrieves the comment from the last session which is associated to the surface passed as argument.
    /// </summary>
    /// <param name="surface">The surface for which the comment is to be retrieved</param>
    /// <returns>The comment</returns>
    public string GetComment(GameObject surface)
    {
        // Getting surface ID
        var meta = surface.GetComponent<Metadata>();
        string surfaceID = null;
        if (meta != null)
        {
            surfaceID = meta.GetParameter("Id");
        }
        else
        {
            throw new Exception("No Id parameter found on surface!");
        }

        string phpScript = "http://bimexpo/GetComment.php";
        string[] phpReturnedList = { };
        WWWForm form = new WWWForm();
        form.AddField("projectId", projectId);
        form.AddField("clientId", clientId);
        form.AddField("surfaceID", surfaceID);

        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                // Just wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                string receivedTilesString = www.downloadHandler.text;
                phpReturnedList = receivedTilesString.Split(';');
            }
        }

        bool startRecordingResults = false;
        string comment = "";

        foreach (string item in phpReturnedList)
        {
            if (startRecordingResults)
            {
                comment = item;
                break;
            }
            if (item.Contains("RETURNS"))
            {
                startRecordingResults = true;
            }
        }
        return comment;
    }

    /// <summary>
    /// Flexible function that gets the result of a PHP script as a JSON array, and that uses a callback function to then pass that result.
    /// </summary>
    /// <param name="scriptName">The PHP script to be called via WebRequest.</param>
    /// <param name="callback">The callback function that will be used to retrieve the JSON array via its argument.</param>
    /// <returns></returns>
    private IEnumerator GetJSONResultFromDBCoroutine(string scriptName, Action<JSONArray> callback, WWWForm form = null)
    {
        string[] phpReturnedList = { };
        UnityWebRequest myWWW;
        if (form != null)
        {
            myWWW = UnityWebRequest.Post(scriptName, form);
        }
        else
        {
            myWWW = UnityWebRequest.Get(scriptName);
        }

        using (UnityWebRequest www = myWWW)
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                DBAccessError?.Invoke(this, "Can't access to database!");
                //throw new Exception("DB Connection Error!"); 
            }
            else
            {
                // Get the response from DB and split messages from json response
                string receivedTilesString = www.downloadHandler.text;
                string jsonArrayString = "";
                phpReturnedList = receivedTilesString.Split(';');
                bool startRecordingResults = false;
                foreach (string item in phpReturnedList)
                {
                    if (startRecordingResults)
                    {
                        jsonArrayString = item;
                        break;
                    }
                    if (item.Contains("RETURNS"))
                    {
                        startRecordingResults = true;
                    }
                }

                // Parse the JSON result into a JSONArray
                JSONArray jsonArray = JSON.Parse(jsonArrayString) as JSONArray;

                // Once the results are obtained, pass them on to callback.
                callback(jsonArray);    
            }
        }
    }

    /// <summary>
    /// Automatically sets default materials on the building, based on materials identified as such in the DB.
    /// </summary>
    private IEnumerator SetDefaultMaterialsJSON()
    {
        List<List<string>> defaultsList = new List<List<string>> { };
        string phpScript = "http://bimexpo/ReadDefaults.php";
        JSONArray jsonMaterials = new JSONArray();

        bool wait = true;
        StartCoroutine(GetJSONResultFromDBCoroutine(phpScript, (jsonResult) =>
        {
            jsonMaterials = jsonResult; // Recuperate jsonResult (which is the argument of the callback method, passed in inside GetJSONResultFromDB. So it is jsonArray.)
            wait = false;               // This line is reached only upon callback completion inside GetJSONResultFromDB.
        }));

        while (wait)    // Wait for the call to DB in GetJSONResultFromDB is done, so we're sure we have now retrieved jsonResult inside jsonMaterials.
        {
            yield return null;
        }

        // Read the JSON result
        for (int i = 0; i < jsonMaterials.Count; i++)
        {
            string surfaceType = jsonMaterials[i].AsObject["surface_type"];
            string inOut = jsonMaterials[i].AsObject["in_out"];
            string matName = jsonMaterials[i].AsObject["material_name"];
            string tiled = jsonMaterials[i].AsObject["tiled"];
            List<string> subList = new List<string>();
            defaultsList.Add(new List<string> { surfaceType, inOut, matName, tiled });
        }

        foreach (List<string> subList in defaultsList)
        {
            // Load the material
            GameObject root = GameObject.Find("Root");
            Component[] children = root.GetComponentsInChildren(typeof(Transform));
            Material matToApply = Resources.Load<Material>("defaults/materials/" + subList[2]);
            matToApply.shader = Shader.Find("UnityReflect/URPOpaque");

            // Detect brick walls and apply material
            foreach (Transform tr in children)
            {
                var meta = tr.gameObject.GetComponent<Metadata>();
                if (meta != null)
                {
                    if (meta.GetParameter("Type").Contains("Brique") && subList[0] == "mur" && subList[1] == "out")
                    {
                        tr.gameObject.GetComponent<MeshRenderer>().material = matToApply;
                    }
                    else if (meta.GetParameter("Type").Contains("Carrelage_Mural") && subList[1] == "in" && subList[0] == "mur" && subList[3] == "1")
                    {
                        tr.gameObject.GetComponent<MeshRenderer>().material = matToApply;
                    }
                    else if (meta.GetParameter("Type").Contains("Plafonnage") && subList[1] == "in" && subList[0] == "mur" && subList[3] == "0")
                    {
                        tr.gameObject.GetComponent<MeshRenderer>().material = matToApply;
                    }
                    else if (meta.GetParameter("Type").Contains("Carrelage") && meta.GetParameter("Family").Contains("Floor") && subList[0] == "sol" && subList[1] == "in" && subList[3] == "1")
                    {
                        tr.gameObject.GetComponent<MeshRenderer>().material = matToApply;
                    }
                }
            }
        }

        yield return null;
    }

    /// <summary>
    /// [OBSOLETE] Automatically sets default materials on the building, based on materials identified as such in the DB.
    /// </summary>
    private IEnumerator SetDefaultMaterials()
    {
        string[] input1 = { "mur", "out" };
        string[] input2 = { "mur", "in" };
        List<List<string>> defaultsList = new List<List<string>>{new List<string> ( input1 ), new List<string>(input2)};
        int nbItems = defaultsList.Count;   // Do it here to avoid infinite loop

        for (int i = 0; i < nbItems; i++)
        {
            List<string> subList = defaultsList[i];
            // Identify the material for outdoor walls, from DB
            WWWForm form = new WWWForm();
            form.AddField("surface_type", subList[0]);
            form.AddField("in_out", subList[1]);
            string phpScript = "http://bimexpo/ReadDefaults.php";
            string[] phpReturnedList = { };
            string materialName = "";

            using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
            {
                www.SendWebRequest();
                while (www.result == UnityWebRequest.Result.InProgress)
                {
                    // Just wait
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    string receivedTilesString = www.downloadHandler.text;
                    Debug.Log(receivedTilesString);
                    phpReturnedList = receivedTilesString.Split(';');
                }
            }
            bool startRecordingResults = false;
            foreach (string item in phpReturnedList)
            {
                if (startRecordingResults)
                {
                    materialName = item;
                }
                if (item.Contains("RETURNS"))
                {
                    startRecordingResults = true;
                }
            }

            subList.Add(materialName);
            defaultsList.Add(subList);
        }
        foreach (List<string> subList in defaultsList)
        {
            // Load the material
            GameObject root = GameObject.Find("Root");
            Component[] children = root.GetComponentsInChildren(typeof(Transform));
            Material matToApply = Resources.Load<Material>("defaults/materials/" + subList[2]);
            //matToApply.shader = Shader.Find("Unlit/Texture");
            matToApply.shader = Shader.Find("UnityReflect/URPOpaque");
            //matToApply.shader.

            // Detect brick walls and apply material
            foreach (Transform tr in children)
            {
                var meta = tr.gameObject.GetComponent<Metadata>();
                if (meta != null)
                {
                    if (meta.GetParameter("Type").Contains("Brique") && subList[1] == "out")
                    {
                        tr.gameObject.GetComponent<MeshRenderer>().material = matToApply;
                    }
                    else if (meta.GetParameter("Type").Contains("Carrelage_Mural") && subList[1] == "in")
                    {
                        tr.gameObject.GetComponent<MeshRenderer>().material = matToApply;
                    }
                }
            }
        }
        
        yield return null;
    }

    /// <summary>
    /// Restores the previous choices a user may have done in another session.
    /// These choices are retrieved from DB and overwrite any choices in the current session.
    /// </summary>
    public IEnumerator RestorePreviousConfig()
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", clientId);
        form.AddField("projectId", projectId);
        string phpScript = "http://bimexpo/GetChoices.php";
        JSONArray jsonResponse = new JSONArray();
        bool wait = true;
        StartCoroutine(GetJSONResultFromDBCoroutine(phpScript, (jsonResult) =>
        {
            jsonResponse = jsonResult; // Recuperate jsonResult (which is the argument of the callback method, passed in inside GetJSONResultFromDB. So it is jsonArray.)
            wait = false;              // This line is reached only upon callback completion inside GetJSONResultFromDB.
        }, form));

        while (wait)    // Wait for the call to DB in GetJSONResultFromDB is done, so we're sure we have now retrieved jsonResult inside jsonMaterials.
        {
            yield return null;
        }

        // Read the JSON result
        DateTime mostRecentSession = DateTime.MinValue;
        List<List<string>> idsAndLibelleAndSession = new List<List<string>>();
        for (int i = 0; i < jsonResponse.Count; i++)
        {
            string id_surface = jsonResponse[i].AsObject["id_surface"];
            string libelle = jsonResponse[i].AsObject["libelle"];
            string sess = jsonResponse[i].AsObject["session"];
            if (mostRecentSession == DateTime.MinValue)
                {
                    mostRecentSession = DateTime.Parse(sess);
                }
            idsAndLibelleAndSession.Add(new List<string> { id_surface, libelle, sess });
        }

        // Now actually restore it
        GameObject root = GameObject.Find("Root");
        NewTilesChoiceMenuScript ntcms = null;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.name == "NewTileChoiceMenu")
            {
                ntcms = go.GetComponent<NewTilesChoiceMenuScript>();
            }
        }

        foreach (List<string> item in idsAndLibelleAndSession)
        {
            if (DateTime.Parse(item[2]) == mostRecentSession)
            {
                Component[] children = root.GetComponentsInChildren(typeof(Transform));
                foreach (Transform tr in children)
                {
                    var meta = tr.gameObject.GetComponent<Metadata>();
                    if (meta != null)
                    {
                        if (meta.GetParameter("Id") == item[0])
                        {
                            //RoomScriptableObject.current_room = tr.gameObject.GetComponent<Metadata>().GetParameter(RoomScriptableObject.room_parameter);
                            //NewTileChoice?.Invoke(this, new MyEventArgs(item[1], RoomScriptableObject.current_room, tr.gameObject));
                            //tr.gameObject.GetComponent<MeshRenderer>().material = _copyingMat;
                            RoomScriptableObject.current_surface = tr.gameObject;
                            ntcms.ApplyChosenMaterialToSurface(item[1]);
                            //ntcms.SaveChosenMaterialToDB(tr.gameObject, item[1]);
                            continue;
                        }
                    }
                }
            }
        }
    }

    public IEnumerator SetSurfaceValidity(int surfaceId, bool valid)
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        form.AddField("surfaceId", surfaceId);
        if (valid)
        {
            form.AddField("validity", "1");
        }
        else
        {
            form.AddField("validity", "0");
        }

        var fao = GameObject.Find("Root").GetComponent<FindAllObjects>();
        fao.surfacesValidities[surfaceId] = valid;

        string phpScript = "http://bimexpo/SetSurfaceValidity.php";

        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            yield return www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                // Just wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }

        // Now check if that changes any room validity
        bool roomFound = false;
        string roomName = null;
        foreach (KeyValuePair<string, List<int>> item in fao.surfacesAPerRoom)
        {
            roomName = item.Key;
            foreach (int surfId in item.Value)
            {
                if (surfId == surfaceId)
                {
                    roomFound = true;
                    break;
                }
            }
            if (roomFound)
            {
                break;
            }
        }
        if (!roomFound)
        {
            throw new Exception("Room not found!");
        }

        var prlms = GameObject.Find("PerRoomListMenu").GetComponent<PerRoomListMenu>();
        StartCoroutine(CheckRoomValidityFromDB(roomName, (validationResult) =>
        {
            if (validationResult)
            {
                fao.roomValidities[roomName] = true;
                if (prlms.room_name.text == roomName)
                {
                    prlms.thisRoomValidity = true;
                }
            }
            else
            {
                fao.roomValidities[roomName] = false;
                if (prlms.room_name.text == roomName)
                {
                    prlms.thisRoomValidity = false;
                }
            }
        }));
    }

    public IEnumerator SetRoomValidity(string room_name, bool valid)
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        form.AddField("room_name", room_name);
        if (valid)
        {
            form.AddField("validity", "1");
        }
        else
        {
            form.AddField("validity", "0");
        }

        string phpScript = "http://bimexpo/SetRoomValidity.php";

        using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
        {
            yield return www.SendWebRequest();
            while (www.result == UnityWebRequest.Result.InProgress)
            {
                // Just wait
            }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    public IEnumerator GetValidatedRooms()
    {
        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);

        string phpScript = "http://bimexpo/GetValidatedRooms.php";
        JSONArray jsonResponse = new JSONArray();
        bool wait = true;
        StartCoroutine(GetJSONResultFromDBCoroutine(phpScript, (jsonResult) =>
        {
            jsonResponse = jsonResult; // Recuperate jsonResult (which is the argument of the callback method, passed in inside GetJSONResultFromDB. So it is jsonArray.)
            wait = false;              // This line is reached only upon callback completion inside GetJSONResultFromDB.
        }, form));

        while (wait)    // Wait for the call to DB in GetJSONResultFromDB is done, so we're sure we have now retrieved jsonResult inside jsonMaterials.
        {
            yield return null;
        }

        // Read the JSON result
        for (int i = 0; i < jsonResponse.Count; i++)
        {
            string id_surface = jsonResponse[i].AsObject["room_name"];
            int validated = jsonResponse[i].AsObject["validated"].AsInt;
        }
    }

    public IEnumerator GetAllSurfacesValidities(Action<Dictionary<int, Tuple<bool, string>>> performActionOnSurfacesList)
    {
        var surfacesDict = new Dictionary<int, Tuple<bool, string>>();
        var fao = GameObject.Find("Root").GetComponent<FindAllObjects>();

        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);

        string phpScript = "http://bimexpo/CheckAllSurfacesValidities.php";
        JSONArray jsonResponse = new JSONArray();
        bool wait = true;
        StartCoroutine(GetJSONResultFromDBCoroutine(phpScript, (jsonResult) =>
        {
            jsonResponse = jsonResult; // Recuperate jsonResult (which is the argument of the callback method, passed in inside GetJSONResultFromDB. So it is jsonArray.)
            wait = false;              // This line is reached only upon callback completion inside GetJSONResultFromDB.
        }, form));

        while (wait)    // Wait for the call to DB in GetJSONResultFromDB is done, so we're sure we have now retrieved jsonResult inside jsonMaterials.
        {
            yield return null;
        }

        // Read the JSON result
        for (int i = 0; i < jsonResponse.Count; i++)
        {
            int id_surface = jsonResponse[i].AsObject["id_surface"].AsInt;
            bool validated = (jsonResponse[i].AsObject["validated"] == "1");
            string room_name = jsonResponse[i].AsObject["room"];
            var myTuple = new Tuple<bool, string>(validated, room_name);
            surfacesDict.Add(id_surface, myTuple);
        }
        performActionOnSurfacesList(surfacesDict);
    }

    public IEnumerator CheckRoomValidityFromDB(string roomName, Action<bool> performAction)
    {
        var prlms = GameObject.Find("PerRoomListMenu").GetComponent<PerRoomListMenu>();

        WWWForm form = new WWWForm();
        form.AddField("clientId", GameObject.Find("Root").GetComponent<DBInteractions>().clientId);
        form.AddField("projectId", GameObject.Find("Root").GetComponent<DBInteractions>().projectId);
        form.AddField("room", roomName);

        string phpScript = "http://bimexpo/CheckRoomValidity.php";
        JSONArray jsonResponse = new JSONArray();
        bool wait = true;
        StartCoroutine(GetJSONResultFromDBCoroutine(phpScript, (jsonResult) =>
        {
            jsonResponse = jsonResult; // Recuperate jsonResult (which is the argument of the callback method, passed in inside GetJSONResultFromDB. So it is jsonArray.)
            wait = false;              // This line is reached only upon callback completion inside GetJSONResultFromDB.
        }, form));

        while (wait)    // Wait for the call to DB in GetJSONResultFromDB is done, so we're sure we have now retrieved jsonResult inside jsonMaterials.
        {
            yield return null;
        }

        // Read the JSON result
        bool valid = true;
        for (int i = 0; i < jsonResponse.Count; i++)
        {
            string id_surface = jsonResponse[i].AsObject["id_surface"];
            int validated = jsonResponse[i].AsObject["validated"].AsInt;
            if (validated != 1)
            {
                valid = false;
                break;
            }
        }
        performAction(valid);
    }

    public void SaveCurrentSessionToDB()
    {
        StartCoroutine(SaveTilesToDB());
        StartCoroutine(SaveScreenshotsToDB());
        StartCoroutine(SaveCommentsToDB());
    }

    IEnumerator SaveTilesToDB()
    {
        WWWForm form = new WWWForm();
        form.AddField("session", sessionSqlFormattedDate);
        form.AddField("clientId", clientId);
        form.AddField("projectId", projectId);
        int nb_modified = 0;
        foreach (DataTable room_table in RoomScriptableObject.roomsDataSet.Tables)
        {
            string filter = "modified";
            foreach (DataRow row in room_table.Select(filter))
            {
                form.AddField("ids[]", row["id"].ToString());
                form.AddField("tiles[]", row["tile"].ToString());
                form.AddField("prices[]", row["price"].ToString());
                nb_modified += 1;
            }
        }

        if (nb_modified > 0)
        {
            string phpScript = "http://bimexpo/SaveAllTileChoicesToDB.php";
            using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
            {
                www.SendWebRequest();
                while (www.result == UnityWebRequest.Result.InProgress)
                {
                    // Just wait
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    DBAccessError?.Invoke(this, "Can't access to database!");
                }
                else
                {
                    MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
                    mh.ShowInfoMessage("Informations sauvegardées en DB");
                }
            }
        }
        yield return null;
    }

    IEnumerator SaveScreenshotsToDB()
    {
        WWWForm form = new WWWForm();
        form.AddField("session", sessionSqlFormattedDate);
        form.AddField("clientId", clientId);
        form.AddField("projectId", projectId);
        int nb_sc = 0;
        foreach (DataTable room_table in RoomScriptableObject.roomsDataSet.Tables)
        {
            foreach (DataRow row in room_table.Rows)
            {
                if (!DBNull.Value.Equals(row["screenshots"]))
                {
                    List<string> sc_list = (List<string>)(IEnumerable<string>)row["screenshots"];
                    foreach (string sc_file_path in sc_list)
                    {
                        form.AddField("ids[]", row["id"].ToString());
                        form.AddField("screenshots[]", sc_file_path);
                        nb_sc += 1;
                    }
                }
            }
        }

        if (nb_sc > 0)
        {
            string phpScript = "http://bimexpo/SaveAllScreenshotsToDB.php";
            using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
            {
                www.SendWebRequest();
                while (www.result == UnityWebRequest.Result.InProgress)
                {
                    // Just wait
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    DBAccessError?.Invoke(this, "Can't access to database!");
                }
                else
                {
                    MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
                    mh.ShowInfoMessage("Informations sauvegardées en DB");
                }
            }
        }
        yield return null;
    }

    IEnumerator SaveCommentsToDB()
    {
        WWWForm form = new WWWForm();
        form.AddField("session", sessionSqlFormattedDate);
        form.AddField("clientId", clientId);
        form.AddField("projectId", projectId);
        int nb_comments = 0;
        string filter = "comment <> ''";
        foreach (DataTable room_table in RoomScriptableObject.roomsDataSet.Tables)
        {
            foreach (DataRow row in room_table.Select(filter))
            {
                form.AddField("ids[]", row["id"].ToString());
                form.AddField("comments[]", row["comment"].ToString());
                nb_comments += 1;
            }
        }

        if (nb_comments > 0)
        {
            string phpScript = "http://bimexpo/SaveAllCommentsToDB.php";
            using (UnityWebRequest www = UnityWebRequest.Post(phpScript, form))
            {
                www.SendWebRequest();
                while (www.result == UnityWebRequest.Result.InProgress)
                {
                    // Just wait
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    DBAccessError?.Invoke(this, "Can't access to database!");
                }
                else
                {
                    string receivedTilesString = www.downloadHandler.text;
                    Debug.Log(receivedTilesString);
                    MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
                    mh.ShowInfoMessage("Informations sauvegardées en DB");
                }
            }
        }
        yield return null;
    }
}
