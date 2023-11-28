using Godot;
public abstract partial class InteractableArea3D : Area3D
{
    public abstract void SetColor(Color col);
    public abstract void SetAsTarget(bool active);
}

public interface IDescribableNode
{
    string Description();
    string Name();
}