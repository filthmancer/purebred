extends "res://Scripts/Actors/Actor.gd"


var rng;
var timer = 0;
var state = "wander"
var mine_tick = 1;


func initialise(_server, node_target):
	super.initialise(_server, node_target)
	_server.main.connect("OnTick", on_tick)
	update_target();

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if state == "wander":
		if path != null and path.size() > 0:
			move_to_next_path_point(delta);
	pass
	
func on_tick():
	if state == "wander":
		timer -= 1;
		if timer <= 0:
			update_target();
			
		if node_current is LinkInstance && node_current.IsFirewall():
			path = [node_last];
		elif node_current is ServerNode && node_current.HasComponent("cage"):
			state = "trapped"
			print("trapped on ", node_current);
	elif state == "trapped":
		if node_current is ServerNode:
			if !node_current.HasComponent("cage"):
				state = "wander";
				print("wandering");
	elif state == "mine":
		if node_current is ServerNode:
			node_current.GainResource("credits", mine_tick, self)
			update_target();	
	

func update_target():
	
	if node_current is ServerNode && node_current.Heat < 10 && node_current.Credits < 10:
		state = "mine";
	else:
		state = "wander"
		timer = rng.randi_range(10, 25);
		var target = server.GetRandomNode();
		path = server.GetPathFromTo(node_current.ID, target.ID, ["avoid_firewalls"]);
	pass;
