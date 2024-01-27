
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[Tool]
public partial class Network : Node
{
    public struct ComponentBuildData
    {
        public int NodeID;
        public string ComponentID;
        public int TargetCost;
    }

    public struct TransferData
    {
        public Guid ID;
        public int FromID, ToID;
        public int Amount;
        public string Type;
    }

    public class TransferVisuals
    {
        public Node3D Object;
        public Vector3 startPosition;
        public Vector3 endPosition;
        public float rate = 0;
    }

    private static List<ComponentBuildData> ActiveBuilds = new List<ComponentBuildData>();
    private static List<ComponentBuildData> _ActiveBuilds_Temp = new List<ComponentBuildData>();
    private static List<TransferData> ActiveTransfers = new List<TransferData>();
    private static List<TransferData> _ActiveTransfers_Temp = new List<TransferData>();
    private static Dictionary<Guid, TransferVisuals> _ActiveTransfers_Objects = new Dictionary<Guid, TransferVisuals>();
    private NetworkData serverData;

    public Main main;

    [Export]
    public bool RegenerateNodes
    {
        get => false;
        set
        {
            if (value)
            {

                var children = GetChildren().Where(n => n is ServerNode);
                int IDCurrent = 0;
                foreach (var child in children)
                {
                    ServerNode instance = child as ServerNode;
                    instance.ID = IDCurrent++;
                    child.Name = "NODE - " + instance.ID;
                }
            }
        }
    }

    public Dictionary<int, ServerNode> nodeInstances = new Dictionary<int, ServerNode>();
    public Dictionary<int, LinkInstance> linkInstances = new Dictionary<int, LinkInstance>();

    public Dictionary<int, Area3D> AllInstances = new Dictionary<int, Area3D>();

    public int Heat = 0;
    private int Heat_nodes = 0;
    public int HeatMax = 50;
    public float TickRate = 1.0F;
    private float TickRate_last = 0.0F;

    public int Credits = 1000;
    public int CreditsMax() { return CreditsMax_nodes; }
    private int CreditsMax_nodes = 0;

    public int Data = 0;
    public int DataMax() { return DataMax_nodes; }
    private int DataMax_nodes = 0;

    //private float Credits_thisTick, Data_thisTick;

    private InteractableArea3D interactable_highlighted;
    public InteractableArea3D interactable_selected { get; private set; }

    private AStar2D pathfinding;
    public override void _Ready()
    {
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) return;
        TickRate_last += (float)delta;
        if (TickRate_last > TickRate)
        {
            TickRate_last = 0.0F;
            main.EmitGodotSignal(nameof(Main.OnTick));

            /// Heat from components
            Heat_nodes = 0;
            CreditsMax_nodes = 0;
            DataMax_nodes = 0;
            Credits = 0;
            Data = 0;
            foreach (var node in nodeInstances.Values)
            {
                Heat_nodes += node.Heat;
                CreditsMax_nodes += node.GetCreditsMax();
                DataMax_nodes += node.GetDataMax();
                Data += node.Data;
                Credits += node.Credits;
            }

            foreach (var link in linkInstances.Values)
            {
                Heat_nodes += link.GetHeat();
                CreditsMax_nodes += link.GetCreditsMax();
                DataMax_nodes += link.GetDataMax();
            }

            Heat = Math.Max(Heat_nodes, 0);
            Credits = Math.Clamp(Credits, 0, CreditsMax());
            Data = Math.Clamp(Data, 0, DataMax());

            TickActiveBuilds();
        }
        TickActiveTransfers((float)delta);
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
                    interactable_selected = null;

                    //! Passing the highlighted element here as it is the same as the LAST selected element
                    main.EmitGodotSignal(nameof(Main.HighlightDeselected), interactable_highlighted);

