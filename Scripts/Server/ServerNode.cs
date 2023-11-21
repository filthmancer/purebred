using Godot;
using System;
public partial class ServerNode : Area3D, IDisposablePoolResource
{
    public enum ServerNodeFlags { None }
    public IDisposablePool Pool { get; set; }
    [Export]
    public int ID;
    [Export]
    public Node[] linked_nodes;
    [Export]
    public ServerNodeFlags Flags;
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
        GD.Print("Acquired ServerNode: " + this.Name);
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

    public void _on_mouse_entered()
    {
        server.SetTargetNode(this, true);
    }

    public void _on_mouse_exited()
    {
        server.SetTargetNode(this, false);
    }
}