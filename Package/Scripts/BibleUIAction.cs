using UdonSharp;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BibleUIAction : UdonSharpBehaviour
{
    public BibleController controller;
    public string action;
    public int index;
    public void Invoke() { controller.HandleAction(action, index); }
}
