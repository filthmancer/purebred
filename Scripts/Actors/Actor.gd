extends Area3D

var server;
var target_pos;
var speed = 5;
var path = []
var lastpoint;
var node_current;
const ServerNode = preload("res://Scripts/Network/ServerNode.cs");
const LinkInstance = preload("res://Scripts/Network/LinkInstance.cs");

var node_last;
# Called when the node enters the scene tree for the first time.
func _ready():
	target_pos = position;
	var main = get_node("/root/Main");
	main.connect("MoveActors", move_to_node);
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	update_move(delta);
	pass
	
func update_move(delta):
	if path != null and path.size() > 0:
		move_to_next_path_point(delta)
	else:
		move_to_point(delta, lastpoint)
	

func initialise(_server, node_target):
	server = _server;
	node_current = node_target;
	position = node_current.position;


func move_to_node(target= null, args = ["avoid_cages"]):
	if target != null && target.get_script() == ServerNode && target != node_current:
		path = server.GetPathFromTo(node_current.ID, target.ID, args);
		lastpoint = path[path.size()-1].position + Vector3(randf_range(-2,2), 0, randf_range(-2,2));


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
		if node_last != null && node_last is ServerNode:
			node_last.RemoveComponent(self);
		node_current = path[0];
		if node_current != null && node_current is ServerNode:
			node_current.AddComponent(self);
		path.remove_at(0);
		
func move_to_point(delta, target_pos):
	var dist = abs(position - target_pos);
	if dist.length() > 0.2:
		position += ((target_pos - position).normalized() * (delta * speed));
		return true
	else:
		position = target_pos;
		return false
		
