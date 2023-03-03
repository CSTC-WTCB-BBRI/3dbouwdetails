using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Reflect;
using UnityEngine.UIElements;
using System.Data;
using Unity.Reflect.Viewer.UI;
using System.Collections.Generic;

public class ContextualMenu : MonoBehaviour
{
    Camera cam;
    Vector3 screenCenter;
    Label info1, info2;
    Button copy;
    public Transform lastObjectHit { get; private set; }
    ChangeMaterial cms;
    bool isSurfaceHit = false;
    bool isSurfaceSwappable = false;
    bool showHideTextIsChanging, showHideSwapTextIsChanging, firstButtonPressed, reset, _splitMode, _rectangleMode = false;
    NewTilesChoiceMenuScript tileMenu;
    float lastTabTime, lastArrowTime, timeOfFirstButton = 0.0f;
    bool first_enter = true;
    GameObject cursor;
    Plane hit_plane;
    List<Vector3> plane_vertices;
    GameObject line;
    int rectangle_count = 0;
    LineRenderer lineRend = null;
    private Vector3 initialMousePosition, currentMousePosition;
    Vector3 corner1, corner2;
    Vector3 line_end_point;
    Material mat;
    int original_obj_layer, original_culling_mask;
    GameObject minimapImage, minimapZoom;
    bool paint_already_drawn = false;
    VisualElement paint_box = null;
    bool _copying = false;
    Material _copyingMat = null;
    Transform initial_cam_pos = null;
    float _paintbox_desired_pixel_x_position, _paintbox_desired_pixel_y_position;

    List<GameObject> copy_surfaces = new List<GameObject>();

    event EventHandler<MyEventArgs> NewTileChoice;

    [SerializeField]
    GameObject prefab_line;
    [SerializeField]
    float delta_multiplier;
    [SerializeField]
    Texture2D copy_texture;
    [SerializeField]
    Texture2D crosshair_texture;

    private IEnumerator coroutineShowInfo1, coroutineShowInfo2, coroutineCopy;
    // Start is called before the first frame update
    void Start()
    {
        info1 = GetComponent<UIDocument>().rootVisualElement.Q<Label>("contextualIndic");
        info2 = GetComponent<UIDocument>().rootVisualElement.Q<Label>("contextualSwap");
        copy = GetComponent<UIDocument>().rootVisualElement.Q<Button>("copy");
        info1.text = "Tab pour choisir un autre carrelage";
        info2.text = "Flèches <- -> pour changer l'orientation";
        info1.style.color = new Color(255, 255, 255, 0);
        info2.style.color = new Color(255, 255, 255, 0);
        copy.style.backgroundColor = new Color(255, 255, 255, 0);
        copy.style.unityBackgroundImageTintColor = new Color(255, 255, 255, 0);
        copy.RegisterCallback<ClickEvent>(ev => EnterCopyingModeViaButton());

        cam = Camera.main;
        // Get the screen center location in pixels, i.e. where the crosshair is supposed to be.
        screenCenter = new Vector3(Screen.width/2.0f, Screen.height/2.0f, 0);
        cms = GameObject.Find("Root").GetComponent<ChangeMaterial>();
        tileMenu = GameObject.Find("NewTileChoiceMenu").GetComponent<NewTilesChoiceMenuScript>();

        corner1 = new Vector3(0, 0, 0);
        corner2 = new Vector3(0, 0, 0);
        mat = Resources.Load("rect_mat") as Material;

        NewTileChoice += RoomScriptableObject.RecordTileChoice;
    }

