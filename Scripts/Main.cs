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

    [Signal]
    public delegate void HighlightUpdatedEventHandler(Node node);

    public static Pool<ServerNode> pool_serverNode = new Pool<ServerNode>(10, p => ServerNode.Instantiate(p), PoolLoadingMode.Eager);
    public static Pool<DebugRender> pool_debugLink = new Pool<DebugRender>(10, p => DebugRender.Instantiate(p), PoolLoadingMode.Eager);
    public override void _Ready()
    {
        server.main = this;
        server.OnGenerationComplete += SetupActor;
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

public static class Signals
{
    public const string HighlightUpdated = "highlight_updated";
}