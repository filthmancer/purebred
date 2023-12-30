using Godot;
using System;
using System.Collections.Generic;

public partial class LinkInstance : InteractableArea3D, IDisposablePoolResource, IDescribableNode
{
    [Export]
    public DebugRender render;
    public Network.LinkType LinkType = Network.LinkType.Standard;
    public Network.LinkFlags Flags = Network.LinkFlags.None;

    public IDisposablePool Pool { get; set; }

    private Network server;

    public List<ServerNode> Nodes;
    public List<Vector3> Points => render.Points;

    public int ID => IDFromNodes(Nodes[0], Nodes[1]);

    public static int IDFromNodes(ServerNode nodeA, ServerNode nodeB)
    {
        return (nodeA.ID * 100) + (nodeB.ID * 10000);
    }
    public string Description()
    {
        string desc = $"Links {Nodes[0].Name()} and {Nodes[1].Name()}. ";
        foreach (var flag in System.Enum.GetValues(typeof(Network.LinkFlags)))
        {
            if ((Network.LinkFlags)flag == Network.LinkFlags.None) continue;
            if (Flags == (Network.LinkFlags)flag) desc += flag.ToString() + ", ";
        }
        return desc;
    }

    public string Name()
    {
        return $"Link: {Nodes[0].Name()} -> {Nodes[1].Name()}";
    }

    private static PackedScene prefab;
    public static LinkInstance Instantiate(IDisposablePool _pool)
    {
        if (prefab == null)
            prefab = GD.Load<PackedScene>("res://scenes/link_instance.tscn");

        var inst = prefab.Instantiate<LinkInstance>();
        inst.Pool = _pool;
        return inst;
    }

    public override void _Ready()
    {

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public void Initialise(Network _server, ServerNode[] _nodes)
    {
        server = _server;
        Nodes = new List<ServerNode>(_nodes);
        for (int i = 0; i < _nodes.Length; i++)
        {
            render.AddPoint(_nodes[i].Position - this.Position);
        }
        render.RegenerateLine();

        var collision = GetNode<CollisionShape3D>("debug_render/collision");
        if (collision != null)
        {
            collision.MakeConvexFromSiblings();
        }
    }

    public int GetHeat()
    {
        return Flags.HasFlag(Network.LinkFlags.Firewall) ? 5 : 0;
    }

    public int GetCreditsMax()
    {
        return 0;
    }
    public int GetDataMax()
    {
        return 0;
    }

    public void Acquire()
    {

    }
    public void Dispose()
    {

    }

    private Color _col;
    public void SetColor()
    {
        render.SetColor(_col);
    }

    public override void UpdateTarget(InteractableArea3D target, InteractionState state)
    {
        if (target == this)
        {
            switch (state)
            {
                case InteractionState.Selected:
                    _col = new Color(1.0F, 0.0F, 0.0F);
                    break;
                case InteractionState.Highlighted:
                    _col = new Color(1.0F, 0.5F, 0.5F);
                    break;
                case InteractionState.Deselected:
                    _col = new Color(1.0F, 1.0F, 1.0F);
                    break;
            }

        }
        else
        {
            if (target is ServerNode node && Nodes.Contains(node))
            {
                switch (state)
                {
                    case InteractionState.Highlighted:
                        _col = new Color(1.0F, 0.5F, 0.5F);
                        break;
                    case InteractionState.Selected:
                        _col = new Color(1.0F, 0.5F, 0.5F);
                        break;
                    case InteractionState.Unhighlighted:
                        if (server.interactable_selected != target)
                        {
                            _col = new Color(1.0F, 1.0F, 1.0F);
                        }
                        break;
                    default:
                        _col = new Color(1.0F, 1.0F, 1.0F);
                        break;
                }

            }
            else
            {
                _col = new Color(1.0F, 1.0F, 1.0F);
            }
        }
        if (Flags == Network.LinkFlags.Firewall)
        {
            _col = new Color(0.0F, 1.0F, 0.0F);
        }
        SetColor();
    }

    public void BuildComponent(string id)
    {
        if (id == "firewall")
        {
            Flags = Network.LinkFlags.Firewall;
        }
    }

    public bool IsFirewall()
    {
        return Flags.HasFlag(Network.LinkFlags.Firewall);
    }

    // public override void SetAsHighlight(bool active)
    // {
    //     render.SetAsTarget(active);
    // }

    // public override void SetAsTarget(bool active)
    // {
    //     render.SetAsTarget(active);
    // }
}