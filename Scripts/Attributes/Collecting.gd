extends Node3D

var actor;
var transfer_node;
var transfer_amount = 1;
var transfer_timer = 5
func initialise(_actor):
	actor = _actor;
	
func get_motivation(node):
	if node is ServerNode:
		if node.Credits < 50 and node.Credits > 0:
			return 1;
		return calc_collect_chance(node)
	return 1;
	pass

func get_action():
	if actor.node_current is ServerNode:
		transfer_timer -= 1;
		if transfer_timer == 0:
			transfer_node = get_transfer_target(actor.node_current);
			actor.node_current.server.TransferResource("credits", transfer_amount, transfer_node,actor.node_current)
			transfer_timer = 5;
			
		#actor.node_current.GainResource("credits", mine_tick, actor)

func get_attribute_name():
	return "foraging";

func calc_collect_chance(node):
	if node.Credits == 0:
		return 0;
	if node.Credits > 50:
		return 0;
	var creds = clampf(5.0/abs(node.Credits), 0.0, 1.0);
	return 0.5 + creds;
	
func get_transfer_target(node):
	var credits_target = 0;
	var neighbours = node.GetNeighbours(1);
	var node_target = neighbours[0];
	for n in neighbours:
		if n.Credits > credits_target and n.Credits < n.CreditsMax:
			credits_target = n.Credits
			node_target = n;	
	return node_target;
