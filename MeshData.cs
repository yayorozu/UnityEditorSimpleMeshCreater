using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yorozu.SimpleMesh
{
	[Serializable]
	internal class MeshData
	{
		[SerializeField]
		private Mesh _mesh;

		[SerializeField]
		private List<Poly> polys;
		
		[SerializeField]
		private MeshRenderer _meshRenderer;

		private GameObject _gameObject;

		private int _editIndex;

		public void Init(Material material)
		{
			_mesh = new Mesh();
			polys = new List<Poly>();
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

		internal void Draw()
		{
			if (GUILayout.Button("Add Vertex"))
			{
				AddPoly();
			}

			for (var i = 0; i < polys.Count; i++)
			{
				polys[i].IsExpand = EditorGUILayout.Foldout(polys[i].IsExpand, $"Poly{i}");
				if (!polys[i].IsExpand)
					continue;

				using (new EditorGUILayout.VerticalScope("Box"))
				{
					using (var check = new EditorGUI.ChangeCheckScope())
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							polys[i].IsDesc = EditorGUILayout.Toggle("Index Sort Desc", polys[i].IsDesc);
							if (GUILayout.Button("Delete"))
							{
								RemoveElement(i);
								GUIUtility.ExitGUI();
							}
						}
						for (var si = 0; si < polys[i].Vertices.Length; si++)
						{
							var vertex = polys[i].Vertices[si];
							using (new EditorGUILayout.HorizontalScope())
							{
								if (GUILayout.Button($"{i * 3 + si}",GUILayout.Width(30)))
								{
									_editIndex = i * 3 + si;
								}
								if (vertex.IsShare)
								{
									EditorGUILayout.LabelField($"ShareIndex:{vertex.ShareIndex}");
								}
								else
								{
									EditorGUILayout.LabelField($"Vertex:{_mesh.vertices[vertex.MeshIndex]}");
									EditorGUILayout.LabelField($"UV:{_mesh.uv[vertex.MeshIndex]}", GUILayout.Width(100));
									GUI.enabled = false;
									EditorGUILayout.ColorField(_mesh.colors[vertex.MeshIndex], GUILayout.Width(50));
									GUI.enabled = true;
								}
							}

						}

						if (check.changed)
						{
							// 法線等計算
							var triangles = new List<int>();
							foreach (var poly in polys)
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

		public void DrawEdit()
		{
			var polyIndex = Mathf.FloorToInt(_editIndex / 3);

			using (new EditorGUILayout.VerticalScope("box"))
			{
				if (_editIndex < 0 || polyIndex >= polys.Count)
				{
					EditorGUILayout.LabelField($"None Select Edit Index");
					return;
				}
				EditorGUILayout.LabelField($"Edit Index {_editIndex}");

				var elementIndex = _editIndex % 3;
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					polys[polyIndex].Vertices[elementIndex].Draw(_mesh);
					if (check.changed)
					{
						BuildMesh();
					}
				}
			}
		}

		private void RemoveElement(int index)
		{
			polys.RemoveAt(index);
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
			foreach (var poly in polys)
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
			foreach (var poly in polys)
				triangles.AddRange(poly.GetIndexes());
			_mesh.triangles = triangles.ToArray();

			polys.Add(new Poly(polys.Count * 3));
		}

		/// <summary>
		/// Scene に線を描画
		/// </summary>
		internal void DrawHandles()
		{
			foreach (var p in polys)
			{
				Handles.DrawLines(new[]
				{
					_mesh.vertices[p.Vertices[0].GetIndex()], _mesh.vertices[p.Vertices[1].GetIndex()],
					_mesh.vertices[p.Vertices[1].GetIndex()], _mesh.vertices[p.Vertices[2].GetIndex()],
					_mesh.vertices[p.Vertices[2].GetIndex()], _mesh.vertices[p.Vertices[0].GetIndex()],
				});
			}
			
			var polyIndex = Mathf.FloorToInt(_editIndex / 3);
			if (_editIndex >= 0 && polyIndex < polys.Count)
			{
				var elementIndex = _editIndex % 3;
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					var index = polys[polyIndex].Vertices[elementIndex].GetIndex();
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
		}

		/// <summary>
		/// Mesh を保存
		/// </summary>
		internal void Save()
		{
			var path = EditorUtility.SaveFilePanel("Select Mesh Path", "Assets/", "", ".asset");
			if (string.IsNullOrEmpty(path))
				return;

			AssetDatabase.CreateAsset(_mesh, path);
			AssetDatabase.Refresh();
		}
	}
}