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
    public void OnPickedUp(PlayerCore player)
    {
        Debug.Log($"Pickup {pickupType}");
    }
    public void GetPickupType(out PickupType type)
    {
        type = pickupType;
    }
}