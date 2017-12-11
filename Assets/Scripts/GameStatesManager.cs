using HoloToolkit.Sharing;
using HoloToolkit.Sharing.Spawning;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using System;
using System.Linq;
using UnityEngine;

public class GameStatesManager : MonoBehaviour, IInputClickHandler
{
    /// <summary>
    /// Debug text for displaying information.
    /// </summary>
    public TextMesh DebugText;

    public TextMesh UserInfoText;

    public GameObject BoardPrefab;
    public GameObject SynchronizedParent;
    public GoloLensPerfabSpawnManager BoardSpawnManager;
    public GameObject RedStonePrefab;
    public GameObject WhiteStonePrefab;
    public int DebugTextMaxLines;


    private GameStates gameState;
    private GameObject boardobject;
    private bool isCheckingifBoardNeedsPlacing = false;
    private bool isPlacingObject = false;
    private bool isWaitingForPlacement = false;
    private bool isBoardCreated = false;
    private bool canPlaceBoard = false;
    private bool isPlaying = false;
    private bool anchorDownloaded = false;
    private bool isplayer1 = false;
    private StoneColor myStoneColor;
    private GameObject focusedObject;
   

    // Use this for initialization
    void Start()
    {
        this.gameState = GameStates.BeforeGameStarts;
        // this.myStoneColor = StoneColor.Red;
        BoardSpawnManager.GameObjectSpawned += BoardSpawnManager_GameObjectSpawned;
        //HACK: Change for Turnbased playing
        TurnManager.Instance.IsMyTurn = true;

        if (DebugTextMaxLines == 0)
            DebugTextMaxLines = 20;

        #if UNITY_EDITOR
            anchorDownloaded = true;
        #endif

    }



    // Update is called once per frame
    void Update()
    {
        switch (gameState)
        {
            case GameStates.BeforeGameStarts:
                ((SharingWorldAnchorManager)SharingWorldAnchorManager.Instance).AnchorUploaded += GameStatesManager_AnchorUploaded;
                gameState = GameStates.GameStart;
                break;
            case GameStates.GameStart:
                if (!isCheckingifBoardNeedsPlacing)
                {
                    isCheckingifBoardNeedsPlacing = true;
                    InputManager.Instance.PushModalInputHandler(gameObject);
                    WriteUserInfoText("Looking for Servers, please Wait");
                }
                else
                {
                    CheckIfBoardNeedsPlacing();
                }
                break;
            case GameStates.AnchorSharing:
                if (anchorDownloaded)
                {
                    if (isplayer1)
                    {
                        gameState = GameStates.PlacingBoard;
                    }
                    else
                    {
                        gameState = GameStates.WaitingForPlacingBoard;
                    }
                }
                break;
            case GameStates.PlacingBoard:
                if (!isPlacingObject)
                {
                    isPlacingObject = true;
                    CreateGameObject();
                    ToggleSpatialMesh();
                    InputManager.Instance.PushModalInputHandler(gameObject);
                    canPlaceBoard = true;
                }
                else if (canPlaceBoard)
                {
                    PlaceObject();
                }
                break;
            case GameStates.WaitingForPlacingBoard:
                if (!isWaitingForPlacement)
                {

                    isWaitingForPlacement = true;
                    ((SharingWorldAnchorManager)SharingWorldAnchorManager.Instance).AnchorDownloaded +=
                        GameStatesManager_AnchorDownloaded;
                }
                break;
            case GameStates.Playing:
                if (isPlaying)
                {
                    UpdateFocusedObject();
                    break;
                }
                SpatialMappingManager.Instance.enabled = false;
                Destroy(SpatialMappingManager.Instance.gameObject);
                WriteUserInfoText("Yay, you're playing now!");
                this.isPlaying = true;
                break;
            default:
                break;
        }

    }

    private void GameStatesManager_AnchorUploaded(bool obj)
    {
        if (obj)
            anchorDownloaded = true;
    }

