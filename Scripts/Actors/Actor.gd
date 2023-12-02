extends Area3D

var server;
var target_pos;
var speed = 5;
var path = []
var node_current;
const ServerNode = preload("res://Scripts/Server/ServerNode.cs");
const LinkInstance = preload("res://Scripts/Server/LinkInstance.cs");

var node_last;
# Called when the node enters the scene tree for the first time.
func _ready():
	target_pos = position;
	var main = get_node("/root/Main");
	main.connect("MoveActors", move_to_node);
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


func move_to_node(target= null):
	if target != null && target.get_script() == ServerNode&& target != node_current:
		path = server.GetPathFromTo(node_current, target, ["avoid_cages"]);


func move_to_next_path_point(delta):
	if path[0] is LinkInstance:
		target_pos = path[0].position;
	elif path[0] is ServerNode:
		target_pos = path[0].position
		
	var dist = abs(position - target_pos);
	if dist.length() > 0.2:
		position += ((target_pos - position).normalized() * (delta * speed));
	else:
		position = target_pos;
		node_last = node_current;
		node_current = path[0];
		path.remove_at(0);
