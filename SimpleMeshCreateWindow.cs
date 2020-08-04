using UnityEditor;
using UnityEngine;

namespace Yorozu.SimpleMesh
{
	public class CreateSimpleMeshWindow : EditorWindow
	{
		[MenuItem("Tools/CreateSimpleMesh")]
		private static void ShowWindow()
		{
			var window = GetWindow<CreateSimpleMeshWindow>();
			window.titleContent = new GUIContent("Create Mesh");
			window.minSize = new Vector2(350f, 1f);
			window.Show();
		}

		[SerializeField]
		private MeshData _data;

		private Material _material;
		private Vector2 _position;

		private void OnEnable()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			SceneView.duringSceneGui += OnSceneGUI;
		}

		private void OnDestroy()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			if (_data != null)
			{
				_data.Dispose();
				_data = null;
			}
		}

		private void OnGUI()
		{
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				_material = EditorGUILayout.ObjectField("Material", _material, typeof(Material), false) as Material;
				if (check.changed)
				{
					_data?.SetMaterial(_material);
				}
			}

			if (_data == null)
			{
				if (GUILayout.Button("Create Mesh"))
				{
					_data = new MeshData();
					_data.Init(_material);
				}
				return;
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Reset Mesh"))
				{
					_data.Dispose();
					_data = new MeshData();
					_data.Init(_material);
					return;
				}
				if (GUILayout.Button("Delete Mesh"))
				{
					_data.Dispose();
					_data = null;
					return;
				}
			}

			GUILayout.Space(5);
			EditorGUILayout.LabelField("Mesh Data");
			_data.DrawEdit();

			// Tool Button
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("UV Fit"))
				{
					_data.FitUV();
				}
				if (GUILayout.Button("Round Position"))
				{
					_data.RoundPosition();
				}
			}

			using (new EditorGUILayout.VerticalScope("helpBox"))
			{
				using (var scroll = new EditorGUILayout.ScrollViewScope(_position))
				{
					_position = scroll.scrollPosition;
					_data.OnGUI();
				}
			}

			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Save Mesh"))
			{
				_data.Save();
			}
		}

		private void OnSceneGUI(SceneView sceneView)
		{
			_data?.DrawHandles();
		}
	}
}
