using Godot;
public abstract partial class InteractableArea3D : Area3D
{
    public enum InteractionState
    {
        Deselected, Highlighted, Selected
    }
    public abstract void UpdateTarget(InteractableArea3D target, InteractionState state);
    public void InitialiseInteractionEvents(Main main)
    {
        main.HighlightDeselected += n => UpdateTarget(n, InteractionState.Deselected);
        main.HighlightUpdated += n => UpdateTarget(n, InteractionState.Highlighted);
        main.HighlightSelected += n => UpdateTarget(n, InteractionState.Selected);
    }

}

public interface IDescribableNode
{
    string Description();
    string Name();
}