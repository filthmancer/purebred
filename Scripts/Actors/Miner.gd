extends "res://Scripts/Actors/Actor.gd"

const Miner = preload ("res://Scripts/Actors/Miner.gd")
var rng;
var timer = 0;
var state = "wander"
var mine_tick = 0.1


func initialise(_server, node_target):
	super.initialise(_server, node_target)
	rng = RandomNumberGenerator.new();
	_server.main.connect("OnTick", on_tick)
	update_target();

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	update_move(delta)
	if state == "wander":
		if path == null || path.size() == 0:
			state = "mine"
	pass
	
func on_tick():
	var next_state = "wander"
	if !(node_current is ServerNode):
		return;
	if state == "mine":
		node_current.GainResource("credits", mine_tick, self)
		if node_current is ServerNode:
			var minechance = calculate_mine_chance(node_current)
			if minechance:
				next_state = "mine"
			else:
				next_state = "wander";
				update_target()
	elif state == "wander":
		if path == null || path.size() == 0:
			timer -= 1;
			if timer <= 0:
				update_target()
		
	if node_current.HasComponent("cage"):
		next_state = "trapped"
		
	state = next_state
	

func update_target():
	timer = rng.randi_range(10, 25);
	var targets = server.GetAllNeighbours(node_current, 1);
	var targetvalues = []
		
	var index = 0
	var total = 0.0
	for t in targets:
		targetvalues.append(1)
		for comp in t.GetComponents():
			if comp is Miner:
				targetvalues[index] += 1
		var targetheat = float(1+ t.Heat) 
		var currentHeat = float(1 + node_current.Heat)
		targetvalues[index] += 1 / (targetheat/currentHeat )
		total += targetvalues[index]
		index+=1
	
	var accrued = 0.0
	index = 0
	for t in targetvalues:
		accrued += t
		var rand = rng.randf()
		if rand < (accrued /  total):
			break;
		index+=1
		
	move_to_node(targets[index], ["avoid_firewalls"]);
	#path = server.GetPathFromTo(node_current.ID, targets[index].ID, ["avoid_firewalls"]);
	
	pass;
	
func calculate_mine_chance(node):
	var minechance = 1.0;
	timer -= 1;
	if timer > 0:
		return true;
	# remove chance based on current credits on this node
	minechance -= clamp(node_current.Credits / 500, 0.0, 0.5);
	# remove chance based on current heat on this node
	var heat = float(node_current.Heat) / float(node_current.HeatMax) * 0.2;
	heat = clampf(heat, 0.0, 0.2);
	minechance -= heat;
	return rng.randf() < minechance;
	
func get_heat():
	return 1;
