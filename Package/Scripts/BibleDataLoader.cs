using UdonSharp;
using UnityEngine;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BibleDataLoader : UdonSharpBehaviour
{
    public TextAsset verseFile;
    public string[] bookNames;
    public int[] bookChapterStarts;
    public int[] chapterVerseStarts;
    [System.NonSerialized] public string[] verses;

    public void LoadBibleData()
    {
        if (verses != null && verses.Length == 31102) return;
        if (verseFile == null || bookNames.Length != 66 || bookChapterStarts.Length != 67 || chapterVerseStarts.Length != 1190)
        {
            Debug.LogError("[Bible] Incomplete data. Run Tools > VRChat Bible UI > Upgrade Existing World.");
            return;
        }
        verses = verseFile.text.Split('\n');
        if (verses.Length != 31102) Debug.LogError("[Bible] Expected 31,102 KJV verses.");
    }
    public int ChapterCount(int book) { return bookChapterStarts[book + 1] - bookChapterStarts[book]; }
    public int ChapterId(int book, int chapter) { return bookChapterStarts[book] + chapter; }
    public int VerseStart(int book, int chapter) { return chapterVerseStarts[ChapterId(book, chapter)]; }
    public int VerseCount(int book, int chapter)
    {
        int id = ChapterId(book, chapter);
        return chapterVerseStarts[id + 1] - chapterVerseStarts[id];
    }
    public int BookForVerse(int id)
    {
        for (int b = 0; b < 66; b++)
            if (id < chapterVerseStarts[bookChapterStarts[b + 1]]) return b;
        return 65;
    }
    public int ChapterForVerse(int book, int id)
    {
        for (int c = 0; c < ChapterCount(book); c++)
            if (id < chapterVerseStarts[ChapterId(book, c) + 1]) return c;
        return ChapterCount(book) - 1;
    }
    public string Reference(int id)
    {
        int b = BookForVerse(id);
        int c = ChapterForVerse(b, id);
        return bookNames[b] + " " + (c + 1) + ":" + (id - VerseStart(b, c) + 1);
    }
}
