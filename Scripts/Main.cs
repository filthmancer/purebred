using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Main : Node3D
{
    [Export]
    public Network server;
    [Export]
    public Camera3D mainCamera;
    [Export]
    public float keyboardCameraSpeed = 1;
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
    public delegate void ServerGenerationCompleteEventHandler(Network server);
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
    private Vector3 _input_velocity, _applied_velocity;

    //* Panning has less effect the smaller the camera size is
    float zoomFactor => Math.Clamp(1 - mainCamera_zoomThreshold.X / mainCamera.Size, 0.1F, 2);
    public override void _UnhandledInput(InputEvent @event)
    {
        Vector3 pos = cam_parent.Position;
        GD.Print(@event);

        if (@event is InputEventMagnifyGesture magnifyGesture)
        {
            mainCamera.Size = Math.Clamp(mainCamera.Size / magnifyGesture.Factor, mainCamera_zoomThreshold.X, mainCamera_zoomThreshold.Y);
        }
        else if (@event is InputEventPanGesture panGesture)
        {
            pos += cam_parent.Basis.X * (panGesture.Delta.X * zoomFactor);
            pos += cam_parent.Basis.Z * (panGesture.Delta.Y * zoomFactor);
        }
        else if (@event is InputEventMouseButton mouseButton)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.WheelDown:
                    mainCamera.Size = Math.Clamp(mainCamera.Size * 1.02F, mainCamera_zoomThreshold.X, mainCamera_zoomThreshold.Y);
                    break;
                case MouseButton.WheelUp:
                    mainCamera.Size = Math.Clamp(mainCamera.Size * 0.98F, mainCamera_zoomThreshold.X, mainCamera_zoomThreshold.Y);
                    break;
                case MouseButton.Left:

                    break;
            }
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
        ProcessCameraInput(delta);
        ProcessFocusTarget(delta);
    }

    private void ProcessCameraInput(double delta)
    {
        var newInput = new Vector3();
        if (Input.IsActionPressed("cam_right"))
        {
            newInput += -cam_parent.Basis.X;
        }
        if (Input.IsActionPressed("cam_left"))
        {
            newInput += cam_parent.Basis.X;
        }
        if (Input.IsActionPressed("cam_up"))
        {
            newInput += cam_parent.Basis.Z;
        }
        if (Input.IsActionPressed("cam_down"))
        {
            newInput += -cam_parent.Basis.Z;
        }
        if (newInput == Vector3.Zero)
            _input_velocity = Vector3.Zero;
        else
        {
            _input_velocity += newInput;
            isTargeting = false;
        }

        _input_velocity = _input_velocity.Normalized();

        if (_input_velocity == Vector3.Zero)
        {
            _applied_velocity *= 0.92F;
            if (_applied_velocity.Length() < 0.001F) _applied_velocity = Vector3.Zero;
        }
        else _applied_velocity = _input_velocity * keyboardCameraSpeed * (float)delta * zoomFactor;

        Vector3 pos = cam_parent.Position;
        pos += _applied_velocity;

        pos.X = Math.Clamp(pos.X, -20, 20);
        pos.Z = Math.Clamp(pos.Z, -20, 20);
        cam_parent.Position = pos;
    }

    private bool isTargeting = false;
    private Node3D target;
    private Vector3 targeting_startingPosition;
    private float targeting_time = 0.0F;
    private void ProcessFocusTarget(double delta)
    {
        if (server.interactable_selected != null)
        {
            if (server.interactable_selected != target)
            {
                isTargeting = false;
            }
            if (Input.IsActionJustPressed("focus"))
            {
                isTargeting = true;
                target = server.interactable_selected;
                targeting_startingPosition = cam_parent.Position;
                targeting_time = 0F;
            }
        }
        else
            isTargeting = false;

        if (isTargeting)
        {
            var position = (server.interactable_selected is InteractableActor) ? (server.interactable_selected.GetParent() as Node3D).Position :
                                                                                server.interactable_selected.Position;

            targeting_time = Math.Clamp(targeting_time + (float)delta * 4, 0F, 1F);

            if (cam_parent.Position.DistanceTo(position) > 0.5F)
                cam_parent.Position = targeting_startingPosition.Lerp(position, targeting_time);
            else cam_parent.Position = position;
        }
    }

    private void SetupActor(Network s)
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

    public bool PurchaseItem(string id, ServerNode node, out RComponent componentResource)
    {
        componentResource = null;
        if (!serverComponents.ContainsKey(id))
            return false;

        componentResource = serverComponents[id];

        int cost = serverComponents[id].cost;
        if (server.Credits < cost)
            return false;

        return true;
    }

    public RComponent GetComponent(string id)
    {
        if (serverComponents.ContainsKey(id))
        {
            return serverComponents[id];
        }
        return null;
    }

    public bool LoadServer(string id)
    {
        if (!serverScenes.ContainsKey(id))
        {
            return false;
        }

        server = serverScenes[id].packedScene.Instantiate() as Network;
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

    public const string Credits = "res://data/CreditChunk.tscn";
}