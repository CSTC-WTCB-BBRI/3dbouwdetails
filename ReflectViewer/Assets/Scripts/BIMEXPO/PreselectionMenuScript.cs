using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.ObjectModel;

public class PreselectionMenuScript : MonoBehaviour
{
    private VisualElement myBox, mainMenu;
    private Button okButton, showHideMenuButton, produceAmendmentButton, restoreButton;
    private List<string> localSelectedTiles = new List<string>();
    public ReadOnlyCollection<string> selectedTiles { get { return localSelectedTiles.AsReadOnly(); } } // selectedTiles can be read but not modified outside this class
    private Toggle wallToggle, slabToggle;
    //private ReadOnlyCollection<string> tileNames, wallTileNames, slabTileNames;
    private List<string> tileNames, wallTileNames, slabTileNames;
    private string allTileNames;
    private string tilePicturesFolder = "";
    public Toggle highlight { get; private set; }

    void OnEnable()
    {
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        tilePicturesFolder = Directory.GetParent(currentDir).Parent.Parent.FullName + "\\pictures_carrelages\\";

        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        myBox = rootVisualElement.Q<VisualElement>("img-container");
        okButton = rootVisualElement.Q<Button>("ok-button");
        wallToggle = rootVisualElement.Q<Toggle>("wallToggle");
        slabToggle = rootVisualElement.Q<Toggle>("slabToggle");
        showHideMenuButton = rootVisualElement.Q<Button>("show-hide-menu");
        mainMenu = rootVisualElement.Q<VisualElement>("main-menu");
        highlight = rootVisualElement.Q<Toggle>("includedToggle");

        // Also handling the amendment production from this menu
        produceAmendmentButton = rootVisualElement.Q<Button>("produce-amendment");

        // And the restoration of previous choices
        restoreButton = rootVisualElement.Q<Button>("restore-previous");

        // Register callbacks
        var webScript = GameObject.Find("Root").GetComponent<Web>();
        showHideMenuButton.clicked += ShowHidePreselectionMenu;
        okButton.RegisterCallback<ClickEvent>(ev => StartCoroutine(webScript.ValidatePreSelection()));
        okButton.RegisterCallback<ClickEvent>(ev => ShowHidePreselectionMenu());
        wallToggle.RegisterCallback<ClickEvent>(ev => StartCoroutine(UpdateDisplay()));
        slabToggle.RegisterCallback<ClickEvent>(ev => StartCoroutine(UpdateDisplay()));
        produceAmendmentButton.clicked += webScript.ProduceAmendmentWrapper;            // Can't directly use a coroutine in an action, so it's wrapped inside a function.
        restoreButton.RegisterCallback<ClickEvent>(ev => StartCoroutine(webScript.RestorePreviousConfig()));

        var DBScript = GameObject.Find("Root").GetComponent<DBInteractions>();

        tileNames = new List<string>();
        wallTileNames = new List<string>();
        slabTileNames = new List<string>();
        StartCoroutine(getAllTilesNames());         //Finds all tiles in DB and store them in tileNames
        StartCoroutine(getAllTilesNames("walls"));  //Finds all tiles in DB and store them in wallTileNames
        StartCoroutine(getAllTilesNames("slabs"));  //Finds all tiles in DB and store them in slabTileNames
    }

    IEnumerator getAllTilesNames(string filter = "all")
    {
        yield return new WaitForSeconds(5); // I want to be sure the CSV has been already loaded!
        var webScript = GameObject.Find("Root").GetComponent<Web>();
        if (filter == "all")
        {
            tileNames.Clear();
            yield return StartCoroutine(webScript.ListAllTileNamesInDB("all"));
            foreach (string item in webScript.allTileNames)
            {
                tileNames.Add(item);
            }
        }
        else if (filter == "walls")
        {
            wallTileNames.Clear();
            yield return StartCoroutine(webScript.ListAllTileNamesInDB("walls"));
            foreach (string item in webScript.wallTileNames)
            {
                wallTileNames.Add(item);
            }
        }
        else if (filter == "slabs")
        {
            slabTileNames.Clear();
            yield return StartCoroutine(webScript.ListAllTileNamesInDB("slabs"));
            foreach (string item in webScript.slabTileNames)
            {
                slabTileNames.Add(item);
            }
        }

        yield return null;
    }
    void ShowHidePreselectionMenu()
    {
        if (mainMenu.style.display == DisplayStyle.Flex)
        {
            mainMenu.style.display = DisplayStyle.None;
        }
        else
        {
            mainMenu.style.display = DisplayStyle.Flex;
        }
    }
    
