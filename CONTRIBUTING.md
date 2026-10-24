# Contributing

Bug reports and feature requests are welcome without a donation. Use the issue
forms and include package/Unity/SDK versions, platform and reproduction steps.
Explain the reading or accessibility need when suggesting a feature.

Original contributions use CC0; preserve third-party notices. Do not include
personal notes, account credentials or another creator's world assets in reports.

## Development

Create a separate VCC Worlds project using the documented Unity/SDK versions.
Add `Package/` as a local package, or copy its contents into
`Packages/com.cristian2737.bible`. Keep one installation of the Bible.

Open your own world scene, add/validate the Bible through its Tools menu, and
test with ClientSim. Keep `.meta` files/GUIDs stable when updating assets.
Preserve the `bible.kjv.v1.` save keys and formats, existing scenes/world IDs,
private annotation handling and host-authority checks.

For runtime changes, check selection/ranges, search, notes/bookmarks/highlights,
resizing, Words only, both sides, hosting/sharing and simulated leave/rejoin.
Build PC and Android for cross-platform changes. Test controller/VR changes on
real hardware and networking/persistence changes in an actual VRChat instance.

Do not claim those hardware or server tests passed based on ClientSim alone.
The existing verification report records the initial release's completed checks.
