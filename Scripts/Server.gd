extends Node

@export var node_scene: PackedScene
@export var generated_nodes: bool
@export var generated_links: bool
@export var node_instances: Array
@export var debug_render = true
@export var main_node : Node;

@export var link_instances:Array


var screen_size = Vector2(1200,800)
var link_list = []
var link_count = 5
var rng : RandomNumberGenerator = RandomNumberGenerator.new()
var highlighted_node

# Called when the node enters the scene tree for the first time.
func _ready():
	rng.randomize()
	link_list.resize(link_count)
	
	if generated_nodes:
		generate_nodes(10);

	node_instances = []
	for n in get_children():
		if(!n.is_in_group("ServerNodes")):
			continue;
		node_instances.append(n)
		n.Initialise(n.position, self)
	
	if generated_links:
		generate_links();
	else:
		parse_links_from_nodes();
	
	if debug_render:
		var link_prefab = load("res://Assets/Prefabs/debug_render.tscn")
		for l in link_list:
			var a = node_instances[l[0]]
			var b = node_instances[l[1]]
			var link = link_prefab.instantiate()
			link.Initialise([a.position, b.position])
			add_child(link);
			link_instances.append([link,[a,b]]);
		
	

	
func generate_nodes(num):
	for n in num:
		var node = node_scene.instantiate()
		var node_spawn_location = Vector2(randf() * screen_size.x, randf()* screen_size.y)
		node.Initialise(node_spawn_location, self)
		add_child(node)
	

func generate_links():
	var link_current = 0;
	link_list = []
	while link_current < link_count:
		var a = -1
		var b = -1
		while a == -1 || b == -1 ||  a == b || link_list.has([a,b]) || link_list.has([b,a]):
			a = rng.randi_range(0, node_instances.size() - 1)
			b = rng.randi_range(0, node_instances.size() - 1)
		link_list.append([a,b]);
		link_current += 1
		
func parse_links_from_nodes():
	link_list = [];
	for n in node_instances:
		for l in n.linked_nodes:
			if !link_list.has([n.ID, l.ID]) && !link_list.has([l.ID, n.ID]):
				link_list.append([n.ID, l.ID]);
	


var processed = false;
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(_delta):
	if !processed:
		processed = true;
		main_node.SaveServerLayout("serverA");
	pass
	
func set_target_node(node, active):
	if !active: 
		node = null;
	highlighted_node = node;
	for l in link_instances:
		var color = Color(0.5,0.5,0.5);
		if highlighted_node != null:
			color = Color(1,0,0) if l[1].has(highlighted_node) else Color(0.5,0.5,0.5)
		l[0].SetColor(color);

