using HoloToolkit.Sharing;
using HoloToolkit.Sharing.Spawning;
using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoloLensPerfabSpawnManager : PrefabSpawnManager {

    public event EventHandler<GameObjectSpawnedEventArgs> GameObjectSpawned;

    protected override GameObject CreatePrefabInstance(SyncSpawnedObject dataModel, GameObject prefabToInstantiate, GameObject parentObject, string objectName)
    {
        var instance =  base.CreatePrefabInstance(dataModel, prefabToInstantiate, parentObject, objectName);
        
        if (GameObjectSpawned != null)
        {
            GameObjectSpawned(this, new GameObjectSpawnedEventArgs()
            {
                SpawnedObject = dataModel
                ,
                isLocal = dataModel.Owner != null ? (dataModel.Owner.GetID() == SharingStage.Instance.Manager.GetLocalUser().GetID()) : false
            });

        }
        return instance;
    }
}

public class GameObjectSpawnedEventArgs : EventArgs
{
    public SyncSpawnedObject SpawnedObject { get; set; }
    public bool isLocal { get; set; }
}
