using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

public partial class Main : Node3D
{
    [Export]
    public Node serverNode;

    public static Pool<ServerNode> pool_serverNode = new Pool<ServerNode>(10, p => ServerNode.Instantiate(p), PoolLoadingMode.Eager);
    public static Pool<DebugRender> pool_debugLink = new Pool<DebugRender>(10, p => DebugRender.Instantiate(p), PoolLoadingMode.Eager);
    public override void _Ready()
    {
        //LoadServerLayout("serverA");
    }

    public override void _Process(double delta)
    {

    }

    Dictionary<int, ServerNode> nodeInstances = new Dictionary<int, ServerNode>();



    public void SaveServerLayout(string path)
    {
        SaveServerLayout(serverNode, path);
    }

    public static void SaveServerLayout(Node _serverNode, string path)
    {
        // List<NodeSerializedLayout> nodes = new List<NodeSerializedLayout>();
        // List<LinkSerializedLayout> links = new List<LinkSerializedLayout>();
        // foreach (var node in _serverNode.Get("node_instances").AsGodotArray<Node>())
        // {
        //     nodes.Add(new NodeSerializedLayout()
        //     {
        //         ID = node.Get("ID").AsInt16(),
        //         pos = node.Get("position").AsVector3(),
        //         Flags = 0
        //     });
        // }
        // foreach (var link in _serverNode.Get("link_list").AsGodotArray<int[]>())
        // {
        //     links.Add(new LinkSerializedLayout()
        //     {
        //         NodeA = link[0],
        //         NodeB = link[1],
        //         Flags = 0
        //     });
        // }
        // ServerSerializedLayout server = new ServerSerializedLayout()
        // {
        //     Nodes = nodes.ToArray(),
        //     Links = links.ToArray()
        // };
        // Task.Run(async () => await File.SaveJson(path, server));
    }

}