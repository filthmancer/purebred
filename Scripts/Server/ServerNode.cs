using Godot;
using System;
using System.Linq;
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

    public string Description()
    {
        string desc = "";
        foreach (var flag in System.Enum.GetValues(typeof(Server.NodeFlags)))
        {
            if (Flags == (Server.NodeFlags)flag) desc += flag.ToString() + ", ";
        }
        return desc;
    }

    public string Name()
    {
        return NodeType.ToString() + " Node";
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

    public override void SetColor(Color col)
    {
        GetNode<SpriteBase3D>("Sprite").Modulate = col;
    }

    public override void SetAsTarget(bool active)
    {
        Color col = active ? new Color(1.0F, 0.5F, 0.5F) : new Color(1.0F, 1.0F, 1.0F);
        SetColor(col);
        if (active)
        {
            foreach (var link in server.linkInstances)
            {
                if (link.Value[0] == this || link.Value[1] == this)
                    link.Key.SetColor(col);
            }
        }

    }
}