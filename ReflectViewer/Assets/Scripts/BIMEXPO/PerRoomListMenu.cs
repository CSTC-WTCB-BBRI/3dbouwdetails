using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Reflect;
using UnityEngine.UIElements;
using System;

public class PerRoomListMenu : MonoBehaviour
{
    List<string> roomNames;
    public List<bool> roomValidated { get; private set; }
    public Label room_name { get; private set; }
    VisualElement root;
    List<int> surfacesInRoom; //, validatedSurfaces;
    private bool _roomValidity;
    public bool thisRoomValidity { get { return _roomValidity; }  set { _roomValidity = value;  RoomValidityChanged.Invoke(); } }
    public static event Action RoomValidityChanged = delegate { };

    // Called when building is fully loaded
    public void Initialize()
    {
        var fao = GameObject.Find("Root").GetComponent<FindAllObjects>();
        roomNames = fao.roomNames;
        //roomValidated = Enumerable.Repeat(false, roomNames.Count).ToList(); // to do: retrieve from DB which rooms are validated yet or not
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("USS/room-inspector"));
        room_name = rootVisualElement.Q<Label>("roomName");
        root = rootVisualElement.Q<VisualElement>("root");
        root.style.display = DisplayStyle.None;
        //thisRoomValidity = false; //TODO:  get this info from roomValidated, 5 lines above
        RoomValidityChanged += ColorTitle;
    }

    private void ColorTitle()
    {
        if (_roomValidity)
        {
            room_name.RemoveFromClassList("item-notdone");
            room_name.AddToClassList("item-done");
            //roomValidated[roomNames.IndexOf(room_name.text)] = true;
        }
        else
        {
            room_name.RemoveFromClassList("item-done");
            room_name.AddToClassList("item-notdone");
            //roomValidated[roomNames.IndexOf(room_name.text)] = false;
        }
    }

    public void RefreshMenu()
    {
        if (root.style.display == DisplayStyle.None)
        {
            root.style.display = DisplayStyle.Flex;
        }
        ClearMenu();
    }

    void ClearMenu()
    {
        ListView list = root.Q<ListView>();
        Label errorMsg = root.Q<Label>("errorLabel");
        if (list != null)
        {
            root.Remove(list);
        }
        if (errorMsg != null)
        {
            root.Remove(errorMsg);
        }
    }

    void ReStyleTitle(bool isRoomValid)
    {
        this.room_name.RemoveFromClassList("item-done");
        this.room_name.RemoveFromClassList("item-notdone");
        if (isRoomValid)
        {
            thisRoomValidity = true;
            this.room_name.AddToClassList("item-done");
        }
        else
        {
            thisRoomValidity = false;
            this.room_name.AddToClassList("item-notdone");
        }
    }

    /// <summary>
    /// For a given surface Id, checks if the surface has been validated by user.
    /// </summary>
    /// <param name="surfaceId">The Id of the surface to check</param>
    /// <returns></returns>
    bool IsSurfaceValidated(int surfaceId)
    {
        var fao = GameObject.Find("Root").GetComponent<FindAllObjects>();
        return fao.surfacesValidities[surfaceId];
    }

    public void PopulateMenu(Button sender)
    {
        // Title
        string room_name = sender.text;
        this.room_name.text = room_name;
        var web = GameObject.Find("Root").GetComponent<Web>();
        StartCoroutine(web.CheckRoomValidityFromDB(this.room_name.text, ReStyleTitle));
        
        // Get surfaces of this room
        var fao = GameObject.Find("Root").GetComponent<FindAllObjects>();
        try
        {
            surfacesInRoom = fao.surfacesAPerRoom[room_name];
        }
        catch (KeyNotFoundException)
        {
            Label errorMsg = new Label("No tilable surfaces!");
            errorMsg.name = "errorLabel";
            errorMsg.AddToClassList("item-notdone");
            root.Add(errorMsg);
            return;
        }

        //validatedSurfaces = new List<int>();

        // The "makeItem" function will be called as needed
        // when the ListView needs more items to render
        Func<VisualElement> makeItem = () => new Toggle();

        // As the user scrolls through the list, the ListView object
        // will recycle elements created by the "makeItem"
        // and invoke the "bindItem" callback to associate
        // the element with the matching data item (specified as an index in the list)
        Action<VisualElement, int> bindItem = (e, i) =>
        {
            (e as Toggle).label = surfacesInRoom[i].ToString(); if (IsSurfaceValidated(surfacesInRoom[i]))
            {
                (e as Toggle).AddToClassList("green-toggle");
                (e as Toggle).value = true;
            }
            else
            {
                (e as Toggle).AddToClassList("red-toggle");
                (e as Toggle).value = false;
            }
            (e as Toggle).AddToClassList("short-toggle"); (e as Toggle).RegisterCallback<ClickEvent>(ev => ChangeSurfaceStatus(ev.target as Toggle)); };

        // Provide the list view with an explict height for every row
        // so it can calculate how many items to actually display
        const int itemHeight = 16;

        // Create the ListView
        var listView = new ListView(surfacesInRoom, itemHeight, makeItem, bindItem);

        listView.selectionType = SelectionType.Multiple;

        //listView.onItemChosen += obj => Debug.Log(obj);
        //listView.onSelectionChanged += objects => Debug.Log(objects);

        listView.style.flexGrow = 1.0f;

        // Add the ListView to the menu
        root.Add(listView);
    }

    /*
    /// <summary>
    /// Checks wether all the tilable surfaces of the room currently being adressed have been validated/chosen yet.
    /// This check is performed via the class variables surfacesInRoom and validatedSurfaces.
    /// </summary>
    /// <returns>True if the room is considered fully adressed, false otherwise.</returns>
    void IsRoomValidated()
    {
        var web = GameObject.Find("Root").GetComponent<Web>();
        StartCoroutine(web.CheckRoomValidity(this.room_name.text));
    }
    */

    /// <summary>
    /// Adjust the surfaces and title styles accordingly to their status (validated or not).
    /// This method is bound to each toggle of the menu, and called upon check/uncheck of a toggle.
    /// </summary>
    /// <param name="sender">The toggle that has been checked or unchecked.</param>
    void ChangeSurfaceStatus(Toggle sender)
    {
        var web = GameObject.Find("Root").GetComponent<Web>();
        if (sender.value)
        {
            sender.AddToClassList("green-toggle");
            sender.RemoveFromClassList("red-toggle");
            StartCoroutine(web.SetSurfaceValidity(Int32.Parse(sender.label), true));
        }
        else
        {
            StartCoroutine(web.SetSurfaceValidity(Int32.Parse(sender.label), false));
            sender.AddToClassList("red-toggle");
            sender.RemoveFromClassList("green-toggle");
            /*
            try
            {
                validatedSurfaces.RemoveAt(validatedSurfaces.IndexOf(Int32.Parse(sender.label)));
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            */
        }
        //IsRoomValidated();
    }
}
