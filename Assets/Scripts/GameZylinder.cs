using HoloToolkit.Sharing.Spawning;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameZylinder : MonoBehaviour
{
    public StoneColor StoneColor { get; set; }
    public ZylinderPosition Position { get; set; }
    public SyncSpawnedObject Stone { get; set; }

    public bool HasStoneSet() {
        return Stone != null;
    }

}
