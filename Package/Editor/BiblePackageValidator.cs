using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace VRChatBibleUI.Editor
{
    public static class BiblePackageValidator
    {
        public static void Validate(BibleController reader)
        {
            if (reader == null || reader.panels.Length != 10 || reader.verseActions == null || reader.bookmarkName == null || reader.studyHost == null || reader.followSlider == null || reader.chapterReader == null || reader.chapterReader.labels.Length != 176 || reader.chapterButtons.Length != 150 || reader.verseButtons.Length != 176 || reader.bibleDataLoader.verseFile == null || reader.presentation == null || reader.windowHandle == null || reader.manipulator == null || reader.manipulator.handles.Length != 8 || reader.follower == null || reader.settingsSections.Length != 4 || reader.presentation.bookColors.Length != 66)
                throw new InvalidOperationException("Bible reader setup incomplete.");
            if (reader.layoutVersion < 15 || reader.navigationImages.Length != 16 || reader.navigationLabels.Length != 16 || reader.navigationGroups.Length != 16 || reader.navigationValues.Length != 16 || reader.navigationCaptions.Length != 16 || reader.bookmarkEditor == null || reader.libraryList == null || reader.libraryHintLayout == null || reader.searchStop == null || reader.searchClear == null || reader.searchScopeLabel == null || reader.searchResultsScroll == null || reader.noteStatus == null || reader.noteSave == null || reader.noteCopy == null || reader.noteRevert == null || reader.notePaste == null || reader.noteDelete == null || reader.copyHeading == null || reader.copyReturnLabel == null || reader.copyPasteControl == null)
                throw new InvalidOperationException("Navigation state or focused Library setup incomplete.");
            if (reader.bookLabels.Length!=66 || reader.chapterLabels.Length!=150 || reader.chapterImages.Length!=150 || reader.verseLabels.Length!=176 || reader.verseImages.Length!=176 || reader.previousChapter==null || reader.nextChapter==null)
                throw new InvalidOperationException("Current-passage browser or chapter boundary controls incomplete.");
            if (reader.bookScrolls.Length != 2 || reader.bookScrolls.Any(s => s == null) || reader.chapterPickerScroll == null || reader.versePickerScroll == null || reader.bookTargets.Length != 66 || reader.chapterTargets.Length != 150 || reader.verseTargets.Length != 176 || reader.bookTargets.Concat(reader.chapterTargets).Concat(reader.verseTargets).Any(r => r == null))
                throw new InvalidOperationException("Browser scroll restoration setup incomplete.");
            if (reader.referenceInput.onSubmit.GetPersistentEventCount() != 1 || reader.searchInput.onSubmit.GetPersistentEventCount() != 1)
                throw new InvalidOperationException("Reference and search keyboard submission must use their existing button actions.");
            if (reader.layoutVersion < 16 || reader.bookmarkSave == null || reader.bookmarkRemove == null || reader.bookmarkSaveCaption == null || reader.bookmarkName.onSubmit.GetPersistentEventCount() != 1 || reader.bookmarkName.onValueChanged.GetPersistentEventCount() != 1)
                throw new InvalidOperationException("Bookmark editing controls or keyboard submission incomplete.");
            if (reader.layoutVersion < 19 || reader.fullscreenLabels.Length != 2 || reader.fullscreenLabels.Any(label=>label==null) || reader.chapterReader.labels.Any(label=>!label.enableWordWrapping || label.enableAutoSizing) || reader.presentation.cardImages.Length < 20 || reader.presentation.fieldImages.Length < 5 || reader.presentation.mutedTexts.Length < 20 || reader.mainPanel.transform.Find("Surface/Navigation rail") == null || reader.mainPanel.transform.Find("Surface/Passage header") == null)
                throw new InvalidOperationException("Redesigned navigation, header, or theme roles are incomplete.");
            if (reader.bibleDataLoader.verseFile.text.Split('\n').Length != 31102) throw new InvalidDataException("Verse count mismatch.");
            foreach (var action in reader.GetComponentsInChildren<BibleUIAction>(true))
                if (action.controller == null) throw new InvalidOperationException("Unwired Bible UI action: " + action.name);
            var bookChoices = reader.GetComponentsInChildren<BibleUIAction>(true).Where(a => a.action == "Book").Select(a => a.index).OrderBy(i => i).ToArray();
            if (!bookChoices.SequenceEqual(Enumerable.Range(0, 66))) throw new InvalidOperationException("Book browser must expose every canonical book exactly once.");
            Debug.Log("[Bible] VALIDATED: complete KJV and reader controls.");
        }
    }
}
