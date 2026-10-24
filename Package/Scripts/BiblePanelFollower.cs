using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BiblePanelFollower : UdonSharpBehaviour
{
    public BibleController controller;
    private bool tracking;
    private Vector3 previousAnchor;

    public VRCPlayerApi.TrackingData Anchor()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        VRCPlayerApi.TrackingDataType target = VRCPlayerApi.TrackingDataType.Head;
        if (player.IsUserInVR())
        {
            if (controller.followSource == 1) target = VRCPlayerApi.TrackingDataType.LeftHand;
            else if (controller.followSource == 2) target = VRCPlayerApi.TrackingDataType.RightHand;
        }
        return player.GetTrackingData(target);
    }

    public float FollowAmount(float seconds)
    {
        return controller.FollowBlend(seconds);
    }
    public void Capture()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!Utilities.IsValid(player)) return;
        VRCPlayerApi.TrackingData head = Anchor();
        Quaternion inverse = Quaternion.Inverse(head.rotation);
        controller.viewOffset = inverse * (controller.mainPanel.transform.position - head.position);
        controller.viewRotation = inverse * controller.mainPanel.transform.rotation;
        controller.hasViewPlacement = true;
        previousAnchor = head.position;
        tracking = true;
    }

    public void FollowView()
    {
        if (!controller.isOpen || !controller.follow || controller.held) return;
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!Utilities.IsValid(player)) return;
        VRCPlayerApi.TrackingData head = Anchor();
        if (!controller.hasViewPlacement) Capture();
        float amount = !tracking || Vector3.Distance(head.position, previousAnchor) > 3f ? 1f : FollowAmount(Time.deltaTime);
        previousAnchor = head.position;
        tracking = true;
        Transform panel = controller.mainPanel.transform;
        panel.SetPositionAndRotation(Vector3.Lerp(panel.position, head.position + head.rotation * controller.viewOffset, amount), Quaternion.Slerp(panel.rotation, head.rotation * controller.viewRotation, amount));
    }

    public void SnapToAnchor() { tracking = false; FollowView(); }

    private void LateUpdate() { FollowView(); }
}
