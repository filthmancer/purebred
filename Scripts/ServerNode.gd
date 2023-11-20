extends Area3D

var server;
@export var ID:int;
@export var linked_nodes : Array[Node]

# Called when the node enters the scene tree for the first time.
func _ready():
	add_to_group("ServerNodes",true)
	pass


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	

	
func initialise(pos, _server):
	if pos != null:
		position = pos
	server = _server

func _on_mouse_entered():
	server.set_target_node(self);
	pass # Replace with function body.


func _on_mouse_exited():
	server.set_target_node(null);
	pass # Replace with function body.
