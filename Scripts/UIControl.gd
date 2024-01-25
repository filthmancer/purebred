extends Control

const ServerNode = preload("res://Scripts/Network/ServerNode.cs");
const LinkInstance = preload("res://Scripts/Network/LinkInstance.cs");
const IDescribable = preload("res://Scripts/InteractableArea3D.cs")
const Main = preload("res://Scripts/Main.cs")
const action_button:PackedScene = preload("res://scenes/tool_button.tscn");
var server;

var highlight_components = []
var target; 
var button_data = {}
var node_buttons = ["miner", "cage", "heatsink", "wallet"];
var link_buttons = ["firewall"];
# Called when the node enters the scene tree for the first time.
func _ready():
	
	var main = get_node("/root/Main");
	main.connect("ServerGenerationComplete", get_server);
	main.connect("InteractableOver", enter_highlight);
	main.connect("InteractableExit", exit_highlight);
	main.connect("HighlightSelected", select_highlight);
	main.connect("HighlightDeselected", deselect_highlight);

	generate_button_data();
	
	button_data["firewall"] = {
		id = "firewall",
		cost = "0",
		name = "Firewall",
		description = "Blocks all movement between these nodes",
	}
	create_actionbutton(button_data["firewall"], firewall_button_press, Callable(is_link_instance),load("res://Assets/art/wenrexa/СommonElement/Icon05.png"));
	pass # Replace with function body.

func generate_button_data():
	for buttontype in node_buttons:
		var script = load("res://Scripts/Network/Components/" + buttontype + ".gd");
		var res = load("res://data/components/" + buttontype + ".tres");
		button_data[buttontype] = {
			id = buttontype,
			cost= res.get("cost"),
			name= script.get("StaticName") if script.get("StaticName") != null else buttontype,
			description = script.get("StaticDescription"),
		};
		create_actionbutton(button_data[buttontype], Callable(button_select).bind(buttontype), Callable(is_node_instance),res.get("icon"))
	
		
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	if server == null:
		return
	$credits.set_text("CR: " + str(server.Credits) + " / " + str(server.CreditsMax()));
	$data.set_text("D: " + str(server.Data) + " / " + str(server.DataMax()))
	$heat.set_text(str(server.Heat) + "/" + str(server.HeatMax));
	if(target != null):
		tooltip_enter(target.call("Name"), target.call("Description"));
	#update_highlight_description(server.interactable_highlighted);
	_process_tooltip_position();
	pass

func _process_tooltip_position():
	var pos = get_viewport().get_mouse_position();
	
	if(pos.y + $tooltip.size.y > get_viewport_rect().size.y):
		pos.y -= $tooltip.size.y;
	if pos.x + $tooltip.size.x > get_viewport_rect().size.x:
		pos.x -= $tooltip.size.x + 30;
	else:
		pos.x += 30;
	$tooltip.position = pos;
	
	
func get_server(_server):
	server = _server
	
func create_actionbutton(data, _func_select, _func_visibility, icon):
	var button = action_button.instantiate()
	$Buttons.add_child(button);
	button.connect("button_down", _func_select);
	button.connect("mouse_entered", Callable(tooltip_enter).bind(data.name, data.description, data.cost));
	button.connect("mouse_exited", Callable(tooltip_exit));
	data.button = button
	data.visibility = _func_visibility;
	#buttons_visibilitytest[data.id] = _func_visibility;
	button.get_node("Icon").texture = icon;
	button.hide();
	
func tooltip_enter(name, description = null, cost = null):
	$tooltip/name.set_text(name);
	
	if description != null: 
		$tooltip/description.show();
		$tooltip/description.set_text(description);
	else:
		$tooltip/description.hide();
		
	if cost != null:
		$tooltip/cost.show();
		$tooltip/cost.set_text(str(cost));
	else:
		$tooltip/cost.hide();
	$tooltip.show();
	
func tooltip_exit():
	$tooltip.hide();
	
func update_highlight_description(node):
	if node == null:
		exit_highlight(node);
	else:
		var description = str(node);
		var name = str(node);
		if node.has_method("Description"):
			description = node.call("Description");
		if node.has_method("Name"):
			name = node.call("Name");
			
		$description_box/description_text.set_text(description);
		$name_text.set_text(name);
		$description_box.show();
		$name_text.show();
		
		for comp in highlight_components:
			comp.queue_free();
			highlight_components.erase(comp);
		if node is ServerNode:
			for comp in node.GetComponents():
				var newcomp = $description_box/Components/component.duplicate();
				newcomp.show();
				newcomp.get_node("description_text").set_text(comp.Description())
				newcomp.get_node("name_text").set_text(comp.Name());
				$description_box/Components.add_child(newcomp);
				highlight_components.append(newcomp);
	pass
	
func enter_highlight(node):
	target = node;
	tooltip_enter(node.call("Name"), node.call("Description"));
	#if server.interactable_selected!= null &&server.interactable_selected != node:
	#	return;
	#update_highlight_description(node);
	
	
func exit_highlight(node):
	target = null;
	tooltip_exit()
	#if server.interactable_selected != null:
	#	return;
	#$description_box.hide();
	#$name_text.hide();
	#for comp in highlight_components:
	#	comp.queue_free();
	#	highlight_components.erase(comp);
	
func is_node_instance(n):
	return n is ServerNode;
	
func is_link_instance(l):
	return l is LinkInstance;

func has_no_components(n):
	var c = n.GetComponents()
	return c == null || c.length == 0;
	
	
func select_highlight(node = null):
	update_highlight_description(node);
	for id in button_data:
		if node == null:
			button_data[id].button.hide();
		else:
			var visible = button_data[id].visibility.call(node);
			button_data[id].button.visible = visible;
	return;
	
func deselect_highlight(node = null):
	select_highlight(null);
	update_highlight_description(server.interactable_highlighted)
	
func button_select(id):
	if server.interactable_selected != null:
		server.interactable_selected.BuildComponent(id);
		
func component_button_press():
	if server.interactable_selected != null :
		server.interactable_selected.BuildComponent("cage");
		
func miner_button_press():
	if server.interactable_selected != null:
		server.interactable_selected.BuildComponent("miner");
	
func move_button_press():
	get_node("/root/Main").emit_signal("MoveActors", server.interactable_selected);
	
func firewall_button_press():
	if server.interactable_selected != null :
		server.interactable_selected.BuildComponent("firewall");
		
func heatsink_button_press():
	if server.interactable_selected != null:
		server.interactable_selected.BuildComponent("heatsink");
		
func wallet_button_press():
	if server.interactable_selected != null:
		server.interactable_selected.BuildComponent("wallet");
