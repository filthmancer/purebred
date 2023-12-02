
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
    public Dictionary<int, LinkInstance> linkInstances = new Dictionary<int, LinkInstance>();

    public int Heat = 0;
    public int HeatMax = 50;
    public float TickRate = 1.0F;
    private float TickRate_last = 0.0F;

    private InteractableArea3D interactable_highlighted;
    public InteractableArea3D interactable_selected { get; private set; }

    private AStar2D pathfinding;
    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        TickRate_last += (float)delta;
        if (TickRate_last > TickRate)
        {
            TickRate_last = 0.0F;
            main.EmitGodotSignal(nameof(Main.OnTick));

            var heat_total = 0;
            foreach (var node in nodeInstances.Values)
            {
                heat_total += node.GetHeat();
            }

            foreach (var link in linkInstances.Values)
            {
                heat_total += link.GetHeat();
            }

            Heat = heat_total;
        }
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionReleased("select"))
        {
            if (main.GetMouseRaycastTarget(out Node target))
            {
                // If we click again on the selected object, deselect it
                if (target == interactable_selected)
                {
                    interactable_highlighted = interactable_selected;

                    main.EmitGodotSignal(nameof(Main.HighlightDeselected), interactable_selected);
                    interactable_selected = null;

                    main.EmitGodotSignal(nameof(Main.InteractableOver), interactable_highlighted);
                    return;
                }
            }

            if (interactable_highlighted == interactable_selected)
                return;

            //If we have highlighted an interactable that isn't currently selected
            if (interactable_highlighted != null)
            {
                //Send a signal to the old selected obj
                if (interactable_selected != null)
                {
                    main.EmitGodotSignal(nameof(Main.HighlightDeselected), interactable_selected);
                }
                interactable_selected = interactable_highlighted;

                main.EmitGodotSignal(nameof(Main.HighlightSelected), interactable_selected);
            }
            else
            {
                main.EmitGodotSignal(nameof(Main.HighlightDeselected), interactable_selected);
                interactable_selected = null;
            }
        }
    }

    public bool IsSelected(InteractableArea3D target)
    {
        return target == interactable_selected;
    }

    public void Visuals_Update()
    {
        for (int n = 0; n < serverData.Nodes.Count; n++)
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
            nodeInstance.InitialiseInteractionEvents(main);
            nodeInstance.Connect("mouse_entered", Callable.From(() => SetTargetNode(nodeInstance, true)));
            nodeInstance.Connect("mouse_exited", Callable.From(() => SetTargetNode(nodeInstance, false)));

        }

        for (int l = 0; l < serverData.Links.Count; l++)
        {
            var nodeA = nodeInstances[serverData.Links[l].NodeA];
            var nodeB = nodeInstances[serverData.Links[l].NodeB];

            var link = Visuals_LinkBetween(nodeA, nodeB);
            link.Flags = serverData.Links[l].Flags;
        }

    }

    private LinkInstance Visuals_LinkBetween(ServerNode nodeA, ServerNode nodeB)
    {
        if (linkInstances.ContainsKey(LinkInstance.IDFromNodes(nodeA, nodeB)))
        {
            return null;
        }

        var offset = (nodeB.Position - nodeA.Position).Normalized();
        var linkInstance = Main.pool_linkInstance.Acquire();
        Vector3 linkPos = nodeB.Position - (nodeB.Position - nodeA.Position) / 2;
        linkInstance.Position = linkPos;

        linkInstance.Initialise(this, new ServerNode[2] { nodeA, nodeB });

        linkInstance.render.Connect("mouse_entered", Callable.From(() => SetTargetNode(linkInstance, true)));
        linkInstance.render.Connect("mouse_exited", Callable.From(() => SetTargetNode(linkInstance, false)));
        linkInstances[linkInstance.ID] = linkInstance;
        linkInstance.InitialiseInteractionEvents(main);
        AddChild(linkInstance);
        return linkInstance;
    }

    /// <summary>
    /// Returns target component if it is in the main library
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Node3D Visuals_GenerateComponent(string id)
    {
        if (Main.serverComponents.ContainsKey(id))
        {
            var comp = Main.serverComponents[id].scene.Instantiate();
            // comp.Connect("mouse_entered", Callable.From(() => SetTargetNode(comp, true)));
            // comp.Connect("mouse_exited", Callable.From(() => SetTargetNode(comp, false)));
            return comp as Node3D;
        }
        return null;
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
        data.Nodes = new List<NodeData>();
        for (int i = 0; i < nodeInstances.Count; i++)
        {
            data.Nodes.Add(new NodeData()
            {
                ID = nodeInstances[i].ID,
                pos = nodeInstances[i].Position,
                Flags = nodeInstances[i].Flags
            });
        }
        GD.Print("Rebuilding server nodes, node instances: " + nodeInstances.Count);
    }
    private void RebuildLinksFromChildren(ref ServerData data)
    {
        linkInstances = new Dictionary<int, LinkInstance>();
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
                linkInstances[link.ID] = link;
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
        data.Links = new List<LinkData>();
        for (int i = 0; i < linkDataFromChildren.Count; i++)
        {
            data.Links.Add(new LinkData()
            {
                NodeA = linkDataFromChildren[i][0],
                NodeB = linkDataFromChildren[i][1],
                Flags = 0
            });
        }

    }

    public void SetTargetNode(InteractableArea3D target, bool active)
    {
        if (!active)
        {
            target = null;
        }

        if (target == null)
            main.EmitGodotSignal(nameof(Main.InteractableExit), interactable_highlighted);

        interactable_highlighted = target;
        if (interactable_highlighted != null)
            main.EmitGodotSignal(nameof(Main.InteractableOver), interactable_highlighted);
    }
    public ServerNode GetRandomNode()
    {
        return nodeInstances[main.rng.RandiRange(0, nodeInstances.Count - 1)];
    }

    #region Currency
    public void GainResource(string type, float amount, Node cause)
    {
        switch (type)
        {
            case "credits":
                main.Currency += amount;
                break;
        }
    }
    #endregion

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
            var nodeA = serverData.Nodes.Find(n => n.ID == link.NodeA);
            var nodeB = serverData.Nodes.Find(n => n.ID == link.NodeB);
            Vector3 linkPos = nodeB.pos - (nodeB.pos - nodeA.pos) / 2;

            pathfinding.AddPoint(link.ID,
                                new Vector2(linkPos.X, linkPos.Z),
                                1);

            pathfinding.ConnectPoints(link.NodeA, link.ID);
            pathfinding.ConnectPoints(link.ID, link.NodeB);
        }
    }

    private Dictionary<string, System.Func<ServerNode, float, float>> nodeRuleFunctions = new Dictionary<string, Func<ServerNode, float, float>>()
    {
        {"avoid_cages", AvoidCages }
    };

    private Dictionary<string, System.Func<LinkInstance, float, float>> linkRuleFunctions = new Dictionary<string, Func<LinkInstance, float, float>>()
    {
        {"avoid_firewalls", AvoidFirewalls }
    };

    public static float AvoidCages(ServerNode node, float weightIn)
    {
        if (node.components.Find(c => c is Cage) != null)
        {
            return weightIn + 100;
        }
        return weightIn;
    }

    public static float AvoidFirewalls(LinkInstance link, float weightIn)
    {
        if (link.Flags.HasFlag(LinkFlags.Firewall))
        {
            return weightIn + 100;
        }
        return weightIn;
    }

    public Area3D[] GetPathFromTo(ServerNode a, ServerNode b, params string[] rules)
    {
        foreach (var node in pathfinding.GetPointIds())
        {
            float weightIn = pathfinding.GetPointWeightScale(node);
            foreach (string rule in rules)
            {
                if (nodeRuleFunctions.ContainsKey(rule) && nodeInstances.ContainsKey((int)node))
                {
                    weightIn = nodeRuleFunctions[rule](nodeInstances[(int)node], weightIn);
                }
                if (linkRuleFunctions.ContainsKey(rule) && linkInstances.ContainsKey((int)node))
                {
                    weightIn = linkRuleFunctions[rule](linkInstances[(int)node], weightIn);
                }
            }
            pathfinding.SetPointWeightScale(node, weightIn);
        }
        var path = pathfinding.GetIdPath(a.ID, b.ID);
        List<Area3D> pathAsObjects = new List<Area3D>();
        foreach (var point in path)
        {
            if (nodeInstances.ContainsKey((int)point))
                pathAsObjects.Add(nodeInstances[(int)point]);
            else if (linkInstances.ContainsKey((int)point))
                pathAsObjects.Add(linkInstances[(int)point]);
        }
        return pathAsObjects.ToArray();
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
                NodeA = link.Value.Nodes[0].ID,
                NodeB = link.Value.Nodes[1].ID,
                Flags = 0
            });
        }
        ServerData server = new ServerData()
        {
            Nodes = nodes,
            Links = links
        };
        serverData = server;
        File.SaveJsonImmediate(path, server);
    }

    public async Task LoadLayout(string path)
    {
        nodeInstances = new Dictionary<int, ServerNode>();
        linkInstances = new Dictionary<int, LinkInstance>();

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
        public List<NodeData> Nodes;
        public List<LinkData> Links;
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
        public int ID => (NodeA * 100) + (NodeB * 10000);
        public int NodeA, NodeB;
        public LinkFlags Flags;
    }
    #endregion
}