    private void BoardSpawnManager_GameObjectSpawned(object sender, GameObjectSpawnedEventArgs e)
    {
        if (e.SpawnedObject.GameObject.name.Contains("Board"))
        {

            if (e.isLocal)
            {
                myStoneColor = StoneColor.White;
                SharingWorldAnchorManager.Instance.AttachAnchor(boardobject);
            }
            else
            {
                myStoneColor = StoneColor.Red;
            }

            boardobject = e.SpawnedObject.GameObject;
            SetZylinderScripts(boardobject);
            gameState = GameStates.Playing;
        }
        else if (e.SpawnedObject.GameObject.name.Contains("Spielstein") && (e.isLocal && TurnManager.Instance.IsMyTurn || !e.isLocal && !TurnManager.Instance.IsMyTurn))
        {
            GameObject zylinder = FindZylinderByPosition(e.SpawnedObject.GameObject.transform.position - new Vector3(0, 0.01f, 0));

            if (zylinder != null)
            {
                if (zylinder.GetComponent<GameZylinder>().HasStoneSet())
                {
                    BoardSpawnManager.Delete(e.SpawnedObject);
                    BoardSpawnManager.Delete(zylinder.GetComponent<GameZylinder>().Stone);
                    zylinder.GetComponent<GameZylinder>().Stone = null;
                    zylinder.GetComponent<GameZylinder>().StoneColor = StoneColor.None;
                }
                else
                {
                    //   TurnManager.Instance.ChangeCurrentTurn();
                    zylinder.GetComponent<GameZylinder>().Stone = e.SpawnedObject;
                    zylinder.GetComponent<GameZylinder>().StoneColor = e.isLocal ? myStoneColor : (myStoneColor == StoneColor.Red ? StoneColor.White : StoneColor.Red);
                }
            }
        }
    }
    private void GameStatesManager_AnchorDownloaded(bool sucessful, GameObject objectToPlace)
    {
        if (sucessful)
        {
            anchorDownloaded = true;
            //Instantiate<GameObject>(objectToPlace, objectToPlace.transform.position, objectToPlace.transform.rotation);
        }
    }

    private GameObject FindZylinderByPosition(Vector3 position)
    {
        //Collider[] colliders;
        var collidedGameObjects =
                Physics.OverlapSphere(position, 0.01f /*Radius*/)
                .Where(x => x.name.ToLower().Contains("zylinder"))
                .Select(c => c.gameObject)
                .ToArray();

        return collidedGameObjects.First();
    }

    private void CheckIfBoardNeedsPlacing()
    {
        if (SharingStage.Instance.IsConnected)
        {
            WriteUserInfoText("Connected to Server");

            if (SharingStage.Instance.CurrentRoom != null)
            {
                if (SharingStage.Instance.CurrentRoom.GetUserCount() == 1)
                {
                    isplayer1 = true;
                    gameState = GameStates.AnchorSharing;
                }
                else
                {

                    WriteUserInfoText("The other player is placing the object, please wait.");
                    gameState = GameStates.AnchorSharing;
                }
                isCheckingifBoardNeedsPlacing = false;
            }
        }

    }

    private void CreateGameObject()
    {
        WriteDebugText("CreateGameObject");
        if (!isBoardCreated)
        {
            isBoardCreated = true;
            Transform cameraTransform = CameraCache.Main.transform;
            boardobject = Instantiate(BoardPrefab, GetPlacementPosition(cameraTransform.position, cameraTransform.forward, 1.0f), Quaternion.identity);

            boardobject.AddComponent<Interpolator>();
            boardobject.AddComponent<BoxCollider>();
        }
    }
    /// <summary>
    /// Sets the neccessary Scripts on all Zylinder of the Board
    /// </summary>
    /// <param name="gameObject">GameBoard with Zylinders as Children</param>
    private void SetZylinderScripts(GameObject gameObject)
    {
        foreach (var item in gameObject.GetComponentsInChildren<Transform>())
        {
            if (!item.name.Contains("Board") && !item.name.Contains("Cube") && !item.name.ToLower().Contains("line"))
            {

                item.gameObject.AddComponent<BoxCollider>();
                item.gameObject.AddComponent<GameZylinder>();
                item.gameObject.GetComponent<GameZylinder>().Position = new ZylinderPosition() { Row = Convert.ToInt32(item.gameObject.name[0]), Column = Convert.ToInt32(item.gameObject.name[1]) };
                item.name = "Zylinder_" + item.name;
            }
        }
    }

    /// <summary>
    /// Places a new Stone object ontop of a Zylinder
    /// </summary>
    /// <param name="zylinder">Zylinder to place to stone upon</param>
    public void SetStone(GameObject zylinder)
    {
        GameObject stoneperfab = myStoneColor == StoneColor.Red ? RedStonePrefab : WhiteStonePrefab;
        float offset = 0.01f;
        SyncSpawnedObject syncSpawnedObject = null;
        //syncSpawnedObject.GameObject = 
        if (myStoneColor == StoneColor.Red)
            syncSpawnedObject = new SyncStoneRed();
        else
            syncSpawnedObject = new SyncStoneWhite();

        var newBoardPosition = boardobject.transform.InverseTransformPoint(zylinder.transform.position + new Vector3(0, offset, 0));
        BoardSpawnManager.Spawn(syncSpawnedObject, newBoardPosition, zylinder.transform.rotation, boardobject, "Spielstein", true);
        //zylinder.GetComponent<GameZylinder>().Stone = stone;
    }

