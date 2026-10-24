using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BiblePickup : UdonSharpBehaviour
{
    public BibleController bibleController;
    public override void Interact() { bibleController.ToggleBible(); }
    public void PutAway() { if (bibleController.isOpen) bibleController.ToggleBible(); }
}
