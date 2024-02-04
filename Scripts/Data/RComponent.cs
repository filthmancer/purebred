using Godot;
using System;
using System.Collections.Generic;

public partial class RComponent : Resource
{
    [Export]
    public string ID;
    [Export]
    public PackedScene packedScene;
    [Export]
    public int Install_Ticks;
    [Export]
    public int Transfer_Ticks;
    [Export]
    public Texture2D icon;

    [Export]
    public int PurchaseCost_Credits;
    [Export]
    public int PurchaseCost_Data;
    [Export]
    public int InstallCost_Credits;
    [Export]
    public int InstallCost_Data;
}
