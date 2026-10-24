using UdonSharp;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VRC.SDKBase;

// One scene-owned session, independent of personal PlayerObjects and PlayerData.
// Only scripture IDs are synchronized. Each client positions its own screen.
// Underscored custom events are local-only on the installed legacy VRChat SDK;
// remote callers cannot trigger privileged actions on the host's client.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BibleStudyHost : UdonSharpBehaviour
{
    public BibleController controller;
    public GameObject screen;
    public TMP_Text heading;
    public TMP_Text scripture;
    public TMP_Text status;
    public TMP_Text stationStatus;
    public Material[] wordMaterials;
    public RectTransform screenRect;
    public BoxCollider screenCollider;
    public Image background;
    public ScrollRect scriptureScroll;
    public Button[] grantButtons;
    public TMP_Text[] grantLabels;
    private int[] grantPlayerIds;
    [UdonSynced] public bool active;
    [UdonSynced] public int hostId = -1;
    [UdonSynced] public int selectedVerse;
    [UdonSynced] public int selectionEnd;
    [UdonSynced] public int firstVerse;
    [UdonSynced] public int endVerse;
    public bool joined;
    public bool paused;
    public TMP_Text pauseLabel;
    private bool validDisplay;
    private int displayHost = -1;
    private int displayFirst;
    private int displayEnd;
    private int displaySelected;
    private int displaySelectionEnd;
    private bool claiming;
    private bool shown;
    private bool retry;
    private float nextPublish;
    private Vector3 previousHead;

    private void Start() { screen.SetActive(false); _Refresh(); }

    public bool IsHost()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        return active && Utilities.IsValid(player) && player.playerId == hostId && Networking.IsOwner(gameObject);
    }

    public bool HasAssignedHost() { return hostId >= 0 && Utilities.IsValid(VRCPlayerApi.GetPlayerById(hostId)); }
    public bool HasHost() { return active && HasAssignedHost(); }

    public bool CanManage(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player)) return false;
        VRCPlayerApi instanceOwner = Networking.InstanceOwner;
        if (Utilities.IsValid(instanceOwner) && instanceOwner.playerId == player.playerId) return true;
        if (HasAssignedHost()) return player.playerId == hostId;
        return !Utilities.IsValid(instanceOwner) && player.isMaster;
    }

    public void _AssignInitialHost()
    {
        if (!Networking.IsOwner(gameObject) || HasAssignedHost()) return;
        VRCPlayerApi initial = Networking.InstanceOwner;
        if (!Utilities.IsValid(initial)) initial = Networking.Master;
        active = false;
        hostId = Utilities.IsValid(initial) ? initial.playerId : -1;
        RequestSerialization();
        _Refresh();
    }

    public void _StartHosting()
    {
        if (!controller.restored) return;
        if (!CanManage(Networking.LocalPlayer)) { controller.statusText.text = "The instance owner or current host must give you host privileges first."; return; }
        claiming = true;
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        _CompleteClaim();
        if (claiming) SendCustomEventDelayedFrames(nameof(_CompleteClaim), 2);
    }

    public void _CompleteClaim()
    {
        if (!claiming || !Networking.IsOwner(gameObject)) return;
        claiming = false;
        if (!CanManage(Networking.LocalPlayer)) return;
        hostId = Networking.LocalPlayer.playerId;
        active = true;
        endVerse = -1;
        if (!controller.isOpen) controller.ToggleBible();
        _PublishSelection();
        controller.statusText.text = "You are hosting. Your selected scripture updates the study screen; personal notes stay private.";
    }

    public void _StopHosting()
    {
        if (!IsHost()) { controller.statusText.text = "Only the current host can end the study broadcast."; return; }
        active = false;
        RequestSerialization();
        _Refresh();
    }

    public void _JoinScreen()
    {
        if (!controller.restored) return;
        if (!HasHost()) { controller.statusText.text = "No host is broadcasting yet. Ask the assigned host to start, or request host privileges."; return; }
        if (IsHost()) { controller.statusText.text = "Your Bible is driving the hosted screen. Choose verses here for everyone following you."; return; }
        if (controller.isOpen) controller.ToggleBible();
        if (controller.isOpen) return; // Preserve any unsaved note that cannot yet be committed.
        joined = true;
        paused = false;
        shown = false;
        _Refresh();
    }

    public void _HideScreen() { joined = false; screen.SetActive(false); shown = false; _Refresh(); }

    public void _TogglePause()
    {
        if (!joined || !HasHost()) return;
        paused = !paused;
        _Refresh();
    }

    public void _CatchUp() { paused = false; _Refresh(); }

    public void _OpenInBible()
    {
        if (!validDisplay || !HasHost()) return;
        controller.OpenStudyPassage(displaySelected, displaySelectionEnd);
    }

    public void _PublishSelection()
    {
        if (!IsHost() || !controller.restored) return;
        if (selectedVerse == controller.selectedVerse && selectionEnd == controller.selectionEnd && endVerse > firstVerse) return;
        selectedVerse = controller.selectedVerse;
        selectionEnd = controller.selectionEnd;
        // Always include the host's selection, even when their reading scroll is elsewhere.
        int b = controller.bibleDataLoader.BookForVerse(selectedVerse);
        int c = controller.bibleDataLoader.ChapterForVerse(b, selectedVerse);
        int chapterEnd = controller.bibleDataLoader.VerseStart(b, c) + controller.bibleDataLoader.VerseCount(b, c);
        firstVerse = Mathf.Min(selectedVerse, selectionEnd);
        endVerse = Mathf.Min(chapterEnd, Mathf.Max(selectedVerse, selectionEnd) + 1);
        RequestSerialization();
        _Refresh();
    }

    public void _GrantFromList(int index)
    {
        if (!CanManage(Networking.LocalPlayer)) { controller.statusText.text = "Only the instance owner or current host can give host privileges."; return; }
        if (grantPlayerIds == null || index < 0 || index >= grantPlayerIds.Length) return;
        VRCPlayerApi target = VRCPlayerApi.GetPlayerById(grantPlayerIds[index]);
        if (!Utilities.IsValid(target)) { _Refresh(); return; }
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        if (!Networking.IsOwner(gameObject)) { controller.statusText.text = "Host control is transferring. Please try again shortly."; return; }
        active = false;
        hostId = target.playerId;
        RequestSerialization();
        controller.statusText.text = "Host privileges given to " + target.displayName + ". They can choose Start hosting when ready.";
        _Refresh();
    }

    public void _ReclaimHost()
    {
        VRCPlayerApi owner = Networking.InstanceOwner;
        if (!Utilities.IsValid(owner)) owner = Networking.Master;
        if (!Utilities.IsValid(owner) || owner.playerId != Networking.LocalPlayer.playerId) { controller.statusText.text = "The instance owner can reclaim host privileges."; return; }
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
        if (!Networking.IsOwner(gameObject)) return;
        active = false;
        hostId = owner.playerId;
        RequestSerialization();
        _Refresh();
    }

    public override bool OnOwnershipRequest(VRCPlayerApi requestingPlayer, VRCPlayerApi newOwner)
    {
        return CanManage(requestingPlayer) && Utilities.IsValid(newOwner) && newOwner.playerId == requestingPlayer.playerId;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player)
    {
        if (Utilities.IsValid(player) && player.isLocal)
        {
            if (claiming) _CompleteClaim();
            else _AssignInitialHost();
        }
        _Refresh();
    }

    public override void OnPlayerJoined(VRCPlayerApi player) { _AssignInitialHost(); _Refresh(); }
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (Utilities.IsValid(player) && player.playerId == hostId && Networking.IsOwner(gameObject))
        {
            active = false;
            hostId = -1;
            _AssignInitialHost();
        }
        _Refresh();
    }

    private void RefreshGrants()
    {
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        grantPlayerIds = new int[grantButtons.Length];
        bool allowed = CanManage(Networking.LocalPlayer);
        for (int i = 0; i < grantButtons.Length; i++)
        {
            bool valid = i < players.Length && Utilities.IsValid(players[i]);
            grantButtons[i].gameObject.SetActive(valid);
            grantPlayerIds[i] = valid ? players[i].playerId : -1;
            if (!valid) continue;
            grantButtons[i].interactable = allowed;
            grantLabels[i].text = "Give host: " + players[i].displayName + (players[i].playerId == hostId ? " (current host)" : "");
        }
    }

    public override void OnDeserialization() { _Refresh(); }
    public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
    {
        retry = !result.success;
        if (retry) SendCustomEventDelayedSeconds(nameof(_RetryPublish), 1f);
    }
    public void _RetryPublish() { if (retry && Networking.IsOwner(gameObject)) { retry = false; RequestSerialization(); } }

    public void _Refresh()
    {
        VRCPlayerApi host = VRCPlayerApi.GetPlayerById(hostId);
        bool valid = active && Utilities.IsValid(host);
        if (!valid || displayHost != hostId) paused = false;
        string message = valid ? host.displayName + " is hosting: " + controller.bibleDataLoader.Reference(Mathf.Clamp(selectedVerse, 0, 31101)) : HasAssignedHost() ? "Host privileges: " + host.displayName + ". Broadcast stopped." : "Waiting for the instance owner to receive host privileges.";
        RefreshGrants();
        status.text = message + (joined ? !valid ? "\nYour screen: waiting for a host" : paused ? "\nYour screen: paused. Catch up resumes live following." : "\nYour screen: following host" : "\nYour screen: hidden");
        stationStatus.text = message;
        if (controller.presentation != null) controller.presentation.MarkDirty();
        if (!valid || firstVerse < 0 || endVerse <= firstVerse || endVerse > 31102 || endVerse - firstVerse > 176 || selectedVerse < firstVerse || selectedVerse >= endVerse || selectionEnd < firstVerse || selectionEnd >= endVerse)
        {
            validDisplay = false; paused = false; displayHost = -1;
            screen.SetActive(false); shown = false; return;
        }
        if (!paused || displayHost != hostId)
        {
            if (displayHost != hostId) paused = false;
            displayHost = hostId; displayFirst = firstVerse; displayEnd = endVerse;
            displaySelected = selectedVerse; displaySelectionEnd = selectionEnd;
        }
        validDisplay = true;
        if (pauseLabel != null) pauseLabel.text = paused ? "Resume following" : "Pause following";
        BibleDataLoader data = controller.bibleDataLoader;
        data.LoadBibleData();
        if (data.verses == null) return;
        int b = data.BookForVerse(displayFirst);
        int c = data.ChapterForVerse(b, displayFirst);
        int start = data.VerseStart(b, c);
        int low = Mathf.Min(displaySelected, displaySelectionEnd), high = Mathf.Max(displaySelected, displaySelectionEnd);
        heading.text = host.displayName + " — " + data.Reference(low) + (high > low ? "–" + (high - start + 1) : "") + " (KJV)" + (paused ? "  [PAUSED]" : "  [LIVE]");
        string content = "";
        for (int i = displayFirst; i < displayEnd; i++)
        {
            string line = "<b>" + (i - start + 1) + "</b> " + data.verses[i];
            if (i >= low && i <= high) line = "<mark=#82BEE62B>" + line + "</mark>";
            content += line + "\n\n";
        }
        if (scripture.text != content)
        {
            scripture.text = content;
            SendCustomEventDelayedFrames(nameof(_ResetScriptureScroll), 3);
        }
        _ApplyAppearance();
    }

    public void _ResetScriptureScroll() { scriptureScroll.StopMovement(); scriptureScroll.verticalNormalizedPosition = 1f; }

    public void _ApplyAppearance()
    {
        screenRect.sizeDelta = new Vector2(1080f * controller.panelWidth, 860f * controller.panelHeight);
        screenCollider.size = new Vector3(screenRect.sizeDelta.x, screenRect.sizeDelta.y, 1f);
        screen.transform.localScale = Vector3.one * (.0008f * controller.panelScale);
        scripture.fontSize = controller.ReadingFontSize();
        scripture.lineSpacing = controller.wideSpacing ? 16f : 4f;
        Color paper = controller.theme == 1 ? new Color(1f, .98f, .9f) : controller.theme == 2 ? Color.black : new Color(.055f, .075f, .1f);
        paper.a = controller.backgroundOpacity;
        background.color = paper;
        Color words = controller.theme == 1 ? new Color(.08f, .06f, .02f) : Color.white;
        heading.color = words;
        words.a = controller.textOpacity;
        scripture.color = words;
        bool outline = controller.outlineMode == 1 || (controller.outlineMode == 0 && controller.backgroundOpacity < .3f);
        scripture.fontSharedMaterial = wordMaterials[outline ? (controller.theme == 1 ? 2 : 1) : 0];
        heading.fontSharedMaterial = scripture.fontSharedMaterial;
    }

    public void _FollowScreen()
    {
        bool visible = controller.restored && joined && validDisplay && HasHost() && !controller.isOpen && !IsHost();
        screen.SetActive(visible);
        if (!visible) { shown = false; return; }
        if (!shown) _ApplyAppearance();
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!Utilities.IsValid(player)) return;
        VRCPlayerApi.TrackingData head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 position = head.position + head.rotation * new Vector3(0f, -.12f, .95f);
        float amount = !shown || Vector3.Distance(head.position, previousHead) > 3f ? 1f : controller.follower.FollowAmount(Time.deltaTime);
        screen.transform.SetPositionAndRotation(Vector3.Lerp(screen.transform.position, position, amount), Quaternion.Slerp(screen.transform.rotation, head.rotation, amount));
        previousHead = head.position;
        shown = true;
    }

    private void LateUpdate() { _FollowScreen(); }
    private void Update()
    {
        if (Time.time < nextPublish) return;
        nextPublish = Time.time + 1f;
        if (claiming) _CompleteClaim();
        if (!HasAssignedHost()) _AssignInitialHost();
        if (IsHost() && (selectedVerse != controller.selectedVerse || selectionEnd != controller.selectionEnd)) _PublishSelection();
    }
}
