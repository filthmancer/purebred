using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
[Tool]
[GlobalClass]
public partial class ServerNode : InteractableArea3D, IDisposablePoolResource, IDescribableNode
{

    public Vector2 Position2D => new Vector2(Position.X, Position.Z);
    public IDisposablePool Pool { get; set; }
    [Export]
    public int ID;
    [Export]
    public Godot.Collections.Array<ServerNode> linked_nodes;
    [Export]
    public Network.NodeFlags Flags;

    public Network.NodeType NodeType = Network.NodeType.Standard;

    [Export]
    public int Heat = 0;
    [Export]
    public int HeatMax_initial = 20;
    public int HeatMax;
    public int HeatMax_components = 0;

    [Export]
    public int Credits = 0;
    [Export]
    public int CreditsMax = 500;
    private int CreditsMax_components = 0;

    [Export]
    public int Data = 0;
    [Export]
    public int DataMax = 100;
    private int DataMax_components = 0;

    private float Credits_thisTick, Data_thisTick;
    public Dictionary<int, float> Credits_thisTick_transferred = new Dictionary<int, float>();

    [Export]
    public bool LinkNodes
    {
        get => false;
        set
        {
            if (value)
            {
                EditorPlugin plugin = new EditorPlugin();
                var selected = plugin.GetEditorInterface().GetSelection().GetSelectedNodes();
                foreach (var s in selected)
                {
                    if (s != this && s is ServerNode sn)
                    {
                        if (!linked_nodes.Contains(sn))
                        {
                            linked_nodes.Add(sn);
                        }
                    }
                }
            }
        }
    }

    public int ComponentMax = 1;
    public List<Node3D> components = new List<Node3D>();

    public List<Node3D> creditObjects = new List<Node3D>();


    public string Description()
    {
        string desc = NodeType.ToString() + "\n";
        // foreach (var flag in System.Enum.GetValues(typeof(Server.NodeFlags)))
        // {
        //     if ((Server.NodeFlags)flag == Server.NodeFlags.None) continue;
        //     if (Flags == (Server.NodeFlags)flag) desc += flag.ToString() + ", ";
        // }
        desc += $"HEAT: {Heat}\\{HeatMax}\n";

        desc += $"CR: {Credits}\\{GetCreditsMax()}\n";
        desc += $"DT: {Data}\\{GetDataMax()}\n";
        return desc;
    }

    public static string[] NodeNames = new string[24] {"Alpha", "Beta", "Gamma", "Delta", "Epsilon", "Zeta", "Eta", "Theta",
                                        "Iota", "Kappa", "Lambda", "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho", "Sigma",
                                        "Tau", "Upsilon", "Phi", "Chi", "Psi", "Omega"};

    public string Name()
    {
        if (NodeNames.Length - 1 > ID) return NodeNames[ID];
        return "Node";
    }

    public Network server { get; private set; }

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
    public override void _Process(double delta)
    {
        base._Process(delta);
        UpdateCreditObjects();
    }

    public void Initialise(Vector3? _pos, Network _server)
    {
        Position = _pos ?? Position;
        server = _server;
        server.main.OnTick += OnTick;

        foreach (var node in GetChildren(true))
        {
            if (node.Call("is_component").AsBool())
            {
                node.Call("initialise", this);
                components.Add(node as Node3D);
            }
        }
    }

    public void SetColor(Color col)
    {
        GetNode<SpriteBase3D>("Sprite").Modulate = col;
    }

    public void OnTick()
    {
        Credits = Math.Clamp(Credits + (int)Credits_thisTick, 0, GetCreditsMax());
        Data = Math.Clamp(Data + (int)Data_thisTick, 0, GetDataMax());
        UpdateHeat();
        Credits_thisTick = 0;
        Data_thisTick = 0;

        foreach (var transfer in Credits_thisTick_transferred)
        {
            Credits_thisTick += transfer.Value;
        }
        Credits_thisTick_transferred.Clear();
    }

    private void UpdateCreditObjects()
    {
        int creditsObjsRequired = (int)(Credits / 10) + 1;
        var creditPrefab = GD.Load<PackedScene>(AssetPaths.Credits);
        while (creditsObjsRequired > creditObjects.Count)
        {
            if (creditObjects.Count > 0)
                creditObjects.Last().Scale = Vector3.One;

            var creditInstance = creditPrefab.Instantiate<Node3D>();
            var x = -0.6F + creditObjects.Count % 5 * 0.3F;
            var z_offset = -0.6F + (int)(creditObjects.Count / 25) * -1.5F;
            var z = z_offset + creditObjects.Count / 5 * 0.3F;
            var y = (int)(creditObjects.Count / 25) * 0.3F;

            var vec = new Vector3(x, y, z);
            creditInstance.Position = vec;
            AddChild(creditInstance);
            creditObjects.Add(creditInstance);
        }
        while (creditObjects.Count > creditsObjsRequired)
        {
            var obj = creditObjects.Last();
            obj.QueueFree();
            creditObjects.Remove(obj);
        }
        if (creditObjects.Count > 0)
        {
            creditObjects.Last().Scale = new Vector3(1, (float)(Credits % 10 / 10F), 1);
        }

    }


