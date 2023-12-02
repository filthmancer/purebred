extends "res://Scripts/Actors/Actor.gd"

var rng;
var timer;
var state = "wander"

var damage = 0.3;

# Called when the node enters the scene tree for the first time.
func _ready():
	rng = RandomNumberGenerator.new();
	target_pos = position;
	
	pass # Replace with function body.
	
func initialise(_server, node_target):
	server = _server;
	server.main.connect("OnTick", on_tick)
	node_current = node_target;
	position = node_current.position;
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
				
	if node_current is ServerNode:
		for comp in node_current.GetComponents():
			comp.durability -= damage;
		
	
func update_target():
	timer = rng.randi_range(10, 25);
	var target = server.GetRandomNode();
	path = server.GetPathFromTo(node_current, target, ["avoid_firewalls"]);
