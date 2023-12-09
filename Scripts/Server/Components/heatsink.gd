extends "res://Scripts/Server/Components/ServerComponent.gd"

@export var heatMitigated = 10
func initialise(_nodeInstance):
	super(_nodeInstance);
	ID = "heatsink";

func on_tick():
	super();


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func get_heat():
	var heat_total = -heatMitigated;
	#var nodecomps = get_node_components();
	#if nodecomps != null:
	#	for comp in nodecomps:
	#		heat_total -= 2;
			
	var neighbours = get_neighbour_components();
	if neighbours != null:
		for comp in neighbours:
			heat_total -= 2;
		
	return heat_total;
