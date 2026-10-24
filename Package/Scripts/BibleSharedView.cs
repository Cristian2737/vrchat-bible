using UdonSharp;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class BibleSharedView : UdonSharpBehaviour
{
    public BibleController controller;
    public BibleDataLoader data;
    public GameObject visual;
    public TMP_Text heading;
    public TMP_Text text;
    public Image background;
    public GameObject backVisual;
    public Image backBackground;
    public TMP_Text backHeading;
    public TMP_Text backText;
    public RectTransform textRect;
    public RectTransform backTextRect;
    public Material[] wordMaterials;
    [UdonSynced] public bool visible;
    [UdonSynced] public int firstVerse;
    [UdonSynced] public int endVerse;
    [UdonSynced] public int selectedVerse;
    [UdonSynced] public int selectionEnd;
    [UdonSynced] public Vector3 panelPosition;
    [UdonSynced] public Quaternion panelRotation;
    [UdonSynced] public float scale = 1f;
    [UdonSynced] public float width = 1f;
    [UdonSynced] public float height = 1f;
    [UdonSynced] public float backgroundOpacity = 1f;
    [UdonSynced] public float textOpacity = 1f;
    [UdonSynced] public bool showBack = true;
    [UdonSynced] public bool fullscreen;
    [UdonSynced] public int theme;
    [UdonSynced] public bool outline;
    [UdonSynced] public float readingSize = 26f;
    [UdonSynced] public bool wideSpacing;
    private VRCPlayerApi owner;
    private float nextPublish;
    private bool retry;

    private void Start()
    {
        owner = Networking.GetOwner(gameObject);
        visual.SetActive(false);
        if (Utilities.IsValid(owner) && owner.isLocal)
        {
            controller.sharedView = this;
            PublishReading();
        }
        else Refresh();
    }

    public void PublishReading()
    {
        if (!Utilities.IsValid(owner) || !owner.isLocal || !controller.restored) return;
        visible = controller.share && controller.isOpen;
        firstVerse = visible ? controller.chapterReader.firstVisible : 0;
        endVerse = visible ? Mathf.Max(firstVerse + 1, controller.chapterReader.visibleEnd) : 0;
        selectedVerse = controller.selectedVerse;
        selectionEnd = controller.selectionEnd;
        panelPosition = controller.mainPanel.transform.position;
        panelRotation = controller.mainPanel.transform.rotation;
        scale = controller.panelScale;
        width = controller.panelWidth;
        readingSize = controller.ReadingFontSize();
        wideSpacing = controller.wideSpacing;
        height = controller.panelHeight;
        backgroundOpacity = controller.backgroundOpacity;
        textOpacity = controller.textOpacity;
        showBack = controller.showBack;
        fullscreen = controller.fullscreen;
        theme = controller.theme;
        outline = controller.outlineMode == 1 || (controller.outlineMode == 0 && (controller.fullscreen || controller.backgroundOpacity < 0.3f));
        RequestSerialization();
        visual.SetActive(false);
    }

    private void Update()
    {
        if (!Utilities.IsValid(owner)) return;
        if (owner.isLocal)
        {
            if (!controller.isOpen || !controller.share || Time.time < nextPublish) return;
            if (!retry && Vector3.SqrMagnitude(controller.mainPanel.transform.position - panelPosition) < 0.0025f && Quaternion.Angle(controller.mainPanel.transform.rotation, panelRotation) < 3f) return;
            nextPublish = Time.time + 0.5f;
            retry = false;
            PublishReading();
        }
        else if (visible && visual.activeSelf)
        {
            visual.transform.position = Vector3.Lerp(visual.transform.position, panelPosition, 1f - Mathf.Exp(-12f * Time.deltaTime));
            visual.transform.rotation = Quaternion.Slerp(visual.transform.rotation, panelRotation, 1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }

    public override void OnPostSerialization(VRC.Udon.Common.SerializationResult result)
    {
        if (!result.success)
        {
            retry = true;
            SendCustomEventDelayedSeconds(nameof(RetryPublish), 1f);
        }
    }

    public void RetryPublish() { if (retry) { retry = false; PublishReading(); } }
    public override void OnDeserialization() { Refresh(); }

    public void Refresh()
    {
        if (!Utilities.IsValid(owner)) owner = Networking.GetOwner(gameObject);
        if (!Utilities.IsValid(owner) || owner.isLocal || !visible || firstVerse < 0 || endVerse <= firstVerse || endVerse > 31102 || endVerse - firstVerse > 176)
        {
            visual.SetActive(false);
            return;
        }
        data.LoadBibleData();
        if (data.verses == null) return;
        int low = Mathf.Clamp(Mathf.Min(selectedVerse, selectionEnd), 0, 31101);
        int high = Mathf.Clamp(Mathf.Max(selectedVerse, selectionEnd), low, 31101);
        heading.text = owner.displayName + " — selected " + data.Reference(low) + (high > low ? "–" + data.Reference(high) : "") + " (KJV)";
        int b = data.BookForVerse(firstVerse);
        int c = data.ChapterForVerse(b, firstVerse);
        int start = data.VerseStart(b, c);
        string scripture = "";
        for (int i = firstVerse; i < endVerse; i++)
        {
            string line = "<b>" + (i - start + 1) + "</b> " + data.verses[i];
            if (i >= low && i <= high) line = "<mark=#82BEE62B>" + line + "</mark>";
            scripture += line + "\n\n";
        }
        text.enableAutoSizing = false;
        backText.enableAutoSizing = false;
        // The local viewport can include part of its final verse. Keep the remote
        // excerpt inside its window too, including at large accessibility sizes.
        text.overflowMode = TextOverflowModes.Truncate;
        backText.overflowMode = TextOverflowModes.Truncate;
        text.fontSize = Mathf.Clamp(readingSize, 18f, 54f);
        backText.fontSize = text.fontSize;
        text.lineSpacing = wideSpacing ? 16f : 4f;
        backText.lineSpacing = text.lineSpacing;
        text.text = scripture;
        backText.text = scripture;
        backHeading.text = heading.text;
        backVisual.SetActive(showBack);
        heading.gameObject.SetActive(true);
        backHeading.gameObject.SetActive(true);
        Color paper = theme == 1 ? new Color(1f, 0.98f, 0.9f) : theme == 2 ? Color.black : new Color(0.055f, 0.075f, 0.10f);
        paper.a = fullscreen ? 0f : Mathf.Clamp01(backgroundOpacity);
        background.color = paper;
        backBackground.color = paper;
        Color words = theme == 1 ? new Color(0.08f, 0.06f, 0.02f) : Color.white;
        heading.color = words;
        backHeading.color = words;
        words.a = Mathf.Clamp01(textOpacity);
        text.color = words;
        backText.color = words;
        text.fontSharedMaterial = wordMaterials[outline ? (theme == 1 ? 2 : 1) : 0];
        backText.fontSharedMaterial = text.fontSharedMaterial;
        textRect.offsetMax = new Vector2(-35, -90);
        backTextRect.offsetMax = textRect.offsetMax;
        ((RectTransform)visual.transform).sizeDelta = new Vector2(1080f * Mathf.Clamp(width, 0.8f, 1.8f), 860f * Mathf.Clamp(height, 0.8f, 1.8f));
        visual.transform.position = panelPosition;
        visual.transform.rotation = panelRotation;
        visual.transform.localScale = Vector3.one * (0.0008f * Mathf.Clamp(scale, 0.7f, 1.6f));
        visual.SetActive(true);
    }
}
