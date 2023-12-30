using Godot;
public abstract partial class InteractableArea3D : Area3D
{
    public enum InteractionState
    {
        Highlighted, Unhighlighted, Selected, Deselected
    }
    public abstract void UpdateTarget(InteractableArea3D target, InteractionState state);
    public virtual void InitialiseInteractionEvents(Main main)
    {
        main.HighlightDeselected += n => UpdateTarget(n, InteractionState.Deselected);
        main.InteractableOver += n => UpdateTarget(n, InteractionState.Highlighted);
        main.InteractableExit += n => UpdateTarget(n, InteractionState.Unhighlighted);
        main.HighlightSelected += n => UpdateTarget(n, InteractionState.Selected);
    }
}

public interface IDescribableNode
{
    string Description();
    string Name();
}