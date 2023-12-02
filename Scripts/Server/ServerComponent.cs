using Godot;
using System;

public partial class ServerComponent : Area3D, IDescribableNode
{
	public virtual string ID => "servercomponent";
	public ServerNode nodeInstance;
	public virtual string Name()
	{
		return "Component";
	}
	public virtual string Description()
	{
		return "Component";
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}