
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public partial class Server : Node
{

    private ServerData serverData;

    public Main main;

    public Dictionary<int, ServerNode> nodeInstances = new Dictionary<int, ServerNode>();
    public Dictionary<LinkInstance, ServerNode[]> linkInstances = new Dictionary<LinkInstance, ServerNode[]>();

    private InteractableArea3D highlightedArea;

    private AStar2D pathfinding;
    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("select") && highlightedArea != null)
        {
            main.EmitGodotSignal(nameof(Main.HighlightSelected), highlightedArea);
        }
    }

    public void Visuals_Update()
    {
        for (int n = 0; n < serverData.Nodes.Length; n++)
        {
            if (!nodeInstances.ContainsKey(serverData.Nodes[n].ID))
            {
                nodeInstances[serverData.Nodes[n].ID] = Main.pool_serverNode.Acquire();
                AddChild(nodeInstances[serverData.Nodes[n].ID]);
            }
            var nodeInstance = nodeInstances[serverData.Nodes[n].ID];
            nodeInstance.Position = serverData.Nodes[n].pos;
            nodeInstance.ID = serverData.Nodes[n].ID;
            nodeInstance.Flags = serverData.Nodes[n].Flags;

            nodeInstance.Initialise(null, this);
            nodeInstance.Connect("mouse_entered", Callable.From(() => SetTargetNode(nodeInstance, true)));
            nodeInstance.Connect("mouse_exited", Callable.From(() => SetTargetNode(nodeInstance, false)));

        }

        for (int l = 0; l < serverData.Links.Length; l++)
        {
            var nodeA = nodeInstances[serverData.Links[l].NodeA];
            var nodeB = nodeInstances[serverData.Links[l].NodeB];

            var link = Visuals_LinkBetween(nodeA, nodeB);
            link.Flags = serverData.Links[l].Flags;
        }
    }

    private LinkInstance Visuals_LinkBetween(ServerNode nodeA, ServerNode nodeB)
    {
        if (linkInstances.ContainsValue(new ServerNode[2] { nodeA, nodeB }))
        {
            return null;
        }
        var offset = (nodeB.Position - nodeA.Position).Normalized();
        var linkInstance = Main.pool_linkInstance.Acquire();
        linkInstance.Initialise(new ServerNode[2] { nodeA, nodeB });
        linkInstance.render.Connect("mouse_entered", Callable.From(() => SetTargetNode(linkInstance, true)));
        linkInstance.render.Connect("mouse_exited", Callable.From(() => SetTargetNode(linkInstance, false)));
        linkInstances[linkInstance] = new ServerNode[2] { nodeA, nodeB };
        AddChild(linkInstance);
        return linkInstance;
    }

    public void RebuildDataFromChildren()
    {
        serverData = new ServerData();
        RebuildNodesFromChildren(ref serverData);
        RebuildLinksFromChildren(ref serverData);

        UpdatePathfinding();
        Visuals_Update();
        main.EmitGodotSignal(nameof(Main.ServerGenerationComplete), this);
    }
    private void RebuildNodesFromChildren(ref ServerData data)
    {
        nodeInstances = new Dictionary<int, ServerNode>();
        foreach (var child in GetChildren())
        {
            if (child is not ServerNode)
                continue;

            ServerNode node = child as ServerNode;
            nodeInstances[node.ID] = node;
            node.Initialise(null, this);
        }
        data.Nodes = new NodeData[nodeInstances.Count];
        for (int i = 0; i < nodeInstances.Count; i++)
        {
            data.Nodes[i] = new NodeData()
            {
                ID = nodeInstances[i].ID,
                pos = nodeInstances[i].Position,
                Flags = nodeInstances[i].Flags
            };
        }
        GD.Print("Rebuilding server nodes, node instances: " + nodeInstances.Count);
    }
    private void RebuildLinksFromChildren(ref ServerData data)
    {
        linkInstances = new Dictionary<LinkInstance, ServerNode[]>();
        var linkDataFromChildren = new List<int[]>();

        foreach (var child in GetChildren())
        {
            if (child is not LinkInstance)
                continue;

            LinkInstance link = child as LinkInstance;
            ServerNode a = nodeInstances.First(kvp => kvp.Value.Position == link.Points[0]).Value;
            ServerNode b = nodeInstances.First(kvp => kvp.Value.Position == link.Points[1]).Value;

            if (a != null && b != null)
            {
                linkInstances[link] = new ServerNode[2] { a, b };
                var l = new int[2] { a.ID, b.ID };
                if (!linkDataFromChildren.Contains(l))
                    linkDataFromChildren.Add(l);
            }
        }

        foreach (var a in nodeInstances)
        {
            foreach (Node b in a.Value.linked_nodes)
            {
                if (b == null) continue;
                var l = new int[2] { a.Key, b.Get("ID").AsInt32() };
                if (!linkDataFromChildren.Contains(l))
                    linkDataFromChildren.Add(l);
            }
        }

        /// TODO: UPDATE linkDataFromChildren TO BE LINK DATA INSTANCES, SO WE CAN SEND FLAGS
        data.Links = new LinkData[linkDataFromChildren.Count];
        for (int i = 0; i < linkDataFromChildren.Count; i++)
        {
            data.Links[i] = new LinkData()
            {
                NodeA = linkDataFromChildren[i][0],
                NodeB = linkDataFromChildren[i][1],
                Flags = 0
            };
        }

    }

    public void SetTargetNode(InteractableArea3D target, bool active)
    {
        foreach (var link in linkInstances)
        {
            link.Key.SetColor(new Color(1.0F, 1.0F, 1.0F));
        }

        if (!active)
        {
            target = null;
        }

        if (target != highlightedArea && highlightedArea != null)
            highlightedArea.SetAsTarget(false);

        highlightedArea = target;
        if (highlightedArea != null) highlightedArea.SetAsTarget(true);

        main.EmitGodotSignal(nameof(Main.HighlightUpdated), highlightedArea);
    }
    #region Pathfinding
    private void UpdatePathfinding()
    {
        pathfinding = new AStar2D();
        foreach (var node in serverData.Nodes)
        {
            pathfinding.AddPoint(node.ID,
                                new Vector2(node.pos.X, node.pos.Z),
                                1);
        }
        foreach (var link in serverData.Links)
        {
            pathfinding.ConnectPoints(link.NodeA, link.NodeB);
        }
    }

    public ServerNode[] GetPathFromTo(ServerNode a, ServerNode b)
    {
        var path = pathfinding.GetIdPath(a.ID, b.ID);
        List<ServerNode> pathAsNodes = new List<ServerNode>();
        foreach (var point in path)
        {
            pathAsNodes.Add(nodeInstances[(int)point]);
        }
        return pathAsNodes.ToArray();
    }

    #endregion
    #region Data
    public void SaveLayout(string path)
    {
        List<NodeData> nodes = new List<NodeData>();
        List<LinkData> links = new List<LinkData>();

        foreach (var node in nodeInstances)
        {
            nodes.Add(new NodeData()
            {
                ID = node.Key,
                pos = node.Value.Position,
                Flags = 0
            });
        }
        foreach (var link in linkInstances)
        {
            links.Add(new LinkData()
            {
                NodeA = link.Value[0].ID,
                NodeB = link.Value[1].ID,
                Flags = 0
            });
        }
        ServerData server = new ServerData()
        {
            Nodes = nodes.ToArray(),
            Links = links.ToArray()
        };
        serverData = server;
        File.SaveJsonImmediate(path, server);
    }

    public async Task LoadLayout(string path)
    {
        nodeInstances = new Dictionary<int, ServerNode>();
        linkInstances = new Dictionary<LinkInstance, ServerNode[]>();

        var server = await File.LoadJson<Server.ServerData>(path);
        serverData = server;

        UpdatePathfinding();

        Visuals_Update();

        main.EmitGodotSignal(nameof(Main.ServerGenerationComplete), this);
    }

    [Flags]
    public enum NodeFlags { None = 0, Burning = 1 }
    [Flags]
    public enum LinkFlags { None = 0, Firewall = 1 }

    public enum NodeType { Standard, }
    public enum LinkType { Standard, Fast, Slow }

    public struct ServerData
    {
        public NodeData[] Nodes;
        public LinkData[] Links;
        public NodeData GetNodeData(int ID)
        {
            return Nodes.First(n => n.ID == ID);
        }
    }

    public struct NodeData
    {
        public int ID;
        public Vector3 pos;
        public NodeFlags Flags;
    }
    public struct LinkData
    {
        public int NodeA, NodeB;
        public LinkFlags Flags;
    }
    #endregion
}
