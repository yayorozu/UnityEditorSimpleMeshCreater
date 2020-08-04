using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Yorozu.SimpleMesh
{
	internal static class CacheOption
	{
		private static Dictionary<Vector2Int, GUILayoutOption> _dic = new Dictionary<Vector2Int, GUILayoutOption>();

		internal static GUILayoutOption Get(int width = 0, int height = 0)
		{
			var key = new Vector2Int(width, height);
			if (!_dic.ContainsKey(key))
			{
				if (width > 0)
					_dic.Add(key, GUILayout.Width(width));
				if (height > 0)
					_dic.Add(key, GUILayout.Height(width));
			}

			return _dic[key];
		}

		internal static GUILayoutOption Width(int width)
		{
			return Get(width, 0);
		}

		internal static GUILayoutOption Height(int height)
		{
			return Get(0, height);
		}
	}

	[Serializable]
	internal class MeshData
	{
		[SerializeField]
		private Mesh _mesh;

		[SerializeField]
		private List<Poly> _polys;

		[SerializeField]
		private MeshRenderer _meshRenderer;

		private GameObject _gameObject;

		private int _editIndex;

		internal void Init(Material material)
		{
			_mesh = new Mesh();
			_polys = new List<Poly>();
			_editIndex = -1;

			_gameObject = new GameObject("Mesh Object");
			var meshFilter = _gameObject.AddComponent<MeshFilter>();
			_meshRenderer = _gameObject.AddComponent<MeshRenderer>();
			meshFilter.mesh = _mesh;
			SetMaterial(material);
		}

		internal void Dispose()
		{
			GameObject.DestroyImmediate(_mesh);
			GameObject.DestroyImmediate(_gameObject);
		}

		internal void SetMaterial(Material material)
		{
			_meshRenderer.material = material;
		}

		/// <summary>
		/// 0, 0 ~ 1, 1 の範囲で自動的にUVをセット
		/// </summary>
		internal void FitUV()
		{
			// Min Max を調査
			var minX = _mesh.vertices.Min(v => v.x);
			var minY = _mesh.vertices.Min(v => v.y);

			var maxX = _mesh.vertices.Max(v => v.x);
			var maxY = _mesh.vertices.Max(v => v.y);

			var uv = _mesh.uv;
			for (var i = 0; i < uv.Length; i++)
			{
				var u = Mathf.InverseLerp(minX, maxX, _mesh.vertices[i].x);
				var v = Mathf.InverseLerp(minY, maxY, _mesh.vertices[i].y);
				uv[i] = new Vector2(u, v);
			}
			_mesh.uv = uv;
		}

		/// <summary>
		/// 座標を丸める
		/// </summary>
		internal void RoundPosition()
		{
			var vertices = _mesh.vertices;
			for (var i = 0; i < vertices.Length; i++)
			{
				vertices[i] = new Vector3(
					Mathf.RoundToInt(vertices[i].x),
					Mathf.RoundToInt(vertices[i].y),
					Mathf.RoundToInt(vertices[i].z)
				);
			}
			_mesh.vertices = vertices;
		}

		internal void OnGUI()
		{
			if (GUILayout.Button("Add Vertex"))
			{
				AddPoly();
			}

			for (var i = 0; i < _polys.Count; i++)
			{
				EditorGUILayout.LabelField($"Poly{i}");

				using (new EditorGUILayout.VerticalScope("Box"))
				{
					using (var check = new EditorGUI.ChangeCheckScope())
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							_polys[i].IsDesc = EditorGUILayout.Toggle("Index Sort Desc", _polys[i].IsDesc);
							if (GUILayout.Button("Delete"))
							{
								RemoveElement(i);
								return;
							}
						}
						for (var si = 0; si < _polys[i].Vertices.Length; si++)
						{
							var vertex = _polys[i].Vertices[si];
							using (new EditorGUILayout.HorizontalScope())
							{
								if (GUILayout.Button($"{i * 3 + si}", CacheOption.Width(30)))
								{
									_editIndex = i * 3 + si;
									GUI.FocusControl(string.Empty);
								}
								if (vertex.IsShare)
								{
									EditorGUILayout.LabelField($"ShareIndex:{vertex.ShareIndex}");
								}
								else
								{
									EditorGUILayout.LabelField($"Vertex:{_mesh.vertices[vertex.MeshIndex]}\t" +
									                           $"UV:{_mesh.uv[vertex.MeshIndex]}");

									GUI.enabled = false;
									EditorGUILayout.ColorField(_mesh.colors[vertex.MeshIndex], CacheOption.Width(50));
									GUI.enabled = true;
								}
							}

						}

						if (check.changed)
						{
							// 法線等計算
							var triangles = new List<int>();
							foreach (var poly in _polys)
								triangles.AddRange(poly.GetIndexes());

							_mesh.triangles = triangles.ToArray();
							_mesh.RecalculateBounds();
							_mesh.RecalculateNormals();
							_mesh.RecalculateTangents();
						}
					}
				}
			}
		}

		internal void DrawEdit()
		{
			var polyIndex = Mathf.FloorToInt(_editIndex / 3);

			using (new EditorGUILayout.VerticalScope("box"))
			{
				if (_editIndex < 0 || polyIndex >= _polys.Count)
				{
					EditorGUILayout.LabelField($"None Select Edit Index");
					return;
				}

				EditorGUILayout.LabelField($"Edit Index {_editIndex}");

				var elementIndex = _editIndex % 3;
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					_polys[polyIndex].Vertices[elementIndex].Draw(_mesh);
					if (check.changed)
					{
						BuildMesh();
					}
				}
			}
		}

		private void RemoveElement(int index)
		{
			_polys.RemoveAt(index);
			var startIndex = index * 3;
			var vertices = _mesh.vertices;
			var uv = _mesh.uv;
			var colors = _mesh.colors;
			for (int i = 0; i < 3; i++)
			{
				ArrayUtility.RemoveAt(ref vertices, startIndex);
				ArrayUtility.RemoveAt(ref uv, startIndex);
				ArrayUtility.RemoveAt(ref colors, startIndex);
			}
			// 一度空にしないとエラーになる
			_mesh.triangles = new int[0];
			_mesh.vertices = new Vector3[0];
			_mesh.uv = new Vector2[0];
			_mesh.colors = new Color[0];

			_mesh.vertices = vertices;
			_mesh.uv = uv;
			_mesh.colors = colors;
			BuildMesh();
		}

		private void BuildMesh()
		{
			// 法線等計算
			var triangles = new List<int>();
			foreach (var poly in _polys)
				triangles.AddRange(poly.GetIndexes());

			_mesh.triangles = triangles.ToArray();
			_mesh.RecalculateBounds();
			_mesh.RecalculateNormals();
			_mesh.RecalculateTangents();
		}

		/// <summary>
		/// 要素追加
		/// </summary>
		private void AddPoly()
		{
			var vertices = _mesh.vertices;
			ArrayUtility.Add(ref vertices, Vector3.zero);
			ArrayUtility.Add(ref vertices, Vector3.zero);
			ArrayUtility.Add(ref vertices, Vector3.zero);

			var uv = _mesh.uv;
			ArrayUtility.Add(ref uv, Vector2.zero);
			ArrayUtility.Add(ref uv, Vector2.zero);
			ArrayUtility.Add(ref uv, Vector2.zero);

			var colors = _mesh.colors;
			ArrayUtility.Add(ref colors, Color.white);
			ArrayUtility.Add(ref colors, Color.white);
			ArrayUtility.Add(ref colors, Color.white);

			_mesh.vertices = vertices;
			_mesh.uv = uv;
			_mesh.colors = colors;
			var triangles = new List<int>();
			foreach (var poly in _polys)
				triangles.AddRange(poly.GetIndexes());
			_mesh.triangles = triangles.ToArray();

			_polys.Add(new Poly(_polys.Count * 3));
		}

		/// <summary>
		/// Scene に線を描画
		/// </summary>
		internal void DrawHandles()
		{
			foreach (var p in _polys)
			{
				Handles.color = Color.red;
				Handles.DrawLines(new[]
				{
					_mesh.vertices[p.Vertices[0].GetIndex()], _mesh.vertices[p.Vertices[1].GetIndex()],
					_mesh.vertices[p.Vertices[1].GetIndex()], _mesh.vertices[p.Vertices[2].GetIndex()],
					_mesh.vertices[p.Vertices[2].GetIndex()], _mesh.vertices[p.Vertices[0].GetIndex()],
				});
				Handles.color = Color.white;
			}

			var polyIndex = Mathf.FloorToInt(_editIndex / 3);

			if (_editIndex < 0 || polyIndex >= _polys.Count)
				return;

			var elementIndex = _editIndex % 3;
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				var index = _polys[polyIndex].Vertices[elementIndex].GetIndex();
				var pos = _mesh.vertices[index];
				pos = Handles.PositionHandle(pos, Quaternion.identity);
				if (check.changed)
				{
					var c = _mesh.vertices;
					c[index] = pos;
					_mesh.vertices = c;
					BuildMesh();
				}
			}
		}

		/// <summary>
		/// Mesh を保存
		/// </summary>
		internal void Save()
		{
			var path = EditorUtility.SaveFilePanelInProject("Select Mesh Path", "Mesh", "asset", "");
			if (string.IsNullOrEmpty(path))
				return;

			var saveMesh = GameObject.Instantiate(_mesh);
			AssetDatabase.CreateAsset(saveMesh, path);
			AssetDatabase.Refresh();
		}
	}
}
