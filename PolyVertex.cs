using System;
using System.CodeDom;
using UnityEditor;
using UnityEngine;

namespace Yorozu.SimpleMesh
{
	[Serializable]
	internal class PolyVertex
	{
		public int MeshIndex;
		public int ShareIndex;

		public PolyVertex(int index)
		{
			MeshIndex = index;
			ShareIndex = -1;
		}

		public int GetIndex()
		{
			return ShareIndex >= 0 ? ShareIndex : MeshIndex;
		}

		public bool IsShare => ShareIndex >= 0;

		public void Draw(Mesh mesh)
		{
			var isSkip = false;
			using (new EditorGUILayout.HorizontalScope())
				isSkip = DrawVertex(mesh);

			if (!isSkip)
			{
				using (new EditorGUILayout.HorizontalScope())
					DrawUV(mesh);

				using (new EditorGUILayout.HorizontalScope())
					DrawColor(mesh);
			}
		}

		private bool DrawVertex(Mesh mesh)
		{
			EditorGUILayout.LabelField("Vertex", GUILayout.Width(60));
			var isShare = IsShare;
			using (var check2 = new EditorGUI.ChangeCheckScope())
			{
				isShare = EditorGUILayout.ToggleLeft("IsShare", isShare, GUILayout.Width(90));
				if (check2.changed)
				{
					if (isShare)
						ShareIndex = 0;
					else
						ShareIndex = -1;
				}
			}

			if (isShare)
			{
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					ShareIndex = EditorGUILayout.IntField(ShareIndex);
					if (check.changed)
					{
						Mathf.Clamp(ShareIndex, 0, int.MaxValue);
					}
				}
			}
			else
			{
				ShareIndex = -1;
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					var vert = mesh.vertices[MeshIndex];
					vert = EditorGUILayout.Vector3Field(GUIContent.none, vert);
					if (check.changed)
					{
						var c = mesh.vertices;
						c[MeshIndex] = vert;
						mesh.vertices = c;
					}
				}
			}

			return isShare;
		}

		private void DrawUV(Mesh mesh)
		{
			EditorGUILayout.PrefixLabel("UV");
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				var uv = mesh.uv[MeshIndex];
				uv = EditorGUILayout.Vector2Field(GUIContent.none, uv);
				if (check.changed)
				{
					var c = mesh.uv;
					c[MeshIndex] = uv;
					mesh.uv = c;
				}
			}
		}

		private void DrawColor(Mesh mesh)
		{
			EditorGUILayout.PrefixLabel("Color");
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				var color = mesh.colors[MeshIndex];
				color = EditorGUILayout.ColorField(GUIContent.none, color);
				if (check.changed)
				{
					var c = mesh.colors;
					c[MeshIndex] = color;
					mesh.colors = c;
				}
			}
		}
	}
}