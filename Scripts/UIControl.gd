extends Control

const ServerNode = preload("res://Scripts/Server/ServerNode.cs");
const IDescribable = preload("res://Scripts/InteractableArea3D.cs")
# Called when the node enters the scene tree for the first time.
func _ready():
	var main = get_node("/root/Main");
	main.connect("HighlightUpdated", update_highlight_description);
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
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
