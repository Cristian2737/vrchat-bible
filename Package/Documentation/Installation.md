# VRChat Bible - KJV

Created and maintained by Cristian2737: https://cristian2737.itch.io/

Free download. Donations are optional support for this project and future
projects. All Bible features are included without payment, keys or accounts.
Original code and UI are dedicated under CC0; see LICENSE.md for third-party
exceptions. Credit is appreciated, not required by the CC0 dedication.

## Requirements

- A VRChat Worlds project managed with Creator Companion.
- Unity 2022.3.22f1 and Worlds SDK 3.7.5 are the initial test environment.
- Worlds SDK 3.7.5 or newer is required for persistence. UdonSharp is included
  with the Worlds SDK. Newer SDK versions need their own compatibility check.
- TextMesh Pro and Unity UI, installed as SDK dependencies.

This is a world asset. It does not put a portable Bible on an avatar.

## Install

1. Back up your project and scene.
2. Choose one distribution format. Import VRChatBible-1.0.0.unitypackage,
   or install com.cristian2737.bible through its VCC listing/local package.
   Install one copy of the package, not both formats together.
3. Open your existing world scene.
4. Choose Tools > VRChat Bible > Add Bible to Open Scene. This also creates
   missing global TMP settings under Assets/VRChatBibleGenerated, preserving
   existing settings. If TMP settings already exist, you can alternatively
   drag Prefabs/VRChatBible.prefab into the scene once.
5. Position the VRChat Bible container, launcher and hosting station as desired.
   Keep the reader and PlayerObject template inside the container.
6. Choose Tools > VRChat Bible > Validate Bible in Open Scene, then save.
7. Test with ClientSim and the VRChat SDK. Build and upload your own world
   through the SDK's normal controls.

The installer adds one prefab to the open scene. It does not open another
scene, replace existing objects, create a world descriptor, change your world
ID or upload anything. Re-running the menu selects/validates the existing
Bible instead of adding a second copy. Adding it through the menu supports Undo.
Your world still needs its normal VRChat Scene Descriptor/spawn setup.

The package includes its own font atlas, materials and font license; importing
TextMesh Pro Examples/Extras is not required.

## Use

Press B on desktop, double either empty-hand grip, click either thumbstick,
or use the launcher. Books opens Old/New Testament tabs and canonical groups:
choose a book, chapter, then verse. Direct references accept John 3:16-18.
Click scripture to select a verse; enable Range and click the last verse to
select a range. Reading scrolls continuously without pages.

Verse actions include copy, highlights, notes and bookmarks. Copy opens a
keyboard-selectable text field; use the VRChat keyboard's Copy command or
Ctrl+A/Ctrl+C. Editable note fields accept Paste/Ctrl+V. World code does not
write to your operating-system clipboard.

Tools contains Back/Forward, Find selection, window adjustment and Words only /
Show window. Opposite side handles change width; top/bottom change height;
opposite corners change both. One handle moves/rotates the window. Grabbing
preserves follow/pin mode. Settings groups placement, appearance, accessibility
and controls/sharing, including independent opacity, outlines and readable back.

The instance owner starts with study-host privileges and can delegate them.
Viewers join a local following screen, pause/catch up, open its displayed range
in their own Bible and return to their personal selection/scroll. Private notes
and annotation marks are excluded from shared scripture screens.

## Saved data and limits

VRChat persistence saves each player's place, range, settings, notes, highlights,
bookmarks and history in that world. Different worlds have separate saves.
Keep the same world ID when updating a world to retain its server-side data.

One Bible system per world uses the stable bible.kjv.v1. key prefix. Replacing
the asset must not rename those keys. Other world systems share VRChat's overall
persistence budget; test the complete world together. Notes are personal in the
Bible interface, not encrypted secret storage inaccessible to other world code.

Limits: 64 named bookmarks, 32 history entries, 128 notes, 1,024 characters per
note, 12,000 total note characters, 512 highlighted verses and 200 search results.
Unavailable or full-storage actions explain their state. Unreadable annotation
data is protected from overwriting. Undo is session-local.

## Updates and removal

Keep asset .meta files and GUIDs when updating. Back up the scene, import/update
the same distribution format, and validate. Preserve your customized prefab
instance overrides. Do not install a second copy or delete saved PlayerData.
If changing distribution formats, back up first and remove the old package files
before importing the other format; restore the same GUID-based references.

To remove the Bible from a scene, remove its VRChat Bible container. This does
not erase saved server data. Do not clear a world's PlayerData to update it.

## Help and feature requests

Use the project's itch.io discussion board when published. Include package,
Unity and SDK versions, platform, the relevant Unity Console error and steps to
reproduce. Feature requests are welcome whether or not you donate.

See Changelog.md, Support.md and LICENSE.md.
