using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public partial class Server : Node
{
    [Export]
    public string dataToLoad;
    public Dictionary<int, ServerNode> nodeInstances = new Dictionary<int, ServerNode>();
    public Dictionary<DebugRender, ServerNode[]> linkInstances = new Dictionary<DebugRender, ServerNode[]>();

    public List<int[]> linkIDs = new List<int[]>();

    private ServerNode highlightedNode;
    public override void _Ready()
    {
        if (dataToLoad != null && dataToLoad != "")
        {
            LoadLayout(dataToLoad);
        }
    }

    public override void _Process(double delta)
    {

    }

    public void UpdateVisuals()
    {
        var linkPrefab = GD.Load<PackedScene>("res://scenes/debug_render.tscn");
        foreach (int[] link in linkIDs)
        {
            var a = nodeInstances[link[0]];
            var b = nodeInstances[link[1]];

            if (linkInstances.ContainsValue(new ServerNode[2] { a, b }))
            {
                continue;
            }
            var linkInstance = linkPrefab.Instantiate<DebugRender>();
            linkInstance.Initialise(new Vector3[2] { a.Position, b.Position });
            linkInstances[linkInstance] = new ServerNode[] { a, b };
            AddChild(linkInstance);
        }
    }

    public void RegenerateData()
    {
        rebuildNodes();
        if (linkInstances.Count == 0)
        {
            parseLinksFromNodes();
        }
    }

    private void rebuildNodes()
    {
        nodeInstances = new Dictionary<int, ServerNode>();
        foreach (var child in GetChildren())
        {
            if (child is not ServerNode)
                continue;
            ServerNode node = child as ServerNode;
            nodeInstances[node.ID] = node;
            (node).Initialise(null, this);
        }
        GD.Print("Rebuilding server nodes, node instances: " + nodeInstances.Count);
    }

    private void parseLinksFromNodes()
    {
        linkIDs = new List<int[]>();
        foreach (var a in nodeInstances)
        {
            foreach (Node b in a.Value.linked_nodes)
            {
                if (b == null) continue;
                linkIDs.Add(new int[2] { a.Key, b.Get("ID").AsInt32() });
            }
        }
        GD.Print("Parsing server links, link instances: " + linkIDs.Count);
    }

    public void SetTargetNode(ServerNode node, bool active)
    {
        if (!active)
            node = null;
        highlightedNode = node;
        foreach (var link in linkInstances)
        {
            var color = new Color(0.5F, 0.5F, 0.5F);
            if (highlightedNode != null && link.Value.Contains(highlightedNode))
                color = new Color(1, 0, 0);
            link.Key.SetColor(color);
        }
    }

    public void SaveLayout()
    {
        var path = "serverC";

        List<Server.NodeSerializedLayout> nodes = new List<Server.NodeSerializedLayout>();
        List<Server.LinkSerializedLayout> links = new List<Server.LinkSerializedLayout>();

        foreach (var node in nodeInstances)
        {
            nodes.Add(new Server.NodeSerializedLayout()
            {
                ID = node.Key,
                pos = node.Value.Position,
                Flags = 0
            });
        }
        foreach (var link in linkIDs)
        {
            links.Add(new Server.LinkSerializedLayout()
            {
                NodeA = link[0],
                NodeB = link[1],
                Flags = 0
            });
        }
        Server.ServerSerializedLayout server = new Server.ServerSerializedLayout()
        {
            Nodes = nodes.ToArray(),
            Links = links.ToArray()
        };
        File.SaveJsonImmediate(path, server);
    }

    public async Task LoadLayout(string path)
    {
        var server = await File.LoadJson<Server.ServerSerializedLayout>(path);

        nodeInstances = new Dictionary<int, ServerNode>();
        for (int n = 0; n < server.Nodes.Length; n++)
        {
            var nodeInstance = Main.pool_serverNode.Acquire();
            nodeInstance.Position = server.Nodes[n].pos;
            nodeInstance.ID = server.Nodes[n].ID;
            nodeInstance.Flags = (ServerNode.ServerNodeFlags)server.Nodes[n].Flags;
            nodeInstances[server.Nodes[n].ID] = nodeInstance;

            nodeInstance.Initialise(null, this);
            //CallDeferred("add_child", nodeInstance);
            AddChild(nodeInstance);
        }

        linkIDs = new List<int[]>();
        linkInstances = new Dictionary<DebugRender, ServerNode[]>();
        for (int l = 0; l < server.Links.Length; l++)
        {
            var linkInstance = Main.pool_debugLink.Acquire();
            var nodeA = nodeInstances[server.Links[l].NodeA];
            var nodeB = nodeInstances[server.Links[l].NodeB];
            linkIDs.Add(new int[2] { nodeA.ID, nodeB.ID });
            linkInstances[linkInstance] = new ServerNode[2] { nodeA, nodeB };

            linkInstance.Initialise(new Vector3[2] { nodeA.Position, nodeB.Position });
            //CallDeferred("add_child", linkInstance);
            AddChild(linkInstance);
        }

        // RegenerateData();
        UpdateVisuals();
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
