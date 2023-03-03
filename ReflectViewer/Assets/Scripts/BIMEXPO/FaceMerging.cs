using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Reflect;
using System.IO;

public class FaceMerging : MonoBehaviour
{
    public GameObject selectedObject; //Leave empty as usual
    List<Material> matPoss;
    List<string> namePoss;
    public Text textCosts; //Empty text object
    int curScenario1 = 1;
    double curCost1 = 0.00;
    double totArea1 = 0;
    public GameObject root;
    public ChangeMaterial changeMatScript;
    public Metadata metaCur;
    public TilesChoiceMenuScript tilesChoiceMenuScript;
    public Material defMat; //default material that gets used when adding/removing objects from merged list

    public List<GameObject> listCustom; //To be left empty
    public List<List<GameObject>> listOfListCustom; //The list of all merged lists, starts empty but is built upon automatically, start empty
    public int curList; //The index of the current list in listOfListCustom (yes, kinda confusing...), start empty too or at 0
    public int countDebug;
    public int countDebug2;
    bool mergedSelected;

    // Start is called before the first frame update
    void Start()
    {
        root = GameObject.Find("Root");
        if (changeMatScript == null)
        {
            changeMatScript = root.GetComponent<ChangeMaterial>();
        }
        if (tilesChoiceMenuScript == null)
        {
            root = GameObject.Find("TileChoiceMenu");
            tilesChoiceMenuScript = root.GetComponent<TilesChoiceMenuScript>();
            root.SetActive(false);
            root = GameObject.Find("Root");
        }

        listCustom = new List<GameObject>();
        //listCustom.Add(root); //DEBUG
        listOfListCustom = new List<List<GameObject>>();
        listOfListCustom.Add(listCustom);
        curList = 0;
        mergedSelected = false;

    }

