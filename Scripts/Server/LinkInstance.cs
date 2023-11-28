using Godot;
using System;
using System.Collections.Generic;

public partial class LinkInstance : InteractableArea3D, IDisposablePoolResource, IDescribableNode
{
    [Export]
    public DebugRender render;
    public Server.LinkType LinkType = Server.LinkType.Standard;
    public Server.LinkFlags Flags = Server.LinkFlags.None;

    public IDisposablePool Pool { get; set; }

    public ServerNode[] Nodes;
    public List<Vector3> Points => render.Points;
    public string Description()
    {
        string desc = $"Links {Nodes[0].Name()} and {Nodes[1].Name()}. ";
        foreach (var flag in System.Enum.GetValues(typeof(Server.LinkFlags)))
        {
            if ((Server.LinkFlags)flag == Server.LinkFlags.None) continue;
            if (Flags == (Server.LinkFlags)flag) desc += flag.ToString() + ", ";
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

    public void Initialise(ServerNode[] nodes)
    {
        Nodes = nodes;
        for (int i = 0; i < nodes.Length; i++)
        {
            render.AddPoint(nodes[i].Position);
        }
        render.RegenerateLine();

        var collision = GetNode<CollisionShape3D>("debug_render/collision");
        if (collision != null)
        {
            collision.MakeConvexFromSiblings();
        }
    }

    public void Acquire()
    {

    }
    public void Dispose()
    {

    }

    public override void SetColor(Color col)
    {
        render.SetColor(col);
    }

    public override void SetAsTarget(bool active)
    {
        render.SetAsTarget(active);
    }
}