extends GridContainer
var button_ids = []
var button_data = {}
const action_button:PackedScene = preload("res://scenes/tool_button.tscn");
var control;
var main;
# Called when the node enters the scene tree for the first time.
func _ready():
	main = get_node("/root/Main");
	control = get_node("/root/Main/UI");
	main.connect("OnMarketPurchaseCompleted", on_purchase);
	main.connect("ServerGenerationComplete", get_server);
	
	pass # Replace with function body.

func get_server(_server):
	button_ids = main.GetMarketIDs();
	generate_button_data();

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func on_purchase(id):
	#button_ids.erase(id);
	generate_button_data();
	
func generate_button_data():
	for id in button_ids:
		if button_data.has(id):
			continue;
		var script = load("res://Scripts/Network/Components/" + id + ".gd");
		var res = load("res://data/components/" + id + ".tres");
		button_data[id] = {
			id = id,
			cost= res.get("PurchaseCost_Credits"),
			name= script.get("StaticName") if script.get("StaticName") != null else id,
			description = script.get("StaticDescription"),
		};
		create_actionbutton(button_data[id], Callable(button_select).bind(id),res.get("icon"))

func create_actionbutton(data, _func_select, icon):
	var button = action_button.instantiate()
	add_child(button);
	button.connect("button_down", _func_select);
	button.connect("mouse_entered", Callable(control.tooltip_enter).bind(data.name, data.description, data.cost));
	button.connect("mouse_exited", Callable(control.tooltip_exit));
	data.button = button
	button.get_node("Icon").texture = icon;
	#button.hide();
	
			
func button_select(id):
	main.PurchaseItemFromMarket(id);
