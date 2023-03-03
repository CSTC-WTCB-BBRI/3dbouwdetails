using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements;
using System;
using UnityEngine.Reflect;
using System.Data;

public class DetailsMenuScript : MonoBehaviour
{
    int AnimationDurationMs = 500;

    /// <summary>
    /// Shows or hide the details menu based on its current visibility status (opacity).
    /// </summary>
    /// <param name="forceShow">Boolean allowing to force the menu's visibility</param>
    public void ShowHide(bool forceShow = false)
    {
        var rve = GetComponent<UIDocument>().rootVisualElement;
        VisualElement main = rve.Q<VisualElement>("main");
        float startOpacity = main.style.opacity.value;
        float endOpacity = 1.0f - startOpacity;

        if (startOpacity == 1.0f && forceShow)
        {
            return;
        }
        // see https://github.com/Unity-Technologies/UIToolkitUnityRoyaleRuntimeDemo/blob/7f5d60d438f46a437dfed54dcbfc6ceb15eb02de/Assets/Scripts/UI/EndScreen.cs#L79
        main.experimental.animation.Start(new StyleValues { opacity = startOpacity }, new StyleValues { opacity = endOpacity }, AnimationDurationMs).Ease(Easing.OutQuad);

        if ((int)endOpacity == 1)
        {
            main.pickingMode = PickingMode.Position;
        }
        else
        {
            main.pickingMode = PickingMode.Ignore;

            // Remove contents of the menu
            ClearMenu();
        }
    }

    /// <summary>
    /// Remove all the elements of the menu.
    /// </summary>
    public void ClearMenu()
    {
        var rve = GetComponent<UIDocument>().rootVisualElement;
        VisualElement ii = rve.Q<VisualElement>("included-info");
        VisualElement nii = rve.Q<VisualElement>("not-included-info");
        VisualElement nis = rve.Q<VisualElement>("not-included-summary");
        List<VisualElement> elementsToRemove = new List<VisualElement>();
        foreach (var item in ii.Children())
        {
            elementsToRemove.Add(item);
        }
        foreach (var item in elementsToRemove)
        {
            ii.Remove(item);
        }
        elementsToRemove.Clear();
        foreach (var item in nii.Children())
        {
            elementsToRemove.Add(item);
        }
        foreach (var item in elementsToRemove)
        {
            nii.Remove(item);
        }
        elementsToRemove.Clear();
        foreach (var item in nis.Children())
        {
            elementsToRemove.Add(item);
        }
        foreach (var item in elementsToRemove)
        {
            nis.Remove(item);
        }
    }

    public void PopulateMenu()
    {
        NewTilesChoiceMenuScript ntms = GameObject.Find("NewTileChoiceMenu").GetComponent<NewTilesChoiceMenuScript>();
        MenusHandler mh = GameObject.Find("Root").GetComponent<MenusHandler>();
        var rve = GetComponent<UIDocument>().rootVisualElement;
        VisualElement ii = rve.Q<VisualElement>("included-info");
        VisualElement nii = rve.Q<VisualElement>("not-included-info");

        string room = RoomScriptableObject.current_room;

        // Get the DataTable
        DataTable table = RoomScriptableObject.roomsDataSet.Tables[room];

        // Title
        rve.Q<Label>("title-label").text = room + " - Details des choix";

        // Callbacks upon OK click
        rve.Q<Button>("ok-button").RegisterCallback<ClickEvent>(ev => mh.ShowCrosshair());
        rve.Q<Button>("ok-button").RegisterCallback<ClickEvent>(ev => ShowHide());
        rve.Q<Button>("ok-button").RegisterCallback<ClickEvent>(ev => ntms.UnMakeMenuDiscrete());

        if (table == null)
        {
            Label surfaceId = new Label("Pas de surface concernée dans cette pièce");
            Label surfaceId1 = new Label("Pas de surface concernée dans cette pièce");
            surfaceId.AddToClassList("info-label");
            surfaceId1.AddToClassList("info-label");
            ii.Add(surfaceId);
            nii.Add(surfaceId1);
            return;
        }

        // Info on included surfaces
        string filter = "tile_type = 'A'";
        DataRow[] foundRows = table.Select(filter);

        foreach (DataRow row in foundRows)
        {
            string label_text;
            if (row["tile"].ToString() == "-")
            {
                label_text = "Pas de choix réalisé!";
            }
            else
            {
                label_text = "Surface " + row["id"].ToString() + ": " + row["tile"].ToString();
            }

            Label surfaceId = new Label(label_text);
            surfaceId.AddToClassList("info-label");
            surfaceId.AddToClassList("wrapped-label");
            ii.Add(surfaceId);
        }

        // Info on non-included surfaces
        bool atLeastOneChoiceDone = false;

        filter = "tile <> '-' AND tile_type = '0'";
        foundRows = table.Select(filter);
        List<string> tilesDone = new List<string>();

        for (int i = 0; i < foundRows.Length; i++)
        {
            string tile = foundRows[i]["tile"].ToString();
            if (!tilesDone.Contains(tile))
            {
                tilesDone.Add(tile);
                string subfilter = "tile_type = '0' AND tile = '" + tile + "'";
                DataRow[] subfoundRows = table.Select(subfilter);
                float area = 0.0f;
                float total_price = 0.0f;
                foreach (DataRow row in subfoundRows)
                {
                    area += (float)row["area"];
                    total_price += (float)row["price"];
                }
                string price = total_price.ToString();
                Label tileLabel = new Label("Carrelage: " + tile + "\n\tSuperficie: " + area + " m²\n\tPrix: " + price + " euros");
                tileLabel.AddToClassList("wrapped-label");
                //tileLabel.style.flexWrap = Wrap.Wrap;
                nii.Add(tileLabel);
                atLeastOneChoiceDone = true;
            }
        }
        
        if (!atLeastOneChoiceDone)
        {
            string choiceForThisObject;
            choiceForThisObject = "Pas de choix réalisé";
            Label surfaceId = new Label(choiceForThisObject);
            surfaceId.AddToClassList("info-label");
            nii.Add(surfaceId);
        }
        else
        {
            VisualElement summary_ve = rve.Q<VisualElement>("not-included-summary");
            string total_overprice = RoomScriptableObject.GetOverPrice(room).ToString();
            Label summary = new Label("Supplément total: " + total_overprice + " euros.");
            summary_ve.Add(summary);
        }
    }
}
