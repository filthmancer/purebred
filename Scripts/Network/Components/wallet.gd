extends "res://Scripts/Network/Components/ServerComponent.gd"

const Wallet = preload("res://Scripts/Network/Components/wallet.gd")
@export var creditMaxIncrease = 500
@export var creditsRouteRate = 3;
func Name():
	return "Wallet";
func Description():
	return "Credit Max Increase: " + str(creditMaxIncrease) + "\nRoutes neighbouring credits to this node."
			
func initialise(_nodeInstance):
	super(_nodeInstance);
	ID = "wallet";
	
func on_tick():
	super();
	for nbour in get_neighbours():
		if nodeInstance.Credits >= nodeInstance.GetCreditsMax():
			return;
		if nbour.Credits > 0:
			for comp in nbour.GetComponents():
				if comp is Wallet:
					return;
			var routedCredits = min(creditsRouteRate, nbour.Credits);
			nbour.Credits = clamp(nbour.Credits - routedCredits, 0, nbour.GetCreditsMax());
			nodeInstance.Credits += routedCredits;
			


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func get_heat():
	return 0;
	
func get_creditsmax():
	return creditMaxIncrease;
