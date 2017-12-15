using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SharingWithUNET;
using HoloToolkit.Unity.SpatialMapping;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[NetworkSettings(sendInterval = 0.033f)]
public class BoardController : NetworkBehaviour, IInputClickHandler
{

    public GameObject RedStonePrefab;
    public GameObject WhiteStonePrefab;

    private Transform myTransform;
    private float lerpRate = 0.3f;

    [SyncVar] private GameStates2 gameState;
    [SyncVar] private Vector3 syncPos;
    [SyncVar] private Quaternion syncYRot;

    private GameObject focusedObject;
    private Vector3 lastPos;
    private Quaternion lastRot;

    private static BoardController _Instance = null;
    /// <summary>
    /// Instance of the PlayerController that represents the local player.
    /// </summary>
    public static BoardController Instance
    {
        get
        {
            return _Instance;
        }
    }
    // Use this for initialization
    void Start()
    {

        _Instance = this;
        InputManager.Instance.PushModalInputHandler(gameObject);
        transform.SetParent(SharedCollection.Instance.transform, false);
        this.gameObject.AddComponent<Interpolator>();
        this.gameObject.AddComponent<BoxCollider>();
        SetZylinderScripts();
        myTransform = transform;
        this.gameState = GameStates2.PlaceBoard;
    }
    private void Update()
    {
        if (gameState == GameStates2.PlaceBoard)
        {
            if (isServer)
            {
                PlaceObject();
                TransmitMotion();
            }

        }
        else if (gameState == GameStates2.PlayingInit)
        {
            if (isServer)
                TransmitMotion();

            SpatialMappingManager.Instance.enabled = false;
            Destroy(this.gameObject.GetComponent<BoxCollider>());
            Destroy(SpatialMappingManager.Instance.gameObject);

            //The Server starts the Game
            TurnManager.Instance.IsMyTurn = isServer;

            gameState = GameStates2.Playing;
        }
        else if (gameState == GameStates2.Playing)
        {
            if (isServer)
                TransmitMotion();

            UpdateFocusedObject();
        }

    }
    public void UpdateFocusedObject()
    {
        GameObject oldfocusedObject = focusedObject;
        Transform cameraTransform = CameraCache.Main.transform;
        int layer = 1 << LayerMask.NameToLayer(this.name);
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
        if (!isServer) { return; }
        Transform cameraTransform = CameraCache.Main.transform;
        Vector3 placementPosition = GetPlacementPosition(cameraTransform.position, cameraTransform.forward, 1.0f);

        // update the placement to match the user's gaze.
        this.GetComponent<Interpolator>().SetTargetPosition(placementPosition);
        // Rotate this object to face the user.
        this.GetComponent<Interpolator>().SetTargetRotation(Quaternion.Euler(0, cameraTransform.localEulerAngles.y, 0));

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
    void TransmitMotion()
    {
        if (!isServer)
        {
            return;
        }

        //Transform情報更新(キャッシュされたTransformから取得するほうが早い)
        lastPos = myTransform.position;
        lastRot = myTransform.rotation;

        
        //SyncVar変数を変更し、全クライアントと同期を図る
        syncPos = SharedCollection.Instance.transform.InverseTransformPoint(myTransform.position); 
        //localEulerAngles: Quaternion→オイラー角(360度表記)
        syncYRot = myTransform.rotation;

    }
    //現在のTransform情報とSyncVar情報とを補間する

    /// <summary>
    /// Sets the neccessary Scripts on all Zylinder of the Board
    /// </summary>
    /// <param name="gameObject">GameBoard with Zylinders as Children</param>
    private void SetZylinderScripts()
    {
        foreach (var item in this.GetComponentsInChildren<Transform>())
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
        if (!zylinder.GetComponent<GameZylinder>().HasStoneSet() && (TurnManager.Instance.IsMyTurn || true))
        {
            GameObject stoneperfab = isServer ? RedStonePrefab : WhiteStonePrefab;
            float offset = 0.01f;
            var stone = Instantiate(stoneperfab, SharedCollection.Instance.transform.InverseTransformPoint(zylinder.transform.position + new Vector3(0, offset, 0)), zylinder.transform.rotation);
            NetworkServer.Spawn(stone);
        }
        else if (zylinder.GetComponent<GameZylinder>().HasStoneSet()
            && ((zylinder.GetComponent<GameZylinder>().StoneColor == StoneColor.Red && isServer)
            || (zylinder.GetComponent<GameZylinder>().StoneColor == StoneColor.White && isClient)))
        {
            CmdDeleteStone(zylinder.GetComponent<GameZylinder>().Position.Row, zylinder.GetComponent<GameZylinder>().Position.Column);
        }
    }
    [Command]
    private void CmdDeleteStone(int row, int column)
    {
        GameObject stone = FindZylinderByPosition(row, column);
        if (stone != null)
            Destroy(stone);
    }

    private GameObject FindZylinderByPosition(int row, int column)
    {
        foreach (var item in this.GetComponentsInChildren<Transform>())
        {
            if (item.name.Contains("Zylinder"))
            {
                if (item.gameObject.GetComponent<GameZylinder>().Position.Row == row && item.gameObject.GetComponent<GameZylinder>().Position.Column == column)
                    return item.gameObject.GetComponent<GameZylinder>().Stone;
            }
        }
        return null;
    }
    public void OnInputClicked(InputClickedEventData eventData)
    {
        switch (gameState)
        {

            case GameStates2.PlaceBoard:
                gameState = GameStates2.PlayingInit;
                break;
            case GameStates2.Playing:
                SetStone(focusedObject);
                break;
            default:
                break;
        }
    }
}
