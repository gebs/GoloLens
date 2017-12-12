using HoloToolkit.Unity.SharingWithUNET;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class StoneController : NetworkBehaviour
{

    // Use this for initialization
    void Start()
    {
        transform.SetParent(SharedCollection.Instance.transform, false);
        GameObject zylinder = FindZylinderByPosition(transform.position - new Vector3(0, 0.01f, 0));

        if (zylinder != null)
        {
            zylinder.GetComponent<GameZylinder>().StoneColor = BoardController.Instance.isServer ? StoneColor.Red : StoneColor.White;
            zylinder.GetComponent<GameZylinder>().Stone = gameObject;
        }
        TurnManager.Instance.ChangeCurrentTurn();
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

}
