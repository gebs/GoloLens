using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    private static TurnManager instance;

    public static TurnManager Instance
    {
        get
        {
            if (instance == null)
                return instance = new TurnManager();
            else
                return instance;
        }
    }
    private TurnManager()
    {

    }
    public bool IsMyTurn { get; set; }

    public void ChangeCurrentTurn()
    {
        this.IsMyTurn = !this.IsMyTurn;
    }

}
