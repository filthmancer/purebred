extends "res://Scripts/Network/Components/ServerComponent.gd"


@export var durability_decrease_per_tick = 0.2

func Name():
	return "Virus Trap";
func Description():
	return "Traps the next virus that enters this node. Durability decreases over time. Virus will continue to attack this component while it is trapped";
	
func initialise(_nodeInstance):
	super(_nodeInstance);
	ID = "cage";

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func on_tick():
	super();
	if durability > 0:
		durability -= durability_decrease_per_tick;
	else:
		print("TRAP DESTROYED");
		
	
		
func get_heat():
	return 1;

