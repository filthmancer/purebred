using Godot;
using System;

public partial class RComponent : Resource
{
    [Export]
    public string ID;
    [Export]
    public PackedScene packedScene;
    [Export]
    public int cost;
    [Export]
    public Texture2D icon;
}
