extends Area3D

var server;
var target_pos;
var speed = 5;
var path = []
var node_current;
# Called when the node enters the scene tree for the first time.
func _ready():
	target_pos = position;
	var main = get_node("/root/Main");
	main.connect("HighlightUpdated", check_highlight);
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if path != null and path.size() > 0:
		move_to_next_path_point(delta)
	pass
	

func initialise(_server, node_target):
	server = _server;
	node_current = node_target;
	position = node_current.position;
	print(server);

func check_highlight(node_target):
	if node_target != null && node_target != node_current:
		move_to_node(node_target)


func move_to_node(node_target):
	path = server.GetPathFromTo(node_current, node_target);


func move_to_next_path_point(delta):
	target_pos = path[0].position
	var dist = abs(position - target_pos);
	if dist.length() > 0.2:
		position += ((target_pos - position).normalized() * (delta * speed));
	else:
		position = target_pos;
		node_current = path[0];
		path.remove_at(0);
