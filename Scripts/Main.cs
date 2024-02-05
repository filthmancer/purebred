using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public delegate void OnMarketPurchaseCompletedEventHandler(string purchaseID);
    [Signal]
    public delegate void MoveActorsEventHandler(string id);
    [Signal]
    public delegate void OnTickEventHandler();

    public static Pool<ServerNode> pool_serverNode = new Pool<ServerNode>(50, p => ServerNode.Instantiate(p), PoolLoadingMode.Lazy);
    public static Pool<LinkInstance> pool_linkInstance = new Pool<LinkInstance>(50, p => LinkInstance.Instantiate(p), PoolLoadingMode.Lazy);

    public static Dictionary<string, RComponent> marketComponents = new Dictionary<string, RComponent>();
    public static Dictionary<string, RComponent> purchasedComponents = new Dictionary<string, RComponent>();

    private static Dictionary<string, RServer> serverScenes = new Dictionary<string, RServer>();

    private Node3D cam_parent;


    public override void _Ready()
    {
        cam_parent = GetNode<Node3D>("CamParent");

        //ServerGenerationComplete += SetupActor;

        foreach (var server in File.LoadObjects<RServer>(AssetPaths.Servers))
        {
            serverScenes[server.ID] = server;
        }

        foreach (var component in File.LoadObjects<RComponent>(AssetPaths.Components))
        {
            marketComponents[component.ID] = component;
        }
        //purchasedComponents = marketComponents;

        LoadServer("server_a");
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("buy"))
        {
            PurchaseItemFromMarket("wallet");
        }
        ProcessCameraInput(delta);
        ProcessFocusTarget(delta);
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

    public bool CanInstallItem(string id, ServerNode node, out RComponent componentResource)
    {
        componentResource = null;
        if (!purchasedComponents.ContainsKey(id))
            return false;

        componentResource = purchasedComponents[id];

        int Cost_Credits = purchasedComponents[id].InstallCost_Credits;
        if (server.Credits < Cost_Credits)
            return false;

        int Cost_Data = purchasedComponents[id].InstallCost_Data;
        if (server.Data < Cost_Data)
            return false;
        return true;
    }

    public bool PurchaseItemFromMarket(string id)
    {
        if (!marketComponents.ContainsKey(id))
            return false;

        int Cost_Credits = marketComponents[id].PurchaseCost_Credits;
        if (server.Credits < Cost_Credits)
            return false;

        int Cost_Data = marketComponents[id].PurchaseCost_Data;
        if (server.Data < Cost_Data)
            return false;

        server.AddActiveBuild(new RemoteTransferTask()
        {
            NodeID = -1,
            OnCompletion = () => CompletePurchase(id),
            Costs = new Dictionary<string, float>()
            {
                {"credits", Cost_Credits},
                {"data", Cost_Data}
            },
            Install_Ticks = 1
        });
        return true;
    }

    public void CompletePurchase(string purchaseID)
    {
        purchasedComponents.Add(purchaseID, marketComponents[purchaseID]);
        EmitGodotSignal(nameof(OnMarketPurchaseCompleted), purchaseID);
    }

    public string[] GetPurchasedIDs()
    {
        return purchasedComponents.Keys.ToArray();
    }

    public string[] GetMarketIDs()
    {
        return marketComponents.Keys.ToArray();
    }

    public RComponent GetComponent(string id)
    {
        if (marketComponents.ContainsKey(id))
        {
            return marketComponents[id];
        }
        return null;
    }

    #region Input

    //* Panning has less effect the smaller the camera size is
    private bool _input_isMouseDragging = false;
    private Vector2 _input_mouse_initialPosition, _input_mouse_dragDelta;

    //Length limit on drag speed
    private static float _input_cameraDrag_maxSpeed = 4;
    private Vector3 _input_cameraDrag_velocity, _input_cameraDrag_appliedVelocity;

    private readonly Vector2 input_cameraZoom_thresholds = new Vector2(5, 30);
    private float _input_zoomFactor => Math.Clamp(1 - input_cameraZoom_thresholds.X / mainCamera.Size, 0.1F, 2);

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMagnifyGesture magnifyGesture)
        {
            mainCamera.Size = Math.Clamp(mainCamera.Size / magnifyGesture.Factor, input_cameraZoom_thresholds.X, input_cameraZoom_thresholds.Y);
        }
        else if (@event is InputEventPanGesture panGesture)
        {
            _input_mouse_dragDelta = panGesture.Delta * 3;
            _input_mouse_initialPosition = panGesture.Position;
            _input_isMouseDragging = _input_mouse_dragDelta.Length() > 0.05F;
        }
        else if (@event is InputEventMouseButton mouseButton)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.WheelDown:
                    mainCamera.Size = Math.Clamp(mainCamera.Size * 1.02F, input_cameraZoom_thresholds.X, input_cameraZoom_thresholds.Y);
                    break;
                case MouseButton.WheelUp:
                    mainCamera.Size = Math.Clamp(mainCamera.Size * 0.98F, input_cameraZoom_thresholds.X, input_cameraZoom_thresholds.Y);
                    break;
                case MouseButton.Left:
                    if (_input_isMouseDragging != mouseButton.IsPressed())
                    {
                        _input_isMouseDragging = mouseButton.IsPressed();
                        _input_mouse_initialPosition = mouseButton.Position;
                    }
                    break;
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            if (_input_isMouseDragging)
            {
                _input_mouse_dragDelta = mouseMotion.Position - _input_mouse_initialPosition;
                _input_mouse_initialPosition = mouseMotion.Position;
            }
        }
    }

    private void ProcessCameraInput(double delta)
    {
        var newInput = new Vector3();
        if (_input_isMouseDragging)
        {
            newInput += cam_parent.Basis.X * _input_mouse_dragDelta.X;
            newInput += cam_parent.Basis.Z * _input_mouse_dragDelta.Y;
            _input_mouse_dragDelta = Vector2.Zero;
        }
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
            _input_cameraDrag_velocity = Vector3.Zero;
        else
        {
            _input_cameraDrag_velocity += newInput;
            _focus_isTargeting = false;
        }

        _input_cameraDrag_velocity = _input_cameraDrag_velocity.LimitLength(_input_cameraDrag_maxSpeed);

        if (_input_cameraDrag_velocity == Vector3.Zero)
        {
            _input_cameraDrag_appliedVelocity *= 0.92F;
            if (_input_cameraDrag_appliedVelocity.Length() < 0.001F) _input_cameraDrag_appliedVelocity = Vector3.Zero;
        }
        else _input_cameraDrag_appliedVelocity = _input_cameraDrag_velocity * keyboardCameraSpeed * (float)delta * _input_zoomFactor;

        Vector3 pos = cam_parent.Position;
        pos += _input_cameraDrag_appliedVelocity;

        pos.X = Math.Clamp(pos.X, -20, 20);
        pos.Z = Math.Clamp(pos.Z, -20, 20);
        cam_parent.Position = pos;
    }

    private bool _focus_isTargeting = false;
    private Node3D _focus_target;
    private Vector3 _focus_target_startingPosition;
    private float _focus_target_activeTime = 0.0F;

    private void ProcessFocusTarget(double delta)
    {
        if (server == null) return;
        if (server.interactable_selected != null)
        {
            if (server.interactable_selected != _focus_target)
            {
                _focus_isTargeting = false;
            }
            if (Input.IsActionJustPressed("focus"))
            {
                _focus_isTargeting = true;
                _focus_target = server.interactable_selected;
                _focus_target_startingPosition = cam_parent.Position;
                _focus_target_activeTime = 0F;
            }
        }
        else
            _focus_isTargeting = false;

        if (_focus_isTargeting)
        {
            var position = (server.interactable_selected is InteractableActor) ? (server.interactable_selected.GetParent() as Node3D).Position :
                                                                                server.interactable_selected.Position;

            _focus_target_activeTime = Math.Clamp(_focus_target_activeTime + (float)delta * 4, 0F, 1F);

            if (cam_parent.Position.DistanceTo(position) > 0.5F)
                cam_parent.Position = _focus_target_startingPosition.Lerp(position, _focus_target_activeTime);
            else cam_parent.Position = position;
        }
    }
    #endregion

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