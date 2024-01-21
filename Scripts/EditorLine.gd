@tool
extends EditorNode3DGizmoPlugin
const ServerNode = preload("res://Scripts/Network/ServerNode.cs")

func _get_gizmo_name():
	return "EditorLine"
	
func _init():
	create_material("main", Color(1,1,1))
	create_handle_material("handles")
	
func _set_handle(gizmo, handle_id, secondary, camera, screen_pos):
	#print("HII");
	_redraw(gizmo);
	
func _has_gizmo(for_node_3d):
	return for_node_3d is ServerNode

func _redraw(gizmo):
	gizmo.clear()
	
	var node3d = gizmo.get_node_3d()
	
	var lines = PackedVector3Array()
	var handles = PackedVector3Array()
	
	for l in node3d.linked_nodes:
		lines.push_back(Vector3.ZERO);
		lines.push_back(l.position - node3d.position);
		handles.push_back(l.position - node3d.position);
	
	gizmo.add_lines(lines, get_material("main",gizmo), true);
	gizmo.add_handles(handles, get_material("handles", gizmo), []);

