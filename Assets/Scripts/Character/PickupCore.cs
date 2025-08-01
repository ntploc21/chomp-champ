using UnityEngine;
using UnityEngine.U2D.Animation;
using System.Collections.Generic;

public enum PickupType
{
    Dash,
    DoubleScore,
    Speed
}

public class PickupCore : MonoBehaviour, IPickup
{
    public PickupType pickupType;
    public Vector3Int spawnCell;
    public PickupManager pickupManager;
    public void OnPickedUp(PlayerCore player)
    {
        // Debug.Log($"Pickup {pickupType}");
        pickupManager?.FreeCell(spawnCell);
    }
    public void GetPickupType(out PickupType type)
    {
        type = pickupType;
    }
}