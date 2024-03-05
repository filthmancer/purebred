extends Node3D

var actor;
var timer = 1;
var last_minechance =1;
var mine_tick = 0.1
func initialise(_actor):
	actor = _actor;
	
func get_motivation(node):
	if node is ServerNode:
		return calculate_mine_chance(node)
	return 1;
	pass

func get_action():
	if actor.node_current is ServerNode:
		actor.node_current.GainResource("credits", mine_tick, actor)

func get_attribute_name():
	return "foraging";

func calculate_mine_chance(node):
	var minechance = 1.0;
	timer -= 1;
	if timer > 0:
		return last_minechance;
	# remove chance based on current credits on this node
	var cred = clampf(float(node.Credits) / 200.0, 0.0, 0.3);
	minechance -= cred;
	# remove chance based on current heat on this node
	var heat = float(node.Heat) / float(node.HeatMax) * 0.5;
	heat = clampf(heat, 0.0, 0.5);
	minechance -= heat;
	timer = 1;
	last_minechance = minechance;
	return  minechance;
