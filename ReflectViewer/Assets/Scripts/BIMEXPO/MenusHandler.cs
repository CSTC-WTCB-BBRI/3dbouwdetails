using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Unity.Reflect.Viewer.UI;
using UnityEngine.Reflect;
using System.Reflection;
using System.IO;
using System;
using System.Linq;

public class MenusHandler : MonoBehaviour
{
    public GameObject hitSurface = null;    // The surface clicked by the user.
    public UnityEvent m_MyEvent = new UnityEvent();
    private bool buildingLoaded = false;
    private List<string> busyIds = new List<string>();

    private void Start()
    {
        UIStateManager.stateChanged += UIStateManager_stateChanged; // Listening to UI state change in order to know when the building is loaded.
    }

    public void changeCameraFreeze()
    {
        FindAllObjects.freefly_cam_script.enabled = !FindAllObjects.freefly_cam_script.enabled;
        //var obj = Resources.FindObjectsOfTypeAll<GameObject>(); //.FirstOrDefault(g => g.CompareTag("MainCamera"));
        //obj.GetComponent<FreeFlyCamera>().enabled = !obj.GetComponent<FreeFlyCamera>().enabled;
        //GameObject.Find("Main Camera").GetComponent<FreeFlyCamera>().enabled = !GameObject.Find("Main Camera").GetComponent<FreeFlyCamera>().enabled;
    }

