extends Control

const ServerNode = preload("res://Scripts/Server/ServerNode.cs");
const LinkInstance = preload("res://Scripts/Server/LinkInstance.cs");
const IDescribable = preload("res://Scripts/InteractableArea3D.cs")
const Main = preload("res://Scripts/Main.cs")
const action_button:PackedScene = preload("res://scenes/tool_button.tscn");
var server;

var buttons = {}
var buttons_visibilitytest = {}
var callable_is_node_instance;
var callable_is_link_instance;
# Called when the node enters the scene tree for the first time.
func _ready():
	server = get_node("/root/Main/Server")
	var main = get_node("/root/Main");
	main.connect("InteractableOver", update_highlight_description);
	main.connect("HighlightSelected", select_highlight);
	
	callable_is_node_instance = Callable(is_node_instance);
	callable_is_link_instance = Callable(is_link_instance);
	
	create_actionbutton("build_cage", component_button_press, callable_is_node_instance, "res://Assets/art/wenrexa/СommonElement/Icon02.png");
	create_actionbutton("build_miner", miner_button_press, callable_is_node_instance, "res://Assets/art/wenrexa/СommonElement/Icon03.png");
	create_actionbutton("move_allies", move_button_press, callable_is_node_instance, "res://Assets/art/wenrexa/СommonElement/Icon01.png");
	create_actionbutton("build_firewall", firewall_button_press, callable_is_link_instance, "res://Assets/art/wenrexa/СommonElement/Icon05.png");
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	$credits.set_text("CR: " + str(server.main.Currency));
	$heat.set_text(str(server.Heat) + "/" + str(server.HeatMax));
	pass
	
func create_actionbutton(id, _func_select, _func_visibility, icon):
	var button = action_button.instantiate()
	$Buttons.add_child(button);
	button.connect("button_down", _func_select);
	buttons[id] =  button;
	buttons_visibilitytest[id] = _func_visibility;
	button.get_node("Icon").texture = load(icon);
	
	button.hide();
	
func update_highlight_description(node):
	if node == null:
		$description_box/description_text.text = "";
		$name_text.set_text("");
	else:
		var description = str(node);
		var name = str(node);
		if node.has_method("Description"):
			description = node.call("Description");
		if node.has_method("Name"):
			name = node.call("Name");
			
		$description_box/description_text.set_text(description);
		$name_text.set_text(name);
	pass
	
func is_node_instance(n):
	return n is ServerNode;
	
func is_link_instance(l):
	return l is LinkInstance;

	
func select_highlight(node = null):
	for button in buttons:
		if node == null:
			buttons[button].hide();
		else:
			var visible = buttons_visibilitytest[button].call(node);
			buttons[button].visible = visible;
	return;
		
		
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
