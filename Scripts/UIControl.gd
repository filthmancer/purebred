extends Control

const ServerNode = preload("res://Scripts/Server/ServerNode.cs");
const IDescribable = preload("res://Scripts/InteractableArea3D.cs")
const Main = preload("res://Scripts/Main.cs")
var server;
# Called when the node enters the scene tree for the first time.
func _ready():
	server = get_node("/root/Main/Server")
	var main = get_node("/root/Main");
	main.connect("HighlightUpdated", update_highlight_description);
	main.connect("HighlightSelected", select_highlight);
	$ToolButtonA.connect("button_up", component_button_press)
	$ToolButtonB.connect("button_up", move_button_press);
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func component_button_press():
	server.targetedNode.BuildComponent("servercomponent");
	
func move_button_press():
	get_node("/root/Main").call("EmitGodotSignal","MoveActors", server.targetedNode, "allies");
	
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

	
func select_highlight(node = null):
	if node == null:
		$ToolButtonA.hide();
		$ToolButtonB.hide();
	else:
		$ToolButtonA.show();
		$ToolButtonB.show();
