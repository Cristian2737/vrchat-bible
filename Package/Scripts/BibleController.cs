using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.SDK3.Persistence;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BibleController : UdonSharpBehaviour
{
    public BibleDataLoader bibleDataLoader;
    public GameObject mainPanel;
    public GameObject[] panels;
    public TMP_Text locationText;
    public TMP_Text verseDisplayText;
    public TMP_Text statusText;
    public TMP_Text readingHint;
    public TMP_Text rangeLabel;
    public GameObject verseActions;
    public GameObject readingGuide;
    public GameObject readingTools;
    public TMP_Text toolsLabel;
    public TMP_Text[] fullscreenLabels = new TMP_Text[0];
    public TMP_Text bookmarkLabel;
    public TMP_Text continueLabel;
    public TMP_Text libraryHint;
    public TMP_InputField bookmarkName;
    public Button bookmarkSave;
    public Button bookmarkRemove;
    public TMP_Text bookmarkSaveCaption;
    public GameObject bookmarkEditor;
    public RectTransform libraryList;
    public RectTransform libraryHintLayout;
    public Image[] navigationImages = new Image[0];
    public TMP_Text[] navigationLabels = new TMP_Text[0];
    public string[] navigationCaptions = new string[0];
    public int[] navigationGroups = new int[0];
    public int[] navigationValues = new int[0];
    public Button historyBack;
    public Button historyForward;
    public GameObject undoButton;
    public GameObject returnButton;
    public bool actionsVisible;
    public bool toolsVisible;
    public bool readingRuler;
    public bool adjustWindow;
    public int comfortPreset;
    public BibleChapterReader chapterReader;
    public TMP_InputField copyInput;
    public string copiedVerses = "";
    public int selectionEnd;
    public bool rangeSelection;
    public TMP_Text selectionText;
    public TMP_Text searchStatus;
    public TMP_Text searchScopeLabel;
    public Button searchStop;
    public Button searchClear;
    public ScrollRect searchResultsScroll;
    public TMP_Text settingsText;
    public TMP_Text noteReference;
    public TMP_Text noteStatus;
    public Button noteSave;
    public Button noteRevert;
    public Button noteCopy;
    public Button notePaste;
    public Button noteDelete;
    public TMP_Text copyHeading;
    public TMP_Text copyReturnLabel;
    public GameObject copyPasteControl;
    public ScrollRect verseScrollRect;
    public TMP_InputField referenceInput;
    public TMP_InputField searchInput;
    public TMP_InputField noteInput;
    public Toggle searchThisBook;
    public TMP_Text[] searchLabels;
    public GameObject[] searchRows;
    public TMP_Text[] savedLabels;
    public GameObject[] savedRows;
    public GameObject[] chapterButtons;
    public GameObject[] verseButtons;
    public TMP_Text[] bookLabels = new TMP_Text[0];
    public TMP_Text[] chapterLabels = new TMP_Text[0];
    public TMP_Text[] verseLabels = new TMP_Text[0];
    public Image[] chapterImages = new Image[0];
    public Image[] verseImages = new Image[0];
    public Button previousChapter;
    public Button nextChapter;
    public GameObject[] testamentPanels;
    public ScrollRect[] bookScrolls = new ScrollRect[0];
    public ScrollRect chapterPickerScroll;
    public ScrollRect versePickerScroll;
    public RectTransform[] bookTargets = new RectTransform[0];
    public RectTransform[] chapterTargets = new RectTransform[0];
    public RectTransform[] verseTargets = new RectTransform[0];
    public BibleWindowPresentation presentation;
    public BibleWindowHandle windowHandle;
    public BibleWindowManipulator manipulator;
    public BiblePanelFollower follower;
    public BibleStudyHost studyHost;
    public Slider followSlider;
    public GameObject[] settingsSections;
    public TMP_Text[] settingValues;
    public string[] settingActions;
    public int layoutVersion;
    public Image[] backgrounds;
    public TMP_Text[] interfaceTexts;
    public int searchVersesPerFrame = 100;
    public int maxHighlights = 512;
    public int maxNotes = 128;
    public int maxNoteCharacters = 12000;
    public KeyCode toggleKey = KeyCode.B;
    public float doubleGripWindow = 0.35f;
    public AudioSource audioSource;
    [HideInInspector] public BibleSharedView sharedView;
    [HideInInspector] public bool isOpen;
    [HideInInspector] public bool restored;
    [HideInInspector] public int selectedVerse;
    [HideInInspector] public int book;
    [HideInInspector] public int chapter;
    [HideInInspector] public bool share;
    [HideInInspector] public bool follow = true;
    [HideInInspector] public int followSource;
    [HideInInspector] public float followStrength = 1f;
    [HideInInspector] public float panelScale = 1f;
    [HideInInspector] public float panelDistance = 0.85f;
    [HideInInspector] public int fontSize = 26;
    [HideInInspector] public int theme;
    [HideInInspector] public bool wideSpacing;
    [HideInInspector] public bool sounds;
    [HideInInspector] public bool vrShortcut = true;
    [HideInInspector] public bool thumbstickShortcut = true;
    [HideInInspector] public float panelWidth = 1f;
    [HideInInspector] public float panelHeight = 1f;
    [HideInInspector] public float backgroundOpacity = 1f;
    [HideInInspector] public float textOpacity = 1f;
    [HideInInspector] public bool fullscreen;
    [HideInInspector] public bool showBack = true;
    [HideInInspector] public bool originalColors = true;
    [HideInInspector] public int outlineMode;
    [HideInInspector] public bool held;
    [HideInInspector] public bool hasPlacement;
    [HideInInspector] public bool hasViewPlacement;
    [HideInInspector] public Vector3 viewOffset;
    [HideInInspector] public Quaternion viewRotation = Quaternion.identity;
    private int settingsSection;
    private const string Prefix = "bible.kjv.v1.";
    private DataDictionary highlights = new DataDictionary();
    private DataDictionary notes = new DataDictionary();
    private int currentPanel;
    private int navigationBook;
    private int navigationChapter;
    private int testament;
    private float[] bookPositions = new float[] { 1f, 1f };
    private bool[] bookVisited = new bool[2];
    private float chapterPosition = 1f;
    private ScrollRect pendingBrowserScroll;
    private RectTransform pendingBrowserTarget;
    private float pendingBrowserPosition;
    private int browserReadyFrame;
    private int[] matches = new int[200];
    private int matchCount;
    private int searchCursor;
    private int searchStart;
    private int searchEnd;
    private string searchScopeName = "the KJV";
    private string query = "";
    private bool searching;
    private DataList savedKeys;
    private bool savedNotes;
    private int[] libraryIds = new int[512];
    private int libraryCount;
    private int bookmarkEditingVerse = -1;
    private bool noteDirty;
    private bool copyingNote;
    private int editingVerse;
    private float lastNoteEdit;
    private float lastLeftGrip = -10f;
    private float lastRightGrip = -10f;
    private float lastShortcut = -10f;
    private float nextScrollSave;
    private float savedScroll = 1f;
    private bool readOnlySave;
    private DataDictionary bookmarks = new DataDictionary();
    private bool readOnlyBookmarks;
    private int savedMode;
    private int[] history = new int[32];
    private int[] historyAnchors = new int[32];
    private int[] historyOffsets = new int[32];
    private int historyCount;
    private int historyIndex;
    private int undoKind;
    private string undoKey = "";
    private string undoValue = "";
    private DataDictionary undoHighlights = new DataDictionary();
    private DataDictionary undoHighlightMissing = new DataDictionary();
    private bool hasReturn;
    private int returnVerse;
    private int returnEnd;
    private int returnAnchor;
    private float returnOffset;

    private void Start()
    {
        mainPanel.SetActive(false);
        statusText.text = "Waiting for your world save...";
    }

    public override void OnPlayerRestored(VRCPlayerApi player)
    {
        if (!Utilities.IsValid(player) || !player.isLocal || restored) return;
        bibleDataLoader.LoadBibleData();
        if (bibleDataLoader.verses == null || bibleDataLoader.verses.Length != 31102)
        {
            statusText.text = "Bible data is incomplete. Please tell the world author.";
            return;
        }
        selectedVerse = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "verse"), 0, 31101);
        selectionEnd = selectedVerse;
        if (PlayerData.HasKey(player, Prefix + "font")) fontSize = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "font"), 20, 42);
        if (PlayerData.HasKey(player, Prefix + "scale")) panelScale = Mathf.Clamp(PlayerData.GetFloat(player, Prefix + "scale"), 0.7f, 1.6f);
        if (PlayerData.HasKey(player, Prefix + "distance")) panelDistance = Mathf.Clamp(PlayerData.GetFloat(player, Prefix + "distance"), 0.6f, 1.5f);
        theme = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "theme"), 0, 2);
        wideSpacing = PlayerData.GetBool(player, Prefix + "spacing");
        sounds = PlayerData.GetBool(player, Prefix + "sounds");
        share = PlayerData.GetBool(player, Prefix + "share");
        if (PlayerData.HasKey(player, Prefix + "follow")) follow = PlayerData.GetBool(player, Prefix + "follow");
        followSource = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "followSource"), 0, 2);
        if (PlayerData.HasKey(player, Prefix + "followStrength")) followStrength = Mathf.Clamp01(PlayerData.GetFloat(player, Prefix + "followStrength"));
        if (PlayerData.HasKey(player, Prefix + "shortcut")) vrShortcut = PlayerData.GetBool(player, Prefix + "shortcut");
        if (PlayerData.HasKey(player, Prefix + "thumbstick")) thumbstickShortcut = PlayerData.GetBool(player, Prefix + "thumbstick");
        if (PlayerData.HasKey(player, Prefix + "width")) panelWidth = Mathf.Clamp(PlayerData.GetFloat(player, Prefix + "width"), 0.8f, 1.8f);
        if (PlayerData.HasKey(player, Prefix + "height")) panelHeight = Mathf.Clamp(PlayerData.GetFloat(player, Prefix + "height"), 0.8f, 1.8f);
        if (PlayerData.HasKey(player, Prefix + "backgroundOpacity")) backgroundOpacity = Mathf.Clamp01(PlayerData.GetFloat(player, Prefix + "backgroundOpacity"));
        if (PlayerData.HasKey(player, Prefix + "textOpacity")) textOpacity = Mathf.Clamp01(PlayerData.GetFloat(player, Prefix + "textOpacity"));
        fullscreen = PlayerData.GetBool(player, Prefix + "fullscreen");
        if (PlayerData.HasKey(player, Prefix + "back")) showBack = PlayerData.GetBool(player, Prefix + "back");
        if (PlayerData.HasKey(player, Prefix + "originalColors")) originalColors = PlayerData.GetBool(player, Prefix + "originalColors");
        outlineMode = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "outlineMode"), 0, 2);
        if (PlayerData.HasKey(player, Prefix + "pinX"))
        {
            mainPanel.transform.position = new Vector3(PlayerData.GetFloat(player, Prefix + "pinX"), PlayerData.GetFloat(player, Prefix + "pinY"), PlayerData.GetFloat(player, Prefix + "pinZ"));
            mainPanel.transform.rotation = Quaternion.Euler(PlayerData.GetFloat(player, Prefix + "pinPitch"), PlayerData.GetFloat(player, Prefix + "pinYaw"), PlayerData.GetFloat(player, Prefix + "pinRoll"));
            hasPlacement = true;
        }
        if (PlayerData.HasKey(player, Prefix + "viewX"))
        {
            viewOffset = new Vector3(PlayerData.GetFloat(player, Prefix + "viewX"), PlayerData.GetFloat(player, Prefix + "viewY"), PlayerData.GetFloat(player, Prefix + "viewZ"));
            viewRotation = Quaternion.Euler(PlayerData.GetFloat(player, Prefix + "viewPitch"), PlayerData.GetFloat(player, Prefix + "viewYaw"), PlayerData.GetFloat(player, Prefix + "viewRoll"));
            hasViewPlacement = true;
        }
        if (follow && !hasViewPlacement) hasPlacement = false;
        if (PlayerData.HasKey(player, Prefix + "query")) searchInput.SetTextWithoutNotify(PlayerData.GetString(player, Prefix + "query"));
        searchThisBook.SetIsOnWithoutNotify(PlayerData.GetBool(player, Prefix + "scope"));
        if (PlayerData.HasKey(player, Prefix + "annotations"))
        {
            DataToken root;
            if (VRCJson.TryDeserializeFromJson(PlayerData.GetString(player, Prefix + "annotations"), out root) && root.TokenType == TokenType.DataDictionary)
            {
                DataToken h;
                DataToken n;
                if (root.DataDictionary.TryGetValue("highlights", TokenType.DataDictionary, out h) && root.DataDictionary.TryGetValue("notes", TokenType.DataDictionary, out n))
                {
                    highlights = h.DataDictionary;
                    notes = n.DataDictionary;
                }
                else readOnlySave = true;
            }
            else readOnlySave = true;
        }
        restored = true;
        readingRuler = PlayerData.GetBool(player, Prefix + "readingRuler");
        comfortPreset = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "comfortPreset"), 0, 3);
        RestoreReadingTools(player);
        noteInput.readOnly = readOnlySave;
        ResolveLocation();
        int restoredEnd;
        int chapterStart = bibleDataLoader.VerseStart(book, chapter);
        if (PlayerData.TryGetInt(player, Prefix + "selectionEnd", out restoredEnd) && restoredEnd >= chapterStart && restoredEnd < chapterStart + bibleDataLoader.VerseCount(book, chapter))
        {
            selectionEnd = restoredEnd;
            rangeSelection = selectionEnd != selectedVerse;
            actionsVisible = rangeSelection;
        }
        ApplySettings();
        UpdateSettingsText();
        RenderReading();
        ShowPanel(0);
        RefreshSearchControls();
        // Older saves used a five-verse excerpt. Keep their exact selected verse on migration.
        chapterReader.anchorVerse = selectedVerse;
        if (PlayerData.HasKey(player, Prefix + "chapterAnchor"))
        {
            int anchor = PlayerData.GetInt(player, Prefix + "chapterAnchor");
            int start = bibleDataLoader.VerseStart(book, chapter);
            if (anchor >= start && anchor < start + bibleDataLoader.VerseCount(book, chapter)) chapterReader.anchorVerse = anchor;
            chapterReader.anchorOffset = Mathf.Max(0f, PlayerData.GetFloat(player, Prefix + "chapterOffset"));
        }
        chapterReader.Reflow();
        statusText.text = readOnlySave ? "Saved annotations could not be read. Editing disabled to protect them." : "Your Bible is ready. Changes save automatically.";
        Publish();
    }

    private void Update()
    {
        bool typing = referenceInput.isFocused || searchInput.isFocused || noteInput.isFocused || copyInput.isFocused || (bookmarkName != null && bookmarkName.isFocused);
        if (!typing && Input.GetKeyDown(toggleKey)) ToggleBible();
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!typing && thumbstickShortcut && Utilities.IsValid(player) && player.IsUserInVR() &&
            (Input.GetButtonDown("Oculus_CrossPlatform_PrimaryThumbstick") || Input.GetButtonDown("Oculus_CrossPlatform_SecondaryThumbstick"))) ShortcutToggle();
        if (!restored) return;
        if (noteDirty && Time.time - lastNoteEdit > 1.0f) CommitNote();
        if (!isOpen) return;
        if (searching) SearchStep();
        if (currentPanel == 0 && Time.time >= nextScrollSave)
        {
            nextScrollSave = Time.time + 2f;
            float scroll = verseScrollRect.verticalNormalizedPosition;
            if (Mathf.Abs(scroll - savedScroll) > 0.001f)
            {
                savedScroll = scroll;
                SaveProgress();
            }
        }
    }

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!value || held || !vrShortcut || !Utilities.IsValid(player) || !player.IsUserInVR()) return;
        bool left = args.handType == HandType.LEFT;
        if (Utilities.IsValid(player.GetPickupInHand(left ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right))) return;
        if (referenceInput.isFocused || searchInput.isFocused || noteInput.isFocused || copyInput.isFocused || (bookmarkName != null && bookmarkName.isFocused)) return;
        float previous = left ? lastLeftGrip : lastRightGrip;
        if (Time.time - previous <= doubleGripWindow)
        {
            if (left) lastLeftGrip = -10f; else lastRightGrip = -10f;
            ShortcutToggle();
        }
        else if (left) lastLeftGrip = Time.time; else lastRightGrip = Time.time;
    }

    private void ShortcutToggle()
    {
        if (Time.time - lastShortcut < 0.4f) return;
        lastShortcut = Time.time;
        ToggleBible();
    }

    public void ToggleBible()
    {
        if (isOpen)
        {
            if (!CommitNote()) return;
            if (windowHandle != null) windowHandle.Release();
            SaveProgress();
            SavePlacement();
            RememberBrowserPosition();
            isOpen = false;
            mainPanel.SetActive(false);
        }
        else
        {
            isOpen = true;
            mainPanel.SetActive(true);
            if (!hasPlacement) Recenter();
            else if (follow && follower != null) follower.SnapToAnchor();
            SendCustomEventDelayedFrames(nameof(RestoreScroll), 2);
            if (pendingBrowserScroll != null) QueueBrowserScroll(pendingBrowserScroll, pendingBrowserTarget, pendingBrowserPosition);
        }
        if (manipulator != null) manipulator.RefreshHandles();
        if (presentation != null) presentation.MarkDirty();
        Publish();
    }

    public void RestoreScroll() { chapterReader.Reflow(); }
    public void SaveNoteDraft() { CommitNote(); }
    public void Recenter()
    {
        VRCPlayerApi player = Networking.LocalPlayer;
        if (!Utilities.IsValid(player)) return;
        VRCPlayerApi.TrackingData head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 direction = Quaternion.Euler(0f, head.rotation.eulerAngles.y, 0f) * Vector3.forward;
        mainPanel.transform.position = head.position + direction * panelDistance + Vector3.down * 0.12f;
        mainPanel.transform.rotation = Quaternion.LookRotation(direction);
        hasPlacement = true;
        if (follower != null) follower.Capture();
        SavePlacement();
        Publish();
    }

    public void HandleAction(string action, int index)
    {
        if (action == "Toggle") { ToggleBible(); return; }
        if (!restored) { statusText.text = "Waiting for your saved Bible. Please try again shortly."; return; }
        if (action == "NoteChanged")
        {
            noteDirty = true; lastNoteEdit = Time.time; RefreshNoteState();
            if (currentPanel == 5) statusText.text = "Note edited. Check the draft status above.";
            return;
        }
        if (action == "SearchChanged" || action == "SearchScope") { RefreshSearchControls(); return; }
        if (action == "BookmarkChanged") { RefreshBookmarkState(); return; }
        bool keepDraft = action == "CopyNote" || action == "RevertNote" || (action == "CopyReturn" && copyingNote);
        if (noteDirty && action != "DeleteNote" && !keepDraft && !CommitNote()) return;
        if (sounds && audioSource != null && audioSource.clip != null) audioSource.Play();
        if (action == "Read") { ShowPanel(0); }
        else if (action == "VerseActions") { chapterReader.Sample(); actionsVisible = !actionsVisible; if (actionsVisible) toolsVisible = false; RefreshReadingTools(); chapterReader.Reflow(); }
        else if (action == "ReadingTools") { chapterReader.Sample(); toolsVisible = !toolsVisible; if (toolsVisible) actionsVisible = false; RefreshReadingTools(); chapterReader.Reflow(); }
        else if (action == "History") TravelHistory(index);
        else if (action == "Bookmark") ToggleBookmark();
        else if (action == "RenameBookmark") RenameBookmark();
        else if (action == "RemoveBookmark")
        {
            DataToken name;
            if (bookmarks.TryGetValue(selectedVerse.ToString(), TokenType.String, out name) && name.String.Length > 0) ToggleBookmark();
        }
        else if (action == "Undo") UndoAnnotation();
        else if (action == "ReturnReading") ReturnToReading();
        else if (action == "ComfortPreset") ApplyComfortPreset(Mathf.Clamp(index, 0, 3));
        else if (action == "ReadingRuler") { readingRuler = !readingRuler; SettingsChanged(); }
        else if (action == "AdjustWindow") { adjustWindow = !adjustWindow; if (manipulator != null) manipulator.RefreshHandles(); statusText.text = adjustWindow ? "Adjustment handles visible. Grab an edge to move; opposite edges resize." : "Adjustment handles hidden. Use Tools > Adjust window to show them."; }
        else if (action == "Study") { ShowPanel(9); studyHost._Refresh(); }
        else if (action == "StartHost") studyHost._StartHosting();
        else if (action == "GrantHost") studyHost._GrantFromList(index);
        else if (action == "ReclaimHost") studyHost._ReclaimHost();
        else if (action == "StopHost") studyHost._StopHosting();
        else if (action == "JoinHost") studyHost._JoinScreen();
        else if (action == "HideHost") studyHost._HideScreen();
        else if (action == "PauseHost") studyHost._TogglePause();
        else if (action == "CatchUpHost") studyHost._CatchUp();
        else if (action == "OpenHostPassage") studyHost._OpenInBible();
        else if (action == "Books")
        {
            RememberBrowserPosition();
            testament = book < 39 ? 0 : 1;
            ShowPanel(1); ShowTestament();
            FocusBrowserBook(book);
        }
        else if (action == "BackBooks")
        {
            testament = navigationBook < 39 ? 0 : 1;
            ShowPanel(1); ShowTestament();
            RestoreBookPosition();
        }
        else if (action == "Testament")
        {
            RememberBrowserPosition();
            testament = Mathf.Clamp(index, 0, 1);
            ShowPanel(1); ShowTestament();
            RestoreBookPosition();
        }
        else if (action == "Book")
        {
            if (index < 0 || index >= 66) return;
            navigationBook = index;
            for (int i = 0; i < chapterButtons.Length; i++) chapterButtons[i].SetActive(i < bibleDataLoader.ChapterCount(index));
            locationText.text = bibleDataLoader.bookNames[index] + " - choose a chapter";
            ShowPanel(2);
            QueueBrowserScroll(chapterPickerScroll, index == book && chapter < chapterTargets.Length ? chapterTargets[chapter] : null, 1f);
        }
        else if (action == "Chapter")
        {
            if (index < 0 || index >= bibleDataLoader.ChapterCount(navigationBook)) return;
            navigationChapter = index;
            int count = bibleDataLoader.VerseCount(navigationBook, navigationChapter);
            for (int i = 0; i < verseButtons.Length; i++) verseButtons[i].SetActive(i < count);
            locationText.text = bibleDataLoader.bookNames[navigationBook] + " " + (index + 1) + " - choose a verse";
            ShowPanel(7);
            int verseIndex = selectedVerse - bibleDataLoader.VerseStart(navigationBook, navigationChapter);
            QueueBrowserScroll(versePickerScroll, navigationBook == book && navigationChapter == chapter && verseIndex >= 0 && verseIndex < verseTargets.Length ? verseTargets[verseIndex] : null, 1f);
        }
        else if (action == "Verse")
        {
            if (index >= 0 && index < bibleDataLoader.VerseCount(navigationBook, navigationChapter)) GoTo(bibleDataLoader.VerseStart(navigationBook, navigationChapter) + index);
        }
        else if (action == "BackChapters")
        {
            locationText.text = bibleDataLoader.bookNames[navigationBook] + " - choose a chapter";
            ShowPanel(2);
            QueueBrowserScroll(chapterPickerScroll, null, chapterPosition);
        }
        else if (action == "ChapterStep")
        {
            int id = bibleDataLoader.ChapterId(book, chapter) + index;
            if (id >= 0 && id < 1189) GoTo(bibleDataLoader.chapterVerseStarts[id]);
        }
        else if (action == "ContinueChapter") { int next = bibleDataLoader.ChapterId(book, chapter) + 1; GoTo(next < 1189 ? bibleDataLoader.chapterVerseStarts[next] : 0); }
        else if (action == "FindSelection")
        {
            chapterReader.anchorVerse = Mathf.Min(selectedVerse, selectionEnd);
            chapterReader.anchorOffset = 0f;
            chapterReader.Reflow(); SaveProgress();
            statusText.text = "Returned to " + bibleDataLoader.Reference(chapterReader.anchorVerse) + ".";
        }
        else if (action == "SelectReadingVerse") SelectReadingVerse(index);
        else if (action == "Range") { chapterReader.Sample(); actionsVisible = true; toolsVisible = false; rangeSelection = !rangeSelection; if (!rangeSelection) selectionEnd = selectedVerse; RenderReading(); SaveProgress(); Publish(); }
        else if (action == "Copy") CopySelection();
        else if (action == "PasteNote") PasteIntoNote();
        else if (action == "VerseStep") GoTo(Mathf.Clamp(selectedVerse + index, 0, 31101));
        else if (action == "Reference") JumpToReference();
        else if (action == "Highlight") { if (currentPanel == 0) chapterReader.Sample(); ToggleHighlight(); }
        else if (action == "Note") OpenSelectedNote();
        else if (action == "CopyNote")
        {
            if (noteInput.text.Length == 0) return;
            copyingNote = true; copyInput.SetTextWithoutNotify(noteInput.text);
            RefreshCopyControls(); ShowPanel(8);
            statusText.text = "Draft ready for keyboard Copy. Back to note keeps your draft.";
        }
        else if (action == "CopyReturn") { ShowPanel(copyingNote ? 5 : 0); }
        else if (action == "RevertNote") RevertNoteDraft();
        else if (action == "SaveNote") { CommitNote(); }
        else if (action == "DeleteNote")
        {
            if (readOnlySave) return;
            CaptureNoteUndo(editingVerse.ToString());
            if (noteDirty) undoValue = noteInput.text;
            notes.Remove(editingVerse.ToString());
            noteInput.SetTextWithoutNotify("");
            noteDirty = false;
            SaveAnnotations();
            RenderReading();
        }
        else if (action == "Search") { ShowPanel(3); RefreshSearchControls(); }
        else if (action == "RunSearch") StartSearch();
        else if (action == "CancelSearch")
        {
            if (searching) { searching = false; searchStatus.text = "Stopped: " + matchCount + " matches in " + searchScopeName + ". Search again to finish."; RefreshSearchControls(); }
        }
        else if (action == "ClearSearch")
        {
            searching = false; matchCount = 0; query = "";
            searchInput.SetTextWithoutNotify("");
            PlayerData.SetString(Prefix + "query", "");
            searchStatus.text = "Search all 31,102 verses, or just the current book.";
            ResetSearchScroll(); RenderSearch();
        }
        else if (action == "SearchResult")
        {
            int result = index;
            if (result >= 0 && result < matchCount) GoTo(matches[result]);
        }
        else if (action == "Saved") { savedMode = Mathf.Clamp(index, 0, 3); savedNotes = savedMode == 1; ShowPanel(4); RenderSaved(); }
        else if (action == "SavedResult")
        {
            int item = index;
            if (savedMode == 3) { if (item >= 0 && item < historyCount) TravelHistory(historyCount - 1 - item - historyIndex); }
            else if (item >= 0 && item < libraryCount) { GoTo(libraryIds[item]); if (savedMode == 1) OpenSelectedNote(); }
        }
        else if (action == "Settings") { ShowPanel(6); ShowSettingsSection(settingsSection); UpdateSettingsText(); }
        else if (action == "SettingsTab") { ShowSettingsSection(index); }
        else if (action == "Font") { fontSize = Mathf.Clamp(fontSize + index * 2, 20, 42); SettingsChanged(); }
        else if (action == "Theme") { theme = (theme + 1) % 3; SettingsChanged(); }
        else if (action == "Spacing") { wideSpacing = !wideSpacing; SettingsChanged(); }
        else if (action == "Scale") { panelScale = Mathf.Clamp(panelScale + index * 0.1f, 0.7f, 1.6f); SettingsChanged(); }
        else if (action == "Distance")
        {
            panelDistance = Mathf.Clamp(panelDistance + index * 0.1f, 0.6f, 1.5f);
            VRCPlayerApi player = Networking.LocalPlayer;
            if (Utilities.IsValid(player))
            {
                Vector3 head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                Vector3 direction = mainPanel.transform.position - head;
                if (direction.sqrMagnitude > 0.001f) mainPanel.transform.position = head + direction.normalized * panelDistance;
                if (follower != null) follower.Capture();
            }
            SettingsChanged();
        }
        else if (action == "Follow") { if (windowHandle != null) windowHandle.Release(); follow = !follow; if (follower != null) follower.Capture(); hasPlacement = true; SettingsChanged(); }
        else if (action == "Attachment")
        {
            if (windowHandle != null) windowHandle.Release();
            followSource = (followSource + 1) % 3;
            follow = true;
            if (follower != null) follower.Capture();
            hasPlacement = true;
            SettingsChanged();
        }
        else if (action == "Share") { share = !share; SettingsChanged(); }
        else if (action == "Sounds") { sounds = !sounds; SettingsChanged(); }
        else if (action == "Shortcut") { vrShortcut = !vrShortcut; SettingsChanged(); }
        else if (action == "Thumbstick") { thumbstickShortcut = !thumbstickShortcut; SettingsChanged(); }
        else if (action == "Width") { panelWidth = Mathf.Clamp(panelWidth + index * 0.1f, 0.8f, 1.8f); SettingsChanged(); }
        else if (action == "Height") { panelHeight = Mathf.Clamp(panelHeight + index * 0.1f, 0.8f, 1.8f); SettingsChanged(); }
        else if (action == "BackgroundOpacity") { backgroundOpacity = Mathf.Clamp01(Mathf.Round((backgroundOpacity + index * 0.1f) * 10f) * 0.1f); SettingsChanged(); }
        else if (action == "TextOpacity") { textOpacity = Mathf.Clamp01(Mathf.Round((textOpacity + index * 0.1f) * 10f) * 0.1f); SettingsChanged(); }
        else if (action == "Fullscreen") { if (windowHandle != null) windowHandle.Release(); fullscreen = !fullscreen; ShowPanel(0); SettingsChanged(); }
        else if (action == "Back") { showBack = !showBack; SettingsChanged(); }
        else if (action == "OriginalColors") { originalColors = !originalColors; SettingsChanged(); }
        else if (action == "Outline") { outlineMode = (outlineMode + 1) % 3; SettingsChanged(); }
        else if (action == "Recenter") { Recenter(); statusText.text = "Window returned in front of you. Your size and reading place are preserved."; }
        if (presentation != null) presentation.MarkDirty();
    }

    private void ShowTestament()
    {
        for (int i = 0; i < testamentPanels.Length; i++) testamentPanels[i].SetActive(i == testament);
        locationText.text = testament == 0 ? "Old Testament - 39 books" : "New Testament - 27 books";
        RefreshNavigation();
    }

    private void RememberBrowserPosition()
    {
        if (currentPanel == 1)
        {
            for (int i = 0; i < bookScrolls.Length; i++)
                if (testamentPanels[i].activeSelf)
                {
                    bookPositions[i] = Mathf.Clamp01(bookScrolls[i].verticalNormalizedPosition);
                    bookVisited[i] = true;
                }
        }
        else if (currentPanel == 2 && chapterPickerScroll != null)
            chapterPosition = Mathf.Clamp01(chapterPickerScroll.verticalNormalizedPosition);
    }

    private void FocusBrowserBook(int index)
    {
        if (testament < bookScrolls.Length)
            QueueBrowserScroll(bookScrolls[testament], index >= 0 && index < bookTargets.Length ? bookTargets[index] : null, 1f);
    }

    private void RestoreBookPosition()
    {
        if (bookVisited[testament])
        {
            if (testament < bookScrolls.Length) QueueBrowserScroll(bookScrolls[testament], null, bookPositions[testament]);
        }
        else FocusBrowserBook((book < 39 ? 0 : 1) == testament ? book : -1);
    }

    private void QueueBrowserScroll(ScrollRect scroll, RectTransform target, float position)
    {
        pendingBrowserScroll = scroll;
        pendingBrowserTarget = target;
        pendingBrowserPosition = position;
        if (scroll == null) return;
        scroll.StopMovement();
        // ContentSizeFitter and nested book groups need layout to settle after activation/resizing.
        browserReadyFrame = Time.frameCount + 4;
        SendCustomEventDelayedFrames(nameof(RestoreBrowserScroll), 4);
    }

    public void RestoreBrowserScroll()
    {
        if (pendingBrowserScroll == null) return;
        if (!isOpen) return;
        if (Time.frameCount < browserReadyFrame)
        {
            SendCustomEventDelayedFrames(nameof(RestoreBrowserScroll), 1);
            return;
        }
        float position = pendingBrowserPosition;
        if (pendingBrowserTarget != null)
        {
            RectTransform content = pendingBrowserScroll.content;
            float height = pendingBrowserScroll.viewport.rect.height;
            float extent = content.rect.height - height;
            float center = -content.InverseTransformPoint(pendingBrowserTarget.position).y + pendingBrowserTarget.rect.height * .5f;
            position = extent > 0f ? 1f - Mathf.Clamp01((center - height * .5f) / extent) : 1f;
        }
        pendingBrowserScroll.StopMovement();
        pendingBrowserScroll.verticalNormalizedPosition = Mathf.Clamp01(position);
        pendingBrowserScroll = null;
    }

    private void ShowPanel(int index)
    {
        RememberBrowserPosition();
        pendingBrowserScroll = null;
        if (currentPanel == 0) { chapterReader.Sample(); savedScroll = verseScrollRect.verticalNormalizedPosition; }
        currentPanel = index;
        for (int i = 0; i < panels.Length; i++) panels[i].SetActive(i == index);
        if (index == 0)
        {
            locationText.text = bibleDataLoader.Reference(selectedVerse);
            SendCustomEventDelayedFrames(nameof(RestoreScroll), 2);
        }
        if (presentation != null) presentation.Apply();
        RefreshReadingTools();
    }

    private void ResolveLocation()
    {
        book = bibleDataLoader.BookForVerse(selectedVerse);
        chapter = bibleDataLoader.ChapterForVerse(book, selectedVerse);
    }

    public void GoTo(int id)
    {
        if (id < 0 || id >= 31102) return;
        CaptureHistoryPosition();
        if (historyCount == 0) { history[0] = selectedVerse; historyCount = 1; historyIndex = 0; CaptureHistoryPosition(); }
        if (history[historyIndex] != id)
        {
            historyCount = historyIndex + 1;
            if (historyCount == history.Length)
            {
                for (int i = 1; i < history.Length; i++) { history[i - 1] = history[i]; historyAnchors[i - 1] = historyAnchors[i]; historyOffsets[i - 1] = historyOffsets[i]; }
                historyCount--;
            }
            historyIndex = historyCount++;
            history[historyIndex] = id; historyAnchors[historyIndex] = id; historyOffsets[historyIndex] = 0;
        }
        Navigate(id, id, 0f);
    }

    private void Navigate(int id, int anchor, float offset)
    {
        // Do not sample old chapter geometry using the new chapter's indices.
        currentPanel = -1;
        selectedVerse = id;
        selectionEnd = id;
        rangeSelection = false;
        ResolveLocation();
        chapterReader.anchorVerse = anchor;
        chapterReader.anchorOffset = offset;
        actionsVisible = false;
        savedScroll = 1f;
        RenderReading();
        ShowPanel(0);
        savedScroll = 1f;
        verseScrollRect.StopMovement();
        chapterReader.Reflow();
        SendCustomEventDelayedFrames(nameof(RestoreScroll), 2);
        SaveProgress();
        SaveHistory();
        Publish();
    }

    private void RenderReading()
    {
        int start = bibleDataLoader.VerseStart(book, chapter);
        int count = bibleDataLoader.VerseCount(book, chapter);
        int low = Mathf.Min(selectedVerse, selectionEnd);
        int high = Mathf.Max(selectedVerse, selectionEnd);
        float size = ReadingFontSize();
        for (int row = 0; row < chapterReader.labels.Length; row++)
        {
            chapterReader.rows[row].gameObject.SetActive(row < count);
            if (row >= count) continue;
            int i = start + row;
            string key = i.ToString();
            string line = "<b>" + (row + 1) + "</b> " + bibleDataLoader.verses[i];
            DataToken color;
            if (highlights.TryGetValue(key, out color) && color.IsNumber)
            {
                int shade = (int)color.Number;
                string hex = theme == 1 ? (shade == 1 ? "FFF17688" : shade == 2 ? "81D4FA88" : "CE93D888") : (shade == 1 ? "FFE08244" : shade == 2 ? "80DEEA44" : "CE93D844");
                line = "<mark=#" + hex + ">" + line + "</mark>";
            }
            if (notes.ContainsKey(key)) line += " <b>[note]</b>";
            Color selectionColor = theme == 1 ? new Color(.15f, .35f, .55f, .12f) : new Color(.5f, .75f, 1f, .11f);
            chapterReader.selectionImages[row].color = i >= low && i <= high ? selectionColor : new Color(0f, 0f, 0f, 0f);
            chapterReader.labels[row].text = line;
            chapterReader.labels[row].fontSize = size;
            chapterReader.labels[row].lineSpacing = wideSpacing ? 16f : 4f;
        }
        verseDisplayText = chapterReader.labels[selectedVerse - start];
        if (currentPanel == 0) locationText.text = bibleDataLoader.Reference(selectedVerse);
        selectionText.text = "Selected: " + bibleDataLoader.Reference(low) + (high > low ? "-" + (high - start + 1) : "");
        rangeLabel.text = rangeSelection ? "Range: on" : "Select range";
        readingHint.text = rangeSelection ? "Click the last verse of your range. Turn range selection off to select one verse." : "Click a verse to select. Scroll to read the chapter.";
        RefreshReadingTools();
        chapterReader.Reflow();
        if (presentation != null) presentation.MarkDirty();
    }

    public float ReadingFontSize()
    {
        // Sublinear growth keeps text comfortable while larger windows still fit more scripture.
        float factor = 1f + (panelWidth - 1f) * .18f + (panelHeight - 1f) * .12f + (panelScale - 1f) * .18f;
        return Mathf.Clamp(fontSize * factor, 18f, 54f);
    }

    // Choose ink for the intended panel composition. A transparent window's
    // world backdrop is unknown; the user's outline preference remains in effect.
    public Color BookTextColor(Color button)
    {
        Color panel = theme == 1 ? new Color(1f,.98f,.9f) : theme == 2 ? Color.black : new Color(.055f,.075f,.1f);
        float alpha = Mathf.Clamp01(button.a);
        float luminance = .2126f * LinearChannel(button.r * alpha + panel.r * (1f-alpha))
            + .7152f * LinearChannel(button.g * alpha + panel.g * (1f-alpha))
            + .0722f * LinearChannel(button.b * alpha + panel.b * (1f-alpha));
        return (luminance+.05f)/.05f >= 1.05f/(luminance+.05f) ? Color.black : Color.white;
    }

    // sRGB relative luminance: https://www.w3.org/WAI/WCAG21/Techniques/general/G18
    private float LinearChannel(float value)
    {
        return value <= .04045f ? value/12.92f : Mathf.Pow((value+.055f)/1.055f,2.4f);
    }

    public void SelectReadingVerse(int index)
    {
        int start = bibleDataLoader.VerseStart(book, chapter);
        if (index < 0 || index >= bibleDataLoader.VerseCount(book, chapter)) return;
        chapterReader.Sample();
        int id = start + index;
        if (rangeSelection) selectionEnd = id;
        else { selectedVerse = id; selectionEnd = id; }
        actionsVisible = true;
        toolsVisible = false;
        RenderReading();
        SaveProgress();
        Publish();
    }

    public void CopySelection()
    {
        copyingNote = false;
        int low = Mathf.Min(selectedVerse, selectionEnd);
        int high = Mathf.Max(selectedVerse, selectionEnd);
        int start = bibleDataLoader.VerseStart(book, chapter);
        copiedVerses = bibleDataLoader.Reference(low) + (high > low ? "-" + (high - start + 1) : "") + " (KJV)\n";
        for (int i = low; i <= high; i++) copiedVerses += (i - start + 1) + " " + bibleDataLoader.verses[i] + (i < high ? "\n" : "");
        copyInput.SetTextWithoutNotify(copiedVerses);
        RefreshCopyControls();
        ShowPanel(8);
        statusText.text = "Selection copied inside the Bible. Click the text field to use keyboard Copy.";
    }

    private void PasteIntoNote()
    {
        if (readOnlySave || copiedVerses.Length == 0) { statusText.text = "Select and copy a verse first."; return; }
        if (currentPanel != 5) HandleAction("Note", 0);
        string combined = noteInput.text + (noteInput.text.Length > 0 ? "\n\n" : "") + copiedVerses;
        if (combined.Length > 1024)
        {
            statusText.text = "This selection would exceed the 1,024-character note limit. Copy a smaller range; your note is unchanged.";
            return;
        }
        noteInput.SetTextWithoutNotify(combined);
        noteDirty = true;
        lastNoteEdit = Time.time;
        CommitNote();
        if (presentation != null) presentation.MarkDirty();
    }

    private void JumpToReference()
    {
        string reference = referenceInput.text.Trim().Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Replace('\u00a0', ' ').Replace('\u2013', '-').Replace('\u2014', '-');
        while (reference.Contains("  ")) reference = reference.Replace("  ", " ");
        int split = -1;
        for (int i = 0; i < reference.Length - 1; i++)
            if (reference[i] == ' ' && reference[i + 1] >= '0' && reference[i + 1] <= '9') { split = i; break; }
        if (split < 1) { statusText.text = "Use John 3:16, John 3:16-18, or Psalm 23."; return; }
        string name = reference.Substring(0, split).ToLowerInvariant().Replace(".", "");
        if (name == "psalm") name = "psalms";
        if (name == "song of songs" || name == "song of solomon") name = "song of solomon";
        if (name.StartsWith("iii ")) name = "3 " + name.Substring(4);
        else if (name.StartsWith("ii ")) name = "2 " + name.Substring(3);
        else if (name.StartsWith("i ")) name = "1 " + name.Substring(2);
        int b = -1;
        for (int i = 0; i < 66; i++)
        {
            string candidate = bibleDataLoader.bookNames[i].ToLowerInvariant();
            if (candidate == name) { b = i; break; }
        }
        if (b < 0 && name.Length >= 3)
        {
            for (int i = 0; i < 66; i++)
            {
                if (!bibleDataLoader.bookNames[i].ToLowerInvariant().StartsWith(name)) continue;
                if (b >= 0) { statusText.text = "That book abbreviation is ambiguous. Enter the full book name."; return; }
                b = i;
            }
        }
        if (b < 0) { statusText.text = "Book not found. Use its full name, such as 1 John."; return; }
        string[] numbers = reference.Substring(split + 1).Replace(" ", "").Split(':');
        int c;
        int v = 1;
        int last = 1;
        if (numbers.Length > 2) { statusText.text = "Choose verses within one chapter (e.g. John 3:16-18)."; return; }
        if (!int.TryParse(numbers[0], out c) || c < 1 || c > bibleDataLoader.ChapterCount(b))
        {
            statusText.text = bibleDataLoader.bookNames[b] + ": choose chapter 1-" + bibleDataLoader.ChapterCount(b) + ".";
            return;
        }
        int count = bibleDataLoader.VerseCount(b, c - 1);
        if (numbers.Length == 2)
        {
            string[] verses = numbers[1].Split('-');
            bool valid = verses.Length <= 2 && int.TryParse(verses[0], out v);
            last = v;
            if (verses.Length == 2 && !int.TryParse(verses[1], out last)) valid = false;
            if (!valid || v < 1 || last < v || last > count)
            {
                statusText.text = bibleDataLoader.bookNames[b] + " " + c + ": use verses 1-" + count + " (e.g. 1-" + Mathf.Min(3, count) + ").";
                return;
            }
        }
        int start = bibleDataLoader.VerseStart(b, c - 1);
        GoTo(start + v - 1);
        if (last > v)
        {
            selectionEnd = start + last - 1;
            rangeSelection = true; actionsVisible = true; toolsVisible = false;
            RenderReading(); SaveProgress(); Publish();
        }
        statusText.text = (last > v ? "Selected " : "Opened ") + bibleDataLoader.Reference(selectedVerse) + (last > v ? "-" + last : "") + ".";
    }

    private void ToggleHighlight()
    {
        if (readOnlySave) return;
        DataToken previous;
        int color = highlights.TryGetValue(selectedVerse.ToString(), out previous) && previous.IsNumber ? (int)previous.Number : 0;
        int next = (Mathf.Clamp(color, 0, 3) + 1) % 4;
        int low = Mathf.Min(selectedVerse, selectionEnd), high = Mathf.Max(selectedVerse, selectionEnd);
        int needed = 0;
        for (int i = low; i <= high; i++) if (!highlights.ContainsKey(i.ToString())) needed++;
        if (next != 0 && highlights.Count + needed > maxHighlights)
        {
            statusText.text = "Highlight storage is full for this selection. Remove highlights or select fewer verses. Nothing changed.";
            return;
        }
        undoHighlights = new DataDictionary(); undoHighlightMissing = new DataDictionary();
        for (int i = low; i <= high; i++)
        {
            string key = i.ToString();
            if (!highlights.TryGetValue(key, out previous)) undoHighlightMissing.SetValue(key, 1);
            undoHighlights.SetValue(key, previous);
            if (next == 0) highlights.Remove(key); else highlights.SetValue(key, next);
        }
        undoKind = 2;
        SaveAnnotations();
        RenderReading();
    }

    private bool CommitNote()
    {
        if (!noteDirty) return true;
        if (!restored || readOnlySave) { statusText.text = "Notes cannot be saved yet. Your draft is still here."; return false; }
        string key = editingVerse.ToString();
        string draft = noteInput.text;
        bool empty = string.IsNullOrWhiteSpace(draft);
        DataToken old;
        bool exists = notes.TryGetValue(key, TokenType.String, out old);
        if ((!notes.ContainsKey(key) && empty) || (exists && old.String == draft)) { noteDirty = false; RefreshNoteState(); return true; }
        if (empty) draft = "";
        int length = draft.Length;
        DataList values = notes.GetValues();
        for (int i = 0; i < values.Count; i++) if (values[i].TokenType == TokenType.String) length += values[i].String.Length;
        if (exists) length -= old.String.Length;
        if (!empty && (length > maxNoteCharacters || (!notes.ContainsKey(key) && notes.Count >= maxNotes)))
        {
            statusText.text = "Storage full. Draft retained; shorten, copy, or revert.";
            lastNoteEdit = Time.time + 10f;
            RefreshNoteState();
            return false;
        }
        CaptureNoteUndo(key);
        if (empty) { notes.Remove(key); noteInput.SetTextWithoutNotify(""); }
        else notes.SetValue(key, draft);
        noteDirty = false;
        SaveAnnotations();
        RenderReading();
        return true;
    }

    private void SaveAnnotations()
    {
        if (!restored || readOnlySave) return;
        DataDictionary root = new DataDictionary();
        root.SetValue("highlights", highlights);
        root.SetValue("notes", notes);
        DataToken json;
        if (!VRCJson.TrySerializeToJson(root, JsonExportType.Minify, out json))
        {
            statusText.text = "Could not serialize annotations. Please keep this session open.";
            Debug.LogError("[Bible] Annotation serialization failed.");
            return;
        }
        PlayerData.SetString(Prefix + "annotations", json.String);
        statusText.text = "World save queued. " + notes.Count + "/" + maxNotes + " notes; " + highlights.Count + "/" + maxHighlights + " highlights.";
        RefreshReadingTools();
    }

    private void SaveProgress()
    {
        if (!restored) return;
        PlayerData.SetInt(Prefix + "verse", selectedVerse);
        PlayerData.SetInt(Prefix + "selectionEnd", selectionEnd);
        if (currentPanel == 0) savedScroll = verseScrollRect.verticalNormalizedPosition;
        PlayerData.SetFloat(Prefix + "scroll", savedScroll);
        if (currentPanel == 0) chapterReader.Sample();
        PlayerData.SetInt(Prefix + "chapterAnchor", chapterReader.anchorVerse);
        PlayerData.SetFloat(Prefix + "chapterOffset", chapterReader.anchorOffset);
        SaveHistory();
    }

    private void StartSearch()
    {
        string candidate = searchInput.text.Trim();
        if (candidate.Length < 3)
        {
            searching = false; matchCount = 0;
            searchStatus.text = "Enter at least 3 characters. Search matches a phrase, ignoring case.";
            ResetSearchScroll(); RenderSearch(); return;
        }
        query = candidate;
        matchCount = 0;
        searchStart = searchThisBook.isOn ? bibleDataLoader.VerseStart(book, 0) : 0;
        searchCursor = searchStart;
        searchEnd = searchThisBook.isOn ? bibleDataLoader.chapterVerseStarts[bibleDataLoader.bookChapterStarts[book + 1]] : 31102;
        searchScopeName = searchThisBook.isOn ? bibleDataLoader.bookNames[book] : "the KJV";
        searching = true;
        searchStatus.text = "Searching " + searchScopeName + "... 0%";
        PlayerData.SetString(Prefix + "query", query);
        PlayerData.SetBool(Prefix + "scope", searchThisBook.isOn);
        ResetSearchScroll();
        RenderSearch();
    }

    private void ResetSearchScroll()
    {
        if (searchResultsScroll == null) return;
        searchResultsScroll.StopMovement(); searchResultsScroll.verticalNormalizedPosition = 1f;
        searchResultsScroll.content.anchoredPosition = Vector2.zero;
    }

    private void RefreshSearchControls()
    {
        if (searchScopeLabel != null) searchScopeLabel.text = "Search only " + bibleDataLoader.bookNames[book] + " (on next search)";
        if (searchStop != null) searchStop.interactable = searching;
        if (searchClear != null) searchClear.interactable = matchCount > 0 || searchInput.text.Length > 0 || query.Length > 0;
        if (presentation != null) presentation.MarkDirty();
    }

    private void SearchStep()
    {
        int until = Mathf.Min(searchCursor + searchVersesPerFrame, searchEnd);
        for (; searchCursor < until; searchCursor++)
        {
            if (bibleDataLoader.verses[searchCursor].IndexOf(query, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            matches[matchCount++] = searchCursor;
            if (matchCount == matches.Length) { searching = false; break; }
        }
        if (searchCursor >= searchEnd) searching = false;
        int percent = Mathf.RoundToInt(100f * (searchCursor - searchStart) / Mathf.Max(1, searchEnd - searchStart));
        searchStatus.text = searching ? "Searching " + searchScopeName + "... " + percent + "%; " + matchCount + " matches" : matchCount == 200 ? "First 200 matches in " + searchScopeName + ". Refine your phrase." : matchCount == 0 ? "No matches in " + searchScopeName + ". Try a shorter phrase or search all books." : matchCount + (matchCount == 1 ? " match in " : " matches in ") + searchScopeName + ".";
        RenderSearch();
    }

    private void RenderSearch()
    {
        RefreshSearchControls();
        for (int i = 0; i < searchRows.Length; i++)
        {
            int result = i;
            searchRows[i].SetActive(result < matchCount);
            if (result < matchCount)
            {
                int id = matches[result];
                string verse = bibleDataLoader.verses[id];
                int hit = verse.IndexOf(query, System.StringComparison.OrdinalIgnoreCase);
                int begin = Mathf.Max(0, hit - 16);
                while (begin > 0 && verse[begin - 1] != ' ') begin--;
                int end = Mathf.Min(verse.Length, Mathf.Max(begin + 96, hit + query.Length + 28));
                while (end < verse.Length && verse[end] != ' ') end++;
                searchLabels[i].text = "<b>" + bibleDataLoader.Reference(id) + "</b>\n" + (begin > 0 ? "..." : "") + verse.Substring(begin, hit - begin) + "<b>" + verse.Substring(hit, query.Length) + "</b>" + verse.Substring(hit + query.Length, end - hit - query.Length) + (end < verse.Length ? "..." : "");
            }
        }
    }

    private void OpenSelectedNote()
    {
        editingVerse = selectedVerse;
        noteReference.text = "Note for " + bibleDataLoader.Reference(editingVerse);
        DataToken value;
        noteInput.SetTextWithoutNotify(notes.TryGetValue(editingVerse.ToString(), TokenType.String, out value) ? value.String : "");
        noteDirty = false;
        ShowPanel(5);
    }

    private void RefreshCopyControls()
    {
        if (copyHeading != null) copyHeading.text = copyingNote ? "Copy note for " + bibleDataLoader.Reference(editingVerse) : "Copy selected scripture";
        if (copyReturnLabel != null) copyReturnLabel.text = copyingNote ? "Back to note" : "Back to reading";
        if (copyPasteControl != null) copyPasteControl.SetActive(!copyingNote);
    }

    private void RevertNoteDraft()
    {
        if (!noteDirty || readOnlySave) return;
        undoKind = 4; undoKey = editingVerse.ToString(); undoValue = noteInput.text;
        DataToken saved;
        noteInput.SetTextWithoutNotify(notes.TryGetValue(undoKey, TokenType.String, out saved) ? saved.String : "");
        noteDirty = false;
        RefreshReadingTools();
        statusText.text = "Saved text restored. Undo recovers the discarded draft.";
    }

    private void RefreshNoteState()
    {
        string key = editingVerse.ToString();
        string draft = noteInput.text;
        bool empty = string.IsNullOrWhiteSpace(draft);
        DataToken saved;
        bool exists = notes.TryGetValue(key, TokenType.String, out saved);
        int length = empty ? 0 : draft.Length;
        DataList values = notes.GetValues();
        for (int i = 0; i < values.Count; i++) if (values[i].TokenType == TokenType.String) length += values[i].String.Length;
        if (exists) length -= saved.String.Length;
        int count = notes.Count + (notes.ContainsKey(key) ? (empty ? -1 : 0) : (empty ? 0 : 1));
        bool blocked = !empty && (length > maxNoteCharacters || count > maxNotes);
        bool editable = restored && !readOnlySave;
        if (noteStatus != null) noteStatus.text = "Note: " + draft.Length + "/1,024 chars. " + (!editable ? "Read-only: saved notes protected." : noteDirty ? blocked ? "Unsaved - storage full; copy or revert." : "Unsaved; autosave pending." : "No pending edits.") + "\nAfter saving: " + length + "/" + maxNoteCharacters + " chars; " + count + "/" + maxNotes + " notes.";
        if (noteSave != null) noteSave.interactable = editable && noteDirty && !blocked;
        if (noteRevert != null) noteRevert.interactable = editable && noteDirty;
        if (noteCopy != null) noteCopy.interactable = draft.Length > 0;
        if (notePaste != null) notePaste.interactable = editable && copiedVerses.Length > 0;
        if (noteDelete != null) noteDelete.interactable = editable && (notes.ContainsKey(key) || draft.Length > 0);
        if (presentation != null) presentation.MarkDirty();
    }

    private void RenderSaved()
    {
        if (presentation != null) presentation.MarkDirty();
        savedKeys = savedMode == 2 ? bookmarks.GetKeys() : savedNotes ? notes.GetKeys() : highlights.GetKeys();
        libraryCount = 0;
        DataDictionary source = savedMode == 2 ? bookmarks : savedNotes ? notes : highlights;
        if (savedMode != 3)
        {
            for (int i = 0; i < savedKeys.Count && libraryCount < libraryIds.Length; i++)
            {
                int id;
                DataToken value;
                if (savedKeys[i].TokenType != TokenType.String || !int.TryParse(savedKeys[i].String, out id) || id < 0 || id >= 31102 || savedKeys[i].String != id.ToString()) continue;
                bool valid = savedMode == 0 ? source.TryGetValue(savedKeys[i], out value) && value.IsNumber && value.Number >= 1 && value.Number <= 3 : source.TryGetValue(savedKeys[i], TokenType.String, out value) && value.String.Length > 0;
                if (valid) libraryIds[libraryCount++] = id;
            }
            // Sort the bounded display buffer without changing persistent dictionaries.
            for (int gap = libraryCount / 2; gap > 0; gap /= 2)
                for (int i = gap; i < libraryCount; i++)
                {
                    int id = libraryIds[i], j = i;
                    while (j >= gap && libraryIds[j - gap] > id) { libraryIds[j] = libraryIds[j - gap]; j -= gap; }
                    libraryIds[j] = id;
                }
        }
        int total = savedMode == 3 ? historyCount : libraryCount;
        string title = savedMode == 3 ? "Recent passages" : savedMode == 2 ? "Bookmarks" : savedNotes ? "Your notes" : "Your highlights";
        locationText.text = title + " - " + total + " saved";
        if (bookmarkEditor != null) bookmarkEditor.SetActive(savedMode == 2);
        if (bookmarkName != null && savedMode == 2 && bookmarkEditingVerse != selectedVerse)
        {
            DataToken name;
            bookmarkName.SetTextWithoutNotify(bookmarks.TryGetValue(selectedVerse.ToString(), TokenType.String, out name) ? name.String : "");
            bookmarkEditingVerse = selectedVerse;
        }
        if (libraryList != null) libraryList.offsetMax = new Vector2(0f, savedMode == 2 ? -185f : -115f);
        if (libraryHint != null)
        {
            if (libraryHintLayout != null) libraryHintLayout.anchoredPosition = new Vector2(0f, savedMode == 2 ? -124f : -65f);
            libraryHint.text = savedMode == 2 ? "Name applies to " + bibleDataLoader.Reference(selectedVerse) + ". " + (total == 0 ? "No bookmarks yet; Save name adds one." : "Passages below are in Bible order.") : total == 0 ? "No entries yet. Select a verse to bookmark, highlight, or write a note." : savedMode == 3 ? "Newest first. Back and Forward also restore your scroll position." : savedNotes ? "Choose a note to open it directly. Passages are in Bible order." : "Choose a passage to read it. Passages are in Bible order.";
        }
        for (int i = 0; i < savedRows.Length; i++)
        {
            int item = i;
            if (savedMode == 3)
            {
                savedRows[i].SetActive(i < historyCount);
                if (i < historyCount) savedLabels[i].text = bibleDataLoader.Reference(history[historyCount - 1 - i]) + (historyCount - 1 - i == historyIndex ? "  (current)" : "");
                continue;
            }
            bool valid = item < libraryCount;
            savedRows[i].SetActive(valid);
            if (!valid) continue;
            int id = libraryIds[item];
            DataToken value;
            string preview = savedMode == 2 && bookmarks.TryGetValue(id.ToString(), TokenType.String, out value) ? value.String : savedNotes && notes.TryGetValue(id.ToString(), TokenType.String, out value) ? value.String.Replace('\n', ' ') : bibleDataLoader.verses[id];
            string reference = bibleDataLoader.Reference(id);
            savedLabels[i].text = savedMode == 2 && preview == reference ? reference : reference + "  " + preview.Substring(0, Mathf.Min(preview.Length, 95));
        }
        RefreshBookmarkState();
        RefreshNavigation();
    }

    private void SettingsChanged()
    {
        if (currentPanel == 0) chapterReader.Sample();
        ApplySettings();
        UpdateSettingsText();
        RenderReading();
        PlayerData.SetInt(Prefix + "font", fontSize);
        PlayerData.SetInt(Prefix + "theme", theme);
        PlayerData.SetBool(Prefix + "spacing", wideSpacing);
        PlayerData.SetBool(Prefix + "share", share);
        PlayerData.SetBool(Prefix + "follow", follow);
        PlayerData.SetInt(Prefix + "followSource", followSource);
        PlayerData.SetFloat(Prefix + "followStrength", followStrength);
        PlayerData.SetBool(Prefix + "sounds", sounds);
        PlayerData.SetBool(Prefix + "shortcut", vrShortcut);
        PlayerData.SetFloat(Prefix + "scale", panelScale);
        PlayerData.SetFloat(Prefix + "distance", panelDistance);
        PlayerData.SetBool(Prefix + "thumbstick", thumbstickShortcut);
        PlayerData.SetBool(Prefix + "fullscreen", fullscreen);
        PlayerData.SetBool(Prefix + "back", showBack);
        PlayerData.SetBool(Prefix + "originalColors", originalColors);
        PlayerData.SetFloat(Prefix + "width", panelWidth);
        PlayerData.SetFloat(Prefix + "height", panelHeight);
        PlayerData.SetFloat(Prefix + "backgroundOpacity", backgroundOpacity);
        PlayerData.SetFloat(Prefix + "textOpacity", textOpacity);
        PlayerData.SetInt(Prefix + "outlineMode", outlineMode);
        PlayerData.SetBool(Prefix + "readingRuler", readingRuler);
        PlayerData.SetInt(Prefix + "comfortPreset", comfortPreset);
        SavePlacement();
        Publish();
    }

    private void ApplySettings()
    {
        Color background = theme == 1 ? new Color(1f, 0.98f, 0.9f, 1f) : theme == 2 ? Color.black : new Color(0.055f, 0.075f, 0.10f, 0.98f);
        Color foreground = theme == 1 ? new Color(0.08f, 0.06f, 0.02f) : Color.white;
        for (int i = 0; i < backgrounds.Length; i++) backgrounds[i].color = background;
        for (int i = 0; i < interfaceTexts.Length; i++) interfaceTexts[i].color = foreground;
        mainPanel.transform.localScale = Vector3.one * (0.0008f * panelScale);
        if (followSlider != null && Mathf.Abs(followSlider.value - followStrength) > .001f) followSlider.value = followStrength;
        if (presentation != null) presentation.Apply();
    }

    private void UpdateSettingsText()
    {
        settingsText.text = "Preferences save automatically";
        if (settingValues == null) return;
        for (int i = 0; i < settingValues.Length; i++) settingValues[i].text = SettingValue(settingActions[i]);
    }

    public string SettingValue(string action)
    {
        if (action == "Width") return (86.4f * panelScale * panelWidth).ToString("F0") + " cm";
        if (action == "Height") return (68.8f * panelScale * panelHeight).ToString("F0") + " cm";
        if (action == "Scale") return (panelScale * 100f).ToString("F0") + "%";
        if (action == "Distance") return panelDistance.ToString("F1") + " m";
        if (action == "Font") return ReadingFontSize().ToString("F0") + " (auto)";
        if (action == "BackgroundOpacity") return (backgroundOpacity * 100f).ToString("F0") + "%";
        if (action == "TextOpacity") return (textOpacity * 100f).ToString("F0") + "%";
        if (action == "Theme") return theme == 0 ? "Dark" : theme == 1 ? "Paper" : "High contrast";
        if (action == "Spacing") return wideSpacing ? "Wide" : "Normal";
        if (action == "ReadingRuler") return readingRuler ? "Guide on" : "Guide off";
        if (action == "Follow") return follow ? (followSource == 0 ? "Follows view" : "Follows controller") : "Pinned in world";
        if (action == "Attachment") return followSource == 1 ? "Left controller" : followSource == 2 ? "Right controller" : "Head / view";
        if (action == "FollowStrength") return followStrength >= .999f ? "Fixed" : (followStrength * 100f).ToString("F0") + "%";
        if (action == "Share") return share ? "Shared scripture" : "Only me";
        if (action == "Sounds") return sounds ? "On" : "Off";
        if (action == "Fullscreen") return fullscreen ? "Words only" : "Full window";
        if (action == "Shortcut") return vrShortcut ? "On" : "Off";
        if (action == "Thumbstick") return thumbstickShortcut ? "On" : "Off";
        if (action == "Back") return showBack ? "Visible" : "Hidden";
        if (action == "OriginalColors") return originalColors ? "Original colors" : "Grayscale";
        if (action == "Outline") return outlineMode == 0 ? "Automatic" : outlineMode == 1 ? "Always on" : "Off";
        return "";
    }

    private void ShowSettingsSection(int section)
    {
        if (settingsSections == null || settingsSections.Length == 0) return;
        settingsSection = Mathf.Clamp(section, 0, settingsSections.Length - 1);
        for (int i = 0; i < settingsSections.Length; i++) settingsSections[i].SetActive(i == settingsSection);
        RefreshNavigation();
        if (presentation != null) presentation.MarkDirty();
    }

    public void ResizeWindow(float width, float height)
    {
        width = Mathf.Clamp(width, 0.8f, 1.8f);
        height = Mathf.Clamp(height, 0.8f, 1.8f);
        if (Mathf.Abs(width - panelWidth) < 0.0001f && Mathf.Abs(height - panelHeight) < 0.0001f) return;
        if (currentPanel == 0) chapterReader.Sample();
        panelWidth = width;
        panelHeight = height;
        if (presentation != null) presentation.Resize();
        if (restored) RenderReading();
    }

    public void BeginHold()
    {
        held = true;
    }

    public void EndHold()
    {
        held = false;
        hasPlacement = true;
        if (follow && follower != null) follower.Capture();
        if (restored) SettingsChanged();
    }

    private void SavePlacement()
    {
        if (!restored || !hasPlacement || held) return;
        Vector3 p = mainPanel.transform.position;
        Vector3 r = mainPanel.transform.rotation.eulerAngles;
        PlayerData.SetFloat(Prefix + "pinX", p.x);
        PlayerData.SetFloat(Prefix + "pinY", p.y);
        PlayerData.SetFloat(Prefix + "pinZ", p.z);
        PlayerData.SetFloat(Prefix + "pinPitch", r.x);
        PlayerData.SetFloat(Prefix + "pinYaw", r.y);
        PlayerData.SetFloat(Prefix + "pinRoll", r.z);
        if (!hasViewPlacement) return;
        PlayerData.SetFloat(Prefix + "viewX", viewOffset.x);
        PlayerData.SetFloat(Prefix + "viewY", viewOffset.y);
        PlayerData.SetFloat(Prefix + "viewZ", viewOffset.z);
        Vector3 viewAngles = viewRotation.eulerAngles;
        PlayerData.SetFloat(Prefix + "viewPitch", viewAngles.x);
        PlayerData.SetFloat(Prefix + "viewYaw", viewAngles.y);
        PlayerData.SetFloat(Prefix + "viewRoll", viewAngles.z);
        SaveComfortPreset();
    }

    private void RestoreReadingTools(VRCPlayerApi player)
    {
        DataToken stored;
        if (PlayerData.HasKey(player, Prefix + "bookmarks"))
        {
            if (VRCJson.TryDeserializeFromJson(PlayerData.GetString(player, Prefix + "bookmarks"), out stored) && stored.TokenType == TokenType.DataDictionary)
                bookmarks = stored.DataDictionary;
            else readOnlyBookmarks = true;
        }
        string historySave = PlayerData.HasKey(player, Prefix + "history") ? PlayerData.GetString(player, Prefix + "history") : "";
        if (historySave == null) historySave = "";
        string[] entries = historySave.Split(';');
        for (int i = 0; i < entries.Length && historyCount < 32; i++)
        {
            string[] parts = entries[i].Split(',');
            int id, anchor, offset;
            if (parts.Length != 3 || !int.TryParse(parts[0], out id) || !int.TryParse(parts[1], out anchor) || !int.TryParse(parts[2], out offset) || id < 0 || id >= 31102) continue;
            int b = bibleDataLoader.BookForVerse(id), c = bibleDataLoader.ChapterForVerse(b, id);
            int start = bibleDataLoader.VerseStart(b, c), end = start + bibleDataLoader.VerseCount(b, c);
            history[historyCount] = id;
            historyAnchors[historyCount] = anchor >= start && anchor < end ? anchor : id;
            historyOffsets[historyCount++] = Mathf.Clamp(offset, 0, 10000000);
        }
        historyIndex = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "historyIndex"), 0, Mathf.Max(0, historyCount - 1));
        hasReturn = PlayerData.GetBool(player, Prefix + "hasReturn");
        returnVerse = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "returnVerse"), 0, 31101);
        returnEnd = returnVerse;
        returnAnchor = returnVerse;
        if (hasReturn)
        {
            int b = bibleDataLoader.BookForVerse(returnVerse), c = bibleDataLoader.ChapterForVerse(b, returnVerse);
            int start = bibleDataLoader.VerseStart(b, c), end = start + bibleDataLoader.VerseCount(b, c);
            returnEnd = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "returnEnd"), start, end - 1);
            returnAnchor = Mathf.Clamp(PlayerData.GetInt(player, Prefix + "returnAnchor"), start, end - 1);
            returnOffset = Mathf.Clamp(PlayerData.GetFloat(player, Prefix + "returnOffset"), 0f, 10000f);
        }
    }

    private void CaptureHistoryPosition()
    {
        if (currentPanel == 0) chapterReader.Sample();
        if (historyCount == 0) return;
        history[historyIndex] = selectedVerse;
        historyAnchors[historyIndex] = chapterReader.anchorVerse;
        historyOffsets[historyIndex] = Mathf.Clamp(Mathf.RoundToInt(chapterReader.anchorOffset * 1000f), 0, 10000000);
    }

    private void SaveHistory()
    {
        if (!restored) return;
        if (historyCount == 0) { historyCount = 1; historyIndex = 0; }
        CaptureHistoryPosition();
        string value = "";
        for (int i = 0; i < historyCount; i++) value += (i == 0 ? "" : ";") + history[i] + "," + historyAnchors[i] + "," + historyOffsets[i];
        if (PlayerData.GetString(Networking.LocalPlayer, Prefix + "history") != value) PlayerData.SetString(Prefix + "history", value);
        if (PlayerData.GetInt(Networking.LocalPlayer, Prefix + "historyIndex") != historyIndex) PlayerData.SetInt(Prefix + "historyIndex", historyIndex);
    }

    private void TravelHistory(int step)
    {
        int target = historyIndex + step;
        if (target < 0 || target >= historyCount) return;
        CaptureHistoryPosition();
        historyIndex = target;
        Navigate(history[target], historyAnchors[target], historyOffsets[target] / 1000f);
    }

    private void ToggleBookmark()
    {
        if (readOnlyBookmarks) { statusText.text = "Saved bookmarks could not be read. Editing is disabled to protect them."; return; }
        string key = selectedVerse.ToString();
        DataToken previous;
        bool exists = bookmarks.TryGetValue(key, TokenType.String, out previous);
        if (!exists && bookmarks.Count >= 64) { statusText.text = "All 64 bookmarks are in use. Remove one before adding another."; return; }
        undoKind = 3; undoKey = key; undoValue = exists ? previous.String : "";
        if (exists) bookmarks.Remove(key);
        else bookmarks.SetValue(key, bibleDataLoader.Reference(selectedVerse));
        bookmarkEditingVerse = -1;
        SaveBookmarks();
        if (currentPanel == 4) RenderSaved();
    }

    private void RenameBookmark()
    {
        if (readOnlyBookmarks || bookmarkName == null) return;
        string name = bookmarkName.text.Trim();
        if (name.Length == 0) { statusText.text = "Enter a bookmark name first."; RefreshBookmarkState(); return; }
        name = name.Substring(0, Mathf.Min(48, name.Length));
        string key = selectedVerse.ToString();
        DataToken previous;
        bool exists = bookmarks.TryGetValue(key, TokenType.String, out previous);
        if (exists && previous.String == name)
        {
            bookmarkName.SetTextWithoutNotify(name);
            RefreshBookmarkState();
            statusText.text = "Bookmark name is already saved.";
            return;
        }
        if (!exists && bookmarks.Count >= 64) { statusText.text = "All 64 bookmarks are in use. Remove one before adding another."; return; }
        undoKind = 3; undoKey = key; undoValue = exists ? previous.String : "";
        bookmarks.SetValue(key, name);
        bookmarkName.SetTextWithoutNotify(name);
        bookmarkEditingVerse = selectedVerse;
        SaveBookmarks();
        RenderSaved();
    }

    private void RefreshBookmarkState()
    {
        if (bookmarkName == null) return;
        DataToken saved;
        bool exists = bookmarks.TryGetValue(selectedVerse.ToString(), TokenType.String, out saved);
        string name = bookmarkName.text.Trim();
        name = name.Substring(0, Mathf.Min(48, name.Length));
        bool editable = restored && !readOnlyBookmarks;
        bool changed = name.Length > 0 && (!exists || name != saved.String);
        bool room = exists || bookmarks.Count < 64;
        bookmarkName.readOnly = !editable;
        if (bookmarkSave != null) bookmarkSave.interactable = editable && changed && room;
        if (bookmarkRemove != null) bookmarkRemove.interactable = editable && exists && saved.String.Length > 0;
        if (bookmarkSaveCaption != null) bookmarkSaveCaption.text = exists ? "Save name" : "Add bookmark";
        if (currentPanel == 4 && savedMode == 2 && libraryHint != null)
        {
            string help = !editable ? "Editing disabled; saved data could not be read." : !room ? "All slots used. Rename or remove an existing bookmark." : exists ? name.Length == 0 ? "Enter a name to rename, or Remove this bookmark." : changed ? "Name changed; Save name updates this bookmark." : "Saved name. Remove deletes this bookmark; Undo restores it." : name.Length > 0 ? "Add bookmark saves this passage." : "Enter a name, or use Bookmark while reading.";
            libraryHint.text = bibleDataLoader.Reference(selectedVerse) + " | " + bookmarks.Count + "/64 bookmarks.\n" + help;
        }
        if (presentation != null) presentation.MarkDirty();
    }

    private void SaveBookmarks()
    {
        DataToken json;
        if (!restored || readOnlyBookmarks) return;
        if (!VRCJson.TrySerializeToJson(bookmarks, JsonExportType.Minify, out json)) { statusText.text = "Bookmark save could not be prepared. Keep this session open."; return; }
        PlayerData.SetString(Prefix + "bookmarks", json.String);
        statusText.text = "Bookmark save queued. " + bookmarks.Count + "/64 bookmarks. Undo is available.";
        RefreshReadingTools();
    }

    private void CaptureNoteUndo(string key)
    {
        DataToken previous;
        undoKind = 1; undoKey = key;
        undoValue = notes.TryGetValue(key, TokenType.String, out previous) ? previous.String : "";
    }

    private void UndoAnnotation()
    {
        if (undoKind == 0) return;
        if (undoKind == 4) { if (!readOnlySave) RestoreUndoDraft(false); return; }
        if (undoKind == 3)
        {
            if (readOnlyBookmarks) return;
            if (undoValue.Length > 0 && !bookmarks.ContainsKey(undoKey) && bookmarks.Count >= 64) { statusText.text = "Remove a bookmark to make room for this undo."; return; }
            if (undoValue.Length == 0) bookmarks.Remove(undoKey); else bookmarks.SetValue(undoKey, undoValue);
            bookmarkEditingVerse = -1;
            undoKind = 0; SaveBookmarks();
        }
        else
        {
            if (readOnlySave) return;
            if (undoKind == 1)
            {
                if (undoValue.Length > 0 && !notes.ContainsKey(undoKey) && notes.Count >= maxNotes) { RestoreUndoDraft(true); return; }
                int length = undoValue.Length;
                DataList keys = notes.GetKeys();
                for (int i = 0; i < keys.Count; i++) { DataToken v; if (keys[i].String != undoKey && notes.TryGetValue(keys[i], TokenType.String, out v)) length += v.String.Length; }
                if (length > maxNoteCharacters) { RestoreUndoDraft(true); return; }
                if (undoValue.Length == 0) notes.Remove(undoKey); else notes.SetValue(undoKey, undoValue);
                if (editingVerse.ToString() == undoKey) { noteInput.SetTextWithoutNotify(undoValue); noteDirty = false; }
            }
            else
            {
                DataList keys = undoHighlights.GetKeys();
                int restoredCount = highlights.Count;
                for (int i = 0; i < keys.Count; i++)
                {
                    bool missing = undoHighlightMissing.ContainsKey(keys[i]);
                    bool exists = highlights.ContainsKey(keys[i]);
                    if (!missing && !exists) restoredCount++;
                    if (missing && exists) restoredCount--;
                }
                if (restoredCount > maxHighlights) { statusText.text = "Remove highlights to make room for this undo."; return; }
                for (int i = 0; i < keys.Count; i++)
                {
                    DataToken previous;
                    if (undoHighlightMissing.ContainsKey(keys[i])) highlights.Remove(keys[i]);
                    else if (undoHighlights.TryGetValue(keys[i], out previous)) highlights.SetValue(keys[i], previous);
                }
            }
            undoKind = 0; SaveAnnotations();
        }
        RenderReading();
        if (currentPanel == 4) RenderSaved();
        statusText.text = "Previous change restored. World save queued.";
    }

    private void RestoreUndoDraft(bool capacityBlocked)
    {
        int id;
        if (!int.TryParse(undoKey, out id) || id < 0 || id >= 31102) return;
        editingVerse = id;
        noteReference.text = "Note for " + bibleDataLoader.Reference(id);
        noteInput.SetTextWithoutNotify(undoValue);
        noteDirty = true; lastNoteEdit = Time.time + (capacityBlocked ? 10f : 0f);
        undoKind = 0;
        ShowPanel(5);
        statusText.text = capacityBlocked ? "Draft recovered but unsaved. Storage full; shorten or copy it." : "Previous draft restored. It has not been saved yet.";
    }

    public void RememberPersonalReading()
    {
        if (hasReturn) return;
        if (currentPanel == 0) chapterReader.Sample();
        hasReturn = true; returnVerse = selectedVerse; returnEnd = selectionEnd;
        returnAnchor = chapterReader.anchorVerse; returnOffset = chapterReader.anchorOffset;
        PlayerData.SetBool(Prefix + "hasReturn", true);
        PlayerData.SetInt(Prefix + "returnVerse", returnVerse); PlayerData.SetInt(Prefix + "returnEnd", returnEnd);
        PlayerData.SetInt(Prefix + "returnAnchor", returnAnchor); PlayerData.SetFloat(Prefix + "returnOffset", returnOffset);
    }

    public void OpenStudyPassage(int first, int last)
    {
        if (!restored || first < 0 || first >= 31102 || last < 0 || last >= 31102 || !CommitNote()) return;
        RememberPersonalReading();
        if (!isOpen) ToggleBible();
        GoTo(first);
        int start = bibleDataLoader.VerseStart(book, chapter);
        selectionEnd = Mathf.Clamp(last, start, start + bibleDataLoader.VerseCount(book, chapter) - 1);
        rangeSelection = selectionEnd != selectedVerse;
        actionsVisible = true;
        toolsVisible = false;
        RenderReading(); SaveProgress(); Publish();
        statusText.text = "Study passage opened. Tools > Return to my reading restores your own place.";
    }

    private void ReturnToReading()
    {
        if (!hasReturn) return;
        GoTo(returnVerse);
        selectionEnd = returnEnd;
        rangeSelection = selectionEnd != selectedVerse;
        if (rangeSelection) { actionsVisible = true; toolsVisible = false; }
        chapterReader.anchorVerse = returnAnchor; chapterReader.anchorOffset = returnOffset;
        hasReturn = false; PlayerData.SetBool(Prefix + "hasReturn", false);
        RenderReading(); SaveProgress(); Publish();
        statusText.text = "Returned to your own reading place.";
    }

    public void RefreshReadingTools()
    {
        RefreshBookmarkState();
        RefreshNoteState();
        RefreshBrowserState();
        if (verseActions != null) verseActions.SetActive(actionsVisible);
        if (readingTools != null) readingTools.SetActive(toolsVisible);
        if (readingGuide != null) readingGuide.SetActive(readingRuler);
        if (toolsLabel != null) toolsLabel.text = toolsVisible ? "Hide tools" : "Tools";
        if (bookmarkLabel != null) bookmarkLabel.text = bookmarks.ContainsKey(selectedVerse.ToString()) ? "Unbookmark" : "Bookmark";
        if (historyBack != null) historyBack.interactable = historyIndex > 0;
        if (historyForward != null) historyForward.interactable = historyIndex + 1 < historyCount;
        if (undoButton != null) undoButton.SetActive(undoKind > 0 && !(fullscreen && panels[0].activeSelf));
        if (returnButton != null) returnButton.SetActive(hasReturn);
        if (continueLabel != null) continueLabel.text = bibleDataLoader.ChapterId(book, chapter) < 1188 ? "End of chapter - continue to the next chapter" : "End of Revelation - return to Genesis";
        RefreshNavigation();
        if (presentation != null) { presentation.LayoutReading(); presentation.MarkDirty(); }
    }

    private void RefreshBrowserState()
    {
        for (int i=0;i<bookLabels.Length;i++)
        {
            string caption = i==book ? "["+bibleDataLoader.bookNames[i]+"]" : bibleDataLoader.bookNames[i];
            if (bookLabels[i].text!=caption) bookLabels[i].text=caption;
        }
        int start = bibleDataLoader.VerseStart(book,chapter);
        for (int i=0;i<chapterLabels.Length;i++)
        {
            bool current = navigationBook==book && i==chapter;
            string number=(i+1).ToString(); string caption=current ? "["+number+"]" : number;
            if (chapterLabels[i].text!=caption) chapterLabels[i].text=caption;
            chapterImages[i].color=current ? new Color(.08f,.36f,.46f,backgroundOpacity) : new Color(.15f,.23f,.3f,backgroundOpacity);
        }
        for (int i=0;i<verseLabels.Length;i++)
        {
            bool current=navigationBook==book && navigationChapter==chapter && i==selectedVerse-start;
            string number=(i+1).ToString(); string caption=current ? "["+number+"]" : number;
            if (verseLabels[i].text!=caption) verseLabels[i].text=caption;
            verseImages[i].color=current ? new Color(.08f,.36f,.46f,backgroundOpacity) : new Color(.15f,.23f,.3f,backgroundOpacity);
        }
        int chapterId=bibleDataLoader.ChapterId(book,chapter);
        if (previousChapter!=null) previousChapter.interactable=chapterId>0;
        if (nextChapter!=null) nextChapter.interactable=chapterId<1188;
    }

    public void RefreshNavigation()
    {
        for (int i=0;i<fullscreenLabels.Length;i++) fullscreenLabels[i].text = fullscreen ? "Show window" : "Words only";
        int main = currentPanel == 1 || currentPanel == 2 || currentPanel == 7 ? 1 : currentPanel == 3 ? 2 : currentPanel == 4 || currentPanel == 5 || (currentPanel == 8 && copyingNote) ? 3 : currentPanel == 9 ? 4 : currentPanel == 6 ? 5 : 0;
        if (navigationImages != null)
            for (int i = 0; i < navigationImages.Length; i++)
            {
                int selected = navigationGroups[i] == 0 ? main : navigationGroups[i] == 1 ? (currentPanel == 5 || (currentPanel == 8 && copyingNote) ? 1 : savedMode) : navigationGroups[i] == 2 ? settingsSection : testament;
                bool active = navigationValues[i] == selected;
                float alpha = fullscreen && currentPanel == 0 ? 0f : backgroundOpacity;
                navigationImages[i].color = active ? new Color(.08f, .36f, .46f, alpha) : new Color(.15f, .23f, .30f, alpha);
                navigationLabels[i].text = active ? "[" + navigationCaptions[i] + "]" : navigationCaptions[i];
            }
        if (currentPanel == 8 && copyingNote) locationText.text = "Copy note - " + bibleDataLoader.Reference(editingVerse);
        else if (currentPanel == 0 || currentPanel == 8) locationText.text = bibleDataLoader.Reference(selectedVerse);
        else if (currentPanel == 1) locationText.text = testament == 0 ? "Old Testament - 39 books" : "New Testament - 27 books";
        else if (currentPanel == 2) locationText.text = bibleDataLoader.bookNames[navigationBook] + " - choose a chapter";
        else if (currentPanel == 7) locationText.text = bibleDataLoader.bookNames[navigationBook] + " " + (navigationChapter + 1) + " - choose a verse";
        else if (currentPanel == 3) locationText.text = "Search";
        else if (currentPanel == 5) locationText.text = "Notes - " + bibleDataLoader.Reference(editingVerse);
        else if (currentPanel == 6) locationText.text = "Settings";
        else if (currentPanel == 9) locationText.text = "Study session";
        if (presentation != null) presentation.MarkDirty();
    }

    private void SaveComfortPreset()
    {
        if (!restored) return;
        string key = Prefix + "preset." + comfortPreset + ".";
        PlayerData.SetBool(key + "saved", true);
        PlayerData.SetFloat(key + "x", viewOffset.x); PlayerData.SetFloat(key + "y", viewOffset.y); PlayerData.SetFloat(key + "z", viewOffset.z);
        Vector3 angles = viewRotation.eulerAngles;
        PlayerData.SetFloat(key + "pitch", angles.x); PlayerData.SetFloat(key + "yaw", angles.y); PlayerData.SetFloat(key + "roll", angles.z);
        PlayerData.SetFloat(key + "width", panelWidth); PlayerData.SetFloat(key + "height", panelHeight); PlayerData.SetFloat(key + "scale", panelScale);
        PlayerData.SetFloat(key + "strength", followStrength); PlayerData.SetInt(key + "font", fontSize);
        PlayerData.SetFloat(key + "distance", panelDistance); PlayerData.SetInt(key + "source", followSource);
    }

    private void ApplyComfortPreset(int preset)
    {
        if (windowHandle != null) windowHandle.Release();
        if (hasViewPlacement) SaveComfortPreset();
        comfortPreset = preset;
        string key = Prefix + "preset." + preset + ".";
        VRCPlayerApi player = Networking.LocalPlayer;
        bool saved = PlayerData.GetBool(player, key + "saved");
        follow = true;
        followSource = saved ? Mathf.Clamp(PlayerData.GetInt(player, key + "source"), 0, 2) : preset > 1 ? preset - 1 : 0;
        panelWidth = saved ? Mathf.Clamp(PlayerData.GetFloat(player, key + "width"), .8f, 1.8f) : 1f;
        panelHeight = saved ? Mathf.Clamp(PlayerData.GetFloat(player, key + "height"), .8f, 1.8f) : 1f;
        panelScale = saved ? Mathf.Clamp(PlayerData.GetFloat(player, key + "scale"), .7f, 1.6f) : preset > 1 ? .7f : 1f;
        fontSize = saved ? Mathf.Clamp(PlayerData.GetInt(player, key + "font"), 20, 42) : 26;
        panelDistance = saved ? Mathf.Clamp(PlayerData.GetFloat(player, key + "distance"), .6f, 1.5f) : .85f;
        followStrength = saved ? Mathf.Clamp01(PlayerData.GetFloat(player, key + "strength")) : preset > 1 ? 1f : .65f;
        viewOffset = saved ? new Vector3(PlayerData.GetFloat(player, key + "x"), PlayerData.GetFloat(player, key + "y"), PlayerData.GetFloat(player, key + "z")) : preset > 1 ? new Vector3(0f, .12f, .4f) : new Vector3(0f, preset == 1 ? -.24f : -.12f, panelDistance);
        viewRotation = saved ? Quaternion.Euler(PlayerData.GetFloat(player, key + "pitch"), PlayerData.GetFloat(player, key + "yaw"), PlayerData.GetFloat(player, key + "roll")) : Quaternion.identity;
        hasPlacement = true; hasViewPlacement = true;
        if (follower != null) follower.SnapToAnchor();
        SettingsChanged();
        statusText.text = "Comfort preset loaded. Adjust its placement and size; your changes are remembered for this preset.";
    }

    public void Publish()
    {
        if (sharedView != null && restored) sharedView.PublishReading();
        if (studyHost != null && restored) studyHost._PublishSelection();
        if (presentation != null) presentation.MarkDirty();
    }

    public void FollowStrengthChanged()
    {
        if (!restored || followSlider == null || Mathf.Abs(followStrength - followSlider.value) < .001f) return;
        followStrength = Mathf.Clamp01(followSlider.value);
        PlayerData.SetFloat(Prefix + "followStrength", followStrength);
        UpdateSettingsText();
        if (hasViewPlacement) SaveComfortPreset();
        if (presentation != null) presentation.MarkDirty();
    }

    public float FollowBlend(float seconds)
    {
        if (followStrength >= .999f) return 1f;
        float strength = Mathf.Clamp01(followStrength);
        return 1f - Mathf.Exp(-(1.2f + 38f * strength * strength) * Mathf.Max(0f, seconds));
    }
}
