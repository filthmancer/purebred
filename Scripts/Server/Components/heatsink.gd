extends "res://Scripts/Server/Components/ServerComponent.gd"

@export var heatMitigated = 10
@export var heatPerNeighbour = 2;
var neighbour_heat_mitigated = 0;
func Name():
	return "Heat Sink";
func Description():
	return "Heat: " + str(-heatMitigated) + "\nNeighbour heat mitigated: " + str(neighbour_heat_mitigated);
	#"Lowers current server heat by " + str(heatMitigated) + "." + "Lowers active heat of neighbouring components by " + str(heatPerNeighbour);
			
func initialise(_nodeInstance):
	super(_nodeInstance);
	ID = "heatsink";
	
func on_tick():
	super();


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func get_heat():
	neighbour_heat_mitigated = 0;		
	var neighbours = get_neighbour_components();
	if neighbours != null:
		for comp in neighbours:
			neighbour_heat_mitigated -= 2;
		
	return -heatMitigated + neighbour_heat_mitigated;
