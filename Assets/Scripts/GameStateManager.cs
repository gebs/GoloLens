﻿using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity.SharingWithUNET;
using HoloToolkit.Unity.SpatialMapping;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameStateManager : Singleton<GameStateManager>, IInputClickHandler
{
    public GameObject BoardPrefab;
    public GameObject UserInfoTextPrefab;

    private NetworkDiscoveryWithAnchors networkDiscovery;
    private GameStates gameState;
    private bool isPlayer1 = false;
    private GameObject boardobject;
    private GameObject userInfoObject;

    // Use this for initialization
    void Start()
    {
        gameState = GameStates.BeforeGameStartsInit;
        networkDiscovery = NetworkDiscoveryWithAnchors.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        switch (gameState)
        {
            case GameStates.BeforeGameStartsInit:
                networkDiscovery.ConnectionStatusChanged += Instance_ConnectionStatusChanged;
                gameState = GameStates.BeforeGameStarts;
                break;
            case GameStates.BeforeGameStarts:
                break;
            case GameStates.WaitingForAnchorsInit:
                userInfoObject = Instantiate(UserInfoTextPrefab, GetPlacementPosition(CameraCache.Main.transform.position, CameraCache.Main.transform.forward, 2.0f), Quaternion.identity);
                userInfoObject.GetComponent<TextMesh>().text = "Please wait for the Anchor to be downloaded...";
                gameState = GameStates.WaitingForAnchors;
                break;
            case GameStates.WaitingForAnchors:
                if (UNetAnchorManager.Instance != null && UNetAnchorManager.Instance.AnchorEstablished)
                {
                    if (isPlayer1)
                    {
                        gameState = GameStates.PlaceBoardInit;
                    }
                    else
                        gameState = GameStates.WaitingForBoardPlacementInit;
                }
                break;
            case GameStates.PlaceBoardInit:
                userInfoObject.SetActive(false);
                CreateGameObject();
                ToggleSpatialMesh();
                gameState = GameStates.PlaceBoard;
                break;
            case GameStates.WaitingForBoardPlacementInit:
                userInfoObject.GetComponent<TextMesh>().text = "Please wait until the other user has placed the board...";
                gameState = GameStates.WaitingForBoardPlacement;
                break;
            default:
                break;
        }
    }
    public void DestoryInfo() {
        Destroy(userInfoObject);
    }
    private void CreateGameObject()
    {
        Transform cameraTransform = CameraCache.Main.transform;
        boardobject = Instantiate(BoardPrefab, SharedCollection.Instance.transform.InverseTransformPoint(GetPlacementPosition(cameraTransform.position, cameraTransform.forward, 1.0f)), Quaternion.identity);

        NetworkServer.SpawnWithClientAuthority(boardobject,GoloLensPlayerController.Instance.gameObject);
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
            SpatialMappingManager.Instance.DrawVisualMeshes = gameState == GameStates.PlaceBoard || gameState == GameStates.PlaceBoardInit;
        }
    }
    private void Instance_ConnectionStatusChanged(object sender, System.EventArgs e)
    {
        Debug.Log("ConnectionStatus Changed");
        if (networkDiscovery.Connected)
        {
            if (networkDiscovery.isServer)
            {
                isPlayer1 = true;
            }
            else
            {

            }
            Debug.Log("IsConnected");
            gameState = GameStates.WaitingForAnchorsInit;
            networkDiscovery.ConnectionStatusChanged -= Instance_ConnectionStatusChanged;
        }

    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("OnInputClicked");
        switch (gameState)
        {
            case GameStates.PlaceBoard:
                break;
            default:
                break;
        }
    }
}