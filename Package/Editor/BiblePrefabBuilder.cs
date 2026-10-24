using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using UdonSharp;
using UdonSharp.Compiler;
using UdonSharpEditor;
using VRC.Udon;
using VRC.SDK3.Components;

namespace VRChatBibleUI.Editor
{
    public static class BiblePrefabBuilder
    {
        private static BibleController controller;
        private static TMP_FontAsset font;
        private static Material faceFont;
        private static Material faceImage;
        private static Sprite rounded;
        private static readonly List<Image> cards = new List<Image>();
        private static readonly List<Image> fields = new List<Image>();
        private static readonly List<TMP_Text> mutedTexts = new List<TMP_Text>();
        private static readonly List<TMP_Text> texts = new List<TMP_Text>();
        private static readonly List<Image> backgrounds = new List<Image>();
        private static readonly List<Image> tabImages = new List<Image>();
        private static readonly List<TMP_Text> tabLabels = new List<TMP_Text>();
        private static readonly List<string> tabCaptions = new List<string>();
        private static readonly List<int> tabGroups = new List<int>();
        private static readonly List<int> tabValues = new List<int>();
        private static Transform systemParent;
        private static GameObject sharedTemplate;
        [Serializable] private class Index
        {
            public string[] bookNames = Array.Empty<string>();
            public int[] bookChapterStarts = Array.Empty<int>();
            public int[] chapterVerseStarts = Array.Empty<int>();
        }

        public static string PackageRoot
        {
            get
            {
                var paths = AssetDatabase.FindAssets("BiblePrefabBuilder t:MonoScript").Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => path.EndsWith("/Editor/BiblePrefabBuilder.cs", StringComparison.Ordinal)).ToArray();
                if (paths.Length != 1) throw new InvalidOperationException("Keep one installation of VRChat Bible in this project. Found " + paths.Length + " builders.");
                return paths[0].Substring(0, paths[0].Length - "/Editor/BiblePrefabBuilder.cs".Length);
            }
        }

        public static string AssetPath(string relative) { return PackageRoot + "/" + relative; }

