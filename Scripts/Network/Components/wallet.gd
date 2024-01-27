extends "res://Scripts/Network/Components/ServerComponent.gd"

const Wallet = preload("res://Scripts/Network/Components/wallet.gd")
const Miner = preload("res://Scripts/Network/Components/Miner.gd")
@export var creditMaxIncrease = 500
@export var creditsRouteRate = 3;
@export var creditsRouteTickRate = 5;
@export var creditsRouteRange = 2;

var tickCurrent = 0;
func Name():
	return "Wallet";
func Description():
	return "Credit Max Increase: " + str(creditMaxIncrease) + "\nRoutes neighbouring credits to this node."
			
func initialise(_nodeInstance):
	super(_nodeInstance);
	ID = "wallet";
	
func on_tick():
	super();
	tickCurrent+=1;
	if tickCurrent == creditsRouteTickRate:
		tickCurrent = 0;
		for nbour in get_neighbours(creditsRouteRange):
			if nodeInstance.Credits >= nodeInstance.GetCreditsMax():
				return;
			if nbour.Credits > 0:
				for comp in nbour.GetComponents():
					if comp is Wallet:
						return;
					if comp is Miner:
						var routedCredits = min(creditsRouteRate, nbour.Credits);
						nodeInstance.server.TransferResource("credits", routedCredits, nodeInstance, nbour);

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func get_heat():
	return 0;
	
func get_creditsmax():
	return creditMaxIncrease;
