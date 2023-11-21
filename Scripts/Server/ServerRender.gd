extends Area2D

var server;
var link_color_default = Color(200,200,200)
var link_color_highlighted = Color(255,0,0)
# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass
	
func initialise(_server):
	server = _server;

func _draw():
	if server == null:
		return
	for l in server.link_list.size():
		var a = server.node_instances[server.link_list[l][0]];
		var b = server.node_instances[server.link_list[l][1]];
		var col = link_color_default;
		if server.highlighted_node == a || server.highlighted_node == b:
			col = link_color_highlighted
		draw_line(a.position,b.position,col, 10)
