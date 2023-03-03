using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements;
using UnityEngine.Reflect;
using System.Data;

public class SlidingMenu : MonoBehaviour
{
    // Event handler
    public event EventHandler<string> RoomButtonClickedEvent;

    // Animation
    private float startPosition, endPosition;
    private const int AnimationDurationMs = 500;

    // UI
    private Button but;
    private VisualElement main, arrowContainer, mm;

    // Start is called before the first frame update
    void OnEnable()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/room-inspector"));
        main = rootVisualElement.Q<VisualElement>("mainB");
        main.style.right = -220.0f; // For some reason I have to set it here first, instead of simply read it from uxml..
        main.style.width = 250.0f; // For some reason I have to set it here first, instead of simply read it from uxml..
        arrowContainer = rootVisualElement.Q<VisualElement>("arrow-container");
        mm = rootVisualElement.Q<VisualElement>("moving-menu");
        but = rootVisualElement.Q<Button>("show");
        but.RegisterCallback<ClickEvent>(ev => Animate());
    }

    /// <summary>
    /// Fills the sliding menu with the list of room names, and register adequate callbacks.
    /// </summary>
    /// <param name="roomList">A list of string containing the names of all the rooms.</param>
    public void PopulateMenu(List<string> roomList)
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        var perRoomScript = GameObject.Find("PerRoomListMenu").GetComponent<PerRoomListMenu>();
        VisualElement mm = rootVisualElement.Q<VisualElement>("info-container");
        var fao = GameObject.Find("Root").GetComponent<FindAllObjects>();
        var mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        var dm = GameObject.Find("DetailsMenu").GetComponent<DetailsMenuScript>();
        NewTilesChoiceMenuScript ntcm = GameObject.Find("NewTileChoiceMenu").GetComponent<NewTilesChoiceMenuScript>();
        Web web = GameObject.Find("Root").GetComponent<Web>();

        for (int i = 0; i < roomList.Count; i++)
        {
            LabelAndButton lnb = new LabelAndButton(roomList[i]);
            lnb.name = roomList[i];
            Button but = lnb.Q<Button>();
            Label lab = lnb.Q<Label>();
            string room = roomList[i];
            but.RegisterCallback<ClickEvent>(ev => fao.GoToLocation(room));
            but.RegisterCallback<ClickEvent>(ev => ChangeRoom(room));
            but.RegisterCallback<ClickEvent>(ev => dm.ShowHide(true));
            but.RegisterCallback<ClickEvent>(ev => dm.ClearMenu());
            but.RegisterCallback<ClickEvent>(ev => dm.PopulateMenu());
            but.RegisterCallback<ClickEvent>(ev => mh.HideCrosshair());
            but.RegisterCallback<ClickEvent>(ev => Animate());
            but.RegisterCallback<ClickEvent>(ev => ntcm.MakeMenuDiscrete());

            lab.RegisterCallback<ClickEvent>(ev => fao.GoToLocation(room));
            lab.RegisterCallback<ClickEvent>(ev => ChangeRoom(room));

            mm.Add(lnb);
        }

        // Register callback on DB save button
        Button saveButton = rootVisualElement.Q<Button>("dbSave");
        saveButton.RegisterCallback<ClickEvent>(ev => web.SaveCurrentSessionToDB());

        // Register callback on amendment button
        Button amendmentButton = rootVisualElement.Q<Button>("amendment");
        amendmentButton.RegisterCallback<ClickEvent>(ev => web.ProduceAmendmentWrapper());

        // Register callback on restore button
        Button restoreButton = rootVisualElement.Q<Button>("restore");
        restoreButton.RegisterCallback<ClickEvent>(ev => StartCoroutine(web.RestorePreviousConfig()));
    }

    void ChangeRoom(string newRoomName)
    {
        RoomScriptableObject.current_room = newRoomName;
    }

    private void Animate()
    {
        // Check which rooms are validated or not
        var web = GameObject.Find("Root").GetComponent<Web>();
        StartCoroutine(web.GetAllSurfacesValidities(RestyleMenu));

        // see https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo/blob/7f5d60d438f46a437dfed54dcbfc6ceb15eb02de/Assets/Scripts/UI/EndScreen.cs#L79
        // Get Starting position
        startPosition = main.style.right.value.value;
        float diff = main.worldBound.width - arrowContainer.worldBound.width;
        endPosition = -(diff + startPosition) ;

        main.experimental.animation.Start(new StyleValues { right = startPosition, opacity = 1 }, new StyleValues { right = endPosition, opacity = 1 }, AnimationDurationMs).Ease(Easing.OutQuad);
    }

    /// <summary>
    /// Color the items of this menu accordingly to their validated or not status.
    /// It's passed as argument to GetAllRoomsValidities so that it gets executed once the list of rooms is obtained.
    /// </summary>
    private void RestyleMenu(Dictionary<int, Tuple<bool, string>> surfacesValidityDict)
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        // Get the DataTables
        DataTableCollection tables = RoomScriptableObject.roomsDataSet.Tables;

        foreach (DataTable table in tables)
        {
            // Info on included surfaces
            string filter = "tile_type = 'A'";
            DataRow[] foundRows = table.Select(filter);
            bool roomValidity = true;
            foreach (DataRow row in foundRows)
            {
                if (row["tile"].ToString() == "-")
                {
                    roomValidity = false;
                    break;
                }
            }
            string room_name = table.TableName;
            LabelAndButton lnb = rootVisualElement.Q<LabelAndButton>(room_name);
            Label l = lnb.Q<Label>();
            l.RemoveFromClassList("red-label");
            l.RemoveFromClassList("green-label");
            if (!roomValidity)
            {
                l.AddToClassList("red-label");
            }
        }

        /*
        // Update lists of rooms and surfaces validities
        var fao = GameObject.Find("Root").GetComponent<FindAllObjects>();
        fao.UpdateSurfacesAndRoomsValiditiesDict(surfacesValidityDict);

        
        foreach (KeyValuePair<string, bool> entry in fao.roomValidities)
        {
            Label b = rootVisualElement.Q<Label>(entry.Key);
            
            if (b != null)
            {
                b.RemoveFromClassList("red-label");
                b.RemoveFromClassList("green-label");
                if (entry.Value)
                {
                    b.AddToClassList("green-label");
                }
                else
                {
                    b.AddToClassList("red-label");
                }
            }
        }
        */
    }

    private void OnApplicationQuit()
    {
        foreach (var item in mm.Children())
        {
            if (item.GetType() == typeof(Button))
            {
                mm.Remove(item);
            }
        } 
    }
}
