using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    //Extra data structures.
    public class SquareGrid
    {
        public Square[,] Squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for(int x = 0; x < nodeCountX; x++)
                for(int y = 0; y < nodeCountY; y++)
                {
                    Vector3 position = new Vector3(-mapWidth / 2f + x * squareSize + squareSize / 2, 0, -mapHeight / 2f + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(position, map[x, y] == 1, squareSize);
                }

            Squares = new Square[nodeCountX - 1, nodeCountY -1];
            for (int x = 0; x < nodeCountX - 1; x++)
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    Squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
        }
    }
        
    public class Square
    {
        public ControlNode TopLeft, TopRight, BottomLeft, BottomRight;
        public Node CenterTop, CenterRight, CenterBottom, CenterLeft;
        public int configuration;

        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;

            CenterTop = topLeft.Right;
            CenterRight = bottomRight.Above;
            CenterBottom = bottomLeft.Right;
            CenterLeft = bottomLeft.Above;

            if (TopLeft.Active)
                configuration += 8;
            if (TopRight.Active)
                configuration += 4;
            if (bottomRight.Active)
                configuration += 2;
            if (bottomLeft.Active)
                configuration += 1;
        }
    }

    public class Node
    {
        public Vector3 Position;
        public int VertexIndex = -1;

        public Node(Vector3 position)
        {
            Position = position;
        }
    }

    public class ControlNode : Node
    {
        public bool Active;
        public Node Above;
        public Node Right;

        public ControlNode(Vector3 pos, bool active, float squareSize)
            : base(pos)
        {
            Active = active;
            Above = new Node(Position + Vector3.forward * squareSize / 2f);
            Right = new Node(Position + Vector3.right * squareSize / 2f);
        }
    }

    //Main class code
    public SquareGrid MainGrid;
    List<Vector3> Vertices = null;
    List<int> Triangles = null;

    public void generateMesh(int[,] map, float squareSize)
    {
        MainGrid = new SquareGrid(map, squareSize);

        Vertices = new List<Vector3>();
        Triangles = new List<int>();

        for (int x = 0; x < MainGrid.Squares.GetLength(0); x++)
            for (int y = 0; y < MainGrid.Squares.GetLength(1); y++)
                TriangulateSquare(MainGrid.Squares[x, y]);

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = Vertices.ToArray();
        mesh.triangles = Triangles.ToArray();
        mesh.RecalculateNormals();
    }

    void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

                //test change to check if account change worked


            // 1 point meshes.
            case 1:
                MeshFromPoints(square.CenterBottom, square.BottomLeft, square.CenterLeft);
                break;
            case 2:
                MeshFromPoints(square.CenterRight, square.BottomRight, square.CenterBottom);
                break;
            case 4:
                MeshFromPoints(square.CenterTop, square.TopRight, square.CenterRight);
                break;
            case 8:
                MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterLeft);
                break;

            // 2 point meshes.
            case 3:
                MeshFromPoints(square.CenterRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                break;
            case 6:
                MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.CenterBottom);
                break;
            case 9:
                MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterBottom, square.BottomLeft);
                break;
            case 12:
                MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterLeft);
                break;

            // diagonal meshes.
            case 5:
                MeshFromPoints(square.CenterTop, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft, square.CenterLeft);
                break;
            case 10:
                MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                break;

            // 3 point meshes.
            case 7:
                MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                break;
            case 11:
                MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.BottomLeft);
                break;
            case 13:
                MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft);
                break;
            case 14:
                MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                break;

            // 4 point meshes.
            case 15:
                MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.BottomLeft);
                break;
        }

    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);
    }

    void AssignVertices(Node[] points)
    {
        for(int i = 0; i < points.Length; i++)
        {
            if (points[i].VertexIndex == -1)
            {
                points[i].VertexIndex = Vertices.Count;
                Vertices.Add(points[i].Position);
            }
        }    
    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        Triangles.Add(a.VertexIndex);
        Triangles.Add(b.VertexIndex);
        Triangles.Add(c.VertexIndex);
    }

    private void OnDrawGizmos()
    {
        //if (MainGrid != null)
        //{
        //    for (int x = 0; x < MainGrid.Squares.GetLength(0); x++)
        //        for (int y = 0; y < MainGrid.Squares.GetLength(1); y++)
        //        {
        //            Gizmos.color = (MainGrid.Squares[x, y].TopLeft.Active) ? Color.black : Color.white;
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].TopLeft.Position, Vector3.one * 0.4f);

        //            Gizmos.color = (MainGrid.Squares[x, y].TopRight.Active) ? Color.black : Color.white;
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].TopRight.Position, Vector3.one * 0.4f);

        //            Gizmos.color = (MainGrid.Squares[x, y].BottomRight.Active) ? Color.black : Color.white;
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].BottomRight.Position, Vector3.one * 0.4f);

        //            Gizmos.color = (MainGrid.Squares[x, y].BottomLeft.Active) ? Color.black : Color.white;
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].BottomLeft.Position, Vector3.one * 0.4f);

        //            Gizmos.color = Color.grey;
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].CenterTop.Position, Vector3.one * 0.15f);
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].CenterRight.Position, Vector3.one * 0.15f);
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].CenterBottom.Position, Vector3.one * 0.15f);
        //            Gizmos.DrawCube(MainGrid.Squares[x, y].CenterLeft.Position, Vector3.one * 0.15f);
        //        }
        //}
    }
}