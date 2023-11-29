using Godot;
using System;
using System.Collections.Generic;
public partial class ServerNode : InteractableArea3D, IDisposablePoolResource, IDescribableNode
{
    public IDisposablePool Pool { get; set; }
    [Export]
    public int ID;
    [Export]
    public Node[] linked_nodes;
    [Export]
    public Server.NodeFlags Flags;

    public Server.NodeType NodeType = Server.NodeType.Standard;

    public int ComponentMax = 1;
    public List<ServerComponent> components = new List<ServerComponent>();


    public string Description()
    {
        string desc = NodeType.ToString() + "\n";
        // foreach (var flag in System.Enum.GetValues(typeof(Server.NodeFlags)))
        // {
        //     if ((Server.NodeFlags)flag == Server.NodeFlags.None) continue;
        //     if (Flags == (Server.NodeFlags)flag) desc += flag.ToString() + ", ";
        // }

        foreach (var component in components)
        {
            desc += component.Name() + ", ";
        }
        return desc;
    }

    public static string[] NodeNames = new string[24] {"Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
                                        "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho", "Sigma",
                                        "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega"};

    public string Name()
    {
        if (NodeNames.Length - 1 > ID) return NodeNames[ID];
        return "Node";
    }

    [Export]
    public float Radius = 0.5F;
    private Server server;

    private static PackedScene prefab;
    public static ServerNode Instantiate(IDisposablePool _pool)
    {
        if (prefab == null)
            prefab = GD.Load<PackedScene>("res://scenes/ServerNode.tscn");

        var inst = prefab.Instantiate<ServerNode>();
        inst.Pool = _pool;
        inst.Position = Vector3.Zero;
        return inst;
    }

    public ServerNode()
    {

    }
    public void Acquire()
    {

    }
    public void Dispose()
    {
        if (Pool == null)
            throw new ArgumentException("Pool has not been assigned!");

        if (Pool.IsDisposed)
        {

        }
        else
        {
            Pool.Release(this);
        }
    }

    public override void _Ready()
    {
        base._Ready();
        if (Pool == null)
        {
            Main.pool_serverNode.AddToPool(this);
        }
        AddToGroup("ServerNodes", true);
    }

    public void Initialise(Vector3? _pos, Server _server)
    {
        Position = _pos ?? Position;
        server = _server;
    }

    public void SetColor(Color col)
    {
        GetNode<SpriteBase3D>("Sprite").Modulate = col;
    }

    public override void UpdateTarget(InteractableArea3D target, InteractionState state)
    {
        if (target == this)
        {
            switch (state)
            {
                case InteractionState.Deselected:
                    SetColor(new Color(1.0F, 1.0F, 1.0F));
                    break;
                case InteractionState.Highlighted:
                    if (server.interactable_selected != this) SetColor(new Color(1.0F, 0.5F, 0.5F));
                    break;
                case InteractionState.Selected:
                    SetColor(new Color(1.0F, 0.0F, 0.0F));
                    break;
                case InteractionState.Unhighlighted:
                    if (server.interactable_selected != this) SetColor(new Color(1.0F, 1.0F, 1.0F));
                    break;
            }
        }
        else
        {
            if (server.interactable_selected != this) SetColor(new Color(1.0F, 1.0F, 1.0F));
        }
    }

    public bool BuildComponent(string id)
    {
        // Already have too many components on this node
        if (components.Count >= ComponentMax)
            return false;

        var component = server.Visuals_GenerateComponent(id);
        if (component == null)
        {
            GD.PushError($"SERVERNODE.BUILDCOMPONENT: {id} was not found in component dictionary");
            return false;
        }
        component.Position = Vector3.Zero;
        component.nodeInstance = this;

        AddChild(component);
        components.Add(component);
        return true;
    }
}