using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

public partial class Main : Node3D
{
    [Export]
    public Server server;
    private Node3D actorInstance;

    public float Currency = 0;

    [Signal]
    public delegate void HighlightUpdatedEventHandler(Node node);
    [Signal]
    public delegate void HighlightSelectedEventHandler(Node node);
    [Signal]
    public delegate void ServerGenerationCompleteEventHandler(Server server);
    [Signal]
    public delegate void MoveActorsEventHandler(string id);

    public static Pool<ServerNode> pool_serverNode = new Pool<ServerNode>(10, p => ServerNode.Instantiate(p), PoolLoadingMode.Eager);
    public static Pool<LinkInstance> pool_linkInstance = new Pool<LinkInstance>(10, p => LinkInstance.Instantiate(p), PoolLoadingMode.Eager);

    public static Dictionary<string, PackedScene> serverComponents = new Dictionary<string, PackedScene>();
    public override void _Ready()
    {
        serverComponents["servercomponent"] = GD.Load<PackedScene>("res://scenes/server_component.tscn");
        server.main = this;
        ServerGenerationComplete += SetupActor;
        server.LoadLayout("serverD");
        // server.RebuildDataFromChildren();
        // server.SaveLayout("serverD");
    }

    public override void _Process(double delta)
    {

    }
    private void SetupActor(Server s)
    {
        var actorPrefab = GD.Load<PackedScene>(AssetPaths.Actor);
        actorInstance = actorPrefab.Instantiate<Node3D>();
        actorInstance.Position = s.nodeInstances[0].Position;
        AddChild(actorInstance);
        actorInstance.Call("initialise", server, server.nodeInstances[0]);
    }

    public bool EmitGodotSignal(string name, params Godot.Variant[] args)
    {
        if (!HasSignal(name))
            return false;
        EmitSignal(name, args);
        return true;
    }
}

public static class AssetPaths
{
    public const string Actor = "res://scenes/actor.tscn";
}