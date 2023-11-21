using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Server : Node
{
    public List<ServerNode> nodeInstances = new List<ServerNode>();
    public Dictionary<DebugRender, ServerNode[]> linkInstances = new Dictionary<DebugRender, ServerNode[]>();

    public List<int[]> linkIDs = new List<int[]>();

    private ServerNode highlightedNode;
    public override void _Ready()
    {
        RegenerateData();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        var linkPrefab = GD.Load<PackedScene>("res://scenes/debug_render.tscn");
        foreach (int[] link in linkIDs)
        {
            var a = nodeInstances[link[0]];
            var b = nodeInstances[link[1]];
            var linkInstance = linkPrefab.Instantiate<DebugRender>();
            linkInstance.Initialise(new Vector3[2] { a.Position, b.Position });
            linkInstances[linkInstance] = new ServerNode[] { a, b };
            AddChild(linkInstance);
        }
    }

    public void RegenerateData()
    {
        GD.Print("Regenerating server instance data");
        rebuildNodes();
        parseLinksFromNodes();
    }

    private void rebuildNodes()
    {
        nodeInstances = new List<ServerNode>();
        foreach (var child in GetChildren())
        {
            if (!child.IsInGroup("ServerNodes") || child is not ServerNode)
                continue;
            nodeInstances.Add(child as ServerNode);
            (child as ServerNode).Initialise(null, this);
        }
        GD.Print("Rebuilding server nodes, node instances: " + nodeInstances.Count);
    }

    private void parseLinksFromNodes()
    {
        linkIDs = new List<int[]>();
        foreach (ServerNode a in nodeInstances)
        {
            foreach (Node b in a.linked_nodes)
            {
                if (b == null) continue;
                linkIDs.Add(new int[2] { a.ID, b.Get("ID").AsInt32() });
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
                ID = node.ID,
                pos = node.Position,
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