                    main.EmitGodotSignal(nameof(Main.InteractableOver), interactable_highlighted);
                    return;
                }
            }

            if (interactable_highlighted == interactable_selected)
                return;

            var interactable_selected_old = interactable_selected;
            //If we have highlighted an interactable that isn't currently selected
            if (interactable_highlighted != null)
            {
                interactable_selected = interactable_highlighted;
                //Send a signal to the old selected obj
                if (interactable_selected != null)
                {
                    main.EmitGodotSignal(nameof(Main.HighlightDeselected), interactable_selected_old);
                }

                main.EmitGodotSignal(nameof(Main.HighlightSelected), interactable_selected);
            }
            else
            {
                interactable_selected = null;
                main.EmitGodotSignal(nameof(Main.HighlightDeselected), interactable_selected_old);
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
                AllInstances[serverData.Nodes[n].ID] = nodeInstances[serverData.Nodes[n].ID];
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
        AllInstances[linkInstance.ID] = linkInstance;
        linkInstance.InitialiseInteractionEvents(main);
        AddChild(linkInstance);
        return linkInstance;
    }

    public Node3D Visuals_Virus(ServerNode a)
    {
        if (!nodeInstances.ContainsValue(a))
        {
            return null;
        }
        var actorPrefab = GD.Load<PackedScene>(AssetPaths.Virus);
        var actorInstance = actorPrefab.Instantiate<Node3D>();
        actorInstance.Position = a.Position;
        AddChild(actorInstance);
        actorInstance.Call("initialise", this, a);
        (actorInstance.GetNode("interactable") as InteractableActor).InitialiseInteractionEvents(main);
        actorInstance.Connect("mouse_entered", Callable.From(() => SetTargetNode((actorInstance.GetNode("interactable") as InteractableActor), true)));
        actorInstance.Connect("mouse_exited", Callable.From(() => SetTargetNode((actorInstance.GetNode("interactable") as InteractableActor), false)));
        return actorInstance;
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
            var comp = Main.serverComponents[id].packedScene.Instantiate();
            // comp.Connect("mouse_entered", Callable.From(() => SetTargetNode(comp, true)));
            // comp.Connect("mouse_exited", Callable.From(() => SetTargetNode(comp, false)));
            return comp as Node3D;
        }
        return null;
    }

    public void RebuildDataFromChildren()
    {
        serverData = new NetworkData();
        RebuildNodesFromChildren(ref serverData);
        RebuildLinksFromChildren(ref serverData);

        UpdatePathfinding();
        Visuals_Update();
        main.EmitGodotSignal(nameof(Main.ServerGenerationComplete), this);
    }
    private void RebuildNodesFromChildren(ref NetworkData data)
    {
        nodeInstances = new Dictionary<int, ServerNode>();
        foreach (var child in GetChildren())
        {
            if (child is not ServerNode)
                continue;

            ServerNode node = child as ServerNode;
            nodeInstances[node.ID] = node;
            AllInstances[node.ID] = node;
            //node.Initialise(null, this);
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
    private void RebuildLinksFromChildren(ref NetworkData data)
    {
        linkInstances = new Dictionary<int, LinkInstance>();
        var linkDataFromChildren = new List<int[]>();

        // Check link instances on the server first
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
                AllInstances[link.ID] = link;
                var lA = new int[2] { a.ID, b.ID };
                var lB = new int[2] { b.ID, a.ID };
                if (!linkDataFromChildren.Any(l => l.SequenceEqual(lA)) && !linkDataFromChildren.Any(l => l.SequenceEqual(lB)))
                    linkDataFromChildren.Add(lA);
            }
        }

        // Then check server node instances
        foreach (var a in nodeInstances)
        {
            foreach (ServerNode b in a.Value.linked_nodes)
            {
                if (b == null) continue;
                var lA = new int[2] { a.Key, b.ID };
                var lB = new int[2] { b.ID, a.Key };
                if (!linkDataFromChildren.Any(l => l.SequenceEqual(lA)) && !linkDataFromChildren.Any(l => l.SequenceEqual(lB)))
                    linkDataFromChildren.Add(lA);
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

    public void AddActiveBuild(ComponentBuildData activeBuild)
    {
        ActiveBuilds.Add(activeBuild);
    }
    public void TickActiveBuilds()
    {
        _ActiveBuilds_Temp = new List<ComponentBuildData>(ActiveBuilds);
        int buildCostTick = 3;
        int amountRequired = 0;
        for (int i = 0; i < _ActiveBuilds_Temp.Count; i++)
        {
            var build = _ActiveBuilds_Temp[i];
            ServerNode target = nodeInstances[build.NodeID];
            amountRequired = build.TargetCost - target.Credits;
            //# If we already have enough credits to complete this
            if (amountRequired <= 0)
            {
                CompleteActiveBuild(build);
                continue;
            }

            foreach (var transfer in ActiveTransfers.FindAll(t => t.ToID == target.ID))
            {
                amountRequired -= transfer.Amount;
            }
            GD.Print("Build: " + build.ComponentID + ": needed : " + amountRequired);
            //# if there are enough credits on their way, don't create more transfers
            if (amountRequired <= 0)
            {
                continue;
            }

            foreach (var sender in nodeInstances)
            {
                if (sender.Value == target) continue;
                // This node has no credits
                if (sender.Value.Credits == 0)
                {
                    continue;
                }

                var path = pathfinding.GetIdPath((int)sender.Value.ID, (int)target.ID);
                //Invalid Path
                if (path.Length == 0)
                {
                    continue;
                }

                int val = Math.Min(buildCostTick, sender.Value.Credits);
                val = Math.Min(val, amountRequired);
                if (val == 0) break;

                amountRequired -= val;

                TransferResource("credits", val, target, sender.Value);
            }
        }
    }

    private void CompleteActiveBuild(ComponentBuildData build)
    {
        var node = nodeInstances[build.NodeID];
        var component = Visuals_GenerateComponent(build.ComponentID);
        if (component == null)
        {
            GD.PushError($"SERVERNODE.BUILDCOMPONENT: {build.ComponentID} was not found in component dictionary");
            return;
        }
        component.Position = Vector3.Zero;
        component.Call("initialise", node);

        node.AddChild(component);
        node.components.Add(component);
        ActiveBuilds.Remove(build);
        node.LoseResourceImmediate("credits", build.TargetCost, component);
    }

    #region Resources

    public void TransferResource(string type, int amount, ServerNode to, ServerNode from)
    {
        var transfer = new TransferData()
        {
            ID = Guid.NewGuid(),
            FromID = from.ID,
            ToID = to.ID,
            Type = type,
            Amount = amount
        };
        ActiveTransfers.Add(transfer);
        from.LoseResourceImmediate(type, amount, to);
    }

    public void TickActiveTransfers(float delta)
    {
        var prefab = GD.Load<PackedScene>(AssetPaths.Credits);
        _ActiveTransfers_Temp = new List<TransferData>(ActiveTransfers);
        foreach (var transfer in _ActiveTransfers_Temp)
        {
            var startNode = nodeInstances[transfer.FromID];
            var endNode = nodeInstances[transfer.ToID];

            if (!_ActiveTransfers_Objects.ContainsKey(transfer.ID))
            {
                var instance = prefab.Instantiate<Node3D>();
                _ActiveTransfers_Objects[transfer.ID] = new TransferVisuals()
                {
                    Object = instance,
                    startPosition = startNode.Position,
                    endPosition = startNode.Position,
                    rate = 1
                };
                AddChild(instance);
                instance.Position = startNode.Position;
            }

            var visuals = _ActiveTransfers_Objects[transfer.ID];

            if (visuals.rate >= 1)
            {
                int currentPoint = (int)pathfinding.GetClosestPoint(new Vector2(visuals.Object.Position.X, visuals.Object.Position.Z));
                if (currentPoint == endNode.ID)
                {
                    ActiveTransfers.Remove(transfer);
                    _ActiveTransfers_Objects.Remove(transfer.ID);
                    visuals.Object.CallDeferred("free");
                    endNode.GainResourceImmediate(transfer.Type, transfer.Amount, nodeInstances[transfer.FromID]);
                    continue;
                }

                var nextPoint = GetPathFromTo(currentPoint, endNode.ID, new string[1] { "avoid_firewalls" })[1];
                visuals.startPosition = visuals.endPosition;
                visuals.endPosition = nextPoint.Position;
                visuals.rate = 0;
            }
            visuals.rate = Mathf.Clamp(visuals.rate + delta * 2, 0, 1);
            visuals.Object.Position = visuals.startPosition.Lerp(visuals.endPosition, visuals.rate);
            //Task.Run(async () => await MoveObjectTo(startingPosition, endingPosition, instance));
        }
    }

    private static async Task MoveObjectTo(Vector3 startingPosition, Vector3 endingPosition, Node3D instance)
    {
        float t = 0.0F;
        while (t < 1)
        {
            t += 0.01F;
            instance.CallDeferred("set_position", startingPosition.Lerp(endingPosition, t));
            await Task.Delay(10);
        }
        instance.CallDeferred("free");
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

    public Area3D[] GetPathFromTo(long a, long b, params string[] rules)
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
        var path = pathfinding.GetIdPath(a, b);
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
        NetworkData server = new NetworkData()
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

        var server = await File.LoadJson<Network.NetworkData>(path);
        serverData = server;

        UpdatePathfinding();

        Visuals_Update();

        main.EmitGodotSignal(nameof(Main.ServerGenerationComplete), this);
    }

    public ServerNode[] GetAllNeighbours(ServerNode serverNode, int range = 1)
    {
        // List<LinkData> links = serverData.Links.FindAll(l => l.NodeA == serverNode.ID || l.NodeB == serverNode.ID);
        // List<ServerNode> nodes = new List<ServerNode>();
        // foreach (var link in links)
        // {
        //     if (link.NodeA == serverNode.ID)
        //         nodes.Add(nodeInstances[link.NodeB]);
        //     else if (link.NodeB == serverNode.ID)
        //         nodes.Add(nodeInstances[link.NodeA]);
        // }
        List<ServerNode> nodes = new List<ServerNode>();
        long[] path;
        foreach (var n in nodeInstances)
        {
            if (n.Value == serverNode) continue;
            path = pathfinding.GetIdPath(serverNode.ID, n.Value.ID);
            if (path.Length - 1 <= range * 2)
            {
                nodes.Add(n.Value);
            }
        }

        return nodes.ToArray();
    }
    [Flags]
    public enum NodeFlags { None = 0, Burning = 1 }
    [Flags]
    public enum LinkFlags { None = 0, Firewall = 1 }

    public enum NodeType { Standard, }
    public enum LinkType { Standard, Fast, Slow }

    public struct NetworkData
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
