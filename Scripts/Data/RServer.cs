using Godot;
using System;
[Tool]
[GlobalClass]
public partial class RServer : Resource
{
    [Export]
    public string ID;
    [Export]
    public PackedScene packedScene;
    [Export]
    public int cost;
}
