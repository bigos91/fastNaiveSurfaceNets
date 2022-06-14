using UnityEngine;
using UnityEngine.Rendering;
public static class MeshExtensions
{
	static MeshUpdateFlags updateFlags =
		MeshUpdateFlags.DontNotifyMeshUsers |	// no need probably ?
		MeshUpdateFlags.DontRecalculateBounds |	// bounds are calculated in job
		MeshUpdateFlags.DontResetBoneBounds |	// 
		MeshUpdateFlags.DontValidateIndices;	// they are probably fine ;)

	public static void SetMesh(this Mesh mesh, NaiveSurfaceNets.Mesher mesher, bool collider = false)
	{
		if (collider)
			ColliderOnly(mesh, mesher);
		else
			DirectUploadMesh(mesh, mesher);
	}

	private static void ColliderOnly(Mesh mesh, NaiveSurfaceNets.Mesher mesher)
	{
		var indices = mesher.Indices;
		var vertices = mesher.Vertices;

		if (indices.Length > 2 && vertices.Length > 2)
		{
			mesh.indexFormat = IndexFormat.UInt32;
			mesh.SetVertices(vertices);
			mesh.SetIndices(indices, MeshTopology.Triangles, 0);
		}
		else
		{
			mesh.Clear();
		}
	}

	private static void DirectUploadMesh(Mesh mesh, NaiveSurfaceNets.Mesher mesher)
	{
		var indices = mesher.Indices;
		var vertices = mesher.Vertices;

		if (indices.Length > 2 && vertices.Length > 2)
		{
			mesh.SetIndexBufferParams(indices.Length, IndexFormat.UInt32);
			mesh.SetVertexBufferParams(vertices.Length, NaiveSurfaceNets.Vertex.VertexFormat);
			var subMeshDescriptor = new SubMeshDescriptor(0, indices.Length, MeshTopology.Triangles);
			mesh.subMeshCount = 1;
			mesh.SetSubMesh(0, subMeshDescriptor, updateFlags);

			mesh.SetIndexBufferData(indices, 0, 0, indices.Length, updateFlags);
			mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, updateFlags);
			mesh.bounds = mesher.Bounds;
		}
		else
		{
			mesh.Clear();
		}
	}
}
