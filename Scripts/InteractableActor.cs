using Godot;
using System;

/// <summary>
/// Wrapper for interactable actors, as we are relying on GD script for their behaviour currently.
/// </summary>
/// TODO: Find Actor GD script instance instead of just looking for parent
public partial class InteractableActor : InteractableArea3D, IDescribableNode
{
	public string Name()
	{
		var name = GetParent().Call("name");
		return name.VariantType != Variant.Type.Nil ? name.AsString() : GetParent().Name;
	}
	public string Description()
	{
		return GetParent().Call("description").AsString();
	}
	public override void _Ready()
	{

	}
	public override void UpdateTarget(InteractableArea3D target, InteractionState state)
	{
		// Send the interaction event to the Actor GD script
		GetParent().Call("interactable_" + state.ToString().ToLower(), target);
	}
}
