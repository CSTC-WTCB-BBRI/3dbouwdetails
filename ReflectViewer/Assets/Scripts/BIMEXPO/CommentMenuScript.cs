using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using System.Data;
using UnityEngine.Reflect;

public class CommentMenuScript : MonoBehaviour
{
    private Button validateButton, photoButton;
    private Label surf;
    private GameObject target;
    private VisualElement m_Container;
    private CustomComment cc;
    NewTilesChoiceMenuScript ntcm;

    void OnEnable()
    {
        //Register the action on button click
        target = GameObject.Find("ContextualMenu").GetComponent<ContextualMenu>().lastObjectHit.gameObject;

        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        validateButton = rootVisualElement.Q<Button>("ok-button");
        photoButton = rootVisualElement.Q<Button>("photo");

        m_Container = rootVisualElement.Q<VisualElement>("Container");
        VisualElement commentBox = rootVisualElement.Q<VisualElement>("CommentBox");
        surf = rootVisualElement.Q<Label>("surface");

        ntcm = GameObject.Find("NewTileChoiceMenu").GetComponent<NewTilesChoiceMenuScript>();
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();

        surf.text = target.name;
        validateButton.RegisterCallback<ClickEvent>(ev => SaveComment(target));
        validateButton.RegisterCallback<ClickEvent>(ev => ntcm.UnMakeMenuDiscrete());
        photoButton.RegisterCallback<ClickEvent>(ev => mh.saveScreenshotWrapper(target));

        // Recuperate the comment if one already exists
        var webScript = GameObject.Find("Root").GetComponent<Web>();
        string comment = webScript.GetComment(target);

        cc = new CustomComment(comment);
        m_Container.Add(cc);

        // Progressively show the comment menu
        commentBox.experimental.animation.Start(new StyleValues { opacity = 0 }, new StyleValues { opacity = 1 }, 500).Ease(Easing.OutQuad);

        // Freeze the camera
        mh.changeCameraFreeze();

        // Hide the crosshair
        mh.HideCrosshair();
    }

    private void OnDisable()
    {
        // UnFreeze the camera
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        mh.changeCameraFreeze();
    }

    void CleanupBeforeDisable()
    {
        // Progressively hide the comment menu
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        VisualElement commentBox = rootVisualElement.Q<VisualElement>("CommentBox");
        commentBox.experimental.animation.Start(new StyleValues { opacity = 1 }, new StyleValues { opacity = 0 }, 500).Ease(Easing.OutQuad);

        // Show the crosshair
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        mh.ShowCrosshair();
    }

    /// <summary>
    /// Saves a comment into local (Unity side) DB. The comment is attached to its surface.
    /// </summary>
    /// <param name="target">The surface currently being focused in game.</param>
    void SaveComment(GameObject target)
    {
        // Save the comment into local DB
        string comment = cc.textElem.value;
        Metadata meta = target.GetComponent<Metadata>();
        string id = meta.GetParameter("Id").ToString();
        RoomScriptableObject.RecordComment(comment, RoomScriptableObject.current_room, id);

        // Cleanup before leaving menu
        CleanupBeforeDisable();
        GameObject.Find("CommentMenu").SetActive(false);
    }
}
