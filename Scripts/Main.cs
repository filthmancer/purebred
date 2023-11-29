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
    [Export]
    public Camera3D mainCamera;
    private Node3D actorInstance;

    public float Currency = 0;

    public RandomNumberGenerator rng = new RandomNumberGenerator();

    [Signal]
    public delegate void InteractableOverEventHandler(InteractableArea3D node);
    [Signal]
    public delegate void InteractableExitEventHandler(InteractableArea3D node);
    [Signal]
    public delegate void HighlightSelectedEventHandler(InteractableArea3D node);
    [Signal]
    public delegate void HighlightDeselectedEventHandler(InteractableArea3D node);
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
        serverComponents["cage"] = GD.Load<PackedScene>("res://scenes/cage.tscn");
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
        var actorPrefab = GD.Load<PackedScene>(AssetPaths.Virus);
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

    public bool GetMouseRaycastTarget(out Node target)
    {
        target = null;

        var mousePos = GetViewport().GetMousePosition();
        var from = mainCamera.ProjectRayOrigin(mousePos);
        var to = from + mainCamera.ProjectRayNormal(mousePos) * 1000;
        var space = GetWorld3D().DirectSpaceState;
        var rayQuery = new PhysicsRayQueryParameters3D()
        {
            From = from,
            To = to,
            CollideWithAreas = true
        };
        var result = space.IntersectRay(rayQuery);
        if (result.ContainsKey("collider"))
        {
            target = result["collider"].As<Node>();
            return true;
        }
        return false;
    }
}

public static class AssetPaths
{
    public const string Actor = "res://scenes/actor.tscn";
    public const string Virus = "res://scenes/virus.tscn";
}