    public void UpdateFocusedObject()
    {
        GameObject oldfocusedObject = focusedObject;
        Transform cameraTransform = CameraCache.Main.transform;
        int layer = 1 << LayerMask.NameToLayer(boardobject.name);
        RaycastHit hitInfo;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hitInfo))
        {
            focusedObject = hitInfo.collider.gameObject;
        }
        else
        {
            focusedObject = null;
        }
    }
    private void PlaceObject()
    {
        Transform cameraTransform = CameraCache.Main.transform;
        Vector3 placementPosition = GetPlacementPosition(cameraTransform.position, cameraTransform.forward, 1.0f);

        // update the placement to match the user's gaze.
        boardobject.GetComponent<Interpolator>().SetTargetPosition(placementPosition);
        // Rotate this object to face the user.
        boardobject.GetComponent<Interpolator>().SetTargetRotation(Quaternion.Euler(0, cameraTransform.localEulerAngles.y, 0));

    }


    /// <summary>
    /// If we're using the spatial mapping, check to see if we got a hit, else use the gaze position.
    /// </summary>
    /// <returns>Placement position infront of the user</returns>
    private static Vector3 GetPlacementPosition(Vector3 headPosition, Vector3 gazeDirection, float defaultGazeDistance)
    {
        RaycastHit hitInfo;
        if (SpatialMappingRaycast(headPosition, gazeDirection, out hitInfo))
        {
            return hitInfo.point;
        }
        return GetGazePlacementPosition(headPosition, gazeDirection, defaultGazeDistance);
    }

    /// <summary>
    /// Does a raycast on the spatial mapping layer to try to find a hit.
    /// </summary>
    /// <param name="origin">Origin of the raycast</param>
    /// <param name="direction">Direction of the raycast</param>
    /// <param name="spatialMapHit">Result of the raycast when a hit occured</param>
    /// <returns>Wheter it found a hit or not</returns>
    private static bool SpatialMappingRaycast(Vector3 origin, Vector3 direction, out RaycastHit spatialMapHit)
    {
        if (SpatialMappingManager.Instance != null)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(origin, direction, out hitInfo, 30.0f, SpatialMappingManager.Instance.LayerMask))
            {
                spatialMapHit = hitInfo;
                return true;
            }
        }
        spatialMapHit = new RaycastHit();
        return false;
    }

    /// <summary>
    /// Get placement position either from GazeManager hit or infront of the user as backup
    /// </summary>
    /// <param name="headPosition">Position of the users head</param>
    /// <param name="gazeDirection">Gaze direction of the user</param>
    /// <param name="defaultGazeDistance">Default placement distance infront of the user</param>
    /// <returns>Placement position infront of the user</returns>
    private static Vector3 GetGazePlacementPosition(Vector3 headPosition, Vector3 gazeDirection, float defaultGazeDistance)
    {
        if (GazeManager.Instance.HitObject != null)
        {
            return GazeManager.Instance.HitPosition;
        }
        return headPosition + gazeDirection * defaultGazeDistance;
    }

    /// <summary>
    /// If the user is in placing mode, display the spatial mapping mesh.
    /// </summary>
    private void ToggleSpatialMesh()
    {
        if (SpatialMappingManager.Instance != null)
        {
            SpatialMappingManager.Instance.DrawVisualMeshes = isPlacingObject;
        }
    }

    private void WriteUserInfoText(string text)
    {
        WriteDebugText(text);
        if (null == UserInfoText)
            return;
        UserInfoText.text = "\n" + text;
    }

    private void WriteDebugText(string text)
    {
        if (null == DebugText)
            return;

        string debugText = DebugText.text;
        string[] debugTextLines = debugText.Split(new string[] { "\n" }, StringSplitOptions.None);

        if (debugTextLines.Length >= DebugTextMaxLines)
        {
            debugText = "";
            for (int i = 1; i < debugTextLines.Length; i++)
            {
                debugText += debugTextLines[i];
            }
        }

        debugText += "\n" + text;
        DebugText.text = debugText;

    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        switch (gameState)
        {
            case GameStates.PlacingBoard:
                isPlacingObject = false;
                ToggleSpatialMesh();
                var newBoardPosition = SynchronizedParent.transform.InverseTransformPoint(boardobject.transform.localPosition);
                BoardSpawnManager.Spawn(new SyncSpawnedObject(), newBoardPosition, boardobject.transform.localRotation, SynchronizedParent, "GameBoard", true);
                Destroy(boardobject);
                break;
            case GameStates.WaitingForPlacingBoard:
                break;
            case GameStates.Playing:
                if (TurnManager.Instance.IsMyTurn)
                    SetStone(focusedObject);
                break;
            default:
                break;
        }
    }
}
