using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameStates2
{
    BeforeGameStartsInit = 0,
    BeforeGameStarts = 1,
    WaitingForAnchorsInit = 2,
    WaitingForAnchors,
    PlaceBoardInit,
    PlaceBoard,
    WaitingForBoardPlacementInit,
    WaitingForBoardPlacement,
    PlayingInit,
    Playing,
    None
}
