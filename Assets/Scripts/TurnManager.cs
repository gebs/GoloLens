using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnManager : Singleton<TurnManager>
{
    public bool IsMyTurn { get; set; }

    public void ChangeCurrentTurn()
    {
        this.IsMyTurn = !this.IsMyTurn;
    }

}
