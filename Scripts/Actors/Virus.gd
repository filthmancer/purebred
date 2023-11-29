extends "res://Scripts/Actors/Actor.gd"

var rng;
var timer;

# Called when the node enters the scene tree for the first time.
func _ready():
	rng = RandomNumberGenerator.new();
	target_pos = position;
	pass # Replace with function body.
	
func initialise(_server, node_target):
	server = _server;
	node_current = node_target;
	position = node_current.position;
	update_target();


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	timer -= delta;
	if timer <= 0:
		update_target();
		
		
	if path != null and path.size() > 0:
		move_to_next_path_point(delta)
	pass
	
func update_target():
	timer = rng.randf_range(5.0, 15.0);
	var target = server.GetRandomNode();
	path = server.GetPathFromTo(node_current, target, ["avoid_cages"]);
