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
    public int Cost_Credits;
    [Export]
    public int Cost_Data;
    [Export]
    public int Install_Ticks;
    [Export]
    public int Transfer_Ticks;
    [Export]
    public Texture2D icon;
}
