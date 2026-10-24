using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BibleWindowPresentation : UdonSharpBehaviour
{
    public BibleController controller;
    public RectTransform window;
    public RectTransform readingPanel;
    public RectTransform scriptureViewport;
    public BoxCollider windowCollider;
    public GameObject[] normalChrome;
    public GameObject[] normalReadingControls;
    public GameObject minimalNavigation;
    public GameObject referenceControls;
    public Image[] chromeImages;
    public Color[] chromeColors;
    public GridLayoutGroup[] grids;
    public GridLayoutGroup[] backGrids;
    public GameObject back;
    public GameObject[] frontObjects;
    public GameObject[] backObjects;
    public TMP_Text[] frontTexts;
    public TMP_Text[] backTexts;
    public Image[] frontImages;
    public Image[] backImages;
    public RectTransform[] frontScrollContents;
    public RectTransform[] backScrollContents;
    public RectTransform[] frontDynamicRects;
    public RectTransform[] backDynamicRects;
    public TMP_Text backScripture;
    public Slider[] frontSliders;
    public Slider[] backSliders;
    public Material[] wordMaterials;
    public Image[] bookImages;
    public Color[] bookColors;
    public TMP_Text[] bookLabels;
    public Image[] cardImages;
    public Image[] fieldImages;
    public TMP_Text[] mutedTexts;
    private bool refreshPending;

    public void Apply()
    {
        Resize();
        bool minimal = controller.fullscreen && controller.panels[0].activeSelf;
        if (controller.manipulator != null) controller.manipulator.RefreshHandles();
        for (int i = 0; i < normalChrome.Length; i++) normalChrome[i].SetActive(!minimal);
        for (int i = 0; i < normalReadingControls.Length; i++) normalReadingControls[i].SetActive(!minimal);
        minimalNavigation.SetActive(minimal);
        LayoutReading();
        float opacity = minimal ? 0f : controller.backgroundOpacity;
        for (int i = 0; i < controller.backgrounds.Length; i++)
        {
            Color color = controller.backgrounds[i].color;
            color.a = opacity;
            controller.backgrounds[i].color = color;
        }
        for (int i = 0; i < chromeImages.Length; i++)
        {
            Color color = chromeColors[i];
            color.a *= opacity;
            chromeImages[i].color = color;
        }
        Color card = controller.theme == 1 ? new Color(.95f,.92f,.84f,opacity) : controller.theme == 2 ? new Color(0f,0f,0f,opacity) : new Color(.085f,.115f,.145f,opacity);
        Color field = controller.theme == 1 ? new Color(1f,1f,.98f,opacity) : controller.theme == 2 ? new Color(0f,0f,0f,opacity) : new Color(.10f,.145f,.18f,opacity);
        Color muted = controller.theme == 1 ? new Color(.32f,.29f,.23f,1f) : controller.theme == 2 ? Color.white : new Color(.70f,.77f,.79f,1f);
        for (int i=0;i<cardImages.Length;i++) cardImages[i].color=card;
        for (int i=0;i<fieldImages.Length;i++) fieldImages[i].color=field;
        for (int i=0;i<mutedTexts.Length;i++) mutedTexts[i].color=muted;
        for (int i = 0; i < bookImages.Length; i++)
        {
            Color color = bookColors[i];
            if (!controller.originalColors) color = new Color(color.grayscale, color.grayscale, color.grayscale, color.a);
            color.a *= opacity;
            bookImages[i].color = color;
            bookLabels[i].color = controller.BookTextColor(color);
        }
        for (int i = 0; i < controller.chapterReader.labels.Length; i++)
        {
            Color words = controller.chapterReader.labels[i].color;
            words.a = controller.textOpacity;
            controller.chapterReader.labels[i].color = words;
        }
        bool outline = controller.outlineMode == 1 || (controller.outlineMode == 0 && opacity < 0.3f);
        for (int i = 0; i < frontTexts.Length; i++)
        {
            if (frontTexts[i] == controller.verseDisplayText) backScripture = backTexts[i];
            Material material = wordMaterials[outline ? (frontTexts[i].color.grayscale > 0.5f ? 1 : 2) : 0];
            if (frontTexts[i].fontSharedMaterial != material) frontTexts[i].fontSharedMaterial = material;
        }
        back.SetActive(controller.isOpen && controller.showBack);
        PositionBack();
        MarkDirty();
    }

    public void LayoutReading()
    {
        bool minimal = controller.fullscreen && controller.panels[0].activeSelf;
        readingPanel.offsetMin = new Vector2(minimal ? 25f : 198f, minimal ? 15f : 62f);
        readingPanel.offsetMax = new Vector2(minimal ? -25f : -28f, minimal ? -15f : -124f);
        float bottom = 60f + (controller.actionsVisible ? 108f : 0f) + (controller.toolsVisible ? 116f : 0f);
        scriptureViewport.offsetMin = new Vector2(0f, bottom);
        scriptureViewport.offsetMax = new Vector2(0f, controller.rangeSelection ? -64f : -38f);
        controller.readingHint.gameObject.SetActive(controller.rangeSelection);
        if (referenceControls != null) referenceControls.SetActive(!controller.panels[0].activeSelf);
        if (controller.verseActions != null)
        {
            RectTransform bar = (RectTransform)controller.verseActions.transform;
            bar.anchoredPosition = new Vector2(0f, 60f + (controller.toolsVisible ? 116f : 0f));
        }
    }

    public void Resize()
    {
        window.sizeDelta = new Vector2(1080f * controller.panelWidth, 860f * controller.panelHeight);
        windowCollider.size = new Vector3(window.sizeDelta.x, window.sizeDelta.y, 1f);
        for (int i = 0; i < grids.Length; i++)
        {
            int columns = grids[i].constraintCount;
            grids[i].cellSize = new Vector2((window.sizeDelta.x - 250f - (columns - 1) * grids[i].spacing.x) / columns, grids[i].cellSize.y);
            backGrids[i].cellSize = grids[i].cellSize;
        }
        ((RectTransform)back.transform).sizeDelta = window.sizeDelta;
    }

    public void MarkDirty()
    {
        if (refreshPending) return;
        refreshPending = true;
        SendCustomEventDelayedFrames(nameof(RefreshBack), 2);
    }

    public void RefreshBack()
    {
        refreshPending = false;
        if (!controller.showBack) return;
        for (int i = 1; i < frontObjects.Length; i++)
            if (backObjects[i].activeSelf != frontObjects[i].activeSelf) backObjects[i].SetActive(frontObjects[i].activeSelf);
        for (int i = 0; i < frontTexts.Length; i++)
        {
            if (frontTexts[i] == controller.verseDisplayText) backScripture = backTexts[i];
            if (backTexts[i].enabled != frontTexts[i].enabled) backTexts[i].enabled = frontTexts[i].enabled;
            if (backTexts[i].text != frontTexts[i].text) backTexts[i].text = frontTexts[i].text;
            backTexts[i].color = frontTexts[i].color;
            if (backTexts[i].fontSize != frontTexts[i].fontSize) backTexts[i].fontSize = frontTexts[i].fontSize;
            if (backTexts[i].lineSpacing != frontTexts[i].lineSpacing) backTexts[i].lineSpacing = frontTexts[i].lineSpacing;
            if (backTexts[i].fontSharedMaterial != frontTexts[i].fontSharedMaterial) backTexts[i].fontSharedMaterial = frontTexts[i].fontSharedMaterial;
        }
        for (int i = 0; i < frontImages.Length; i++) backImages[i].color = frontImages[i].color;
        for (int i = 0; i < frontSliders.Length; i++) backSliders[i].value = frontSliders[i].value;
        for (int i = 0; i < grids.Length; i++) backGrids[i].cellSize = grids[i].cellSize;
        for (int i = 0; i < frontDynamicRects.Length; i++)
        {
            backDynamicRects[i].offsetMin = frontDynamicRects[i].offsetMin;
            backDynamicRects[i].offsetMax = frontDynamicRects[i].offsetMax;
        }
    }

    private void LateUpdate()
    {
        bool visible = controller.isOpen && controller.showBack;
        if (back.activeSelf != visible) back.SetActive(visible);
        if (!visible) return;
        PositionBack();
        // Only scroll offsets need continuous updates; the full mirror refreshes after changes.
        for (int i = 0; i < frontScrollContents.Length; i++)
        {
            backScrollContents[i].anchoredPosition = frontScrollContents[i].anchoredPosition;
            backScrollContents[i].anchorMin = frontScrollContents[i].anchorMin;
            backScrollContents[i].anchorMax = frontScrollContents[i].anchorMax;
            backScrollContents[i].sizeDelta = frontScrollContents[i].sizeDelta;
        }
    }

    private void PositionBack()
    {
        Transform front = controller.mainPanel.transform;
        back.transform.SetPositionAndRotation(front.position + front.forward * 0.004f, front.rotation * Quaternion.Euler(0, 180, 0));
        back.transform.localScale = front.localScale;
    }
}
