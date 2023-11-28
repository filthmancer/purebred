using Godot;
using System;
using System.Collections.Generic;

public partial class DebugRender : InteractableArea3D, IDisposablePoolResource, IDescribableNode
{
	private MeshInstance3D _meshInstance;
	public MeshInstance3D meshInstance
	{
		get
		{
			if (_meshInstance == null)
			{
				_meshInstance = GetNode<MeshInstance3D>("mesh");
			}

			return _meshInstance;
		}
	}

	public IDisposablePool Pool { get; set; }

	public string Description()
	{
		string desc = $"Links {0} and {1}";
		return desc;
	}

	public string Name()
	{
		return "Link";
	}

	List<Vector3> points = new List<Vector3>();
	public List<Vector3> Points => points;
	[Export]
	private float width = 0.1F;

	private static PackedScene prefab;
	public static DebugRender Instantiate(IDisposablePool _pool)
	{
		if (prefab == null)
			prefab = GD.Load<PackedScene>("res://scenes/debug_render.tscn");

		var inst = prefab.Instantiate<DebugRender>();
		inst.Pool = _pool;
		return inst;
	}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	public void Initialise(Vector3[] p)
	{
		for (int i = 0; i < p.Length; i++)
		{
			AddPoint(p[i]);
		}

		RegenerateLine();
	}

	public bool HasPoint(Vector3 p)
	{
		return points.Contains(p);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void SetColor(Color col)
	{
		var mat = (StandardMaterial3D)meshInstance.GetActiveMaterial(0);
		mat.AlbedoColor = col;
	}

	private void AddPoint(Vector3 point)
	{
		points.Add(point);
	}

	private void RegenerateLine()
	{
		meshInstance.Mesh = new ImmediateMesh();
		ImmediateMesh m = meshInstance.Mesh as ImmediateMesh;
		m.SurfaceBegin(Mesh.PrimitiveType.TriangleStrip);
		for (int i = 1; i < points.Count; i++)
		{
			Vector3 start = points[i - 1];
			Vector3 end = points[i];
			Vector3 normal = (end - start).Normalized();
			Vector3 cross = (normal).Cross((start + Vector3.Up) - start);
			Vector3 crossB = (-normal).Cross((start + Vector3.Up) - start);

			Vector3 a = start + cross * width;
			Vector3 b = start - cross * width;
			Vector3 c = end + cross * width;
			Vector3 d = end - cross * width;

			m.SurfaceSetNormal(new Vector3(0, 0, 1));
			m.SurfaceSetUV(new Vector2(0, 0));
			m.SurfaceAddVertex(a);

			m.SurfaceSetNormal(new Vector3(0, 0, 1));
			m.SurfaceSetUV(new Vector2(0, 0));
			m.SurfaceAddVertex(b);

			m.SurfaceSetNormal(new Vector3(0, 0, 1));
			m.SurfaceSetUV(new Vector2(0, 0));
			m.SurfaceAddVertex(c);

			m.SurfaceSetNormal(new Vector3(0, 0, 1));
			m.SurfaceSetUV(new Vector2(0, 0));
			m.SurfaceAddVertex(b);

			m.SurfaceSetNormal(new Vector3(0, 0, 1));
			m.SurfaceSetUV(new Vector2(0, 0));
			m.SurfaceAddVertex(c);

			m.SurfaceSetNormal(new Vector3(0, 0, 1));
			m.SurfaceSetUV(new Vector2(0, 0));
			m.SurfaceAddVertex(d);
		}

		m.SurfaceEnd();
	}

	public void Acquire()
	{

	}
	public void Dispose()
	{

	}

	public override void SetAsTarget(bool active)
	{
		Color col = active ? new Color(1.0F, 0.5F, 0.5F) : new Color(1F, 1F, 1F);
		SetColor(col);
	}
}
