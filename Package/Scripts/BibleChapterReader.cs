using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Unity's layout system measures the real font and wraps each complete verse.
// No guessed character counts, fixed verse batches, or per-frame text rebuilding.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BibleChapterReader : UdonSharpBehaviour
{
    public BibleController controller;
    public TMP_Text[] labels;
    public Image[] selectionImages;
    public RectTransform[] rows;
    public ScrollRect scroll;
    public RectTransform content;
    public RectTransform viewport;
    public int firstVisible;
    public int visibleEnd;
    public int anchorVerse;
    public float anchorOffset;
    private bool pending;
    private float nextSample;

    public void Reflow()
    {
        int start = controller.bibleDataLoader.VerseStart(controller.book, controller.chapter);
        int end = start + controller.bibleDataLoader.VerseCount(controller.book, controller.chapter);
        if (firstVisible < start || firstVisible >= end) { firstVisible = anchorVerse; visibleEnd = Mathf.Min(end, anchorVerse + 1); }
        if (pending) return;
        pending = true;
        SendCustomEventDelayedFrames(nameof(RestoreAnchor), 4);
    }

    public void RestoreAnchor()
    {
        pending = false;
        if (!controller.mainPanel.activeInHierarchy || !controller.panels[0].activeSelf) return;
        int start = controller.bibleDataLoader.VerseStart(controller.book, controller.chapter);
        int count = controller.bibleDataLoader.VerseCount(controller.book, controller.chapter);
        int index = Mathf.Clamp(anchorVerse - start, 0, count - 1);
        float extent = Mathf.Max(0f, content.rect.height - viewport.rect.height);
        float offset = -rows[index].anchoredPosition.y + anchorOffset;
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = extent > 0f ? 1f - Mathf.Clamp01(offset / extent) : 1f;
        Sample();
        controller.Publish();
    }

    private void Update()
    {
        if (pending || !controller.restored || !controller.isOpen || !controller.panels[0].activeSelf || Time.time < nextSample) return;
        nextSample = Time.time + 0.35f;
        int previous = firstVisible;
        int previousEnd = visibleEnd;
        Sample();
        if (previous != firstVisible || previousEnd != visibleEnd) controller.Publish();
    }

    public void Sample()
    {
        if (pending) return;
        int start = controller.bibleDataLoader.VerseStart(controller.book, controller.chapter);
        int count = controller.bibleDataLoader.VerseCount(controller.book, controller.chapter);
        float top = Mathf.Max(0f, content.anchoredPosition.y);
        float bottom = top + viewport.rect.height;
        firstVisible = start;
        visibleEnd = start + count;
        for (int i = 0; i < count; i++)
        {
            float y = -rows[i].anchoredPosition.y;
            if (y + rows[i].rect.height <= top) firstVisible = start + i + 1;
            if (y >= bottom) { visibleEnd = start + i; break; }
        }
        firstVisible = Mathf.Clamp(firstVisible, start, start + count - 1);
        visibleEnd = Mathf.Clamp(visibleEnd, firstVisible + 1, start + count);
        anchorVerse = firstVisible;
        anchorOffset = Mathf.Max(0f, top + rows[firstVisible - start].anchoredPosition.y);
    }
}
