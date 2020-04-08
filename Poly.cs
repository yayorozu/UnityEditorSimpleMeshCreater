using System;

namespace Yorozu.SimpleMesh
{
	/// <summary>
	/// 1ポリゴン
	/// </summary>
	[Serializable]
	internal class Poly
	{
		public PolyVertex[] Vertices;
			
		/// <summary>
		/// 降順
		/// </summary>
		public bool IsDesc;

		public Poly(int startIndex)
		{
			Vertices = new PolyVertex[3];
			for (int i = 0; i < Vertices.Length; i++)
				Vertices[i] = new PolyVertex(startIndex + i);
					
			IsDesc = false;
		}

		public int[] GetIndexes()
		{
			var indexes = new int[3];
			if (IsDesc)
			{
				indexes[0] = Vertices[2].GetIndex();
				indexes[1] = Vertices[1].GetIndex();
				indexes[2] = Vertices[0].GetIndex();
			}
			else
			{
				indexes[0] = Vertices[0].GetIndex();
				indexes[1] = Vertices[1].GetIndex();
				indexes[2] = Vertices[2].GetIndex();
			}
			return indexes;
		}
	}
}