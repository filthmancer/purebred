@tool
extends EditorNode3DGizmoPlugin
const ServerNode = preload("res://Scripts/Server/ServerNode.cs")

func _get_gizmo_name():
	return "EditorLine"
	
func _init():
	create_material("main", Color(1,0,0))
	create_handle_material("handles")
	
func _has_gizmo(for_node_3d):
	return for_node_3d is ServerNode

func _redraw(gizmo):
	gizmo.clear()
	
	var node3d = gizmo.get_node_3d()
	
	var lines = PackedVector3Array()
	
	for l in node3d.linked_nodes:
		lines.push_back(Vector3(0,0,0));
		lines.push_back(l.position- node3d.position);

	
	#var handles = PackedVector3Array()
	#handles.push_back(Vector3(0,1,0))
	#handles.push_back(Vector3(0, 20,0))
	
	gizmo.add_lines(lines, get_material("main",gizmo), false);
	#gizmo.add_handles(handles, get_material("handles", gizmo), []);
		
