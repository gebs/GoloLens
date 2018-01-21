using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SharingWithUNET;
using HoloToolkit.Unity.SpatialMapping;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class BoardController : NetworkBehaviour, IInputClickHandler
{

    [SerializeField] public GameObject RedStonePrefab;
    [SerializeField] public GameObject WhiteStonePrefab;

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


    /// <summary>
    /// The position relative to the shared world anchor.
    /// </summary>
    [SyncVar(hook = "xformchange")]
    private Vector3 localPosition;

    private GameStates gameState;

    private void xformchange(Vector3 update)
    {
        Debug.Log(localPosition + " xform change " + update);
        localPosition = update;
    }

    // <summary>
    /// The rotation relative to the shared world anchor.
    /// </summary>
    [SyncVar]
    private Quaternion localRotation;

    /// <summary>
    /// Sets the localPosition and localRotation on clients.
    /// </summary>
    /// <param name="postion">the localPosition to set</param>
    /// <param name="rotation">the localRotation to set</param>
    [Command]
    public void CmdTransform(Vector3 postion, Quaternion rotation)
    {
        if (!isLocalPlayer)
        {
            localPosition = postion;
            localRotation = rotation;
        }
    }

    private bool Moving;
    private int layerMask;
    private InputManager inputManager;
    public Vector3 movementOffset = Vector3.zero;
    private GameObject focusedObject;

    // Use this for initialization
    private void Start()
    {
        transform.SetParent(SharedCollection.Instance.transform, true);
        _Instance = this;
        if (isServer)
        {
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            Moving = true;
        }

        layerMask = SpatialMappingManager.Instance.LayerMask;
        inputManager = InputManager.Instance;
      //  inputManager.AddGlobalListener(gameObject);
        SetZylinderScripts();
        this.gameState = GameStates.PlaceBoard;

    }

    // Update is called once per frame
    private void Update()
    {
        if (gameState == GameStates.PlaceBoard)
        {
            if (Moving)
            {
                transform.position = Vector3.Lerp(transform.position, ProposeTransformPosition(), 0.2f);

                // Depending on if you are host or client, either setting the SyncVar (host) 
                // or calling the Cmd (client) will update the other users in the session.
                // So we have to do both.
                localPosition = transform.localPosition;
                localRotation = transform.localRotation;
                if (GoloLensPlayerController.Instance != null)
                {
                    GoloLensPlayerController.Instance.SendSharedTransform(gameObject, localPosition, localRotation);
                }
            }
            else
            {

                transform.localPosition = localPosition;
                transform.localRotation = localRotation;
            }
        }
        else if (gameState == GameStates.PlayingInit)
        {
            SpatialMappingManager.Instance.enabled = false;
            Destroy(this.gameObject.GetComponent<BoxCollider>());
            Destroy(SpatialMappingManager.Instance.gameObject);

            //The Server starts the Game
            TurnManager.Instance.IsMyTurn = isServer;

            gameState = GameStates.Playing;
        }
        else if (gameState == GameStates.Playing)
        {
            UpdateFocusedObject();
        }
    }
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

    /// <summary>
    /// Places a new Stone object ontop of a Zylinder
    /// </summary>
    /// <param name="zylinder">Zylinder to place to stone upon</param>
    public void SetStone(GameObject zylinder)
    {
        if (!zylinder.GetComponent<GameZylinder>().HasStoneSet() && (TurnManager.Instance.IsMyTurn))
        {
            float offset = 0.01f;
            GoloLensPlayerController.Instance.SpawnStone(isServer, SharedCollection.Instance.transform.InverseTransformPoint(zylinder.transform.position + new Vector3(0, offset, 0)), zylinder.transform.rotation);
        }
        else if (zylinder.GetComponent<GameZylinder>().HasStoneSet()
            && ((zylinder.GetComponent<GameZylinder>().StoneColor == StoneColor.Red && isServer)
            || (zylinder.GetComponent<GameZylinder>().StoneColor == StoneColor.White && isClient)))
        {
            GameZylinder gz = zylinder.GetComponent<GameZylinder>();

            if (!isServer)
                GoloLensPlayerController.Instance.DeleteStone(gz.Position.Row, gz.Position.Column);
            else
                GoloLensPlayerController.Instance.DeleteStoneRpc(gz.Position.Row, gz.Position.Column);
        }
    }

    /*   [Command]
       private void CmdDeleteStone(int row, int column)
       {
           RpcDeleteStone(row, column);
       }

       [ClientRpc]
       private void RpcDeleteStone(int row, int column)
       {
           GameObject stone = FindZylinderByPosition(row, column);
           if (stone != null)
               Destroy(stone);
       }
       */
    [Command]
    private void CmdChangeState(int state)
    {
        RpcChangeState(state);
    }

    [ClientRpc]
    private void RpcChangeState(int state)
    {
        gameState = (GameStates)Enum.ToObject(typeof(GameStates), state);
    }


    public GameObject FindZylinderByPosition(int row, int column)
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

    private Vector3 ProposeTransformPosition()
    {
        // Put the model 3m in front of the user.
        Vector3 retval = Camera.main.transform.position + Camera.main.transform.forward * 3 + movementOffset;
        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 5.0f, layerMask))
        {
            retval = hitInfo.point + movementOffset;
        }
        return retval;
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (gameState == GameStates.PlaceBoard && isServer)
        {
            Moving = !Moving;
            if (Moving)
            {
              
                if (SpatialMappingManager.Instance != null)
                {
                    SpatialMappingManager.Instance.DrawVisualMeshes = true;
                }
            }
            else
            {
               
                if (SpatialMappingManager.Instance != null)
                {
                    SpatialMappingManager.Instance.DrawVisualMeshes = false;
                }

            }
            CmdChangeState((int)GameStates.PlayingInit);
        }
        else if (gameState == GameStates.Playing)
        {
            SetStone(focusedObject);
        }



        eventData.Use();

    }
}