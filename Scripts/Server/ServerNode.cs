using Godot;
using System;
using System.Collections.Generic;
[Tool]
public partial class ServerNode : InteractableArea3D, IDisposablePoolResource, IDescribableNode
{
    public IDisposablePool Pool { get; set; }
    [Export]
    public int ID;
    [Export]
    public Godot.Collections.Array<ServerNode> linked_nodes;
    [Export]
    public Server.NodeFlags Flags;

    public Server.NodeType NodeType = Server.NodeType.Standard;

    [Export]
    public int Heat = 0;

    [Export]
    public bool LinkNodes
    {
        get => false;
        set
        {
            if (value)
            {
                EditorPlugin plugin = new EditorPlugin();
                var selected = plugin.GetEditorInterface().GetSelection().GetSelectedNodes();
                foreach (var s in selected)
                {
                    if (s != this && s is ServerNode sn)
                    {
                        if (!linked_nodes.Contains(sn))
                        {
                            linked_nodes.Add(sn);
                        }
                    }
                }
            }
        }
    }

    public int ComponentMax = 1;
    public List<Node3D> components = new List<Node3D>();


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
            desc += component.Call("Name").ToString() + ", ";
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
        server.main.OnTick += OnTick;
    }

    public void SetColor(Color col)
    {
        GetNode<SpriteBase3D>("Sprite").Modulate = col;
    }

    public void OnTick()
    {

    }

    public int GetHeat()
    {
        var total = Heat;
        foreach (var comp in components)
        {
            total += comp.Call("get_heat").As<int>();
        }
        return total;
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


        // Attempts to purchase the item
        if (!server.main.PurchaseItem(id))
            return false;

        var component = server.Visuals_GenerateComponent(id);
        if (component == null)
        {
            GD.PushError($"SERVERNODE.BUILDCOMPONENT: {id} was not found in component dictionary");
            return false;
        }
        component.Position = Vector3.Zero;
        component.Call("initialise", this);
        //component.Set("nodeinstance", this);

        AddChild(component);
        components.Add(component);
        return true;
    }

    public bool DestroyComponent(Node3D comp)
    {
        if (!components.Contains(comp))
            return false;
        components.Remove(comp);
        comp.QueueFree();
        return true;
    }

    public Node3D[] GetComponents()
    {
        return components.ToArray();
    }

    public bool HasComponent(string id)
    {
        return components.Find(n => n.Get("ID").ToString() == id) != null;
    }

    public void GainResource(string type, float amount, Node cause)
    {
        server.GainResource(type, amount, cause);
    }
}