    // Update is called once per frame
    void Update()
    {
        if(root == null)
        {
            root = GameObject.Find("Root");
        }
        countDebug = listOfListCustom[curList].Count;
        //countDebug = listOfListCustom.Count;
        if (changeMatScript.selectedObject != null)
        {
            selectedObject = changeMatScript.selectedObject;
        }
        if (selectedObject != null && selectedObject.GetComponent<Metadata>() != null)
        {
            
            metaCur = selectedObject.GetComponent<Metadata>();
            string test = selectedObject.GetComponent<Metadata>().GetParameter("Area");
            //Debug.Log(test);
            double curArrea = 0.0;
            if (test.Split()[0].Length > 0)
            {
                curArrea = double.Parse(test.Split()[0], System.Globalization.CultureInfo.InvariantCulture);
            }
            namePoss = new List<string>();
            curCost1 = 0.0;
            totArea1 = 0.0;
            if (listCustom.Count >= 5000) //If the current merged list is not empty
            {
                foreach (GameObject go in listCustom)
                {
                    matPoss = changeMatScript.CreateUINew(go, 0); //Get the possible materials from changeMatScript
                    if (matPoss.Count >= 1)
                    {
                        foreach (Material mat in matPoss)
                        {
                            namePoss.Add(mat.name);
                            namePoss.Add(mat.name + " (Instance)");
                        }
                        if (namePoss.Contains(go.GetComponent<MeshRenderer>().sharedMaterial.name))
                        {
                            curCost1 += double.Parse(go.GetComponent<Metadata>().GetParameter("Area").Split()[0], System.Globalization.CultureInfo.InvariantCulture) * 20.0;
                        }
                    }
                    test = go.GetComponent<Metadata>().GetParameter("Area");
                    totArea1 += double.Parse(test.Split()[0], System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            //textCosts.text = "Area is " + curArrea.ToString() + "\nThe price of scenario " + curScenario1 + " is " + curCost1.ToString() + "\nSelected area is " + totArea1;// + "\nThe price of scenario " + curScenario2 + " is " + curCost2.ToString() + "\nSelected area is " + totArea2 + "\nTotal area: " + (totArea1 + totArea2) +"\nTotal cost: " + (curCost1 + curCost2).ToString();


            

            if (Input.GetMouseButtonUp(1))
            {
                selectedObject = ClickObjects();
            }
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt)) //right click and ctrl and alt; ADD NEW CUSTOMLIST!!!
            {
                //Debug.Log("ctrl alt");
                listOfListCustom.Add(new List<GameObject>());
                curList = listOfListCustom.Count - 1;
                listOfListCustom[curList].Add(selectedObject);
            }
            else if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl)) //right click and ctrl, ADD TO OR REMOVE FROM CUSTOMLIST
            {
                
                if (!listOfListCustom[curList].Contains(selectedObject)) //If current list doesn't contain the object, add it
                {
                    //Debug.Log("curlist: " + curList.ToString());
                    listOfListCustom[curList].Add(selectedObject);
                    foreach (GameObject go in listOfListCustom[curList])
                    {
                        //Debug.Log(go.name);
                        //changeMatScript.ChangeMaterialClick(defMat, go);
                        changeMatScript.HighlightObject(go, true);
                    }
                }
                for (int i = 0; i < listOfListCustom.Count; i++) //Remove it in any other list
                {
                    if (i != curList && listOfListCustom[i].Contains(selectedObject))
                    {
                        listOfListCustom[i].Remove(selectedObject);
                    }
                }
                Debug.Log(listOfListCustom[curList].Count.ToString());

            }
            //foreach (GameObject go in listOfListCustom[curList])
            //{
            //    changeMatScript.HighlightObject(go, true);
            //}

            mergedSelected = false; ;
            for (int i = 0; i < listOfListCustom.Count; i++)
            {
                if (listOfListCustom[i].Contains(selectedObject))
                {
                    curList = i;
                    mergedSelected = true;
                }
            }

            for (int i = 0; i < listOfListCustom.Count; i++)
            {
                foreach (GameObject go in listOfListCustom[i])
                {
                    if (!listOfListCustom[i].Contains(selectedObject))
                    {
                        changeMatScript.HighlightObject(go, false);
                    }
                }
            }

            if (mergedSelected && curList >= 0 && listOfListCustom[curList] != null && listOfListCustom[curList].Count >= 1)
            {
                foreach (GameObject go in listOfListCustom[curList])
                {
                    //changeMatScript.ChangeMaterialClick(defMat, go);
                    changeMatScript.HighlightObject(go, true);

                    //Debug.Log("REPLACE: " + go.name); //This gets called at least
                    if (changeMatScript.functionReplaceCalled == true && tilesChoiceMenuScript.chosenMaterial != null) //When the function to replace a material is called anywhere, check if the object is part of any merge list and if so change materials on all of them
                    {
                        tilesChoiceMenuScript.selectionDone = true;
                        tilesChoiceMenuScript.target = go;
                        tilesChoiceMenuScript.ApplyChosenMaterialToSurface();
                        tilesChoiceMenuScript.SaveChosenMaterialToDB();
                        tilesChoiceMenuScript.selectionDone = false;
                        changeMatScript.functionReplaceCalled = true;
                    }
                }
                changeMatScript.functionReplaceCalled = false;
            }
        }
    }

    GameObject ClickObjects() //Returns the gameobject that is clicked
    {
        Ray ray;
        GameObject target = null;
        if (Input.touchCount > 2 && Input.touches[2].phase == TouchPhase.Began)
        {
            ray = Camera.main.ScreenPointToRay(Input.touches[2].position); //touch
        }
        else
        {
            ray = Camera.main.ScreenPointToRay(Input.mousePosition); //Mouse
        }
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) // you can also only accept hits to some layer and put your selectable units in this layer
        {
            if (hit.transform != null && hit.transform.IsChildOf(root.transform))
            {
                target = hit.transform.gameObject;
            }
        }
        return target;
    }

    void OnApplicationQuit() //When Unity halts, create a new CSV file
    {
        Debug.Log("Application ending after " + Time.time + " seconds");
        CreateCSV("Test");
    }
    void CreateCSV(string fileName) //Create CSV file: path, attributes. Reset file and fill it
    {

        string path = "C:/Users/cdri/Documents" + "/" + fileName + ".csv";
        File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);  // to put in try/catch if file doesn't exist
        if (File.Exists(path))
        {
            File.WriteAllText(path, String.Empty);
            //File.Delete(path);
        }

        var sr = File.CreateText(path);

        string data = textCosts.text;

        sr.WriteLine(data);

        FileInfo fInfo = new FileInfo(path);
        fInfo.IsReadOnly = true;

        sr.Close();

        //Application.OpenURL(path);
    }

}
