using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine; // Assuming Unity for rendering

/// <summary>
/// Static class that provides Delaunay triangulation for heightmap-based terrain rendering.
/// </summary>
public static class DelaunayTriangulation
{
    /// <summary>
    /// Represents a 2D point with x and y coordinates
    /// </summary>
    public class Point
    {
        public float X { get; set; }
        public float Y { get; set; }
        
        public Point(float x, float y)
        {
            X = x;
            Y = y;
        }
        
        public override string ToString()
        {
            return $"({X}, {Y})";
        }
        
        public static float Distance(Point p1, Point p2)
        {
            return (float)Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
    }
    
    /// <summary>
    /// Represents a triangle defined by three points
    /// </summary>
    public class Triangle
    {
        public Point A { get; private set; }
        public Point B { get; private set; }
        public Point C { get; private set; }
        
        public Triangle(Point a, Point b, Point c)
        {
            A = a;
            B = b;
            C = c;
        }
        
        /// <summary>
        /// Checks if a point is inside this triangle's circumcircle
        /// </summary>
        public bool IsPointInCircumcircle(Point p)
        {
            float ax = A.X;
            float ay = A.Y;
            float bx = B.X;
            float by = B.Y;
            float cx = C.X;
            float cy = C.Y;
            
            float d = ((ax * (by - cy)) + (bx * (cy - ay)) + (cx * (ay - by))) * 2;
            
            if (Math.Abs(d) < float.Epsilon)
                return false;
            
            float x1 = ax * ax + ay * ay;
            float x2 = bx * bx + by * by;
            float x3 = cx * cx + cy * cy;
            
            float ux = ((x1 * (by - cy)) + (x2 * (cy - ay)) + (x3 * (ay - by))) / d;
            float uy = ((x1 * (cx - bx)) + (x2 * (ax - cx)) + (x3 * (bx - ax))) / d;
            
            float dx = p.X - ux;
            float dy = p.Y - uy;
            float r = (float)Math.Sqrt((ux - ax) * (ux - ax) + (uy - ay) * (uy - ay));
            
            return (dx * dx + dy * dy) <= (r * r);
        }
    }
    
    /// <summary>
    /// Generates a mesh from a heightmap using Delaunay triangulation
    /// </summary>
    /// <param name="heightmap">2D array of height values</param>
    /// <returns>A Unity Mesh object representing the terrain</returns>
    public static Mesh GenerateTerrainMesh(float[,] heightmap)
    {
        // Create a new mesh
        Mesh mesh = new Mesh();
        
        // Get heightmap dimensions
        int width = heightmap.GetLength(0);
        int height = heightmap.GetLength(1);
        
        // Convert heightmap to points
        List<Point> points = new List<Point>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                points.Add(new Point(x, z));
            }
        }
        
        // Perform Delaunay triangulation
        List<Triangle> triangles = BowyerWatson(points);
        
