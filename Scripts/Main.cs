using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node3D
{
    [Export]
    public Server server;
    [Export]
    public Camera3D mainCamera;
    private Node3D actorInstance;

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
    [Signal]
    public delegate void OnTickEventHandler();

    public static Pool<ServerNode> pool_serverNode = new Pool<ServerNode>(50, p => ServerNode.Instantiate(p), PoolLoadingMode.Lazy);
    public static Pool<LinkInstance> pool_linkInstance = new Pool<LinkInstance>(50, p => LinkInstance.Instantiate(p), PoolLoadingMode.Lazy);

    public static Dictionary<string, RComponent> serverComponents = new Dictionary<string, RComponent>();

    private static Dictionary<string, RServer> serverScenes = new Dictionary<string, RServer>();

    private Node3D cam_parent;
    private readonly Vector2 mainCamera_zoomThreshold = new Vector2(5, 30);
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMagnifyGesture magnifyGesture)
        {
            mainCamera.Size = Math.Clamp(mainCamera.Size * magnifyGesture.Factor, mainCamera_zoomThreshold.X, mainCamera_zoomThreshold.Y);
        }
        else if (@event is InputEventPanGesture panGesture)
        {
            //* Panning has less effect the smaller the camera size is
            float zoomFactor = Math.Clamp(1 - mainCamera_zoomThreshold.X / mainCamera.Size, 0.1F, 2);
            Vector3 pos = cam_parent.Position;
            pos += cam_parent.Basis.X * (panGesture.Delta.X * zoomFactor);
            pos += cam_parent.Basis.Z * (panGesture.Delta.Y * zoomFactor);
            cam_parent.Position = pos;
        }
    }

    public override void _Ready()
    {
        cam_parent = GetNode<Node3D>("CamParent");

        ServerGenerationComplete += SetupActor;

        foreach (var server in File.LoadObjects<RServer>(AssetPaths.Servers))
        {
            serverScenes[server.ID] = server;
        }

        foreach (var component in File.LoadObjects<RComponent>(AssetPaths.Components))
        {
            serverComponents[component.ID] = component;
        }

        LoadServer("server_a");
    }

    public override void _Process(double delta)
    {

    }
    private void SetupActor(Server s)
    {
        actorInstance = s.Visuals_Virus(s.nodeInstances[0]);
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

    public bool PurchaseItem(string id)
    {
        if (!serverComponents.ContainsKey(id))
            return false;

        if (server.Credits < serverComponents[id].cost)
            return false;

        server.Credits -= serverComponents[id].cost;
        return true;
    }

    public bool LoadServer(string id)
    {
        if (!serverScenes.ContainsKey(id))
        {
            return false;
        }

        server = serverScenes[id].packedScene.Instantiate() as Server;
        AddChild(server);
        server.main = this;
        server.RebuildDataFromChildren();

        return true;
    }

    public interface ISceneData
    {
        PackedScene scene { get; set; }
        void Load();
    }


    public struct ComponentData : ISceneData
    {
        public PackedScene scene { get; set; }
        public int cost;
        public ComponentData(PackedScene _scene, int _cost)
        {
            scene = _scene;
            cost = _cost;
        }
        public void Load()
        {

        }
    }

    public struct ServerData : ISceneData
    {
        public PackedScene scene { get; set; }
        public int nodes;
        public int cost;
        public ServerData(PackedScene _scene, int _cost)
        {
            scene = _scene;
            cost = _cost;
        }
        public void Load()
        {

        }
    }
}

public static class AssetPaths
{
    public const string Actor = "res://scenes/actor.tscn";
    public const string Virus = "res://scenes/virus.tscn";
    public const string Servers = "servers";
    public const string Components = "components";
}