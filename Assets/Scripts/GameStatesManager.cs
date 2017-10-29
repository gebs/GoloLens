using HoloToolkit.Sharing;
using HoloToolkit.Sharing.Spawning;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SpatialMapping;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStatesManager : MonoBehaviour, IInputClickHandler
{
    /// <summary>
    /// Debug text for displaying information.
    /// </summary>
    public TextMesh DebugText;

    public TextMesh UserInfoText;

    public GameObject BoardPerfab;
    public PrefabSpawnManager BoardSpawnManager;

    private GameStates gameState;
    private GameObject boardobject;
    bool isCheckingifBoardNeedsPlacing = false;
    bool isPlacingObject = false;
    bool isWaitingForPlacement = false;
    bool isBoardCreated = false;
    // Use this for initialization
    void Start()
    {
        this.gameState = GameStates.BeforeGameStarts;
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameState)
        {
            case GameStates.BeforeGameStarts:
                gameState = GameStates.GameStart;
                break;
            case GameStates.GameStart:
                if (!isCheckingifBoardNeedsPlacing)
                {
                    isCheckingifBoardNeedsPlacing = true;
                    WriteUserInfoText("Looking for Servers, please Wait");

                }
                else
                {
                    CheckIfBoardNeedsPlacing();
                }
                break;
            case GameStates.PlacingBoard:
                if (!isPlacingObject)
                {
                    isPlacingObject = true;
                    CreateGameObject();
                    ToggleSpatialMesh();
                    InputManager.Instance.PushModalInputHandler(gameObject);
                }
                else
                {
                    PlaceObject();
                }
                break;
            case GameStates.WaitingForPlacingBoard:
                if (!isWaitingForPlacement)
                {

                    ((SharingWorldAnchorManager)SharingWorldAnchorManager.Instance).AnchorDownloaded += GameStatesManager_AnchorDownloaded;
                }
                break;
            default:
                break;
        }

    }

    private void GameStatesManager_AnchorDownloaded(bool sucessfull, GameObject objectToPlace)
    {
        if (sucessfull)
        {

        }
    }

    private void CheckIfBoardNeedsPlacing()
    {
        //WriteDebugText("CheckIfBoardNeedsPlacing");

        if (SharingStage.Instance.IsConnected)
        {
            WriteUserInfoText("Connected to Server");

            if (SharingStage.Instance.CurrentRoom != null)
            {
                if (SharingStage.Instance.CurrentRoom.GetUserCount() == 1)
                {
                    gameState = GameStates.PlacingBoard;
                }
                else
                {
                    WriteUserInfoText("The other player is placing the object, please wait.");
                    gameState = GameStates.WaitingForPlacingBoard;
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
            boardobject = Instantiate(BoardPerfab, GetPlacementPosition(cameraTransform.position, cameraTransform.forward, 1.0f), Quaternion.identity);

            boardobject.AddComponent<Interpolator>();
            boardobject.AddComponent<BoxCollider>();
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

    private void AnchorDownloaded(bool test, GameObject gameObject)
    {

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
        UserInfoText.text = "\r\n" + text;
    }

    private void WriteDebugText(string text)
    {
        if (null == DebugText)
            return;

        DebugText.text += "\r\n" + text;

    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        switch (gameState)
        {
            case GameStates.PlacingBoard:
                isPlacingObject = false;
                ToggleSpatialMesh();
                SharingWorldAnchorManager.Instance.AttachAnchor(boardobject);
                SyncSpawnedObject syncSpawnedObject = new SyncSpawnedObject();
                syncSpawnedObject.GameObject = BoardPerfab;
                BoardSpawnManager.Spawn(syncSpawnedObject, boardobject.transform.localPosition, boardobject.transform.localRotation, null, "GameBoard", true);
                gameState = GameStates.WaitingForPlacingBoard;
                break;
            case GameStates.WaitingForPlacingBoard:
                break;
            default:
                break;
        }
    }
}