        // Generate mesh vertices
        Vector3[] vertices = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            float x = points[i].X;
            float z = points[i].Y;
            float y = heightmap[(int)x, (int)z];
            vertices[i] = new Vector3(x, y, z);
        }
        
        // Generate mesh triangles
        int[] triangleIndices = new int[triangles.Count * 3];
        for (int i = 0; i < triangles.Count; i++)
        {
            Triangle t = triangles[i];
            
            // Find vertex indices
            int aIndex = points.FindIndex(p => p.X == t.A.X && p.Y == t.A.Y);
            int bIndex = points.FindIndex(p => p.X == t.B.X && p.Y == t.B.Y);
            int cIndex = points.FindIndex(p => p.X == t.C.X && p.Y == t.C.Y);
            
            // Add triangle indices
            triangleIndices[i * 3] = aIndex;
            triangleIndices[i * 3 + 1] = bIndex;
            triangleIndices[i * 3 + 2] = cIndex;
        }
        
        // Generate UVs
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / width, vertices[i].z / height);
        }
        
        // Calculate normals
        Vector3[] normals = CalculateNormals(vertices, triangleIndices);
        
        // Assign mesh data
        mesh.vertices = vertices;
        mesh.triangles = triangleIndices;
        mesh.uv = uvs;
        mesh.normals = normals;
        
        return mesh;
    }
    
    /// <summary>
    /// Implements the Bowyer-Watson algorithm for Delaunay triangulation
    /// </summary>
    private static List<Triangle> BowyerWatson(List<Point> points)
    {
        // Find min/max coordinates to create a super triangle
        float minX = points.Min(p => p.X);
        float minY = points.Min(p => p.Y);
        float maxX = points.Max(p => p.X);
        float maxY = points.Max(p => p.Y);
        
        float dx = maxX - minX;
        float dy = maxY - minY;
        float dmax = Math.Max(dx, dy);
        float xmid = (minX + maxX) / 2;
        float ymid = (minY + maxY) / 2;
        
        // Create a super triangle that contains all points
        Point p1 = new Point(xmid - 20 * dmax, ymid - dmax);
        Point p2 = new Point(xmid, ymid + 20 * dmax);
        Point p3 = new Point(xmid + 20 * dmax, ymid - dmax);
        Triangle superTriangle = new Triangle(p1, p2, p3);
        
        // Initialize triangulation with the super triangle
        List<Triangle> triangulation = new List<Triangle> { superTriangle };
        
        // Add each point one by one
        foreach (Point point in points)
        {
            // Find all triangles whose circumcircle contains the point
            List<Triangle> badTriangles = new List<Triangle>();
            foreach (Triangle triangle in triangulation)
            {
                if (triangle.IsPointInCircumcircle(point))
                {
                    badTriangles.Add(triangle);
                }
            }
            
            // Find the boundary of the polygonal hole
            List<Edge> polygon = new List<Edge>();
            foreach (Triangle triangle in badTriangles)
            {
                Edge e1 = new Edge(triangle.A, triangle.B);
                Edge e2 = new Edge(triangle.B, triangle.C);
                Edge e3 = new Edge(triangle.C, triangle.A);
                
                // Add edge if it's not shared with any other bad triangle
                if (badTriangles.Count(t => 
                    (t != triangle && 
                    ((Edge.AreEqual(e1, new Edge(t.A, t.B)) || 
                     Edge.AreEqual(e1, new Edge(t.B, t.C)) || 
                     Edge.AreEqual(e1, new Edge(t.C, t.A)))))) == 0)
                {
                    polygon.Add(e1);
                }
                
                if (badTriangles.Count(t => 
                    (t != triangle && 
                    ((Edge.AreEqual(e2, new Edge(t.A, t.B)) || 
                     Edge.AreEqual(e2, new Edge(t.B, t.C)) || 
                     Edge.AreEqual(e2, new Edge(t.C, t.A)))))) == 0)
                {
                    polygon.Add(e2);
                }
                
                if (badTriangles.Count(t => 
                    (t != triangle && 
                    ((Edge.AreEqual(e3, new Edge(t.A, t.B)) || 
                     Edge.AreEqual(e3, new Edge(t.B, t.C)) || 
                     Edge.AreEqual(e3, new Edge(t.C, t.A)))))) == 0)
                {
                    polygon.Add(e3);
                }
            }
            
            // Remove bad triangles from the triangulation
            foreach (Triangle triangle in badTriangles)
            {
                triangulation.Remove(triangle);
            }
            
            // Re-triangulate the polygonal hole
            foreach (Edge edge in polygon)
            {
                Triangle newTriangle = new Triangle(edge.Start, edge.End, point);
                triangulation.Add(newTriangle);
            }
        }
        
        // Remove triangles that share vertices with the super triangle
        triangulation.RemoveAll(t => 
            t.A == p1 || t.A == p2 || t.A == p3 || 
            t.B == p1 || t.B == p2 || t.B == p3 || 
            t.C == p1 || t.C == p2 || t.C == p3);
        
        return triangulation;
    }
    
    /// <summary>
    /// Helper class to represent an edge between two points
    /// </summary>
    private class Edge
    {
        public Point Start { get; private set; }
        public Point End { get; private set; }
        
        public Edge(Point start, Point end)
        {
            Start = start;
            End = end;
        }
        
        public static bool AreEqual(Edge e1, Edge e2)
        {
            return (e1.Start == e2.Start && e1.End == e2.End) || 
                   (e1.Start == e2.End && e1.End == e2.Start);
        }
    }
    
    /// <summary>
    /// Calculate normals for the mesh
    /// </summary>
    private static Vector3[] CalculateNormals(Vector3[] vertices, int[] triangles)
    {
        Vector3[] normals = new Vector3[vertices.Length];
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int vertexIndexA = triangles[i];
            int vertexIndexB = triangles[i + 1];
            int vertexIndexC = triangles[i + 2];
            
            Vector3 triangleNormal = CalculateTriangleNormal(
                vertices[vertexIndexA],
                vertices[vertexIndexB],
                vertices[vertexIndexC]
            );
            
            normals[vertexIndexA] += triangleNormal;
            normals[vertexIndexB] += triangleNormal;
            normals[vertexIndexC] += triangleNormal;
        }
        
        // Normalize all normals
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }
        
        return normals;
    }
    
    /// <summary>
    /// Calculate the normal of a triangle
    /// </summary>
    private static Vector3 CalculateTriangleNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        return Vector3.Cross(ab, ac).normalized;
    }
    
    /// <summary>
    /// Renders the terrain based on the heightmap
    /// </summary>
    /// <param name="heightmap">2D array of height values</param>
    /// <param name="parentTransform">Parent transform to attach the terrain to</param>
    /// <param name="material">Material to apply to the terrain</param>
    /// <returns>The GameObject containing the terrain</returns>
    public static GameObject RenderTerrain(float[,] heightmap, Transform parentTransform, Material material)
    {
        // Create a new GameObject for the terrain
        GameObject terrainObject = new GameObject("Terrain");
        terrainObject.transform.SetParent(parentTransform);
        
        // Add MeshFilter and MeshRenderer components
        MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        
        // Generate and assign the mesh
        meshFilter.mesh = GenerateTerrainMesh(heightmap);
        
        // Assign the material
        meshRenderer.material = material;
        
        // Add a MeshCollider for physics interactions
        MeshCollider meshCollider = terrainObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;
        
        return terrainObject;
    }
}