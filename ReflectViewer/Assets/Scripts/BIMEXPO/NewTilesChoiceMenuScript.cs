using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Reflect;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class NewTilesChoiceMenuScript : MonoBehaviour
{
    public static bool isMenuShown = false;
    VisualElement tilesContainer, roomInfo, infoBox, infoHolder, roomSurfaces, commentHolder; 
    public static GameObject modifiedObj { get; private set; }
    Label objTitle, roomNameLabel;
    ChangeMaterial cms;
    public Dictionary<int, string> tilesPerSurfaceId;
    // Event handler
    public event EventHandler<MyEventArgs> NewTileChoice;
    void OnEnable()
    {
        RoomScriptableObject.current_surface = null;
        VisualElement rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        tilesContainer = rootVisualElement.Q<VisualElement>("tilesContainer");
        roomInfo = rootVisualElement.Q<VisualElement>("roomInfo");
        commentHolder = roomInfo.Q<VisualElement>("commentHolder");

        // Comment button
        Button commentButton = roomInfo.Q<Button>("comment");
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        commentButton.RegisterCallback<ClickEvent>(ev => mh.ActivateCommentMenu());
        commentButton.RegisterCallback<ClickEvent>(ev => MakeMenuDiscrete());

        roomSurfaces = rootVisualElement.Q<VisualElement>("roomSurfaces");
        roomNameLabel = rootVisualElement.Q<Label>("roomName");
        infoBox = rootVisualElement.Q<VisualElement>("infoText");
        objTitle = rootVisualElement.Q<Label>("title");
        infoHolder = rootVisualElement.Q<VisualElement>("infoHolder");
        tilesPerSurfaceId = new Dictionary<int, string>();

        SlidingMenu sm = GameObject.Find("SlidingMenu").GetComponent<SlidingMenu>();
        sm.RoomButtonClickedEvent += Sm_RoomButtonClickedEvent;

        NewTileChoice += RoomScriptableObject.RecordTileChoice;
    }

    private void Sm_RoomButtonClickedEvent(object sender, string e)
    {
        if (e != null)
        {
            RoomScriptableObject.current_room = e;
        }
    }

    void PopulateInfoMenu()
    {
        roomInfo.AddToClassList("menu-white-background");
        commentHolder.style.display = DisplayStyle.Flex;
        roomNameLabel.style.display = DisplayStyle.Flex;
        roomNameLabel.text = RoomScriptableObject.current_room + " - Choix réalisés";
        List<int> spr = GameObject.Find("Root").GetComponent<FindAllObjects>().surfacesAPerRoom[RoomScriptableObject.current_room];
        foreach (int id in spr)
        {
            string choiceForThisObject;
            try
            {
                choiceForThisObject = tilesPerSurfaceId[id];
            }
            catch (Exception)
            {
                choiceForThisObject = "Pas de choix réalisé!";
            }
            Label surfaceId = new Label("Surface " + id.ToString() + ": " + choiceForThisObject);
            surfaceId.AddToClassList("info-label");
            roomSurfaces.Add(surfaceId);
        }
    }

    public void PopulateMenu(GameObject go)
    {
        isMenuShown = true;
        RoomScriptableObject.current_surface = go;

        // Apply background to show menu
        tilesContainer.AddToClassList("menu-white-background");

        // If the surface is not comprised in the default price, the user should be warned
        if (RoomScriptableObject.current_surface.GetComponent<Metadata>().GetParameter(RoomScriptableObject.surface_type_parameter) == "0")
        {
            Label warning = new Label("Attention, le carrelage de cette surface n'est pas compris dans le prix de base.");
            warning.name = "warningLabel";
            warning.AddToClassList("warning-label");
            warning.style.width = Length.Percent(100.0f);
            tilesContainer.Add(warning);
        }

        //Recuperate the list of preselected tiles - from DB
        //The menu should only suggest tiles for walls or for ground, depending on what was hit
        var webScript = GameObject.Find("Root").GetComponent<Web>();
        List<string> filteredList = new List<string>();

        if (go.GetComponent<Metadata>().GetParameter("Category").Contains("Wall"))
        {
            webScript.RetrievePreselectedTiles("walls"); //No need for coroutine, we have to wait for this menu anyways..
            List<string> selectedTiles = new List<string>(webScript.wallPreselectedTiles);
            filteredList = selectedTiles;
        }
        else if (go.GetComponent<Metadata>().GetParameter("Category").Contains("Floor"))
        {
            webScript.RetrievePreselectedTiles("slabs"); //No need for coroutine, we have to wait for this menu anyways..
            List<string> selectedTiles = new List<string>(webScript.slabPreselectedTiles);
            filteredList = selectedTiles;
        }
        else
            Debug.Log("The hit surface is not categorized as wall or floor.");

        //Populate the menu with this selection
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string tilePicturesFolder = Directory.GetParent(currentDir).Parent.Parent.FullName + "\\pictures_carrelages\\";
        string texturePath;
        foreach (string tileName in filteredList)
        {
            texturePath = webScript.GetTexturePathFromNameM(tileName);
            Button tileButton = new Button();
            tileButton.AddToClassList("tile-button");
            tileButton.text = "";
            tileButton.style.backgroundImage = webScript.LoadTextureFromDiskFolder(tilePicturesFolder + texturePath);
            tileButton.name = tileName;
            tilesContainer.Add(tileButton);
            tileButton.RegisterCallback<MouseEnterEvent>(ev => ShowObjectInfo(go, true, tileName));
            tileButton.RegisterCallback<MouseLeaveEvent>(ev => ShowObjectInfo(null, false));
            tileButton.RegisterCallback<MouseOverEvent>(ev => ApplyChosenMaterialToSurface(tileName));
        }
        PopulateInfoMenu();
    }

    public void ApplyChosenMaterialToSurface(string tileName)
    {
        // First save that choice into the corresponding RoomScriptableObject
        int surface_id = Int32.Parse(RoomScriptableObject.current_surface.GetComponent<Metadata>().GetParameter("Id"));
        NewTileChoice?.Invoke(this, new MyEventArgs(tileName, RoomScriptableObject.current_room));

        // If this surface was already applied some material, then it has some materials with the testshaderlit Shader
        // Erase them before applying a new material, otherwise the materials just keep adding on top of each other.
        foreach (Renderer rend in RoomScriptableObject.current_surface.GetComponents<Renderer>())
        {
            int i = 0;
            List<int> indexesToDestroy = new List<int>();
            foreach (Material mat in rend.sharedMaterials)
            {
                if (mat.shader.name.Contains("TileShader2"))
                {
                    indexesToDestroy.Add(i);
                    i += 1;
                }
            }
            for (int j = 0; j < indexesToDestroy.Count ; j++)
            {
                Destroy(rend.sharedMaterials[j]);
            }
        }

        // Get some scripts
        var cms = GameObject.Find("Root").GetComponent<ChangeMaterial>();
        var webScript = GameObject.Find("Root").GetComponent<Web>();

        // Get the tile dimensions
        List<double> tileDimensions = new List<double>();
        try
        {
            tileDimensions = webScript.GetTileDimensionsFromLibelle(tileName);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            throw;
        }

        // Then continue with material application onto surface
        string chosenTexturePath = webScript.GetTexturePathFromNameM(tileName);
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string tilePicturesFolder = Directory.GetParent(currentDir).Parent.Parent.FullName + "\\pictures_carrelages\\";
        chosenTexturePath = tilePicturesFolder + chosenTexturePath;
        var texture = webScript.LoadTextureFromDiskFolder(chosenTexturePath);
        Vector4 tileDimVect = new Vector4((float)tileDimensions[0], (float)tileDimensions[1], 0f, 0f);
        Material tempMat = new Material(Shader.Find("Shader Graphs/TileShader2"));
        tempMat.mainTexture = texture;
        tempMat.SetVector("_TileSize", tileDimVect);

        Texture2D texMort = (Texture2D)tempMat.mainTexture;
        tempMat.mainTexture = texMort;

        // Un-highlight the surface if it was highlighted
        cms.HighlightObject(RoomScriptableObject.current_surface, false);

        // Apply the tile
        foreach (Renderer rend in RoomScriptableObject.current_surface.GetComponents<Renderer>())
        {
            var mats = new Material[rend.sharedMaterials.Length];
            for (var j = 0; j < rend.sharedMaterials.Length; j++)
            {
                if (!rend.sharedMaterials[j].name.Contains("OutlineMask") && !rend.sharedMaterials[j].name.Contains("OutlineFill"))
                {
                    mats[j] = tempMat;
                }
                else
                {
                    mats[j] = rend.sharedMaterials[j];  // If the was an outline, we simply leave it there.
                }
            }
            rend.sharedMaterials = mats;
        }

        RoomScriptableObject.current_surface.GetComponent<MeshRenderer>().material = tempMat; // This line is probably useless since the render's materials already have been changed.

        // Need to reapply the highlighting here, because we just changed the shader
        if (RoomScriptableObject.current_surface.GetComponent<Metadata>().GetParameter(RoomScriptableObject.surface_type_parameter) == "A")
        {
            cms.HighlightObject(RoomScriptableObject.current_surface, true);
        }
        else if (RoomScriptableObject.current_surface.GetComponent<Metadata>().GetParameter(RoomScriptableObject.surface_type_parameter) == "0")
        {
            cms.HighlightObject(RoomScriptableObject.current_surface, true, Color.yellow);
        }
        

        // Save the choice in a list and update info menu
        try
        {
            tilesPerSurfaceId.Add(Int32.Parse(RoomScriptableObject.current_surface.GetComponent<Metadata>().GetParameter("Id")), tileName);
        }
        catch (Exception)
        {
            tilesPerSurfaceId[Int32.Parse(RoomScriptableObject.current_surface.GetComponent<Metadata>().GetParameter("Id"))] = tileName;
        }
        HideInfoMenu();
        PopulateInfoMenu();
        return;
    }

    /// <summary>
    /// Removes all the buttons from the menu, making it thus invisible.
    /// </summary>
    public void HideMenu()
    {
        isMenuShown = false;
        tilesContainer.RemoveFromClassList("menu-white-background");

        // Remove warning if there was one
        Label wl = null;
        wl = tilesContainer.Q<Label>("warningLabel");
        if (wl != null)
        {
            tilesContainer.Remove(wl);
        }

        List<VisualElement> listOfButtons = new List<VisualElement>();
        foreach (Button b in tilesContainer.Children())
        {
            listOfButtons.Add(b);
        }
        foreach (Button item in listOfButtons)
        {
            tilesContainer.Remove(item);
        }
        HideInfoMenu();
    }

    void HideInfoMenu()
    {
        roomInfo.RemoveFromClassList("menu-white-background");
        roomNameLabel.style.display = DisplayStyle.None;
        commentHolder.style.display = DisplayStyle.None;

        List<Label> listOfSurfaces = new List<Label>();
        foreach (Label l in roomSurfaces.Children())
        {
            listOfSurfaces.Add(l);
        }
        foreach (Label item in listOfSurfaces)
        {
            roomSurfaces.Remove(item);
        }
    }

    /// <summary>
    /// Shows or hides a little menu giving info on the object currently being changed.
    /// </summary>
    /// <param name="go">The object being changed. Can be set to null when hiding the menu.</param>
    /// <param name="show">If true, shows the menu. Hides it otherwise.</param>
    public void ShowObjectInfo(GameObject go, bool show, string tileName = "")
    {
        if (go != null)
        {
            objTitle.text = go.GetComponent<Metadata>().GetParameter("Id");

            if (go.GetComponent<Metadata>().GetParameter("BIMexpoArea") != "")
            {
                string area = "Surface: " + go.GetComponent<Metadata>().GetParameter("BIMexpoArea");
                Label infoLine = new Label();
                infoLine.AddToClassList("info-label");
                infoLine.text = area;
                infoHolder.Add(infoLine);
            }
            if (tileName != "")
            {
                string cleanTileName = tileName;
                tileName = "Carrelage: " + tileName;
                Label infoLine = new Label();
                infoLine.AddToClassList("info-label");
                infoLine.text = tileName;
                infoHolder.Add(infoLine);

                if (go.GetComponent<Metadata>().GetParameter("BIMexpoArea") != "")
                {
                    Web ws = GameObject.Find("Root").GetComponent<Web>();
                    string cleanArea = BuildingInfo.areaRegex.Split(go.GetComponent<Metadata>().GetParameter("BIMexpoArea"))[0];
                    double price = ws.GetTilePriceFromLibelle(cleanTileName);
                    double totalPrice = price * Convert.ToDouble(cleanArea);
                    Label priceLine = new Label();
                    priceLine.AddToClassList("info-label");
                    priceLine.text = "Prix de la surface: " + totalPrice.ToString() + "euros";
                    infoHolder.Add(priceLine);
                }
            }
        }
           
        switch (show)
        {
            case true:
                infoBox.style.display = DisplayStyle.Flex;
                break;
            case false:
                List<Label> listOfInfos = new List<Label>();
                foreach (Label l in infoHolder.Children())
                {
                    listOfInfos.Add(l);
                }
                foreach (Label item in listOfInfos)
                {
                    infoHolder.Remove(item);
                }
                infoBox.style.display = DisplayStyle.None;
                break;
        }
    }

    /// <summary>
    /// Slides up the menu so that there is more screen space.
    /// </summary>
    public void MakeMenuDiscrete()
    {
        if (!isMenuShown)
        {
            return;
        }
        // see https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo/blob/7f5d60d438f46a437dfed54dcbfc6ceb15eb02de/Assets/Scripts/UI/EndScreen.cs#L79
        float startPosition = tilesContainer.style.top.value.value;
        float endPosition = -90.0f;

        tilesContainer.experimental.animation.Start(new StyleValues { top = startPosition, opacity = 1 }, new StyleValues { top = endPosition, opacity = 1 }, 500).Ease(Easing.OutQuad);
        roomInfo.experimental.animation.Start(new StyleValues { top = startPosition, opacity = 1 }, new StyleValues { top = endPosition, opacity = 1 }, 500).Ease(Easing.OutQuad);
    }

    /// <summary>
    /// Puts back the menu in its original place.
    /// </summary>
    public void UnMakeMenuDiscrete()
    {
        if (!isMenuShown)
        {
            return;
        }
        // see https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo/blob/7f5d60d438f46a437dfed54dcbfc6ceb15eb02de/Assets/Scripts/UI/EndScreen.cs#L79
        float startPosition = -90.0f;
        float endPosition = 48.0f;

        tilesContainer.experimental.animation.Start(new StyleValues { top = startPosition, opacity = 1 }, new StyleValues { top = endPosition, opacity = 1 }, 500).Ease(Easing.OutQuad);
        roomInfo.experimental.animation.Start(new StyleValues { top = startPosition, opacity = 1 }, new StyleValues { top = endPosition, opacity = 1 }, 500).Ease(Easing.OutQuad);
    }
}