    // Update is called once per frame
    void Update()
    {
        if (_copying)
        {
            // Adapt contextual messages
            info1.text = "Enter pour appliquer";
            info2.text = "Esc pour annuler";

            // Detect ESC to exit copy mode
            if (Input.GetKey(KeyCode.Escape))
            {
                ExitCopyingModeViaButton();
                return;
            }

            // Detect ENTER to apply copy
            if (Input.GetKey(KeyCode.Return))
            {
                ApplyCopyOnFaces();
                ExitCopyingModeViaButton();
                return;
            }

            // Show contextual text
            if (coroutineShowInfo1 != null)
            {
                StopCoroutine(coroutineShowInfo1);
            }
            coroutineShowInfo1 = ShowHideInfo1(true);
            StartCoroutine(coroutineShowInfo1);

            if (coroutineShowInfo2 != null)
            {
                StopCoroutine(coroutineShowInfo2);
            }
            coroutineShowInfo2 = ShowHideInfo2(true);
            StartCoroutine(coroutineShowInfo2);

            // Getting the surfaces to copy to
            RaycastHit copy_hit;
            Ray copy_ray = cam.ScreenPointToRay(screenCenter);

            if (Physics.Raycast(copy_ray, out copy_hit))
            {
                if (lastObjectHit != copy_hit.transform) // Hitting a new surface
                {
                    // Check if this GO is editable or not, via the metadata
                    GameObject go = copy_hit.transform.gameObject;
                    var meta = go.GetComponent<Metadata>();
                    if (meta != null && RoomScriptableObject.surface_type_values.Contains(meta.GetParameter(RoomScriptableObject.surface_type_parameter)))
                    {
                        copy_surfaces.Add(go);
                        cms.HighlightObject(go, true, new Color(35f/255f, 250f/255f, 70f/255f));
                    }
                }
            }
            return;

            /*
            // Below is the 'old' way of doing it, with the icon on the wall and the mouse click
            // Change mouse icon
            UnityEngine.Cursor.SetCursor(copy_texture, new Vector2(copy_texture.width / 2.0f, copy_texture.height / 2.0f), CursorMode.Auto);

            Ray newray = cam.ScreenPointToRay(Input.mousePosition);
            if (Input.GetMouseButtonUp(0))
            {
                if (Physics.Raycast(newray.origin, newray.direction, out RaycastHit hitInfo))
                {
                    if (hitInfo.transform.gameObject != lastObjectHit.gameObject)
                    {
                        try
                        {
                            if (RoomScriptableObject.surface_type_values.Contains(hitInfo.transform.gameObject.GetComponent<Metadata>().GetParameter(RoomScriptableObject.surface_type_parameter)))
                            {
                                string id = lastObjectHit.gameObject.GetComponent<Metadata>().GetParameter("Id").ToString();
                                string searchExpression = "id = " + id;
                                string copied_tile = RoomScriptableObject.roomsDataSet.Tables[RoomScriptableObject.current_room].Select(searchExpression)[0]["tile"].ToString();
                                NewTileChoice?.Invoke(this, new MyEventArgs(copied_tile, RoomScriptableObject.current_room, hitInfo.transform.gameObject));
                                hitInfo.transform.gameObject.GetComponent<MeshRenderer>().material = _copyingMat;
                            }
                            else
                            {
                                MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
                                mh.ShowInfoMessage("Cette surface ne peut être carrelée");
                            }
                        }
                        catch
                        {
                            MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
                            mh.ShowInfoMessage("Cette surface ne peut être carrelée");
                        }
                    }
                }
                ExitCopyMode();
            }
            return;
            */
        }
        if (_rectangleMode)
        {
            DrawRectangle();
        }
        if (_splitMode)
        {
            //UnityEngine.Cursor.visible = false;
            if (first_enter)
            {
                //line = (GameObject)Instantiate(prefab_line, this.transform.position, Quaternion.identity);
                first_enter = false;
            }

            // Detect exit split mode
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                lastObjectHit.gameObject.layer = original_obj_layer;
                ExitSplitMode();
                return;
            }
            return;
        }
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out hit))
        {
            if (lastObjectHit != hit.transform) // Hitting a new surface
            {
                if (paint_already_drawn)
                {
                    HidePaintIcon();
                    paint_already_drawn = false;
                }

                Transform objectHit = hit.transform;
                GameObject go = objectHit.gameObject;
                if (lastObjectHit != null)
                {
                    cms.HighlightObject(lastObjectHit.gameObject, false);
                }
                    
                // Check if this GO is editable or not, via the metadata
                var meta = go.GetComponent<Metadata>();
                string filter = "id = '" + meta.GetParameter("Id").ToString() + "'";
                if (meta != null && (meta.GetParameter(RoomScriptableObject.surface_type_parameter) == "A"))
                {
                    isSurfaceHit = true;
                    isSurfaceSwappable = false;
                    tileMenu.HideMenu();
                    if (coroutineShowInfo1 != null)
                    {
                        StopCoroutine(coroutineShowInfo1);
                    }
                    coroutineShowInfo1 = ShowHideInfo1(true);
                    StartCoroutine(coroutineShowInfo1);
                    cms.HighlightObject(go, true);
                    // If it has already been tiled, allow the opportunity of switching tile orientation and allow copying
                    foreach (DataRow row in RoomScriptableObject.roomsDataSet.Tables[RoomScriptableObject.current_room].Select(filter))
                    {
                        // There should be only one
                        if (row["tile"].ToString() != "-")
                        {
                            if (coroutineShowInfo2 != null)
                            {
                                StopCoroutine(coroutineShowInfo2);
                                StopCoroutine(coroutineCopy);
                            }
                            coroutineShowInfo2 = ShowHideInfo2(true);
                            coroutineCopy = ShowContextualPaintIcon(true);
                            StartCoroutine(coroutineShowInfo2);
                            StartCoroutine(coroutineCopy);
                            isSurfaceSwappable = true;
                        }
                        else
                        {
                            if (coroutineShowInfo2 != null)
                            {
                                StopCoroutine(coroutineShowInfo2);
                                StopCoroutine(coroutineCopy);
                            }
                            coroutineShowInfo2 = ShowHideInfo2(false);
                            coroutineCopy = ShowContextualPaintIcon(false);
                            StartCoroutine(coroutineShowInfo2);
                            StartCoroutine(coroutineCopy);
                        }

                    }
                    
                }
                else if (meta != null && (meta.GetParameter("Comments") == "0"))
                {
                    isSurfaceHit = true;
                    isSurfaceSwappable = false;
                    tileMenu.HideMenu();
                    if (coroutineShowInfo1 != null)
                    {
                        StopCoroutine(coroutineShowInfo1);
                    }
                    coroutineShowInfo1 = ShowHideInfo1(true);
                    StartCoroutine(coroutineShowInfo1);
                    cms.HighlightObject(go, true, Color.yellow);
                    // If it has already been tiled, allow the opportunity of switching tile orientation
                    foreach (DataRow row in RoomScriptableObject.roomsDataSet.Tables[RoomScriptableObject.current_room].Select(filter))
                    {
                        // There should be only one
                        if (row["tile"].ToString() != "-")
                        {
                            if (coroutineShowInfo2 != null)
                            {
                                StopCoroutine(coroutineShowInfo2);
                                StopCoroutine(coroutineCopy);
                            }
                            coroutineShowInfo2 = ShowHideInfo2(true);
                            coroutineCopy = ShowContextualPaintIcon(true);
                            StartCoroutine(coroutineShowInfo2);
                            StartCoroutine(coroutineCopy);
                            isSurfaceSwappable = true;
                        }
                        else
                        {
                            if (coroutineShowInfo2 != null)
                            {
                                StopCoroutine(coroutineShowInfo2);
                                StopCoroutine(coroutineCopy);
                            }
                            coroutineShowInfo2 = ShowHideInfo2(false);
                            coroutineCopy = ShowContextualPaintIcon(false);
                            StartCoroutine(coroutineShowInfo2);
                            StartCoroutine(coroutineCopy);
                        }
                    }
                }
                else
                {
                    isSurfaceHit = false;
                    isSurfaceSwappable = false;
                    if (coroutineShowInfo1 != null)
                    {
                        StopCoroutine(coroutineShowInfo1);
                    }
                    coroutineShowInfo1 = ShowHideInfo1(false);
                    StartCoroutine(coroutineShowInfo1);
                    if (coroutineShowInfo2 != null)
                    {
                        StopCoroutine(coroutineShowInfo2);
                        StopCoroutine(coroutineCopy);
                    }
                    coroutineShowInfo2 = ShowHideInfo2(false);
                    coroutineCopy = ShowContextualPaintIcon(false);
                    StartCoroutine(coroutineShowInfo2);
                    StartCoroutine(coroutineCopy);
                }
            }
            lastObjectHit = hit.transform;
        }
        else
        {
            isSurfaceHit = false;
            isSurfaceSwappable = false;
            if (coroutineShowInfo1 != null)
            {
                StopCoroutine(coroutineShowInfo1);
            }
            coroutineShowInfo1 = ShowHideInfo1(false);
            StartCoroutine(coroutineShowInfo1);
            if (coroutineShowInfo2 != null)
            {
                StopCoroutine(coroutineShowInfo2);
                StopCoroutine(coroutineCopy);
            }
            coroutineShowInfo2 = ShowHideInfo2(false);
            coroutineCopy = ShowContextualPaintIcon(false);
            StartCoroutine(coroutineShowInfo2);
            StartCoroutine(coroutineCopy);
            if (lastObjectHit != null)
            {
                cms.HighlightObject(lastObjectHit.gameObject, false);
            }
        }
        // Detect Tab key
        if (isSurfaceHit && Input.GetKey(KeyCode.Tab) && (Time.time - lastTabTime > 0.5f))
        {
            lastTabTime = Time.time;
            switch (NewTilesChoiceMenuScript.isMenuShown)
            {
                case true:
                    tileMenu.HideMenu();
                    tileMenu.ShowObjectInfo(null, false);
                    break;
                case false:
                    tileMenu.PopulateMenu(hit.transform.gameObject);
                    break;
            }
        }
        else if (!isSurfaceHit)
        {
            tileMenu.HideMenu();
            tileMenu.ShowObjectInfo(null, false);
        }
        // Detect Arrows
        if (isSurfaceHit && isSurfaceSwappable && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow)) && (Time.time - lastArrowTime > 0.5f))
        {
            lastArrowTime = Time.time;
            float current_rotation = lastObjectHit.GetComponent<Renderer>().material.GetFloat("_Rotation");
            lastObjectHit.GetComponent<Renderer>().material.SetFloat("_Rotation", current_rotation + 90.0f);
        }

        // Detect split tool
        /*
        if (isSurfaceHit && Input.GetKeyDown(KeyCode.S) && firstButtonPressed)
        {
            if (Time.time - timeOfFirstButton < 0.5f)
            {
                original_obj_layer = lastObjectHit.gameObject.layer;
                original_culling_mask = Camera.main.cullingMask;
                lastObjectHit.gameObject.layer = LayerMask.NameToLayer("SplitTool");
                EnterSplitMode(hit);
            }
            else
            {
                Debug.Log("Too late");
            }
            reset = true;
        }

        if (isSurfaceHit && Input.GetKeyDown(KeyCode.S) && !firstButtonPressed)
        {
            firstButtonPressed = true;
            timeOfFirstButton = Time.time;
        }

        if (reset)
        {
            firstButtonPressed = false;
            reset = false;
        }
        */
        
        /*
        // Detect if surface can be copied
        if (isSurfaceHit && isSurfaceSwappable && !paint_already_drawn)
        {
            ShowPaintIcon(hit, paint_already_drawn);
            paint_already_drawn = true;
        }

        if (isSurfaceHit && isSurfaceSwappable && paint_already_drawn)
        {
            ShowPaintIcon(hit, paint_already_drawn);
        }
        */
    }

    void ApplyCopyOnFaces()
    {
        string id = lastObjectHit.gameObject.GetComponent<Metadata>().GetParameter("Id").ToString();
        string searchExpression = "id = " + id;
        string copied_tile = RoomScriptableObject.roomsDataSet.Tables[RoomScriptableObject.current_room].Select(searchExpression)[0]["tile"].ToString();
        foreach (GameObject go in copy_surfaces)
        {
            NewTileChoice?.Invoke(this, new MyEventArgs(copied_tile, RoomScriptableObject.current_room, go));
            go.GetComponent<MeshRenderer>().material = _copyingMat;
        }
        cms.UnHighlightObjects(copy_surfaces);
        copy_surfaces.Clear();
    }

    void HidePaintIcon()
    {
        // Remove line
        Destroy(line);

        // Hide paint icon
        GameObject paint_icon = GameObject.Find("PaintIcon");
        paint_box = paint_icon.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("paint");
        paint_box.style.display = DisplayStyle.None;
        paint_box.UnregisterCallback<MouseDownEvent>(ev => EnterCopyingMode());
    }

    
    void ShowPaintIcon(RaycastHit hit, bool update)
    {
        if (!update)
        {
            // Get the location to start the line
            // 1) Get the plane
            Plane hit_plane = new Plane();
            hit_plane.SetNormalAndPosition(hit.normal, hit.point);

            // Get the start point of line
            Vector3 center = lastObjectHit.GetComponent<MeshRenderer>().bounds.center;

            List<Vector3> vertices = GetAllVerticesInPlane(lastObjectHit.gameObject, hit_plane);
            float max_distance = -1.0f;
            Vector3 p1 = new Vector3();
            Vector3 p2 = new Vector3();
            for (int i = 0; i < vertices.Count; i++)
            {
                for (int j = 0; j < vertices.Count; j++)
                {
                    float distance = Vector3.Distance(vertices[i], vertices[j]);
                    if (distance > max_distance)
                    {
                        max_distance = distance;
                        p1 = vertices[i];
                        p2 = vertices[j];
                    }
                }

            }
            Vector3 distance_vector = new Vector3(Mathf.Abs(p1.x - p2.x), Mathf.Abs(p1.y - p2.y), Mathf.Abs(p1.z - p2.z));
            Vector3 line_start_point = hit.point;
            line_end_point = line_start_point + lastObjectHit.transform.up * 0.2f + hit.normal * 0.1f;

            // Draw the line
            line = Instantiate(prefab_line, this.transform.position, Quaternion.identity);
            line.name = "line_test";
            DrawLineBetweenVertices(line, line_start_point, line_end_point, 0.01f);

            // Draw paint icon
            GameObject paint_icon = GameObject.Find("PaintIcon");
            paint_box = paint_icon.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("paint");
            paint_box.style.display = DisplayStyle.Flex;

            paint_box.RegisterCallback<MouseDownEvent>(ev => EnterCopyingMode());
        }

        // Update paint icon position to keep it at line end
        var delta = Time.unscaledDeltaTime;
        _paintbox_desired_pixel_x_position = cam.WorldToScreenPoint(line_end_point).x;
        _paintbox_desired_pixel_y_position = Screen.height - cam.WorldToScreenPoint(line_end_point).y;

        float _paintbox_current_pixel_left_position = paint_box.style.left.value.value;
        float _paintbox_interpolated_pixel_left_position = Mathf.Lerp(_paintbox_current_pixel_left_position, _paintbox_desired_pixel_x_position, Mathf.Clamp01(delta *delta_multiplier));
        paint_box.style.left = new StyleLength(new Length(_paintbox_interpolated_pixel_left_position, LengthUnit.Pixel));

        float _paintbox_current_pixel_top_position = paint_box.style.top.value.value;
        float _paintbox_interpolated_pixel_top_position = Mathf.Lerp(_paintbox_current_pixel_top_position, _paintbox_desired_pixel_y_position, Mathf.Clamp01(delta*delta_multiplier));
        paint_box.style.top = new StyleLength(new Length(_paintbox_interpolated_pixel_top_position, LengthUnit.Pixel));
    }

    void ExitCopyMode()
    {
        HidePaintIcon();
        _copying = false;
        _copyingMat = null;

        cam.GetComponent<FreeFlyCamera>().SetMovePosition(initial_cam_pos.position, initial_cam_pos.rotation);
        cam.GetComponent<FreeFlyCamera>().settings.freeze = false;
        paint_already_drawn = false;
    }

    void EnterCopyingModeViaButton()
    {
        _copying = true;
        _copyingMat = lastObjectHit.gameObject.GetComponent<MeshRenderer>().material;
        copy.style.backgroundColor = new Color(0, 1, 0, 1);
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        mh.ChangeCrosshairIcon(copy_texture);
    }

    void ExitCopyingModeViaButton()
    {
        _copying = false;
        _copyingMat = null;
        copy.style.backgroundColor = new Color(1, 1, 1, 1);
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        mh.ChangeCrosshairIcon(crosshair_texture);
        cms.UnHighlightObjects(copy_surfaces);
        copy_surfaces.Clear();

        // Adapt contextual messages
        info1.text = "Tab pour choisir un autre carrelage";
        info2.text = "Flèches <- -> pour changer l'orientation";
    }

    void EnterCopyingMode()
    {
        _copying = true;
        _copyingMat = lastObjectHit.gameObject.GetComponent<MeshRenderer>().material;
        initial_cam_pos = cam.transform;
        cam.GetComponent<FreeFlyCamera>().settings.freeze = true;
    }

    void ExitSplitMode()
    {
        _splitMode = false;
        Camera main = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        main.cullingMask = original_culling_mask;

        // Unfreeze Camera
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        mh.changeCameraFreeze();

        // Set background
        cam.clearFlags = CameraClearFlags.Skybox;

        // Show Crosshair
        mh.ShowCrosshair();

        // Show Minimap
        minimapImage.SetActive(true);
        minimapZoom.SetActive(true);

        // Hide UI menu
        VisualElement left_column = GameObject.Find("SplitMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("left_tools");
        left_column.style.display = DisplayStyle.None;
    }

    void EnterSplitMode(RaycastHit hit)
    {
        lastObjectHit.gameObject.GetComponent<MeshRenderer>().sharedMaterial = mat;
        _splitMode = true;
        int splitMask = LayerMask.GetMask("SplitTool");
        Camera main = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        main.cullingMask = splitMask;

        hit_plane = new Plane(hit.normal, hit.point);
        plane_vertices = GetAllVerticesInPlane(lastObjectHit.gameObject, hit_plane);

        // Position the camera
        FreeFlyCamera m_ffCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<FreeFlyCamera>();

        var temp_go = new GameObject("temp");
        temp_go.transform.position = hit.point;
        temp_go.transform.forward = -1.0f*hit.normal;

        m_ffCam.TransformTo(temp_go.transform);
        m_ffCam.MovePosition(5.0f*hit.normal, LookAtConstraint.StandBy);

        Bounds bb = hit.transform.gameObject.GetComponent<MeshRenderer>().bounds;

        m_ffCam.FitInView(bb, 0.0f, 0.9f);
        m_ffCam.ForceStop();
        //main.orthographic = true;

        // Freeze Camera
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        mh.changeCameraFreeze();

        // Set background
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.SolidColor;

        // Hide Minimap
        minimapImage = GameObject.Find("MinimapImage");
        minimapZoom = GameObject.Find("MinimapZoom");
        minimapImage.SetActive(false);
        minimapZoom.SetActive(false);

        // Hide Crosshair
        mh.HideCrosshair();

        // Show UI menu and register events to buttons
        VisualElement left_column = GameObject.Find("SplitMenu").GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("left_tools");
        left_column.style.display = DisplayStyle.Flex;

        Button rectangle = left_column.Q<Button>("rectangle_split");
        rectangle.RegisterCallback<ClickEvent>(ev => EnterRectangleMode());

        // Test slice
        /*
        Plane pl = new Plane(new Vector3(0.0f, 1.0f, 0.0f), new Vector3(6.0f, 3.0f, 7.5f));
        GameObject[] slices = Slicer.Slice(pl, hit.transform.gameObject);
        foreach (GameObject go in slices)
        {
            go.layer = LayerMask.NameToLayer("SplitTool");
        }
        hit.transform.gameObject.layer = LayerMask.NameToLayer("Default");
        */

        // 31/08/21 - AC. I stop here splitting walls. Will get back if it's needed.
    }

    List<Vector3> GetAllVerticesInPlane(GameObject wall, Plane plane)
    {
        Mesh wall_mesh = wall.GetComponent<MeshFilter>().mesh;
        List<Vector3> vertices_list = new List<Vector3>();
        foreach (Vector3 vertex in wall_mesh.vertices)
        {
            if (Mathf.Approximately(plane.GetDistanceToPoint(vertex), 0.0f))
            {
                vertices_list.Add(vertex);
            }
        }
        return vertices_list;
    }

    Vector3[] FindClosestAlignedVertices(List<Vector3> vertices_list, Vector3 screen_input_position, Vector3 screen_align_direction)
    {
        Vector3[] closest_vertices = new Vector3[2];
        foreach (Vector3 vertex in vertices_list)
        {
            Vector3 screen_vertex_pos = cam.WorldToScreenPoint(vertex);
            if (screen_align_direction.x == 1)
            {
                if (Mathf.Approximately(screen_input_position.x, screen_vertex_pos.x))
                {
                    if (closest_vertices.Length == 0)
                    {
                        closest_vertices[0] = screen_vertex_pos;
                    }
                    else if (closest_vertices.Length == 1)
                    {
                        closest_vertices[1] = screen_vertex_pos;
                    }
                    else
                    {
                        if (Vector3.Distance(screen_vertex_pos, screen_input_position) < Vector3.Distance(closest_vertices[0], screen_input_position))
                        {
                            closest_vertices[0] = screen_vertex_pos;
                        }
                        else if (Vector3.Distance(screen_vertex_pos, screen_input_position) < Vector3.Distance(closest_vertices[1], screen_input_position))
                        {
                            closest_vertices[1] = screen_vertex_pos;
                        }
                    }
                }
            }
        }
        return closest_vertices;
    }

    void DrawLineBetweenVertices(GameObject line, Vector3 v1, Vector3 v2, float width)
    {
        LineRenderer l = line.GetComponent<LineRenderer>();
        l.SetPosition(0, v1);
        l.SetPosition(1, v2);
        l.startWidth = width;
        l.endWidth = width;
        l.numCapVertices = 2;
    }

    void EnterRectangleMode()
    {
        _rectangleMode = true;
        rectangle_count += 1;
        line = (GameObject)Instantiate(prefab_line, this.transform.position, Quaternion.identity);
        line.name = "rectangle_" + rectangle_count;
        line.layer = LayerMask.NameToLayer("SplitTool");
        lineRend = line.GetComponent<LineRenderer>();
        lineRend.loop = true;


    }

    Vector3 GetCorner()
    {
        var camera = Camera.main;
        var mousePosition = Input.mousePosition;
        var ray = camera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, camera.nearClipPlane));
        int layer_mask = LayerMask.GetMask("SplitTool");
        if (Physics.Raycast(ray, out var hit, 50.0f, layer_mask) && hit.collider.gameObject == lastObjectHit.gameObject)
        {
            return hit.point;
        }
        else
        {
            return new Vector3(-999, -999, -999);
        }
    }

    void DrawRectangle()
    {   
        if (Input.GetMouseButtonDown(0))
        {
            corner1 = GetCorner();
        }
        if (Input.GetMouseButton(0))
        {
            corner2 = GetCorner();
            if (!corner2.Equals(new Vector3(-999, -999, -999)))
            {
                mat.SetVector("_corner1", corner1);
                mat.SetVector("_corner2", corner2);
            }       
        }
    }

    IEnumerator ShowHideInfo1(bool show)
    {
        if (show)
        {
            float a = info1.style.color.value.a;
            while (a < 1.0f)
            {
                a += 0.05f;
                info1.style.color = new Color(255, 255, 255, a);
                yield return null;
            }            
        }
        else
        {
            float a = info1.style.color.value.a;
            while (a > 0.0f)
            {
                a -= 0.05f;
                info1.style.color = new Color(255, 255, 255, a);
                yield return null;
            }
        }
    }

    IEnumerator ShowHideInfo2(bool show)
    {
        if (show)
        {
            float a = info2.style.color.value.a;
            while (a < 1.0f)
            {
                a += 0.05f;
                info2.style.color = new Color(255, 255, 255, a);
                yield return null;
            }
        }
        else
        {
            float a = info2.style.color.value.a;
            while (a > 0.0f)
            {
                a -= 0.05f;
                info2.style.color = new Color(255, 255, 255, a);
                yield return null;
            }
        }
    }

    /// <summary>
    /// Shows the paint icon in the bottom left contextual menu
    /// </summary>
    IEnumerator ShowContextualPaintIcon(bool show)
    {
        if (show)
        {
            copy.pickingMode = PickingMode.Position;
            float a = copy.style.backgroundColor.value.a;
            while (a < 1.0f)
            {
                a += 0.05f;
                copy.style.backgroundColor = new Color(255, 255, 255, a);
                copy.style.unityBackgroundImageTintColor = new Color(255, 255, 255, a);
                yield return null;
            }
        }
        else
        {
            copy.pickingMode = PickingMode.Ignore;
            float a = copy.style.backgroundColor.value.a;
            while (a > 0.0f)
            {
                a -= 0.05f;
                copy.style.backgroundColor = new Color(255, 255, 255, a);
                copy.style.unityBackgroundImageTintColor = new Color(255, 255, 255, a);
                yield return null;
            }
        }
    }
}
