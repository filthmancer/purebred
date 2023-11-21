using Godot;
using System;
using System.Collections.Generic;

public partial class DebugRender : MeshInstance3D, IDisposablePoolResource
{
	public IDisposablePool Pool { get; set; }

	List<Vector3> points = new List<Vector3>();
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

	public void SetColor(Color col)
	{
		var mat = (StandardMaterial3D)GetActiveMaterial(0);
		mat.AlbedoColor = col;
	}

	private void AddPoint(Vector3 point)
	{
		points.Add(point);
	}

	private void RegenerateLine()
	{
		Mesh = new ImmediateMesh();
		ImmediateMesh m = Mesh as ImmediateMesh;
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
}
