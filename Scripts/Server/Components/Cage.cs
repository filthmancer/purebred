using Godot;
using System;

public partial class Cage : ServerComponent
{
	public override string ID => "cage";
	public override string Name()
	{
		return "Cage";
	}
	public override string Description()
	{
		return "Traps a virus that has entered this node. Viruses are unable to leave the node without breaking the cage";
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