    public void UpdateHeat()
    {
        Heat = 0;
        HeatMax = HeatMax_initial;
        foreach (var comp in components)
        {
            Heat += comp.Call("get_heat").As<int>();
            HeatMax += comp.Call("get_heatmax").As<int>();
        }
        Heat = Math.Clamp(Heat, 0, HeatMax);
    }

    public int GetDataMax()
    {
        var total = DataMax;
        foreach (var comp in components)
        {
            total += comp.Call("get_datamax").As<int>();
        }
        return total;
    }

    public int GetCreditsMax()
    {
        var total = CreditsMax;
        foreach (var comp in components)
        {
            total += comp.Call("get_creditsmax").As<int>();
        }
        return total;
    }

    public override void UpdateTarget(InteractableArea3D target, InteractionState state)
    {
        if (target == this)
        {
            switch (state)
            {
                case InteractionState.Deselected:
                    SetColor(new Color(1.0F, 1.0F, 1.0F));
                    break;
                case InteractionState.Highlighted:
                    if (server.interactable_selected != this) SetColor(new Color(1.0F, 0.5F, 0.5F));
                    break;
                case InteractionState.Selected:
                    SetColor(new Color(1.0F, 0.0F, 0.0F));
                    break;
                case InteractionState.Unhighlighted:
                    if (server.interactable_selected != this) SetColor(new Color(1.0F, 1.0F, 1.0F));
                    break;
            }
        }
        else
        {
            if (server.interactable_selected != this) SetColor(new Color(1.0F, 1.0F, 1.0F));
        }
    }

    public bool BuildComponent(string id)
    {
        // Already have too many components on this node
        if (components.Count >= ComponentMax)
            return false;

        // Attempts to purchase the item
        if (!server.main.PurchaseItem(id, this, out RComponent componentResource))
            return false;

        var activeBuild = new Network.ComponentBuildData()
        {
            NodeID = this.ID,
            ComponentID = id,
            Costs = new Dictionary<string, float>()
            {
                {"credits", componentResource.Cost_Credits},
                {"data", componentResource.Cost_Data}
            },
            Install_Ticks = componentResource.Install_Ticks
        };
        server.AddActiveBuild(activeBuild);
        return true;
    }

    public bool DestroyComponent(Node3D comp)
    {
        if (!components.Contains(comp))
            return false;
        components.Remove(comp);
        comp.QueueFree();
        return true;
    }

    public Node3D[] GetComponents()
    {
        return components.ToArray();
    }

    public ServerNode[] GetNeighbours(int range = 1)
    {
        return server.GetAllNeighbours(this, range);
    }

    public bool HasComponent(string id)
    {
        return components.Find(n => n.Get("ID").ToString() == id) != null;
    }

    public bool RemoveComponent(string id, out Node3D component)
    {
        component = components.Find(n => n.Get("ID").AsString() == id);
        if (component != null)
        {
            components.Remove(component);
            server.AddChild(component);
            return true;
        }
        return false;
    }


    #region Currency
    public void GainResource(string type, float amount, Node cause)
    {
        switch (type)
        {
            case "credits":
                Credits_thisTick += amount;
                break;
            case "data":
                Data_thisTick += amount;
                break;
        }
    }

    public void LoseResource(string type, float amount, Node cause)
    {
        switch (type)
        {
            case "credits":
                Credits_thisTick -= amount;
                break;
            case "data":
                Data_thisTick -= amount;
                break;
        }
    }
    public void GainResourceImmediate(string type, int amount, Node cause)
    {
        switch (type)
        {
            case "credits":
                Credits += amount;
                break;
            case "data":
                Data += amount;
                break;
        }
    }

    public void LoseResourceImmediate(string type, int amount, Node cause)
    {
        switch (type)
        {
            case "credits":
                Credits -= amount;
                break;
            case "data":
                Data -= amount;
                break;
        }
    }

    public int GetResource(string type)
    {
        switch (type)
        {
            case "credits":
                return Credits;
            case "data":
                return Data;
        }
        return -1;
    }
    #endregion
}