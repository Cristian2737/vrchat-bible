using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BibleWindowHandle : UdonSharpBehaviour
{
    public BibleController controller;
    public VRCPickup pickup;
    public BibleWindowManipulator manipulator;
    public int edgeX;
    public int edgeY;
    [System.NonSerialized] public bool grabbed;
    [System.NonSerialized] public bool leftHand;

    public override void OnPickup()
    {
        leftHand = pickup.currentHand == VRC_Pickup.PickupHand.Left;
        grabbed = true;
        manipulator.GrabChanged();
    }

    public override void OnDrop()
    {
        grabbed = false;
        manipulator.GrabChanged();
    }

    public void Release()
    {
        manipulator.ReleaseAll();
    }
}
