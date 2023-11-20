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
        //LoadServerLayout("/serverA");
    }

    public override void _Process(double delta)
    {

    }

    Dictionary<int, ServerNode> nodeInstances = new Dictionary<int, ServerNode>();

    public async Task LoadServerLayout(string path)
    {
        var server = await File.LoadJson<ServerSerializedLayout>(path);

        for (int n = 0; n < server.Nodes.Length; n++)
        {
            var nodeInstance = pool_serverNode.Acquire();
            nodeInstance.Position = server.Nodes[n].pos;
            nodeInstance.ID = server.Nodes[n].ID;
            nodeInstance.Flags = (ServerNode.ServerNodeFlags)server.Nodes[n].Flags;
            nodeInstances[server.Nodes[n].ID] = nodeInstance;
            serverNode.CallDeferred("add_child", nodeInstance);
        }


        for (int l = 0; l < server.Links.Length; l++)
        {
            var linkInstance = pool_debugLink.Acquire();
            var nodeA = nodeInstances[server.Links[l].NodeA];
            var nodeB = nodeInstances[server.Links[l].NodeB];
            linkInstance.Initialise(new Vector3[2] { nodeA.Position, nodeB.Position });
            serverNode.CallDeferred("add_child", linkInstance);
        }
    }

    public void SaveServerLayout(string path)
    {
        List<NodeSerializedLayout> nodes = new List<NodeSerializedLayout>();
        List<LinkSerializedLayout> links = new List<LinkSerializedLayout>();
        foreach (var node in serverNode.Get("node_instances").AsGodotArray<Node>())
        {
            nodes.Add(new NodeSerializedLayout()
            {
                ID = node.Get("ID").AsInt16(),
                pos = node.Get("position").AsVector3(),
                Flags = 0
            });
        }
        foreach (var link in serverNode.Get("link_list").AsGodotArray<int[]>())
        {
            links.Add(new LinkSerializedLayout()
            {
                NodeA = link[0],
                NodeB = link[1],
                Flags = 0
            });
        }
        ServerSerializedLayout server = new ServerSerializedLayout()
        {
            Nodes = nodes.ToArray(),
            Links = links.ToArray()
        };
        Task.Run(async () => await File.SaveJson(path, server));
    }

    public struct ServerSerializedLayout
    {
        public NodeSerializedLayout[] Nodes;
        public LinkSerializedLayout[] Links;
    }

    public struct NodeSerializedLayout
    {
        public int ID;
        public Vector3 pos;
        public int Flags;
    }
    public struct LinkSerializedLayout
    {
        public int NodeA, NodeB;
        public int Flags;
    }
}