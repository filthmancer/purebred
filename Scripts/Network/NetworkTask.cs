using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public abstract class NetworkTask
{
    public int NodeID;
    public Dictionary<string, float> Costs;
    public int Install_Ticks;
    public abstract void Complete(Network network);
    public abstract bool Update(Network network);
}
public class ComponentBuildTask : NetworkTask
{
    public string ComponentID;
    public override void Complete(Network network)
    {
        var node = network.nodeInstances[NodeID];
        var component = network.Visuals_GenerateComponent(ComponentID);
        if (component == null)
        {
            GD.PushError($"SERVERNODE.BUILDCOMPONENT: {ComponentID} was not found in component dictionary");
            return;
        }
        component.Position = Vector3.Zero;
        component.Call("initialise", node);
        node.AddChild(component);
        node.components.Add(component);
        foreach (var cost in Costs)
        {
            node.LoseResourceImmediate(cost.Key, (int)cost.Value, component);
        }
    }

    public override bool Update(Network network)
    {
        bool transfer_complete = true;
        ServerNode target = network.nodeInstances[NodeID];
        int buildCostTick = 3;
        foreach (var cost in Costs)
        {
            float amountRequired = cost.Value - target.GetResource(cost.Key);
            //# If we already have enough credits to complete this
            if (amountRequired <= 0) continue;

            foreach (var transfer in network.ActiveTransfers.FindAll(t => t.ToID == target.ID))
            {
                amountRequired -= transfer.Amount;
            }
            //# if there are enough credits on their way, don't create more transfers
            if (amountRequired <= 0) continue;

            // GD.Print($"Build: {ID} + {resource} needed : {amountRequired}");

            foreach (var sender in network.nodeInstances)
            {
                // This node is the target node
                if (sender.Value == target) continue;
                // This node has no credits
                if (sender.Value.GetResource(cost.Key) <= 0) continue;

                // This node has an active build
                if (network.ActiveBuilds.Any(a => a.NodeID == sender.Value.ID)) continue;

                var path = network.pathfinding.GetIdPath((int)sender.Value.ID, (int)target.ID);
                //No valid path
                if (path.Length == 0) continue;

                int val = Math.Min(buildCostTick, sender.Value.GetResource(cost.Key));
                val = Math.Min(val, (int)amountRequired);
                if (val == 0) break;

                amountRequired -= val;

                network.TransferResource(cost.Key, val, target, sender.Value);
                transfer_complete = false;
            }
        }
        if (transfer_complete)
        {
            // Start installing
            if (Install_Ticks > 0)
            {
                GD.Print($"INSTALLING: {Install_Ticks}");
                Install_Ticks--;
            }
            else
            {
                // If we are completed install
                Complete(network);
                return false;
            }
        }
        return true;
    }
}

public class RemoteTransferTask : NetworkTask
{
    public Action OnCompletion;
    public override void Complete(Network network)
    {
        OnCompletion?.Invoke();
        var node = network.nodeInstances[NodeID];
        foreach (var cost in Costs)
        {
            node.LoseResourceImmediate(cost.Key, (int)cost.Value, node);
        }
    }
    public override bool Update(Network network)
    {
        // If a target for this remote transfer hasn't been set, find a remote access point now
        if (NodeID == -1)
        {
            NodeID = network.nodeInstances.First(n => n.Value.RemoteAccess).Value.ID;
        }
        bool transfer_complete = true;
        ServerNode target = network.nodeInstances[NodeID];
        int buildCostTick = 3;
        foreach (var cost in Costs)
        {
            float amountRequired = cost.Value - target.GetResource(cost.Key);
            //# If we already have enough credits to complete this
            if (amountRequired <= 0) continue;

            //# if there are enough credits on their way, don't create more transfers
            foreach (var transfer in network.ActiveTransfers.FindAll(t => t.ToID == target.ID))
            {
                amountRequired -= transfer.Amount;
                transfer_complete = false;
            }
            if (amountRequired <= 0) continue;

            // GD.Print($"Build: {ID} + {resource} needed : {amountRequired}");

            foreach (var sender in network.nodeInstances)
            {
                // This node is the target node
                if (sender.Value == target) continue;
                // This node has no credits
                if (sender.Value.GetResource(cost.Key) <= 0) continue;

                // This node has an active build
                if (network.ActiveBuilds.Any(a => a.NodeID == sender.Value.ID)) continue;

                var path = network.pathfinding.GetIdPath((int)sender.Value.ID, (int)target.ID);
                //No valid path
                if (path.Length == 0) continue;

                int val = Math.Min(buildCostTick, sender.Value.GetResource(cost.Key));
                val = Math.Min(val, (int)amountRequired);
                if (val == 0) break;

                amountRequired -= val;

                network.TransferResource(cost.Key, val, target, sender.Value);
                transfer_complete = false;
            }
        }
        if (transfer_complete)
        {
            // Start installing
            if (Install_Ticks > 0)
            {
                GD.Print($"INSTALLING: {Install_Ticks}");
                Install_Ticks--;
            }
            else
            {
                // If we are completed install
                Complete(network);
                return false;
            }
        }
        return true;
    }
}