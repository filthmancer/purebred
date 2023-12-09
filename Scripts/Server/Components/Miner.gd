extends "res://Scripts/Server/Components/ServerComponent.gd"

const Miner = preload("res://Scripts/Server/Components/Miner.gd")
@export var mineRate = 1;
@export var neighbour_multiplier = 0.2;
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
	
	var multiplier = 1;
	var nodecomps = get_neighbour_components();
	if nodecomps != null:
		for comp in nodecomps:
			if comp is Miner:
				multiplier += neighbour_multiplier;
	var amt = mineRate * multiplier;
	nodeInstance.GainResource("credits", amt, self);
	
func get_heat():
	return 10;
