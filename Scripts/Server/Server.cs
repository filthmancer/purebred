
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

    private ServerData serverData;

    public Main main;


    public Dictionary<int, ServerNode> nodeInstances = new Dictionary<int, ServerNode>();
    public Dictionary<DebugRender, ServerNode[]> linkInstances = new Dictionary<DebugRender, ServerNode[]>();

    public List<int[]> linkIDs = new List<int[]>();

    private ServerNode highlightedNode;

    public System.Action<Server> OnGenerationComplete = null;
    private AStar2D pathfinding;
    public override void _Ready()
    {
        if (dataToLoad != null && dataToLoad != "")
        {
            nodeInstances = new Dictionary<int, ServerNode>();
            linkInstances = new Dictionary<DebugRender, ServerNode[]>();
            LoadLayout(dataToLoad);
        }
    }

    public override void _Process(double delta)
    {

    }

    public void UpdateVisuals()
    {
        for (int n = 0; n < serverData.Nodes.Length; n++)
        {
            if (nodeInstances.ContainsKey(serverData.Nodes[n].ID))
            {
                continue;
            }

            var nodeInstance = Main.pool_serverNode.Acquire();
            nodeInstance.Position = serverData.Nodes[n].pos;
            nodeInstance.ID = serverData.Nodes[n].ID;
            nodeInstance.Flags = serverData.Nodes[n].Flags;
            nodeInstances[serverData.Nodes[n].ID] = nodeInstance;

            nodeInstance.Initialise(null, this);
            AddChild(nodeInstance);
        }

        for (int l = 0; l < serverData.Links.Length; l++)
        {
            var nodeA = nodeInstances[serverData.Links[l].NodeA];
            var nodeB = nodeInstances[serverData.Links[l].NodeB];

            if (linkInstances.ContainsValue(new ServerNode[2] { nodeA, nodeB }))
            {
                continue;
            }

            var linkInstance = Main.pool_debugLink.Acquire();
            linkInstance.Initialise(new Vector3[2] { nodeA.Position, nodeB.Position });
            linkInstances[linkInstance] = new ServerNode[2] { nodeA, nodeB };
            AddChild(linkInstance);
        }

        // var linkPrefab = GD.Load<PackedScene>("res://scenes/debug_render.tscn");
        // foreach (int[] link in linkIDs)
        // {
        //     var a = nodeInstances[link[0]];
        //     var b = nodeInstances[link[1]];

        //     if (linkInstances.ContainsValue(new ServerNode[2] { a, b }))
        //     {
        //         continue;
        //     }
        //     var linkInstance = linkPrefab.Instantiate<DebugRender>();
        //     linkInstance.Initialise(new Vector3[2] { a.Position, b.Position });
        //     linkInstances[linkInstance] = new ServerNode[] { a, b };
        //     AddChild(linkInstance);
        // }
    }

    public void rebuildDataFromChildren()
    {
        serverData = new ServerData();
        rebuildNodesFromChildren(ref serverData);
        rebuildLinksFromChildren(ref serverData);

        updatePathfinding();
        UpdateVisuals();
    }

    private void updatePathfinding()
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

    private void rebuildNodesFromChildren(ref ServerData data)
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
    private void rebuildLinksFromChildren(ref ServerData data)
    {
        linkInstances = new Dictionary<DebugRender, ServerNode[]>();
        var linkDataFromChildren = new List<int[]>();

        foreach (var child in GetChildren())
        {
            if (child is not DebugRender)
                continue;

            DebugRender link = child as DebugRender;
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

        main.EmitGodotSignal(nameof(Main.HighlightUpdated), highlightedNode);
    }
    #region Pathfinding
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
    public void SaveLayout()
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
        foreach (var link in linkIDs)
        {
            links.Add(new LinkData()
            {
                NodeA = link[0],
                NodeB = link[1],
                Flags = 0
            });
        }
        ServerData server = new ServerData()
        {
            Nodes = nodes.ToArray(),
            Links = links.ToArray()
        };
        serverData = server;
        File.SaveJsonImmediate(dataToLoad, server);
    }

    public async Task LoadLayout(string path)
    {
        var server = await File.LoadJson<Server.ServerData>(path);
        serverData = server;

        updatePathfinding();

        UpdateVisuals();
        OnGenerationComplete(this);
    }

    public enum NodeFlags { None }
    public enum LinkFlags { None }

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
