using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Reflect;
using System.IO;
using System.Reflection;
using UnityEngine.Networking;

public class TilesChoiceMenuScript : MonoBehaviour
{
    public string chosenMaterial, chosenTexturePath;
    public GameObject target;
    private Button okButton, okScButton, cancelButton;
    public bool selectionDone = false;

    //CEDRIC
    ChangeMaterial changeMatScript;

    void OnEnable()
    {
        //Create the menu
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        okButton = rootVisualElement.Q<Button>("ok-button");
        okButton.styleSheets.Add(Resources.Load<StyleSheet>("USS/testVE"));
        okButton.AddToClassList("ok-but");
        okButton.RegisterCallback<ClickEvent>(ev => SaveChosenMaterialToDB());
        okButton.clicked += ApplyChosenMaterialToSurface;
        okButton.RegisterCallback<ClickEvent>(ev => CloseMenu(selectionDone));

        cancelButton = rootVisualElement.Q<Button>("cancel");
        cancelButton.RegisterCallback<ClickEvent>(ev => CloseMenu());

        okScButton = rootVisualElement.Q<Button>("ok-screenshot-button");
        okScButton.styleSheets.Add(Resources.Load<StyleSheet>("USS/testVE"));
        okScButton.AddToClassList("ok-but");
     
        okScButton.RegisterCallback<ClickEvent>(ev => SaveChosenMaterialToDB());
        okScButton.clicked += ApplyChosenMaterialToSurface;

        var mh = GameObject.Find("Root").GetComponent<MenusHandler>();

        okScButton.RegisterCallback<ClickEvent>(ev => mh.saveScreenshotWrapper(target, selectionDone));
        okScButton.RegisterCallback<ClickEvent>(ev => CloseMenu(selectionDone));

        VisualElement imgContainer = rootVisualElement.Q<VisualElement>("img-container");
        imgContainer.styleSheets.Add(Resources.Load<StyleSheet>("USS/testVE"));
        imgContainer.AddToClassList("medaillon-container");

        Label menuTitle = rootVisualElement.Q<Label>("menu-title");
        //menuTitle.styleSheets.Add(Resources.Load<StyleSheet>("USS/testVE"));
        //menuTitle.AddToClassList("title-menu");

        PopulateMenu();
    }

    /// <summary>
    /// Changes the visual aspect of the tile that is selected in the menu.
    /// </summary>
    /// <param name="name">The 'libellé' of the tile</param>
    public void SelectMaterial(string name)
    {
        selectionDone = true;
        var webScript = GameObject.Find("Root").GetComponent<Web>();
        chosenMaterial = name;
        chosenTexturePath = webScript.GetTexturePathFromNameM(name);
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string tilePicturesFolder = Directory.GetParent(currentDir).Parent.Parent.FullName + "\\pictures_carrelages\\";
        chosenTexturePath = tilePicturesFolder + chosenTexturePath;

        //Change visual aspect of the chosen material, in the menu
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        VisualElement myBox = rootVisualElement.Q<VisualElement>("img-container");
        IEnumerable<VisualElement> tiles = myBox.Children();
        foreach (VisualElement tile in tiles)
        {
            if (tile.name == name)
            {
                tile.RemoveFromClassList("medaillon");
                tile.AddToClassList("selected-medaillon");
            }
            else
            {
                tile.RemoveFromClassList("selected-medaillon");
                tile.AddToClassList("medaillon");
            }
        }
    }

