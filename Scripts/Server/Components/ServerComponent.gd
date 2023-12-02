extends Node

@export var durability = 10;
var durability_initial;
var ID = "servercomponent";
var nodeInstance;

func Name():
	return "Server Component";
func Description():
	return "Server Component";
	
func initialise(_nodeInstance):
	nodeInstance = _nodeInstance;
	_nodeInstance.server.main.connect("OnTick", on_tick);
	durability_initial = durability;
	if has_node("lifetime"):
		$lifetime.hide();

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass 

func on_tick():
	if durability <= 0:
		nodeInstance.DestroyComponent(self);
	
	if has_node("lifetime"):
		var dur_current = durability/durability_initial;
		if dur_current != 1:
			$lifetime.scale = Vector3(7 * (dur_current), 0.5,1);
			$lifetime.show();
	pass;
	
func get_heat():
	return 0;
	