    /// <summary>
    /// Based on the menu's checkboxes, shows or hide the tiles applicable to walls or slabs.
    /// </summary>
    IEnumerator UpdateDisplay()
    {
        List<VisualElement> veToRemove = new List<VisualElement>();
        foreach (VisualElement ve in myBox.Children())
        {
            veToRemove.Add(ve);
        }
        foreach (VisualElement ve in veToRemove)
        {
            myBox.Remove(ve);
        }

        var webScript = GameObject.Find("Root").GetComponent<Web>();
        if (wallToggle.value == true)
        {
            foreach (string tileName in wallTileNames)
            {
                string texturePath;
                yield return StartCoroutine(webScript.GetTexturePathFromName(tileName));
                texturePath = webScript.texturePath;
                //texturePath = webScript.GetTexturePathFromName(tileName);
                var myVE = new VisualElement();

                myVE.styleSheets.Add(Resources.Load<StyleSheet>("USS/testVE"));
                myVE.AddToClassList("medaillon");
                myVE.name = tileName;
                myVE.style.backgroundImage = webScript.LoadTextureFromDiskFolder(tilePicturesFolder + texturePath);
                myBox.Add(myVE);

                myVE.RegisterCallback<ClickEvent>(ev => UpdateSelection(tileName));
            }
        }
        if (slabToggle.value == true)
        {
            foreach (string tileName in slabTileNames)
            {
                string texturePath;
                yield return StartCoroutine(webScript.GetTexturePathFromName(tileName));
                texturePath = webScript.texturePath;

                var myVE = new VisualElement();

                myVE.styleSheets.Add(Resources.Load<StyleSheet>("USS/testVE"));
                myVE.AddToClassList("medaillon");
                myVE.name = tileName;
                myVE.style.backgroundImage = webScript.LoadTextureFromDiskFolder(tilePicturesFolder + texturePath);
                myBox.Add(myVE);

                myVE.RegisterCallback<ClickEvent>(ev => UpdateSelection(tileName));
            }
        }
        //Apply style to tiles already preselected
        IEnumerable<VisualElement> tiles = myBox.Children();
        foreach (VisualElement tile in tiles)
        {
            if (localSelectedTiles.Contains(tile.name))
            {
                tile.RemoveFromClassList("medaillon");
                tile.AddToClassList("selected-medaillon");
            }
        }
    }

    /// <summary>
    /// Updates the list of tiles that the user wants to place in the preselection. That list being a public variable of this class.
    /// </summary>
    /// <param name="tileName"></param>
    void UpdateSelection(string tileName)
    {
        if (localSelectedTiles.Contains(tileName))
        {
            localSelectedTiles.Remove(tileName);
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            myBox = rootVisualElement.Q<VisualElement>("img-container");
            IEnumerable<VisualElement> tiles = myBox.Children();
            foreach (VisualElement tile in tiles)
            {
                if (tile.name == tileName)
                {
                    tile.RemoveFromClassList("selected-medaillon");
                    tile.AddToClassList("medaillon");
                }
            }
        }
        else
        {
            localSelectedTiles.Add(tileName);
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            myBox = rootVisualElement.Q<VisualElement>("img-container");
            IEnumerable<VisualElement> tiles = myBox.Children();
            foreach (VisualElement tile in tiles)
            {
                if (tile.name == tileName)
                {
                    tile.RemoveFromClassList("medaillon");
                    tile.AddToClassList("selected-medaillon");
                }
            }
        }
            
    }
}
