@tool
extends EditorPlugin

const EditorLinePlugin = preload("res://Scripts/EditorLine.gd")
var plugin = EditorLinePlugin.new();

func _init():
	add_node_3d_gizmo_plugin(plugin);
	
func _exit_tree():
	remove_node_3d_gizmo_plugin(plugin);