        private static T Required<T>(string relative) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetPath(relative));
            if (asset == null) throw new InvalidOperationException("VRChat Bible asset missing: " + relative + ". Reimport the complete package.");
            return asset;
        }

        public static void CompileUdon()
        {
            UdonSharpCompilerV1.CompileSync(new UdonSharpCompileOptions { IsEditorBuild = true });
            if (UdonSharpProgramAsset.AnyUdonSharpScriptHasError()) throw new InvalidOperationException("UdonSharp compilation failed. See the Unity Console; installation is not marked complete.");
            foreach (var program in UdonSharpProgramAsset.GetAllUdonSharpPrograms())
                if (AssetDatabase.GetAssetPath(program).StartsWith(PackageRoot + "/") && program.CompiledVersion != UdonSharpProgramVersion.CurrentVersion)
                    throw new InvalidOperationException("Bible Udon program did not compile: " + program.name);
            AssetDatabase.SaveAssets();
        }

        public static void EnsureProgramAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { AssetPath("Scripts") }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                Type type = script.GetClass();
                if (type == null || !typeof(UdonSharpBehaviour).IsAssignableFrom(type)) continue;
                string programPath = Path.ChangeExtension(path, ".asset");
                if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(programPath) != null) continue;
                throw new InvalidOperationException("Missing UdonSharp program asset: " + programPath + ". Reimport the complete package.");
            }
            CompileUdon();
        }

        public static BibleController Build(GameObject launcher, Transform parent = null)
        {
            if (launcher == null || launcher.GetComponent<BoxCollider>() == null) throw new ArgumentException("A launcher with a BoxCollider is required.");
            systemParent = parent;
            sharedTemplate = null;
            texts.Clear(); backgrounds.Clear(); cards.Clear(); fields.Clear(); mutedTexts.Clear();
            tabImages.Clear(); tabLabels.Clear(); tabCaptions.Clear(); tabGroups.Clear(); tabValues.Clear();
            rounded = Required<Sprite>("Materials/BibleRounded.png");
            font = Required<TMP_FontAsset>("Fonts/BibleLiberationSDF.asset");
            faceFont = Required<Material>("Materials/Portable/BibleFaceFont.mat");
            faceImage = Required<Material>("Materials/BibleWindowFace.mat");
            var index = JsonUtility.FromJson<Index>(Required<TextAsset>("Data/CompleteKJV/index.json").text);
            if (index.bookNames.Length != 66 || index.chapterVerseStarts.Length != 1190) throw new InvalidDataException("KJV index incomplete.");
            Required<TextAsset>("Data/CompleteKJV/verses.txt");
            Required<TextAsset>("Data/BookPalette.json");
            GameObject root = new GameObject("Personal Bible Reader");
            if (parent != null) root.transform.SetParent(parent, false);
            controller = root.AddUdonSharpComponent<BibleController>();
            controller.layoutVersion = 19;
            controller.presentation = root.AddUdonSharpComponent<BibleWindowPresentation>();
            controller.presentation.controller = controller;
            var wordMaterials = new Material[] { faceFont, null, null };
            for (int i = 1; i < wordMaterials.Length; i++)
            {
                wordMaterials[i] = Required<Material>("Materials/Portable/BibleWordsOutline" + (i == 1 ? "Dark" : "Light") + ".mat");
            }
            controller.presentation.wordMaterials = wordMaterials;
            var data = root.AddUdonSharpComponent<BibleDataLoader>();
            data.verseFile = Required<TextAsset>("Data/CompleteKJV/verses.txt");
            data.bookNames = index.bookNames;
            data.bookChapterStarts = index.bookChapterStarts;
            data.chapterVerseStarts = index.chapterVerseStarts;
            controller.bibleDataLoader = data;
            controller.audioSource = root.AddComponent<AudioSource>();
            controller.audioSource.clip = Required<AudioClip>("Audio/PortableBibleClick.wav");
            controller.audioSource.playOnAwake = false;
            controller.audioSource.volume = 0.25f;
            var follower = root.AddUdonSharpComponent<BiblePanelFollower>();
            follower.controller = controller;
            controller.follower = follower;
            controller.manipulator = root.AddUdonSharpComponent<BibleWindowManipulator>();
            controller.manipulator.controller = controller;
            var pickup = launcher.GetComponent<VRC.SDK3.Components.VRCPickup>();
            if (pickup != null) pickup.pickupable = false;
            var rb = launcher.GetComponent<Rigidbody>();
            if (rb != null) { rb.useGravity = false; rb.isKinematic = true; }
            var interaction = launcher.GetComponent<BiblePickup>();
            if (interaction == null) interaction = launcher.AddUdonSharpComponent<BiblePickup>();
            interaction.bibleController = controller;
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(interaction);
            udon.interactText = "Open / close your Bible";
            udon.proximity = 3f;
            launcher.GetComponent<BoxCollider>().size = new Vector3(0.3f, 0.2f, 0.08f);

            GameObject canvas = Canvas(root.transform, "Personal Bible", 1080, 860, true);
            controller.mainPanel = canvas;
            controller.presentation.window = canvas.GetComponent<RectTransform>();
            controller.presentation.windowCollider = canvas.GetComponent<BoxCollider>();
            canvas.transform.position = launcher.transform.position + Vector3.up * 0.4f;
            RectTransform surface = Panel(canvas.transform, "Surface", 0, 0, 1080, 860);
            Stretch(surface, 0, 0, 0, 0);
            var rail = Panel(surface, "Navigation rail", 18, 18, 156, 824);
            cards.Add(rail.GetComponent<Image>());
            var emblem = Rect(rail, "Open book emblem", 22, 22, 112, 32);
            Decoration(emblem, "Left leaf", 25, 0, 28, 30, new Color(.77f,.65f,.40f));
            Decoration(emblem, "Right leaf", 58, 0, 28, 30, new Color(.77f,.65f,.40f));
            var brand = Label(rail, "BIBLE\nSTUDY", 16, 69, 124, 68, 27);
            brand.fontStyle = FontStyles.Bold; brand.alignment = TextAlignmentOptions.Center; brand.lineSpacing = -7;
            var edition = Label(rail, "66 BOOKS\nKING JAMES", 12, 744, 132, 56, 15);
            edition.alignment = TextAlignmentOptions.Center; edition.characterSpacing = 1.5f;
            mutedTexts.Add(edition);
            edition.rectTransform.anchorMin = edition.rectTransform.anchorMax = Vector2.zero;
            edition.rectTransform.pivot = Vector2.zero; edition.rectTransform.anchoredPosition = new Vector2(12,18);
            var header = Rect(surface, "Passage header", 198, 22, 854, 78);
            var kicker = Label(header, "KING JAMES VERSION", 0, 0, 564, 24, 15);
            kicker.characterSpacing = 2.5f; mutedTexts.Add(kicker);
            controller.locationText = Label(header, "Your reading", 0, 28, 594, 44, 32);
            controller.locationText.fontStyle = FontStyles.Bold;
            controller.locationText.enableWordWrapping = false;
            controller.locationText.enableAutoSizing = true; controller.locationText.fontSizeMin = 20; controller.locationText.fontSizeMax = 32;
            controller.locationText.overflowMode = TextOverflowModes.Ellipsis;
            var fullscreen = Button(header, "Words only", 608, 13, 146, 48, "Fullscreen");
            controller.fullscreenLabels = new TMP_Text[2];
            controller.fullscreenLabels[0] = fullscreen.GetComponentInChildren<TMP_Text>();
            var close = Button(header, "Close", 766, 13, 88, 48, "Toggle");
            AnchorRight((RectTransform)fullscreen.transform, 100, 13, 146, 48);
            AnchorRight((RectTransform)close.transform, 0, 13, 88, 48);
            var divider = Decoration(surface, "Header divider", 198, 104, 854, 1, new Color(.43f,.54f,.57f,.35f));
            string[] tabs = { "Read", "Books", "Search", "Library", "Study", "Settings" };
            string[] actions = { "Read", "Books", "Search", "Saved", "Study", "Settings" };
            for (int i = 0; i < tabs.Length; i++)
            {
                Tab(rail, tabs[i], 12, 160 + i * 64, 132, 54, actions[i], i == 3 ? 2 : 0, 0, i);
            }
            var reference = Rect(surface, "Jump to passage", 25, 119, 1030, 46);
            controller.presentation.referenceControls = reference.gameObject;
            controller.referenceInput = Input(reference, "John 3:16 or John 3:16-18", 0, 0, 840, 46, false, 80);
            var go = Button(reference, "Go", 855, 0, 175, 46, "Reference");
            WireSubmit(controller.referenceInput, go);
            controller.statusText = Label(surface, "Waiting for your world save...", 198, 810, 854, 40, 18);
            controller.statusText.enableAutoSizing=true; controller.statusText.fontSizeMin=15; controller.statusText.fontSizeMax=18;
            mutedTexts.Add(controller.statusText);
            var statusRect = controller.statusText.rectTransform;
            statusRect.anchorMin = Vector2.zero;
            statusRect.anchorMax = new Vector2(1, 0);
            statusRect.pivot = Vector2.zero;
            statusRect.anchoredPosition = new Vector2(198, 10);
            statusRect.sizeDelta = new Vector2(-358, 40);
            var undo = Button(surface, "Undo", 930, 810, 125, 40, "Undo");
            var undoRect = (RectTransform)undo.transform;
            undoRect.anchorMin = undoRect.anchorMax = new Vector2(1,0); undoRect.pivot = new Vector2(1,0);
            undoRect.anchoredPosition = new Vector2(-25,10);
            controller.undoButton = undo.gameObject; undo.gameObject.SetActive(false);

            controller.panels = new GameObject[10];
            string[] names = { "Reading", "Books", "Chapters", "Search", "Saved", "Note editor", "Settings", "Verses", "Copy verses", "Study" };
            for (int i = 0; i < names.Length; i++)
            {
                var body = Rect(surface, names[i], 25, 180, 1030, 620);
                Stretch(body, 25, 180, 25, 60);
                controller.panels[i] = body.gameObject;
            }
            BuildReading();
            BuildNavigation(index);
            BuildSearch();
            BuildSaved();
            BuildNotes();
            BuildSettings();
            BuildStudy(root, launcher);
            controller.navigationImages = tabImages.ToArray(); controller.navigationLabels = tabLabels.ToArray();
            controller.navigationCaptions = tabCaptions.ToArray(); controller.navigationGroups = tabGroups.ToArray(); controller.navigationValues = tabValues.ToArray();
            controller.presentation.normalChrome = surface.Cast<Transform>().Where(t => !controller.panels.Contains(t.gameObject) && t.gameObject != controller.undoButton && t.gameObject != controller.presentation.referenceControls).Select(t => t.gameObject).ToArray();
            for (int i = 0; i < controller.panels.Length; i++) controller.panels[i].SetActive(i == 0);
            controller.backgrounds = backgrounds.ToArray();
            controller.interfaceTexts = texts.ToArray();
            MakeWidthResponsive(surface);
            rail.anchorMin = new Vector2(0,0); rail.anchorMax = new Vector2(0,1); rail.pivot = new Vector2(0,1);
            rail.offsetMin = new Vector2(18,18); rail.offsetMax = new Vector2(174,-18);
            Stretch(header, 198, 22, 28, 0); header.anchorMin = new Vector2(0,1); header.sizeDelta = new Vector2(-226,78); header.anchoredPosition = new Vector2(198,-22);
            var heading = controller.locationText.rectTransform;
            heading.anchorMin = new Vector2(0,1); heading.anchorMax = Vector2.one;
            heading.anchoredPosition = new Vector2(0,-28); heading.sizeDelta = new Vector2(-260,44);
            Stretch(divider,198,104,28,0); divider.anchorMin = new Vector2(0,1); divider.sizeDelta = new Vector2(-226,1); divider.anchoredPosition = new Vector2(198,-104);
            Stretch(reference,198,119,28,0); reference.anchorMin = new Vector2(0,1); reference.sizeDelta = new Vector2(-226,46); reference.anchoredPosition = new Vector2(198,-119);
            for (int i=0;i<controller.panels.Length;i++) Stretch((RectTransform)controller.panels[i].transform,198,180,28,62);
            controller.presentation.cardImages = cards.ToArray(); controller.presentation.fieldImages = fields.ToArray();
            controller.presentation.mutedTexts = mutedTexts.ToArray();
            BuildWindowPresentation(surface, root);
            BuildShared(root, data);
            // A visible launcher remains available for VR, touch, and users who disable shortcuts.
            var sign = Canvas(launcher.transform, "Bible launcher label", 500, 155, true);
            sign.transform.localPosition = new Vector3(0f, 0.15f, -0.055f);
            Panel(sign.transform, "Launcher background", 0, 0, 500, 155);
            Label(sign.transform, "YOUR BIBLE", 20, 12, 460, 40, 30);
            Label(sign.transform, "B | Double either grip | Either stick click", 20, 56, 460, 40, 21);
            Button(sign.transform, "Open / close", 95, 104, 310, 40, "Toggle");

            foreach (var item in new[] { root, launcher, sharedTemplate })
                foreach (var behaviour in item.GetComponentsInChildren<UdonSharpBehaviour>(true)) UdonSharpEditorUtility.CopyProxyToUdon(behaviour);
            controller.mainPanel.SetActive(false);
            return controller;
        }

        private static void BuildReading()
        {
            Transform p = controller.panels[0].transform;
            controller.selectionText = Label(p, "", 0, 0, 1030, 32, 23);
            controller.readingHint = Label(p, "", 0, 34, 1030, 28, 19);
            RectTransform content;
            var scroll = Scroll(p, "Scripture", 0, 72, 1030, 428, out content);
            controller.verseScrollRect = scroll;
            controller.presentation.readingPanel = (RectTransform)p;
            controller.presentation.scriptureViewport = scroll.GetComponent<RectTransform>();
            Stretch(controller.presentation.scriptureViewport, 0, 72, 0, 120);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 24, 0, 12);
            layout.spacing = 5;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            var reader = controller.gameObject.AddUdonSharpComponent<BibleChapterReader>();
            controller.chapterReader = reader;
            reader.controller = controller; reader.scroll = scroll; reader.content = content;
            reader.viewport = scroll.GetComponent<RectTransform>();
            reader.labels = new TMP_Text[176]; reader.rows = new RectTransform[176]; reader.selectionImages = new Image[176];
            for (int i = 0; i < 176; i++)
            {
                var button = Button(content, "", 0, 0, 990, 52, "SelectReadingVerse", i);
                var image = button.GetComponent<Image>(); image.color = Color.clear;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.pressedColor = Color.white;
                button.colors = colors;
                var rowLayout = button.gameObject.AddComponent<VerticalLayoutGroup>();
                rowLayout.padding = new RectOffset(10, 10, 8, 8);
                rowLayout.childControlWidth = rowLayout.childControlHeight = true;
                rowLayout.childForceExpandHeight = false;
                button.gameObject.AddComponent<LayoutElement>().minHeight = 52;
                var text = button.GetComponentInChildren<TMP_Text>();
                text.enableAutoSizing = false;
                text.enableWordWrapping = true;
                text.richText = true; text.alignment = TextAlignmentOptions.TopLeft;
                text.overflowMode = TextOverflowModes.Overflow;
                text.fontSize = 26;
                texts.Add(text);
                reader.labels[i] = text; reader.rows[i] = (RectTransform)button.transform; reader.selectionImages[i] = image;
                button.gameObject.SetActive(false);
            }
            controller.verseDisplayText = reader.labels[0];
            var next = Button(content, "", 0, 0, 990, 68, "ContinueChapter");
            next.gameObject.AddComponent<LayoutElement>().preferredHeight = 68;
            controller.continueLabel = next.GetComponentInChildren<TMP_Text>();
            var guide = Panel(scroll.viewport, "Reading guide", 0, 0, 980, 3);
            guide.anchorMin = new Vector2(0,.72f); guide.anchorMax = new Vector2(1,.72f);
            guide.anchoredPosition = Vector2.zero; guide.sizeDelta = new Vector2(0,3);
            var guideImage = guide.GetComponent<Image>(); guideImage.color = new Color(.3f,.7f,.8f,.55f);
            guideImage.raycastTarget = false; backgrounds.Remove(guideImage);
            controller.readingGuide = guide.gameObject; guide.gameObject.SetActive(false);

            var navigation = BottomBar(p, "Chapter navigation", 0, 54);
            controller.previousChapter = Button(navigation, "Chapter -", 0, 0, 240, 50, "ChapterStep", -1);
            controller.nextChapter = Button(navigation, "Chapter +", 255, 0, 240, 50, "ChapterStep", 1);
            controller.toolsLabel = Button(navigation, "Tools", 510, 0, 245, 50, "ReadingTools").GetComponentInChildren<TMP_Text>();
            Button(navigation, "Verse actions", 770, 0, 260, 50, "VerseActions");

            var bar = BottomBar(p, "Selected verse actions", 60, 104);
            controller.verseActions = bar.gameObject;
            controller.rangeLabel = Button(bar, "Select range", 0, 0, 330, 48, "Range").GetComponentInChildren<TMP_Text>();
            Button(bar, "Copy", 350, 0, 330, 48, "Copy");
            Button(bar, "Highlight / color", 700, 0, 330, 48, "Highlight");
            Button(bar, "Note", 0, 56, 330, 48, "Note");
            controller.bookmarkLabel = Button(bar, "Bookmark", 350, 56, 330, 48, "Bookmark").GetComponentInChildren<TMP_Text>();
            Button(bar, "Hide actions", 700, 56, 330, 48, "VerseActions");
            bar.gameObject.SetActive(false);

            var tools = BottomBar(p, "Reading tools", 60, 110);
            controller.readingTools = tools.gameObject;
            controller.historyBack = Button(tools, "Back", 0, 0, 240, 48, "History", -1);
            controller.historyForward = Button(tools, "Forward", 255, 0, 240, 48, "History", 1);
            Button(tools, "Book / chapter", 510, 0, 245, 48, "Books");
            controller.fullscreenLabels[1] = Button(tools, "Words only", 770, 0, 260, 48, "Fullscreen").GetComponentInChildren<TMP_Text>();
            Button(tools, "Reset position", 0, 56, 240, 48, "Recenter");
            Button(tools, "Adjust window", 255, 56, 240, 48, "AdjustWindow");
            FitButtonLabel(Button(tools, "Find selection", 510, 56, 245, 48, "FindSelection"));
            var returnReading = Button(tools, "Return to my reading", 770, 56, 260, 48, "ReturnReading");
            FitButtonLabel(returnReading);
            controller.returnButton = returnReading.gameObject;
            controller.returnButton.SetActive(false);
            tools.gameObject.SetActive(false);
            controller.presentation.normalReadingControls = new GameObject[0];
            controller.presentation.minimalNavigation = Rect(p, "Fullscreen marker", 0, 0, 0, 0).gameObject;
            controller.presentation.minimalNavigation.SetActive(false);
            Transform copy = controller.panels[8].transform;
            controller.copyHeading = Label(copy, "Copy selected scripture", 0, 0, 1030, 42, 28);
            Label(copy, "Click the field, then use Copy on VRChat's keyboard. On desktop use Ctrl+A, Ctrl+C. Paste into any text field with keyboard Paste / Ctrl+V.", 0, 48, 1030, 85, 23);
            controller.copyInput = Input(copy, "Select a verse and choose Copy selection.", 0, 145, 1030, 330, true, 50000);
            controller.copyInput.readOnly = true;
            Stretch(controller.copyInput.GetComponent<RectTransform>(), 0, 145, 0, 100);
            var copyBar = BottomBar(copy, "Copy actions", 5, 55);
            controller.copyReturnLabel = Button(copyBar, "Back to reading", 0, 0, 500, 50, "CopyReturn").GetComponentInChildren<TMP_Text>();
            controller.copyPasteControl = Button(copyBar, "Paste into note", 530, 0, 500, 50, "PasteNote").gameObject;
        }

        private static void BuildNavigation(Index index)
        {
            var palette = JsonUtility.FromJson<BookPalette>(Required<TextAsset>("Data/BookPalette.json").text);
            var bookImages = new List<Image>();
            var bookColors = new List<Color>();
            var bookLabels = new List<TMP_Text>();
            Transform browser = controller.panels[1].transform;
            Tab(browser, "Old Testament", 0, 0, 508, 48, "Testament", 0, 3, 0);
            Tab(browser, "New Testament", 522, 0, 508, 48, "Testament", 1, 3, 1);
            controller.testamentPanels = new GameObject[2];
            controller.bookScrolls = new ScrollRect[2];
            controller.bookTargets = new RectTransform[66];
            string[][] headings = { new[] { "Pentateuch / Torah", "Historical Books", "Poetry & Wisdom", "Major Prophets", "Minor Prophets" }, new[] { "The Gospels", "History - Acts", "Pauline Epistles", "General Epistles (including Hebrews)", "Prophecy / Apocalypse" } };
            int[][] starts = { new[] { 0, 5, 17, 22, 27, 39 }, new[] { 39, 43, 44, 57, 65, 66 } };
            for (int testament = 0; testament < 2; testament++)
            {
                RectTransform books;
                var scroll = Scroll(browser, testament == 0 ? "Old Testament books" : "New Testament books", 0, 65, 1030, 550, out books);
                Stretch(scroll.GetComponent<RectTransform>(), 0, 65, 0, 5);
                controller.testamentPanels[testament] = scroll.gameObject;
                controller.bookScrolls[testament] = scroll;
                var layout = books.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 14;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
                for (int section = 0; section < headings[testament].Length; section++)
                {
                    var heading = Label(books, headings[testament][section], 0, 0, 1000, 40, 23);
                    heading.fontStyle = FontStyles.Bold;
                    heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                    var group = Rect(books, headings[testament][section] + " books", 0, 0, 1000, 100);
                    var bookGrid = Grid(group, 3, 312, 52);
                    for (int i = starts[testament][section]; i < starts[testament][section + 1]; i++)
                    {
                        var button = Button(group, index.bookNames[i], 0, 0, 312, 52, "Book", i);
                        controller.bookTargets[i] = button.GetComponent<RectTransform>();
                        var image = button.GetComponent<Image>();
                        image.color = palette.colors[i];
                        bookImages.Add(image);
                        bookColors.Add(image.color);
                        bookLabels.Add(FitButtonLabel(button,true));
                    }
                }
                scroll.gameObject.SetActive(testament == 0);
            }
            controller.presentation.bookImages = bookImages.ToArray();
            controller.presentation.bookColors = bookColors.ToArray();
            controller.presentation.bookLabels = bookLabels.ToArray();
            controller.bookLabels = controller.presentation.bookLabels;
            Button(controller.panels[2].transform, "Back to books", 0, 0, 1030, 48, "BackBooks");
            RectTransform chapters;
            var chapterScroll = Scroll(controller.panels[2].transform, "Chapters", 0, 60, 1030, 555, out chapters);
            controller.chapterPickerScroll = chapterScroll;
            controller.chapterTargets = new RectTransform[150];
            Stretch(chapterScroll.GetComponent<RectTransform>(), 0, 60, 0, 5);
            Grid(chapters, 8, 115, 56);
            controller.chapterButtons = new GameObject[150];
            controller.chapterLabels = new TMP_Text[150]; controller.chapterImages = new Image[150];
            for (int i = 0; i < 150; i++)
            {
                var button = Button(chapters, (i + 1).ToString(), 0, 0, 115, 56, "Chapter", i);
                controller.chapterTargets[i] = button.GetComponent<RectTransform>();
                controller.chapterButtons[i] = button.gameObject; controller.chapterLabels[i] = FitButtonLabel(button);
                controller.chapterImages[i] = button.GetComponent<Image>();
            }
            Button(controller.panels[7].transform, "Back to chapters", 0, 0, 1030, 48, "BackChapters");
            RectTransform verses;
            var verseScroll = Scroll(controller.panels[7].transform, "Verses", 0, 60, 1030, 555, out verses);
            controller.versePickerScroll = verseScroll;
            controller.verseTargets = new RectTransform[176];
            Stretch(verseScroll.GetComponent<RectTransform>(), 0, 60, 0, 5);
            Grid(verses, 8, 115, 56);
            controller.verseButtons = new GameObject[176];
            controller.verseLabels = new TMP_Text[176]; controller.verseImages = new Image[176];
            for (int i = 0; i < 176; i++)
            {
                var button = Button(verses, (i + 1).ToString(), 0, 0, 115, 56, "Verse", i);
                controller.verseTargets[i] = button.GetComponent<RectTransform>();
                controller.verseButtons[i] = button.gameObject; controller.verseLabels[i] = FitButtonLabel(button);
                controller.verseImages[i] = button.GetComponent<Image>();
            }
        }

        private static GridLayoutGroup Grid(RectTransform parent, int columns, float width, float height)
        {
            var grid = parent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(width, height);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            return grid;
        }

        private static void BuildSearch()
        {
            Transform p = controller.panels[3].transform;
            controller.searchInput = Input(p, "Word or phrase (Enter or Search)", 0, 0, 550, 48, false, 120);
            var search = Button(p, "Search", 565, 0, 145, 48, "RunSearch");
            WireSubmit(controller.searchInput, search);
            controller.searchStop = Button(p, "Stop", 720, 0, 145, 48, "CancelSearch");
            controller.searchClear = Button(p, "Clear", 875, 0, 155, 48, "ClearSearch");
            var changed = Relay(controller.searchInput.gameObject, "SearchChanged", 0);
            UnityEventTools.AddStringPersistentListener(controller.searchInput.onValueChanged, UdonSharpEditorUtility.GetBackingUdonBehaviour(changed).SendCustomEvent, "Invoke");
            var toggleObject = Rect(p, "Search this book only", 0, 58, 40, 40);
            var image = toggleObject.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.27f, 0.35f);
            var toggle = toggleObject.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            var check = Panel(toggleObject, "Check", 8, 8, 24, 24);
            check.GetComponent<Image>().color = new Color(0.4f, 0.85f, 0.9f);
            backgrounds.Remove(check.GetComponent<Image>());
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = false;
            controller.searchThisBook = toggle;
            var scope = Relay(toggleObject.gameObject, "SearchScope", 0);
            UnityEventTools.AddStringPersistentListener(toggle.onValueChanged, UdonSharpEditorUtility.GetBackingUdonBehaviour(scope).SendCustomEvent, "Invoke");
            controller.searchScopeLabel = Label(p, "Search only Genesis (on next search)", 52, 58, 978, 40, 23);
            controller.searchStatus = Label(p, "Search all 31,102 verses, or just the current book.", 0, 105, 1030, 58, 22);
            RectTransform content;
            var results = Scroll(p, "Search results", 0, 175, 1030, 425, out content);
            controller.searchResultsScroll = results;
            Stretch(results.GetComponent<RectTransform>(), 0, 175, 0, 5);
            BuildResultRows(content, 200, true);
        }

        private static RectTransform BottomBar(Transform parent, string name, float bottom, float height)
        {
            var bar = Rect(parent, name, 0, 0, 1030, height);
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1,0); bar.pivot = Vector2.zero;
            bar.anchoredPosition = new Vector2(0,bottom); bar.sizeDelta = new Vector2(0,height);
            return bar;
        }

        private static void BuildSaved()
        {
            Transform p = controller.panels[4].transform;
            string[] names = { "Highlights", "Notes", "Bookmarks", "History" };
            for (int i = 0; i < names.Length; i++) Tab(p, names[i], i * 260, 0, 250, 50, "Saved", i, 1, i);
            var editor = Rect(p, "Bookmark name editor", 0, 65, 1030, 48);
            controller.bookmarkEditor = editor.gameObject;
            controller.bookmarkName = Input(editor, "Bookmark name (48 characters)", 0, 0, 520, 48, false, 48);
            controller.bookmarkSave = Button(editor, "Add bookmark", 535, 0, 240, 48, "RenameBookmark");
            controller.bookmarkSaveCaption = FitButtonLabel(controller.bookmarkSave);
            controller.bookmarkRemove = Button(editor, "Remove bookmark", 790, 0, 240, 48, "RemoveBookmark");
            FitButtonLabel(controller.bookmarkRemove);
            WireSubmit(controller.bookmarkName, controller.bookmarkSave);
            var changed = Relay(controller.bookmarkName.gameObject, "BookmarkChanged", 0);
            UnityEventTools.AddStringPersistentListener(controller.bookmarkName.onValueChanged, UdonSharpEditorUtility.GetBackingUdonBehaviour(changed).SendCustomEvent, "Invoke");
            controller.libraryHint = Label(p, "", 0, 124, 1030, 54, 21);
            controller.libraryHint.enableAutoSizing=true; controller.libraryHint.fontSizeMin=17; controller.libraryHint.fontSizeMax=21;
            mutedTexts.Add(controller.libraryHint);
            controller.libraryHintLayout = controller.libraryHint.rectTransform;
            RectTransform content;
            var list = Scroll(p, "Library entries", 0, 185, 1030, 425, out content);
            controller.libraryList = list.GetComponent<RectTransform>();
            Stretch(list.GetComponent<RectTransform>(), 0, 185, 0, 5);
            BuildResultRows(content, 512, false);
        }

        private static void BuildResultRows(RectTransform content, int count, bool search)
        {
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.padding = new RectOffset(0, 24, 0, 0);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var rows = new GameObject[count]; var labels = new TMP_Text[count];
            for (int i = 0; i < count; i++)
            {
                float height = search ? 112f : 62f;
                var button = Button(content, "", 0, 0, 1000, height, search ? "SearchResult" : "SavedResult", i);
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
                rows[i] = button.gameObject;
                labels[i] = button.GetComponentInChildren<TMP_Text>();
                labels[i].richText = search;
                labels[i].enableWordWrapping = search;
                if (search) labels[i].fontSize = 21;
                labels[i].overflowMode = TextOverflowModes.Ellipsis;
                labels[i].alignment = search ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
                button.gameObject.SetActive(false);
            }
            if (search) { controller.searchRows = rows; controller.searchLabels = labels; }
            else { controller.savedRows = rows; controller.savedLabels = labels; }
        }

        private static void BuildNotes()
        {
            Transform p = controller.panels[5].transform;
            controller.noteReference = Label(p, "Verse note", 0, 0, 1030, 45, 28);
            controller.noteStatus = Label(p, "", 0, 45, 1030, 70, 21);
            controller.noteStatus.enableAutoSizing=true; controller.noteStatus.fontSizeMin=17; controller.noteStatus.fontSizeMax=21;
            mutedTexts.Add(controller.noteStatus);
            controller.noteInput = Input(p, "Write your note... (up to 1,024 characters)", 0, 125, 1030, 320, true, 1024);
            Stretch(controller.noteInput.GetComponent<RectTransform>(), 0, 125, 0, 195);
            Stretch(controller.noteInput.textViewport, 12, 5, 12, 5);
            Stretch(controller.noteInput.textComponent.rectTransform, 0, 0, 0, 0);
            Stretch(((TMP_Text)controller.noteInput.placeholder).rectTransform, 0, 0, 0, 0);
            var relay = Relay(controller.noteInput.gameObject, "NoteChanged", 0);
            UnityEventTools.AddStringPersistentListener(controller.noteInput.onValueChanged, UdonSharpEditorUtility.GetBackingUdonBehaviour(relay).SendCustomEvent, "Invoke");
            UnityEventTools.AddStringPersistentListener(controller.noteInput.onEndEdit, UdonSharpEditorUtility.GetBackingUdonBehaviour(controller).SendCustomEvent, "SaveNoteDraft");
            var help = BottomBar(p, "Note save help", 120, 65);
            Label(help, "Notes stay personal and autosave after typing. Copy draft exports your text. Revert keeps one Undo until your next change.", 0, 0, 1030, 65, 18);
            var actions = BottomBar(p, "Note actions", 60, 55);
            controller.noteSave = Button(actions, "Save note", 0, 0, 330, 50, "SaveNote");
            controller.notePaste = Button(actions, "Paste copied verses", 350, 0, 330, 50, "PasteNote");
            controller.noteDelete = Button(actions, "Delete this note", 700, 0, 330, 50, "DeleteNote");
            var recovery = BottomBar(p, "Draft recovery", 5, 50);
            controller.noteCopy = Button(recovery, "Copy draft", 0, 0, 500, 50, "CopyNote");
            controller.noteRevert = Button(recovery, "Revert to saved", 530, 0, 500, 50, "RevertNote");
        }

        [Serializable] private class BookPalette { public Color[] colors = Array.Empty<Color>(); }
        private static void BuildSettings()
        {
            string[] sections = { "Size & position", "Appearance", "Accessibility", "Controls & sharing" };
            string[][] actions = { new[] { "Width", "Height", "Scale", "Distance", "Follow", "Attachment", "FollowStrength" }, new[] { "Theme", "BackgroundOpacity", "TextOpacity", "Outline", "OriginalColors", "Back", "Fullscreen" }, new[] { "Font", "Spacing", "ReadingRuler" }, new[] { "Shortcut", "Thumbstick", "Sounds", "Share" } };
            string[][] labels = { new[] { "Window width", "Window height", "Overall size", "Distance from view", "Placement", "Attach relative to", "Follow strength" }, new[] { "Reading theme", "Background opacity", "Word opacity", "Text outlines", "Bible book colors", "Back of window", "Fullscreen" }, new[] { "Reading text size", "Line spacing", "Reading guide" }, new[] { "Double grip shortcut", "Thumbstick shortcut", "Button sounds", "Visibility to others" } };
            string[][] help = {
                new[] { "Use - / +, or hold opposite side handles and move your hands apart or together.", "Use - / +, or hold the top and bottom handles. Opposite corners resize both axes.", "Changes the size of the window and its controls together.", "Keeps the current orientation. A grab beyond one metre brings the window within one metre.", "Follows the selected tracking point, or stays pinned in the world. Grabbing never changes this mode.", "Choose head, left controller, or right controller. Controllers use head tracking on desktop.", "Low = more lag. Maximum = fixed to your chosen tracking point." },
                new[] { "Choose dark, paper, or high contrast reading colors.", "Lower values make the window more see-through; 0% hides its background.", "Changes scripture opacity independently of the window background.", "Automatic outlines appear below 30% background opacity. Always on works at any opacity.", "Uses the preserved book palette, or switches the books to grayscale.", "Shows a readable reflection of the front. Controls operate from the front.", "Keeps scripture, direct verse selection, and chapter navigation. Choose Window to restore the interface." },
                new[] { "Your preferred size; automatically adapts to window width, height, and overall size. More verses fit when text is smaller.", "Adds space between lines to make reading more comfortable.", "Shows a quiet guide across the reading area. Move the scripture past it to track your line." },
                new[] { "Double either grip with that hand empty to open or dismiss the Bible.", "Click either thumbstick to open or dismiss. Shortcuts pause while typing.", "Plays a quiet click when a button is pressed.", "Shares scripture only; notes and highlights stay off the shared page. Saved world data is not encrypted." }
            };
            var values = new List<TMP_Text>();
            var valueActions = new List<string>();
            controller.settingsSections = new GameObject[sections.Length];
            Transform parent = controller.panels[6].transform;
            for (int section = 0; section < sections.Length; section++)
            {
                Tab(parent, sections[section], section * 260, 0, 250, 48, "SettingsTab", section, 2, section);
                RectTransform content;
                float top = section == 0 ? 186 : 65;
                if (section == 0)
                {
                    var presets = Rect(parent, "Comfort presets", 0, 61, 1030, 116);
                    Label(presets, "Comfort presets - each remembers your adjustments", 0, 0, 1030, 32, 22);
                    string[] presetNames = { "Floating", "Seated", "Left hand", "Right hand" };
                    for (int i = 0; i < presetNames.Length; i++) Button(presets, presetNames[i], i * 260, 38, 250, 54, "ComfortPreset", i);
                    // Presets belong to Size & position and are hidden with that section.
                    var wrapper = Rect(parent, "Size and position section", 0, 60, 1030, 520);
                    Stretch(wrapper, 0, 60, 0, 38);
                    presets.SetParent(wrapper, false); presets.anchoredPosition = Vector2.zero;
                    var sizeScroll = Scroll(wrapper, sections[section], 0, 125, 1030, 380, out content);
                    Stretch(sizeScroll.GetComponent<RectTransform>(), 0, 125, 0, 0);
                    controller.settingsSections[section] = wrapper.gameObject;
                }
                else
                {
                    var otherScroll = Scroll(parent, sections[section], 0, top, 1030, 510, out content);
                    Stretch(otherScroll.GetComponent<RectTransform>(), 0, top, 0, 38);
                    controller.settingsSections[section] = otherScroll.gameObject;
                }
                content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                content.sizeDelta = new Vector2(0, actions[section].Length * 160);
                for (int row = 0; row < actions[section].Length; row++)
                {
                    string action = actions[section][row];
                    var line = Panel(content, labels[section][row], 0, row * 160, 1000, 148);
                    cards.Add(line.GetComponent<Image>());
                    var title = Label(line, labels[section][row], 18, 16, 610, 32, 23);
                    title.fontStyle = FontStyles.Bold;
                    var explanation = Label(line, help[section][row], 18, 54, 610, 88, 18);
                    mutedTexts.Add(explanation);
                    bool numeric = action == "Width" || action == "Height" || action == "Scale" || action == "Distance" || action == "Font" || action == "BackgroundOpacity" || action == "TextOpacity";
                    TMP_Text value;
                    if (action == "FollowStrength")
                    {
                        value = Label(line, "", 655, 20, 327, 28, 22);
                        value.alignment = TextAlignmentOptions.Center;
                        controller.followSlider = FollowSlider(line, 655, 58, 327, 54);
                    }
                    else if (numeric)
                    {
                        value = Label(line, "", 645, 45, 130, 54, 22);
                        value.enableAutoSizing = true; value.fontSizeMin = 16; value.fontSizeMax = 22; value.enableWordWrapping = false;
                        value.alignment = TextAlignmentOptions.Center;
                        Button(line, "-", 785, 44, 88, 60, action, -1);
                        Button(line, "+", 887, 44, 88, 60, action, 1);
                    }
                    else value = Button(line, "", 655, 44, 327, 60, action).GetComponentInChildren<TMP_Text>();
                    values.Add(value);
                    valueActions.Add(action);
                }
                controller.settingsSections[section].SetActive(section == 0);
            }
            controller.settingValues = values.ToArray();
            controller.settingActions = valueActions.ToArray();
            controller.settingsText = Label(parent, "Preferences save automatically", 0, 584, 1030, 30, 20);
            var caption = controller.settingsText.rectTransform;
            caption.anchorMin = new Vector2(0, 0);
            caption.anchorMax = new Vector2(1, 0);
            caption.pivot = Vector2.zero;
            caption.anchoredPosition = Vector2.zero;
            caption.sizeDelta = new Vector2(0, 30);
        }

        private static Slider FollowSlider(Transform parent, float x, float y, float width, float height)
        {
            var root = Rect(parent, "Follow strength slider", x, y, width, height);
            var slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            var track = Panel(root, "Track", 8, 16, width - 16, 16);
            track.GetComponent<Image>().color = new Color(.18f, .25f, .3f);
            backgrounds.Remove(track.GetComponent<Image>());
            var fill = Panel(track, "Fill", 0, 0, width - 16, 16);
            Stretch(fill, 0, 0, 0, 0);
            fill.GetComponent<Image>().color = new Color(.3f, .7f, .85f);
            backgrounds.Remove(fill.GetComponent<Image>());
            var handleArea = Rect(root, "Handle area", 16, 0, width - 32, height);
            var handle = Panel(handleArea, "Handle", 0, 0, 28, height);
            handle.anchorMin = new Vector2(1, 0); handle.anchorMax = Vector2.one;
            handle.pivot = new Vector2(.5f, .5f); handle.anchoredPosition = Vector2.zero; handle.sizeDelta = new Vector2(28, 0);
            handle.GetComponent<Image>().color = new Color(.65f, .9f, 1f);
            backgrounds.Remove(handle.GetComponent<Image>());
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            UnityEventTools.AddStringPersistentListener(slider.onValueChanged, UdonSharpEditorUtility.GetBackingUdonBehaviour(controller).SendCustomEvent, "FollowStrengthChanged");
            return slider;
        }

        private static void BuildStudy(GameObject root, GameObject launcher)
        {
            var session = new GameObject("Bible study session");
            session.transform.SetParent(root.transform, false);
            var host = session.AddUdonSharpComponent<BibleStudyHost>();
            controller.studyHost = host; host.controller = controller; host.wordMaterials = controller.presentation.wordMaterials;
            RectTransform controls;
            var studyScroll = Scroll(controller.panels[9].transform, "Study controls", 0, 0, 1030, 620, out controls);
            Stretch(studyScroll.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var controlsLayout = controls.gameObject.AddComponent<VerticalLayoutGroup>();
            controlsLayout.spacing = 8; controlsLayout.padding = new RectOffset(0,24,0,0);
            controlsLayout.childControlWidth = controlsLayout.childControlHeight = true; controlsLayout.childForceExpandHeight = false;
            var intro = Rect(controls, "Hosting controls", 0, 0, 1030, 480);
            intro.gameObject.AddComponent<LayoutElement>().preferredHeight = 480;
            Transform panel = intro;
            Label(panel, "HOST A BIBLE STUDY", 0, 0, 1030, 42, 29);
            Label(panel, "The instance owner starts as host and can give host privileges to another participant. Each viewer gets their own following screen. Notes stay personal.", 0, 55, 1030, 82, 24);
            host.status = Label(panel, "No study host.", 0, 150, 1030, 90, 25);
            Button(panel, "Start hosting", 0, 255, 500, 60, "StartHost");
            Button(panel, "Stop hosting", 530, 255, 500, 60, "StopHost");
            Button(panel, "Join hosted screen", 0, 333, 500, 60, "JoinHost");
            Button(panel, "Hide hosted screen", 530, 333, 500, 60, "HideHost");
            Button(panel, "Instance owner: reclaim host", 0, 408, 1030, 50, "ReclaimHost");
            var people = controls;
            Label(people, "Give host privileges to a participant", 0, 0, 1000, 42, 25).gameObject.AddComponent<LayoutElement>().preferredHeight = 42;
            host.grantButtons = new Button[100]; host.grantLabels = new TMP_Text[100];
            for (int i = 0; i < 100; i++)
            {
                var button = Button(people, "", 0, 0, 1000, 58, "GrantHost", i);
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
                host.grantButtons[i] = button; host.grantLabels[i] = button.GetComponentInChildren<TMP_Text>();
                button.gameObject.SetActive(false);
            }
            // Replaces the retired physical Bible with a discoverable world-space station.
            var station = Canvas(root.transform, "Bible study hosting station", 920, 520, true);
            station.transform.SetPositionAndRotation(launcher.transform.position + Vector3.up * .8f, Quaternion.Euler(0, launcher.transform.eulerAngles.y, 0));
            var face = Panel(station.transform, "Hosting station", 0, 0, 920, 520);
            face.GetComponent<Image>().color = new Color(.055f, .075f, .1f);
            Label(face, "BIBLE STUDY", 25, 20, 870, 60, 40);
            host.stationStatus = Label(face, "No study host.", 25, 100, 870, 100, 27);
            Button(face, "Host a study", 25, 225, 425, 65, "StartHost");
            Button(face, "Stop hosting", 470, 225, 425, 65, "StopHost");
            Button(face, "Join screen", 25, 310, 425, 65, "JoinHost");
            Button(face, "Hide screen", 470, 310, 425, 65, "HideHost");
            Button(face, "Open your Bible", 25, 415, 870, 65, "Toggle");
            // This screen is positioned separately on every client. No ObjectSync is attached.
            host.screen = Canvas(root.transform, "Hosted scripture - local viewer", 1080, 860, true);
            host.screenRect = host.screen.GetComponent<RectTransform>();
            host.screenCollider = host.screen.GetComponent<BoxCollider>();
            var paper = Panel(host.screen.transform, "Hosted scripture background", 0, 0, 1080, 860);
            Stretch(paper, 0, 0, 0, 0); host.background = paper.GetComponent<Image>(); host.background.material = faceImage;
            host.heading = Label(paper, "", 25, 15, 1030, 85, 27);
            RectTransform content;
            var scroll = Scroll(paper, "Hosted verses", 25, 100, 1030, 650, out content);
            host.scriptureScroll = scroll;
            Stretch(scroll.GetComponent<RectTransform>(), 25, 110, 25, 160);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 30, 10, 10);
            layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandHeight = false;
            host.scripture = Label(content, "", 0, 0, 980, 0, 26); host.scripture.richText = true;
            var bar = Rect(paper, "Hosted screen actions", 25, 0, 1030, 135);
            bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1,0); bar.pivot = Vector2.zero;
            bar.anchoredPosition = new Vector2(25, 15); bar.sizeDelta = new Vector2(-50,135);
            host.pauseLabel = Button(bar, "Pause following", 0, 0, 500, 56, "PauseHost").GetComponentInChildren<TMP_Text>();
            Button(bar, "Catch up to host", 530, 0, 500, 56, "CatchUpHost");
            Button(bar, "Open passage in my Bible", 0, 70, 500, 56, "OpenHostPassage");
            Button(bar, "Hide study screen", 530, 70, 500, 56, "HideHost");
            MakeWidthResponsive(paper);
            host.screen.SetActive(false);
        }

        private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void MakeWidthResponsive(RectTransform parent)
        {
            float width = parent.rect.width;
            foreach (Transform child in parent)
            {
                var rect = child as RectTransform;
                if (rect == null) continue;
                if (width > 1f && parent.GetComponent<LayoutGroup>() == null && rect.anchorMin.x == 0f && rect.anchorMax.x == 0f)
                {
                    float left = rect.anchoredPosition.x - rect.pivot.x * rect.sizeDelta.x;
                    float right = left + rect.sizeDelta.x;
                    rect.anchorMin = new Vector2(left / width, rect.anchorMin.y);
                    rect.anchorMax = new Vector2(right / width, rect.anchorMax.y);
                    rect.sizeDelta = new Vector2(0f, rect.sizeDelta.y);
                    rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
                }
                MakeWidthResponsive(rect);
            }
        }

        private static void BuildWindowPresentation(RectTransform surface, GameObject root)
        {
            var view = controller.presentation;
            var allImages = surface.GetComponentsInChildren<Image>(true);
            foreach (var image in allImages) image.material = faceImage;
            view.chromeImages = allImages.Where(i => !backgrounds.Contains(i) && !controller.chapterReader.selectionImages.Contains(i) && i.gameObject != controller.readingGuide).ToArray();
            view.chromeColors = view.chromeImages.Select(i => i.color).ToArray();
            view.grids = surface.GetComponentsInChildren<GridLayoutGroup>(true);
            // A sibling root Canvas gives rear RectMask2D its own coordinate space.
            // Rotating a mask inside the front Canvas produces a negative clip rectangle.
            var rearCanvas = Canvas(root.transform, "Back - readable reflection", 1080, 860, false);
            var reverse = UnityEngine.Object.Instantiate(surface.gameObject, rearCanvas.transform, false);
            reverse.name = "Rear surface";
            foreach (var behaviour in reverse.GetComponentsInChildren<UdonSharpBehaviour>(true))
            {
                var backing = UdonSharpEditorUtility.GetBackingUdonBehaviour(behaviour);
                UnityEngine.Object.DestroyImmediate(behaviour);
                if (backing != null) UnityEngine.Object.DestroyImmediate(backing);
            }
            foreach (var input in reverse.GetComponentsInChildren<Selectable>(true)) { input.interactable = false; input.enabled = false; }
            foreach (var scroll in reverse.GetComponentsInChildren<ScrollRect>(true)) scroll.enabled = false;
            foreach (var graphic in reverse.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            view.back = rearCanvas;
            view.frontObjects = surface.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
            view.backObjects = reverse.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject).ToArray();
            view.frontTexts = surface.GetComponentsInChildren<TMP_Text>(true);
            view.backTexts = reverse.GetComponentsInChildren<TMP_Text>(true);
            view.frontImages = allImages;
            view.backImages = reverse.GetComponentsInChildren<Image>(true);
            view.backGrids = reverse.GetComponentsInChildren<GridLayoutGroup>(true);
            view.frontSliders = surface.GetComponentsInChildren<Slider>(true);
            view.backSliders = reverse.GetComponentsInChildren<Slider>(true);
            view.backScripture = view.backTexts[Array.IndexOf(view.frontTexts, controller.verseDisplayText)];
            var scrolls = surface.GetComponentsInChildren<ScrollRect>(true);
            view.frontScrollContents = scrolls.SelectMany(s => new[] { s.content, s.verticalScrollbar.handleRect }).ToArray();
            view.backScrollContents = reverse.GetComponentsInChildren<ScrollRect>(true).SelectMany(s => new[] { s.content, s.verticalScrollbar.handleRect }).ToArray();
            view.frontDynamicRects = new[] { view.readingPanel, view.scriptureViewport, (RectTransform)controller.verseActions.transform, controller.libraryList, controller.libraryHintLayout };
            view.backDynamicRects = view.frontDynamicRects.Select(r => view.backObjects[Array.IndexOf(view.frontObjects, r.gameObject)].GetComponent<RectTransform>()).ToArray();
            if (view.frontObjects.Length != view.backObjects.Length || view.frontTexts.Length != view.backTexts.Length || view.frontImages.Length != view.backImages.Length)
                throw new InvalidOperationException("Front/back reflection mappings differ.");
            rearCanvas.SetActive(false);

            int[] edgeX = { 0, 0, -1, 1, -1, 1, -1, 1 };
            int[] edgeY = { 1, -1, 0, 0, 1, 1, -1, -1 };
            string[] handleNames = { "Top", "Bottom", "Left", "Right", "Top left", "Top right", "Bottom left", "Bottom right" };
            controller.manipulator.handles = new BibleWindowHandle[8];
            var handleMaterial = Required<Material>("Materials/BibleWindowHandle.mat");
            for (int i = 0; i < 8; i++)
            {
                var handleObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                handleObject.name = "Bible handle - " + handleNames[i];
                handleObject.transform.SetParent(root.transform, false);
                handleObject.GetComponent<Renderer>().enabled = false;
                var grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                grip.name = "Visible grip"; grip.transform.SetParent(handleObject.transform,false);
                UnityEngine.Object.DestroyImmediate(grip.GetComponent<Collider>());
                grip.GetComponent<Renderer>().sharedMaterial = handleMaterial;
                bool corner = edgeX[i] != 0 && edgeY[i] != 0;
                grip.transform.localScale = corner ? new Vector3(.55f,.55f,.28f) : edgeX[i] == 0 ? new Vector3(.85f,.16f,.25f) : new Vector3(.16f,.85f,.25f);
                var body = handleObject.AddComponent<Rigidbody>();
                body.useGravity = false;
                body.isKinematic = true;
                var pickup = handleObject.AddComponent<VRCPickup>();
                pickup.AutoHold = VRC.SDKBase.VRC_Pickup.AutoHoldMode.Yes;
                pickup.DisallowTheft = true;
                pickup.allowManipulationWhenEquipped = true;
                pickup.pickupable = true;
                pickup.proximity = 3f;
                pickup.InteractionText = "Hold to move; opposite handles resize";
                var handle = handleObject.AddUdonSharpComponent<BibleWindowHandle>();
                handle.controller = controller;
                handle.pickup = pickup;
                handle.manipulator = controller.manipulator;
                handle.edgeX = edgeX[i];
                handle.edgeY = edgeY[i];
                controller.manipulator.handles[i] = handle;
                handleObject.SetActive(false);
            }
            controller.windowHandle = controller.manipulator.handles[0];
        }

        private static void BuildShared(GameObject root, BibleDataLoader data)
        {
            var template = new GameObject("Bible Shared Page - PlayerObject");
            sharedTemplate = template;
            if (systemParent != null) template.transform.SetParent(systemParent, false);
            template.AddComponent<VRCPlayerObject>();
            var view = template.AddUdonSharpComponent<BibleSharedView>();
            view.controller = controller;
            view.data = data;
            view.wordMaterials = controller.presentation.wordMaterials;
            var canvas = Canvas(template.transform, "Shared scripture only", 1080, 860, false);
            view.visual = canvas;
            var sharedFace = Panel(canvas.transform, "Shared background", 0, 0, 1080, 860);
            sharedFace.GetComponent<Image>().material = faceImage;
            Stretch(sharedFace, 0, 0, 0, 0);
            view.background = sharedFace.GetComponent<Image>();
            view.heading = Label(sharedFace, "", 30, 20, 1020, 50, 28);
            view.heading.richText = false;
            // Fit the scripture excerpt without adding interactive controls to the remote view.
            view.text = Label(sharedFace, "", 35, 90, 1010, 730, 23);
            view.text.richText = true;
            view.text.enableAutoSizing = true;
            view.text.fontSizeMin = 17;
            view.text.fontSizeMax = 26;
            Stretch(view.text.rectTransform, 35, 90, 35, 40);
            MakeWidthResponsive(sharedFace);
            var rear = UnityEngine.Object.Instantiate(sharedFace.gameObject, canvas.transform, false);
            rear.name = "Shared back";
            var rearRect = rear.GetComponent<RectTransform>();
            rearRect.pivot = new Vector2(0.5f, 0.5f);
            rearRect.anchoredPosition = Vector2.zero;
            rearRect.localPosition = new Vector3(rearRect.localPosition.x, rearRect.localPosition.y, 5f);
            rearRect.localRotation = Quaternion.Euler(0, 180, 0);
            view.backVisual = rear;
            view.backBackground = rear.GetComponent<Image>();
            var rearTexts = rear.GetComponentsInChildren<TMP_Text>(true);
            view.backHeading = rearTexts[0];
            view.backText = rearTexts[1];
            view.textRect = view.text.rectTransform;
            view.backTextRect = view.backText.rectTransform;
            canvas.SetActive(false);
        }

        private static GameObject Canvas(Transform parent, string name, int width, int height, bool interactive)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one * 0.0008f;
            if (interactive)
            {
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<VRCUiShape>();
                var collider = go.GetComponent<BoxCollider>();
                if (collider == null) collider = go.AddComponent<BoxCollider>();
                collider.size = new Vector3(width, height, 1f);
                collider.isTrigger = true;
                go.layer = 8;
            }
            return go;
        }

        private static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 8;
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(width, height);
            return r;
        }

        private static void Round(Image image)
        {
            image.sprite=rounded; image.type=Image.Type.Sliced; image.pixelsPerUnitMultiplier=1.5f;
        }

        private static RectTransform Decoration(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var rect=Rect(parent,name,x,y,w,h); var image=rect.gameObject.AddComponent<Image>();
            image.color=color; image.raycastTarget=false; return rect;
        }

        private static void AnchorRight(RectTransform rect,float right,float top,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=Vector2.one; rect.pivot=Vector2.one;
            rect.anchoredPosition=new Vector2(-right,-top); rect.sizeDelta=new Vector2(width,height);
        }

        private static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            Round(image);
            image.color = new Color(0.055f, 0.075f, 0.10f, 0.98f);
            backgrounds.Add(image);
            return rect;
        }

        private static TMP_Text Label(Transform parent, string value, float x, float y, float w, float h, float size)
        {
            var r = Rect(parent, value.Length > 0 ? value.Substring(0, Math.Min(25, value.Length)) : "Text", x, y, w, h);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSharedMaterial = faceFont;
            t.enableCulling = true;
            t.fontSize = size;
            t.color = Color.white;
            t.text = value;
            t.richText = false;
            t.raycastTarget = false;
            t.enableWordWrapping = true;
            texts.Add(t);
            return t;
        }

        private static Button Button(Transform parent, string label, float x, float y, float w, float h, string action, int index = 0)
        {
            var rect = Rect(parent, label.Length == 0 ? action + " " + index : label, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.12f,.19f,.23f,1f);
            Round(image);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = Label(rect, label, 8, 2, w - 16, h - 4, 23);
            text.alignment = TextAlignmentOptions.Midline;
            text.enableAutoSizing = true; text.fontSizeMin=16; text.fontSizeMax=23; text.enableWordWrapping=false;
            if (action == "Fullscreen")
            {
                text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 23; text.enableWordWrapping = false;
            }
            // Button labels stay white against their fixed dark backgrounds in every theme.
            texts.Remove(text);
            var relay = Relay(rect.gameObject, action, index);
            UnityEventTools.AddStringPersistentListener(button.onClick, UdonSharpEditorUtility.GetBackingUdonBehaviour(relay).SendCustomEvent, "Invoke");
            return button;
        }

        private static TMP_Text FitButtonLabel(Button button, bool bold=false)
        {
            var label=button.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing=true; label.fontSizeMin=16; label.fontSizeMax=23; label.enableWordWrapping=false;
            if (bold) label.fontStyle=FontStyles.Bold;
            return label;
        }

        private static void Tab(Transform parent, string caption, float x, float y, float w, float h, string action, int index, int group, int value)
        {
            var button = Button(parent, caption, x, y, w, h, action, index);
            var label = button.GetComponentInChildren<TMP_Text>();
            label.enableAutoSizing = true; label.fontSizeMin = 16; label.fontSizeMax = 23; label.enableWordWrapping = false;
            tabImages.Add(button.GetComponent<Image>()); tabLabels.Add(label); tabCaptions.Add(caption); tabGroups.Add(group); tabValues.Add(value);
        }

        private static BibleUIAction Relay(GameObject go, string action, int index)
        {
            var relay = go.AddUdonSharpComponent<BibleUIAction>();
            relay.controller = controller;
            relay.action = action;
            relay.index = index;
            return relay;
        }

        private static void WireSubmit(TMP_InputField input, Button button)
        {
            var relay = button.GetComponent<BibleUIAction>();
            UnityEventTools.AddStringPersistentListener(input.onSubmit, UdonSharpEditorUtility.GetBackingUdonBehaviour(relay).SendCustomEvent, "Invoke");
        }

        private static TMP_InputField Input(Transform parent, string placeholder, float x, float y, float w, float h, bool multi, int limit)
        {
            var rect = Panel(parent, "Input", x, y, w, h);
            fields.Add(rect.GetComponent<Image>());
            var border=rect.gameObject.AddComponent<Outline>(); border.effectColor=new Color(.45f,.56f,.58f,.4f); border.effectDistance=new Vector2(1,-1);
            var input = rect.gameObject.AddComponent<TMP_InputField>();
            var area = Rect(rect, "Text area", 12, 5, w - 24, h - 10);
            area.gameObject.AddComponent<RectMask2D>();
            var text = Label(area, "", 0, 0, w - 24, h - 10, 25);
            var hint = Label(area, placeholder, 0, 0, w - 24, h - 10, 23);
            mutedTexts.Add(hint);
            if (!multi) { hint.enableAutoSizing=true; hint.fontSizeMin=16; hint.fontSizeMax=21; hint.enableWordWrapping=false; }
            Stretch(area, 12, 5, 12, 5);
            Stretch(text.rectTransform, 0, 0, 0, 0);
            Stretch(hint.rectTransform, 0, 0, 0, 0);
            input.textViewport = area;
            input.textComponent = (TextMeshProUGUI)text;
            input.placeholder = hint;
            input.targetGraphic = rect.GetComponent<Image>();
            input.characterLimit = limit;
            input.richText = false;
            input.lineType = multi ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            return input;
        }

        private static ScrollRect Scroll(Transform parent, string name, float x, float y, float w, float h, out RectTransform content)
        {
            var rect = Rect(parent, name, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.02f);
            var scroll = rect.gameObject.AddComponent<ScrollRect>();
            var viewport = Rect(rect, "Viewport", 0, 0, w - 24, h);
            Stretch(viewport, 0, 0, 24, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect(viewport, "Content", 0, 0, w - 24, 0);
            content.anchorMax = Vector2.one;
            content.anchorMin = new Vector2(0, 1);
            content.sizeDelta = Vector2.zero;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 35f;
            var barRect = Rect(rect, "Scrollbar", w - 20, 0, 20, h);
            barRect.anchorMin = new Vector2(1, 0);
            barRect.anchorMax = Vector2.one;
            barRect.pivot = new Vector2(1, 0.5f);
            barRect.anchoredPosition = Vector2.zero;
            barRect.sizeDelta = new Vector2(20, 0);
            var trackImage=barRect.gameObject.AddComponent<Image>(); trackImage.color = new Color(.12f,.17f,.2f); Round(trackImage);
            var bar = barRect.gameObject.AddComponent<Scrollbar>();
            var handle = Rect(barRect, "Handle", 0, 0, 20, h);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.sizeDelta = Vector2.zero;
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(.55f,.64f,.64f); Round(handleImage);
            bar.handleRect = handle;
            bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            return scroll;
        }

    }
}
