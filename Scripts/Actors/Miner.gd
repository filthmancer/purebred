extends "res://Scripts/Actors/Actor.gd"

const Miner = preload ("res://Scripts/Actors/Miner.gd")

var mine_tick = 0.1


func initialise(_server, node_target):
	
	var attr_foraging = Node3D.new();
	attr_foraging.set_script(load("res://Scripts/Attributes/Foraging.gd"));
	add_child(attr_foraging);
	attributes.append(attr_foraging);
	super.initialise(_server, node_target);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	super._process(delta);
	if state == "wander":
		if path == null || path.size() == 0:
			state = "mine"
	pass
	
func on_tick():
	if not node_current is ServerNode:
		return;
		
	var next_state = "wander"
	var strongest_motivator = null;
	var strongest_motivator_weight = 0;
	for attr in attributes:
		var m = attr.get_motivation(node_current);
		if label != null:
			label.text = "%.2f" % m;
		if m > strongest_motivator_weight:
			strongest_motivator_weight = m;
			strongest_motivator = attr;
	
	if rng.randf_range(0.0, 0.8) > strongest_motivator_weight:
		strongest_motivator = null;

	if strongest_motivator != null:
		next_state = strongest_motivator.get_attribute_name();
		strongest_motivator.get_action();

	else: #wander
		if path == null || path.size() == 0:
			update_target()
		
	if node_current.HasComponent("cage"):
		next_state = "trapped"
		
	state = next_state
	
	
func get_heat():
	return 3;
