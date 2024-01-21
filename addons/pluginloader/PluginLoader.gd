@tool
extends EditorPlugin

const EditorLinePlugin = preload("res://Scripts/EditorLine.gd")
var plugin = EditorLinePlugin.new();
var inspector : EditorInspector

func _enter_tree():
	inspector = self.get_editor_interface().get_inspector();
	add_node_3d_gizmo_plugin(plugin);

func _exit_tree():
	remove_node_3d_gizmo_plugin(plugin);
	
# Prevents the InputEvent from reaching other Editor classes.
func _forward_3d_gui_input(camera, event):
	#plugin.update();
	return EditorPlugin.AFTER_GUI_INPUT_PASS

func _handles(object):
	return true