    public void ApplyChosenMaterialToSurface()
    {
        // First check if a selection was done
        var mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        if (!selectionDone)
        {
            mh.ShowInfoMessage("Please first make a material selection.");
            return;
        }
        var webScript = GameObject.Find("Root").GetComponent<Web>();

        // Get the tile dimensions
        List<double> tileDimensions = new List<double>();
        try
        {
            tileDimensions = webScript.GetTileDimensionsFromLibelle(chosenMaterial);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            throw;
        }

        // Then continue with material application onto surface
        var texture = webScript.LoadTextureFromDiskFolder(chosenTexturePath);
        Vector4 tileDimVect = new Vector4((float) tileDimensions[0], (float) tileDimensions[1], 0f, 0f);
        Material tempMat = new Material(Shader.Find("Shader Graphs/TileShader2"));
        tempMat.mainTexture = texture;
        tempMat.SetVector("_TileSize", tileDimVect);

        Texture2D texMort = (Texture2D)tempMat.mainTexture;
        tempMat.mainTexture = texMort;

        foreach (Renderer rend in target.GetComponents<Renderer>())
        {
            var mats = new Material[rend.sharedMaterials.Length];
            for (var j = 0; j < rend.sharedMaterials.Length; j++)
            {
                mats[j] = tempMat;
            }
            rend.sharedMaterials = mats;
        }
        target.GetComponent<MeshRenderer>().material = tempMat;
        changeMatScript = GameObject.Find("Root").GetComponent<ChangeMaterial>();
        changeMatScript.HighlightObject(target, false);
        changeMatScript.functionReplaceCalled = true;
        return;
    }

    /// <summary>
    /// Applies a material onto a surface.
    /// </summary>
    /// <param name="materialName">The libelle of the material to be applied, as a string.</param>
    /// <param name="surface">The surface onto which the material is to be applied, as a GameObject.</param>
    public void ApplyMaterialToSurface(string materialName, GameObject surface)
    {
        var webScript = GameObject.Find("Root").GetComponent<Web>();

        // Get the tile dimensions
        List<double> tileDimensions = new List<double>();
        try
        {
            tileDimensions = webScript.GetTileDimensionsFromLibelle(materialName);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            throw;
        }

        string materialTexturePath = webScript.GetTexturePathFromNameM(materialName);
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string tilePicturesFolder = Directory.GetParent(currentDir).Parent.Parent.FullName + "\\pictures_carrelages\\";
        materialTexturePath = tilePicturesFolder + materialTexturePath;

        // Then continue with material application onto surface
        var texture = webScript.LoadTextureFromDiskFolder(materialTexturePath);
        Vector4 tileDimVect = new Vector4((float)tileDimensions[0], (float)tileDimensions[1], 0f, 0f);
        Material tempMat = new Material(Shader.Find("Shader Graphs/TileShader2"));
        tempMat.mainTexture = texture;
        tempMat.SetVector("_TileSize", tileDimVect);

        Texture2D texMort = (Texture2D)tempMat.mainTexture;
        tempMat.mainTexture = texMort;

        foreach (Renderer rend in surface.GetComponents<Renderer>())
        {
            var mats = new Material[rend.sharedMaterials.Length];
            for (var j = 0; j < rend.sharedMaterials.Length; j++)
            {
                mats[j] = tempMat;
            }
            rend.sharedMaterials = mats;
        }
        surface.GetComponent<MeshRenderer>().material = tempMat;

        return;
    }

