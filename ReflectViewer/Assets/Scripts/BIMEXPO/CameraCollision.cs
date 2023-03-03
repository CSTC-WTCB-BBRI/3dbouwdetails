using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Reflect;

public class CameraCollision : MonoBehaviour
{
    public static string room = "Not in a room!";

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 29)
        {
            room = other.gameObject.GetComponent<Metadata>().GetParameter(RoomScriptableObject.room_parameter);
            RoomScriptableObject.current_room = room;
        }
    }
}