    private void UIStateManager_stateChanged(UIStateData obj)
    {
        if (obj.progressData.totalCount > 0 && obj.progressData.currentProgress == obj.progressData.totalCount)    // Then the building is fully loaded
        {
            if (!buildingLoaded)
            {
                //ShowButtons();
                //ShowCrosshair();
                //HideCrosshair();
                //InputField strInput = new InputField();
                //FindAllObjects.FindAll(InputField);
                // TO DO : change FindAll argument to string, and pass "Wall" to it. Then fire it up from here, and remove the Toggle.
                buildingLoaded = true;
            }
        }
    }
    private void Update()
    {
        return;
        bool preselectionDone = GameObject.Find("Root").GetComponent<Web>().preselectionDone;

        // Look out for comments input, only if the tile choice menu is already up
        var tcm = GameObject.Find("TileChoiceMenu");
        if (tcm != null && Input.GetMouseButtonDown(2) && m_MyEvent != null)
        {
            //m_MyEvent.Invoke();
        }
        else if (tcm = null)
        {
            //m_MyEvent.RemoveAllListeners();
        }

        // Look for surfaces that are not tiled by default (not included in price), only if corresponding checkbox is ticked
        var pm = GameObject.Find("PreselectionMenu").GetComponent<PreselectionMenuScript>();
        if (!pm.highlight.value)
        {
            return;
        }
        return;
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            Metadata md = hit.collider.gameObject.GetComponent<Metadata>();
            if (md != null)
            {
                string id = md.GetParameter("Id");
                if (md.GetParameter("Type").Contains("Plafonnage") && !busyIds.Contains(id))
                {
                    StartCoroutine(ShowNonIncludedInfo(hit, id));
                }
            }
        }
    }

    public void ChangeCrosshairIcon(Texture2D newIcon)
    {
        GameObject contextualMenu = GameObject.Find("ContextualMenu");
        var rootVisualElement = contextualMenu.GetComponent<UIDocument>().rootVisualElement;
        VisualElement crosshair = rootVisualElement.Q<VisualElement>("crosshairVE");
        crosshair.style.backgroundImage = Background.FromTexture2D(newIcon);
    }

    public void ShowCrosshair()
    {
        GameObject contextualMenu = GameObject.Find("ContextualMenu");
        var rootVisualElement = contextualMenu.GetComponent<UIDocument>().rootVisualElement;
        VisualElement crosshair = rootVisualElement.Q<VisualElement>("crosshairVE");
        crosshair.style.display = DisplayStyle.Flex;
    }
    public void HideCrosshair()
    {
        GameObject contextualMenu = GameObject.Find("ContextualMenu");
        var rootVisualElement = contextualMenu.GetComponent<UIDocument>().rootVisualElement;
        VisualElement crosshair = rootVisualElement.Q<VisualElement>("crosshairVE");
        crosshair.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Displays the buttons of the preselection menu.
    /// </summary>
    void ShowButtons()
    {
        GameObject preselectionUI = GameObject.Find("PreselectionMenu");
        var rootVisualElement = preselectionUI.GetComponent<UIDocument>().rootVisualElement;
        Button showHideMenu = rootVisualElement.Q<Button>("show-hide-menu");
        //Button amendment = rootVisualElement.Q<Button>("produce-amendment");
        //Button restore = rootVisualElement.Q<Button>("restore-previous");
        //Toggle highlight = rootVisualElement.Q<Toggle>("includedToggle");
        showHideMenu.style.display = DisplayStyle.Flex;
        //amendment.style.display = DisplayStyle.Flex;
        //restore.style.display = DisplayStyle.Flex;
        //highlight.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Sets the Tile choice menu active, so that it appears on screen. This also freezes the player camera so that as long as this menu is up, moving the mouse doesn't change the perspective.
    /// </summary>
    public void ActivateTilesChoiceMenu()
    {
        GameObject[] allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject tileChoiceMenu = null;
        foreach (GameObject go in allGO)
        {
            if (go.name == "TileChoiceMenu")
            {
                //Show menu
                go.SetActive(true);
                tileChoiceMenu = go;
                break;
            }
        }
        m_MyEvent.AddListener(ActivateCommentMenu);
    }

    public void ActivateCommentMenu()
    {
        GameObject[] allGO = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject commentMenu = null;
        foreach (GameObject go in allGO)
        {
            if (go.name == "CommentMenu")
            {
                //Disable player camera rotation until the preselection is made
                //GameObject.FindGameObjectWithTag("Player").GetComponent<FirstPersonController>().cameraCanMove = false;

                //Show menu
                go.SetActive(true);
                commentMenu = go;
                break;
            }
        }
    }

    /// <summary>
    /// Method to show to the user the surfaces for which tiles are not included in the price.
    /// The surface is colored red, then progressively returns to its original color.
    /// </summary>
    IEnumerator ShowNonIncludedInfo(RaycastHit hit, string id)
    {
        busyIds.Add(id);
        Material mat = hit.collider.gameObject.GetComponent<Renderer>().material;
        if (mat.HasProperty("_Tint"))
        {
            Color initColor = mat.GetColor("_Tint");    // This will fail if the shader changes
            yield return null;

            float interp = 0.0f;

            while (interp < 1.0f)
            {
                mat.SetColor("_Tint", Color.Lerp(Color.red, initColor, interp));
                interp += 0.025f;
                yield return new WaitForSeconds(0.03f);
            }

            mat.SetColor("_Tint", Color.Lerp(Color.red, initColor, interp));
            busyIds.Remove(id);
            yield return null;
        }
        else
        {
            yield return null;
        }
    }

    public void saveScreenshotWrapper(GameObject surface, bool confirm=true)
    {
        StartCoroutine(saveScreenshot(surface));
    }

    IEnumerator saveScreenshot(GameObject surface)
    {
        // Get camera coordinates and orientation
        Transform cam = GameObject.FindGameObjectWithTag("MainCamera").transform;
        Vector3 camPos = cam.position;
        Quaternion camRot = cam.rotation;

        // Define the name of the file
        string filename;
        var webScript = GameObject.Find("Root").GetComponent<Web>();
        var meta = surface.GetComponent<Metadata>();
        string sessionDateTime = webScript.sessionSqlFormattedDate.Replace(" ", "").Replace(":", "");
        if (meta != null)
        {
            filename = meta.GetParameter("Id") + sessionDateTime + ".png";
        }
        else
        {
            throw new Exception("No Id attached to surface!");
        }

        // Make the screenshot
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string gitRootDir = Directory.GetParent(currentDir).Parent.Parent.FullName;
        string screenshotsDir = gitRootDir + "\\PHP\\screenshots\\";

        // Wait till the last possible moment before screen rendering to hide the UI, if necessary
        yield return null;
        bool commentMenuInitialVisibility = true;
        bool tileMenuInitialVisibility = true;
        if (GameObject.Find("CommentMenu") != null)
        {
            GameObject.Find("CommentMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("CommentBox").style.opacity = 0;
            GameObject.Find("CommentMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("bckground").style.opacity = 0;
        }
        else
        {
            commentMenuInitialVisibility = false;
        }
        if (GameObject.Find("NewTileChoiceMenu") != null)
        {
            GameObject.Find("NewTileChoiceMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root").style.display = DisplayStyle.None;
        }
        else
        {
            tileMenuInitialVisibility = false;
        }
        // Wait for screen rendering to complete
        yield return new WaitForEndOfFrame();

        // Take screenshot
        ScreenCapture.CaptureScreenshot(screenshotsDir + filename);

        // Show UI after we're done, if necessary
        if (commentMenuInitialVisibility)
        {
            GameObject.Find("CommentMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("CommentBox").style.opacity = 1;
            GameObject.Find("CommentMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("bckground").style.opacity = 1;
        }
        if (tileMenuInitialVisibility)
        {
            GameObject.Find("NewTileChoiceMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root").style.display = DisplayStyle.Flex;
        }

        // Save it to local DB
        //string room = GameObject.Find("NewTileChoiceMenu").GetComponent<NewTilesChoiceMenuScript>().roomName;
        string room = RoomScriptableObject.current_room;
        RoomScriptableObject.RecordScreenshot(room, filename, camPos, camRot, surface.GetComponent<Metadata>().GetParameter("Id").ToString());

        //webScript.saveScreenshotToDB(filename, camPos, camRot, surface);
    }

    public void ShowInfoMessage(string message)
    {
        StartCoroutine(ShowErrorInfo(message));
    }
    IEnumerator ShowErrorInfo(string message)
    {
        var errorInfoMenu = GameObject.Find("ErrorInfo");
        var rootVisualElement = errorInfoMenu.GetComponent<UIDocument>().rootVisualElement;
        VisualElement main = rootVisualElement.Q<VisualElement>("main");
        Label msg = main.Q<Label>("message");
        msg.text = message;
        main.style.display = DisplayStyle.Flex;
        float opac = 100.0f;

        while (opac > 0.0f)
        {
            main.style.opacity = opac;
            opac -= 5.0f;
            yield return new WaitForSeconds(0.15f);
        }
        main.style.display = DisplayStyle.None;
    }

    public void ShowInfoMessage(object sender, string message)
    {
        StartCoroutine(ShowErrorInfo(message));
    }

}
