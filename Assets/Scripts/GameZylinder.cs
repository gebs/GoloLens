using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameZylinder : MonoBehaviour
{
    public StoneColor StoneColor { get; set; }
    public ZylinderPosition Position { get; set; }
    public GameObject Stone { get; set; }

    public bool HasStoneSet() {
        return StoneColor != StoneColor.None;
    }

}
