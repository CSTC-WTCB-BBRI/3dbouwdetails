using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Reflect;

public class BuildingInfo : MonoBehaviour
{
    public static List<string> roomNames = new List<string>();
    public static List<GameObject> roomPlaceHolders = new List<GameObject>();
    public static Regex rx = new Regex(@" \[[0-9]*\]$");
    public static Regex areaRegex = new Regex(@" m²$");

    [SerializeField]
    private RoomScriptableObject tables;

    public static GameObject GetPlaceHolderFromRoomName(string room_name)
    {
        GameObject foundPH = null;
        foreach (GameObject go in roomPlaceHolders)
        {
            string roomName = rx.Split(go.GetComponent<Metadata>().GetParameter("Mark"))[0];
            if (roomName == room_name)
            {
                foundPH = go;
            }
        }
        return foundPH;
    }

    public static void ListRooms()
    {
        foreach (string room in roomNames)
        {
            Debug.Log("Room: " + room);
        }
    }
}
