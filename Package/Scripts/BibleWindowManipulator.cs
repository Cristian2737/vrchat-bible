using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BibleWindowManipulator : UdonSharpBehaviour
{
    public BibleController controller;
    public BibleWindowHandle[] handles;
    public float maximumGrabDistance = 1f;
    public float edgeThickness = 0.055f;
    public float cornerSize = 0.09f;
    private BibleWindowHandle first;
    private BibleWindowHandle second;
    private Vector3 positionOffset;
    private Quaternion rotationOffset;
    private Vector3 initialMidpoint;
    private Vector3 initialPanelPosition;
    private Quaternion initialPanelRotation;
    private Vector3 initialSpan;
    private float initialWidth;
    private float initialHeight;
    private bool resizeWidth;
    private bool resizeHeight;
    private bool releasing;

    public void GrabChanged()
    {
        if (releasing) return;
        first = null;
        second = null;
        for (int i = 0; i < handles.Length; i++)
        {
            if (!handles[i].grabbed) continue;
            if (first == null) first = handles[i];
            else if (handles[i].leftHand != first.leftHand) second = handles[i];
        }
        if (first == null)
        {
            if (controller.held) controller.EndHold();
            RefreshHandles();
            return;
        }
        Transform panel = controller.mainPanel.transform;
        if (!controller.held)
        {
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player))
            {
                Vector3 head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                panel.position = LimitGrabDistance(panel.position, head);
            }
            controller.BeginHold();
        }
        Vector3 a = HandPosition(first);
        Quaternion rotation = HandRotation(first);
        positionOffset = Quaternion.Inverse(rotation) * (panel.position - a);
        rotationOffset = Quaternion.Inverse(rotation) * panel.rotation;
        if (second == null) return;
        Vector3 b = HandPosition(second);
        initialMidpoint = (a + b) * 0.5f;
        initialPanelPosition = panel.position;
        initialPanelRotation = panel.rotation;
        initialSpan = Quaternion.Inverse(panel.rotation) * (b - a);
        initialWidth = controller.panelWidth;
        initialHeight = controller.panelHeight;
        resizeWidth = first.edgeX != 0 && first.edgeX == -second.edgeX;
        resizeHeight = first.edgeY != 0 && first.edgeY == -second.edgeY;
    }

    public Vector3 LimitGrabDistance(Vector3 position, Vector3 head)
    {
        Vector3 delta = position - head;
        return delta.magnitude > maximumGrabDistance ? head + delta.normalized * maximumGrabDistance : position;
    }

    public void MoveHeldWindow()
    {
        if (!controller.held || first == null) return;
        Transform panel = controller.mainPanel.transform;
        Vector3 a = HandPosition(first);
        if (second == null || (!resizeWidth && !resizeHeight))
        {
            Quaternion rotation = HandRotation(first);
            panel.SetPositionAndRotation(a + rotation * positionOffset, rotation * rotationOffset);
            return;
        }
        Vector3 b = HandPosition(second);
        Vector3 span = Quaternion.Inverse(initialPanelRotation) * (b - a);
        float width = initialWidth;
        float height = initialHeight;
        if (resizeWidth) width += (Mathf.Abs(span.x) - Mathf.Abs(initialSpan.x)) / (0.864f * controller.panelScale);
        if (resizeHeight) height += (Mathf.Abs(span.y) - Mathf.Abs(initialSpan.y)) / (0.688f * controller.panelScale);
        controller.ResizeWindow(width, height);
        panel.SetPositionAndRotation(initialPanelPosition + (a + b) * 0.5f - initialMidpoint, initialPanelRotation);
    }

    public void ReleaseAll()
    {
        releasing = true;
        for (int i = 0; i < handles.Length; i++)
        {
            handles[i].grabbed = false;
            if (handles[i].pickup.IsHeld) handles[i].pickup.Drop();
        }
        first = null;
        second = null;
        releasing = false;
        if (controller.held) controller.EndHold();
    }

    public void RefreshHandles()
    {
        bool visible = controller.isOpen && !controller.fullscreen && (controller.adjustWindow || controller.held);
        Transform panel = controller.mainPanel.transform;
        float width = 0.864f * controller.panelScale * controller.panelWidth;
        float height = 0.688f * controller.panelScale * controller.panelHeight;
        for (int i = 0; i < handles.Length; i++)
        {
            BibleWindowHandle handle = handles[i];
            if (handle.gameObject.activeSelf != visible) handle.gameObject.SetActive(visible);
            if (!visible || handle.grabbed) continue;
            bool corner = handle.edgeX != 0 && handle.edgeY != 0;
            handle.transform.SetPositionAndRotation(panel.position + panel.right * (handle.edgeX * (width * 0.5f + edgeThickness * 0.65f)) + panel.up * (handle.edgeY * (height * 0.5f + edgeThickness * 0.65f)), panel.rotation);
            handle.transform.localScale = corner ? new Vector3(cornerSize, cornerSize, 0.045f) : new Vector3(handle.edgeX == 0 ? width : edgeThickness, handle.edgeY == 0 ? height : edgeThickness, 0.04f);
        }
    }

    private Vector3 HandPosition(BibleWindowHandle handle)
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!Utilities.IsValid(player) || !player.IsUserInVR()) return handle.transform.position;
        return player.GetTrackingData(handle.leftHand ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).position;
    }

    private Quaternion HandRotation(BibleWindowHandle handle)
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!Utilities.IsValid(player) || !player.IsUserInVR()) return handle.transform.rotation;
        return player.GetTrackingData(handle.leftHand ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).rotation;
    }

    private void LateUpdate()
    {
        if (!controller.isOpen) return;
        MoveHeldWindow();
        RefreshHandles();
    }
}