    /// <summary>
    /// Saves the choice of a tile on a given surface. This will save the choice in the DB.
    /// If no argument are provided, it uses the class variables chosenMaterial and selectedObject for the material and the surface, respectively.
    /// Otherwise, it uses the arguments provided.
    /// </summary>
    public void SaveChosenMaterialToDB(GameObject surface = null, string material = null)
    {
        var changeMatScript = GameObject.Find("Root").GetComponent<ChangeMaterial>();
        string matToApply = chosenMaterial;
        GameObject surfaceToBeApplied = changeMatScript.selectedObject;

        // First check if both arguments were provided or not
        bool argsProvided = false;
        if (surface != null && material != null)
        {
            argsProvided = true;
        }
        if (material != null)
        {
            matToApply = material;
        }
        if (surface != null)
        {
            surfaceToBeApplied = surface;
        }

        // Then, if no arguments were provided, check if a selection was done
        var mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        if (!selectionDone && !argsProvided)
        {
            mh.ShowInfoMessage("Please first make a material selection.");
            return;
        }

        var webScript = GameObject.Find("Root").GetComponent<Web>();
        
        WWWForm form = new WWWForm();
        form.AddField("surfaceId", surfaceToBeApplied.GetComponent<Metadata>().GetParameter("Id"));
        form.AddField("tileName", matToApply);
        form.AddField("clientId", webScript.clientId);
        form.AddField("projectId", webScript.projectId);
        form.AddField("tilePrice", webScript.GetTilePriceFromLibelle(matToApply).ToString());
        form.AddField("surfaceArea", surfaceToBeApplied.GetComponent<Metadata>().GetParameter("Area"));
        form.AddField("session", webScript.sessionSqlFormattedDate);

        using (UnityWebRequest www = UnityWebRequest.Post("http://bimexpo/SaveMaterialChoiceToDB.php", form))
        {
            // Request and wait for the desired page.
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
                Debug.Log(www.downloadHandler.text);
            }
        }
    }

    void PopulateMenu()
    {
        //Recuperate the hit surface
        target = GameObject.Find("Root").GetComponent<ChangeMaterial>().selectedObject;

        //Recuperate the list of preselected tiles - from DB
        var webScript = GameObject.Find("Root").GetComponent<Web>();
        
        //Populate the menu with this selection
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        VisualElement myBox = rootVisualElement.Q<VisualElement>("img-container");
        Button okButton = rootVisualElement.Q<Button>("ok-button");

        //The menu should only suggest tiles for walls or for ground, depending on what was hit
        List<string> filteredList = new List<string>();
        if (target == null)
        {
            Debug.LogError("Tile choice menu didn't get the selected surface!");
            return;
        }
        if (target.GetComponent<Metadata>().GetParameter("Category").Contains("Wall"))
        {
            webScript.RetrievePreselectedTiles("walls"); //No need for coroutine, we have to wait for this menu anyways..
            List<string> selectedTiles = new List<string>(webScript.wallPreselectedTiles);
            filteredList = selectedTiles;
        }
        //filteredList = player.GetComponent<DBInteractions>().FilterWallsOnlyFromTileList(selectedTiles);
        else if (target.GetComponent<Metadata>().GetParameter("Category").Contains("Floor"))
        {
            webScript.RetrievePreselectedTiles("slabs"); //No need for coroutine, we have to wait for this menu anyways..
            List<string> selectedTiles = new List<string>(webScript.slabPreselectedTiles);
            filteredList = selectedTiles;
        }
        //filteredList = player.GetComponent<DBInteractions>().FilterSlabsOnlyFromTileList(selectedTiles);
        else
            Debug.Log("The hit surface is not categorized as wall or floor.");

        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string tilePicturesFolder = Directory.GetParent(currentDir).Parent.Parent.FullName + "\\pictures_carrelages\\";
        string texturePath;
        foreach (string tileName in filteredList)
        {
            texturePath = webScript.GetTexturePathFromNameM(tileName);
            var myVE = new VisualElement();
            myVE.styleSheets.Add(Resources.Load<StyleSheet>("USS/testVE"));
            myVE.AddToClassList("medaillon");
            myVE.style.backgroundImage = webScript.LoadTextureFromDiskFolder(tilePicturesFolder + texturePath);
            myVE.name = tileName;
            myBox.Add(myVE);
            myVE.RegisterCallback<ClickEvent>(ev => SelectMaterial(tileName));
        }
    }

    /// <summary>
    /// Closes the menu.
    /// </summary>
    void CloseMenu(bool isSelectionDone = true)
    {
        if (isSelectionDone)
        {
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            VisualElement myBox = rootVisualElement.Q<VisualElement>("img-container");
            List<VisualElement> myBoxList = new List<VisualElement>();
            foreach (VisualElement ve in myBox.Children())
            {
                myBoxList.Add(ve);
            }
            foreach (VisualElement item in myBoxList)
            {
                myBox.Remove(item);
            }
            GameObject.Find("TileChoiceMenu").SetActive(false);
            selectionDone = false;
        }
    }
}
