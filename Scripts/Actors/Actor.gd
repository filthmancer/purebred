extends Area3D

var server;
var target_pos;
var speed = 5;
var path = []
var lastpoint;
var node_current;
const ServerNode = preload("res://Scripts/Network/ServerNode.cs");
@export var attributes: Array[Node3D];
const LinkInstance = preload("res://Scripts/Network/LinkInstance.cs");
var label;
var rng;
var state;

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
	if label == null:
		label = Label.new();
		add_child(label);
	else:
		label.set("theme_override_font_sizes/font_size", 30)
		label.position = get_viewport().get_camera_3d().unproject_position(self.position) + Vector2(-20,-100);
		
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
	
func update_move(delta):
	if path != null and path.size() > 0:
		move_to_next_path_point(delta)
	else:
		move_to_point(delta, lastpoint)
	

func initialise(_server, node_target):
	server = _server;
	node_current = node_target;
	position = node_current.position;
	rng = RandomNumberGenerator.new();
	_server.main.connect("OnTick", on_tick)
	lastpoint = node_target.position;
	update_target();
	for attr in attributes:
		attr.initialise(self);


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
		
func update_target():
	var targets = server.GetAllNeighbours(node_current, 1);
	var targetvalues = []
		
	var index = 0
	var total = 0.0
	for t in targets:
		var m = 0
		for attr in attributes:
			m += attr.get_motivation(t);
			
		targetvalues.append(m)
		
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
	
	if index == targets.size():
		index = 0;
		
	move_to_node(targets[index], ["avoid_firewalls"]);
	#path = server.GetPathFromTo(node_current.ID, targets[index].ID, ["avoid_firewalls"]);
	
	pass;
