extends "res://Scripts/Server/Components/ServerComponent.gd"

@export var mineRate = 1;
func initialise(_nodeInstance):
	super(_nodeInstance);
	ID = "miner";
	
# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func on_tick():
	super();
	nodeInstance.GainResource("credits", mineRate, self);
	
func get_heat():
	return 10;
