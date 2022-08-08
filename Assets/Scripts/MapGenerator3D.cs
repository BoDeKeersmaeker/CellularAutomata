using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MapGenerator3D : MonoBehaviour
{
    struct Coord
    {
        public int tileX;
        public int tileY;
        public int tileZ;

        public Coord(int x, int y, int z)
        {
            tileX = x;
            tileY = y;
            tileZ = z;
        }
    }

    struct MapBoundaries
    { 
        public int Width;
        public int Height;
        public int Depth;

        public MapBoundaries(int width, int height, int depth)
        {
            Width = width;
            Height = height;
            Depth = depth;
        }

        public bool IsInMapRange(int x, int y, int z)
        {
            return (x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth);
        }
    }

    class Room : IComparable<Room>
    {
        public List<Coord> Tiles;
        public List<Coord> EdgeTiles;
        public List<Room> ConnectedRooms;
        public int RoomSize;
        public bool IsAccessibleFromMainRoom;
        public bool IsMainRoom;

        public Room()
        {

        }

        public Room(List<Coord> roomTiles, int[,,] map, MapBoundaries bounds)
        {
            Tiles = roomTiles;
            RoomSize = Tiles.Count;
            ConnectedRooms = new List<Room>();
            EdgeTiles = new List<Coord>();

            foreach (Coord tile in Tiles)
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                        for (int z = tile.tileZ - 1; z <= tile.tileZ + 1; z++)
                        {
                            if (x != tile.tileX && y != tile.tileY && z != tile.tileZ)
                                continue;

                            if (bounds.IsInMapRange(x, y, z) && map[x, y, z] == 1)
                                EdgeTiles.Add(tile);
                        }
        }

        public void SetAccessibleFromMainRoom()
        {
            if (IsAccessibleFromMainRoom)
                return;

            IsAccessibleFromMainRoom = true;
            foreach (Room connectedRoom in ConnectedRooms)
                connectedRoom.SetAccessibleFromMainRoom();
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.IsAccessibleFromMainRoom)
                roomB.SetAccessibleFromMainRoom();
            else if (roomB.IsAccessibleFromMainRoom)
                roomA.SetAccessibleFromMainRoom();

            roomA.ConnectedRooms.Add(roomB);
            roomB.ConnectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return ConnectedRooms.Contains(otherRoom);
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.RoomSize.CompareTo(RoomSize);
        }
    }

    [SerializeField]
    private string Seed = "Bo De Keersmaeker";

    [SerializeField, Range(0, 1000)]
    private float WallThresholdSize = 50;
    [SerializeField, Range(0, 1000)]
    private float RoomThresholdSize = 50;
    [SerializeField, Range(1f, 1000f)]
    private float SquareSize = 1f;

    private int[,,] Map;
    private int[,,] NewMap;

    [SerializeField, Range(1, 1000)]
    private int Width = 100;
    [SerializeField, Range(1, 1000)]
    private int Height = 100;
    [SerializeField, Range(1, 1000)]
    private int Depth = 100;
    [SerializeField, Range(0, 100)]
    private int RandomFillPercent = 50;
    [SerializeField, Range(1, 100)]
    private int SmoothingFactor = 9;
    [SerializeField, Range(0, 1000)]
    private int IterationAmount = 5;
    [SerializeField, Range(1, 1000)]
    private int BorderSize = 1;
    [SerializeField, Range(1, 100)]
    private int PassageRadius = 1;

    [SerializeField]
    private bool UseRandomSeed = false;

    //Debug variables.
    [SerializeField]
    private bool DrawDebugPassages = false;
    [SerializeField]
    private bool DrawDebugMesh = false;
    private List<GameObject> DebugCubes = new List<GameObject>();
    List<Tuple<Coord, Coord>> DebugLines = new List<Tuple<Coord, Coord>>();

    private void Start()
    {
        GenerateMap();
        print("Startup took: " + Time.realtimeSinceStartup.ToString() + " seconds.");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            GenerateMap();
    }

    private void GenerateMap()
    {
        DebugLines.Clear();
        Map = new int[Width, Height, Depth];
        NewMap = new int[Width, Height, Depth];
        RandomFillMap();

        for (int i = 0; i < IterationAmount; i++)
            SmoothMap();

        ProcessMap();

        MeshGenerator3D meshGenerator = GetComponent<MeshGenerator3D>();
        meshGenerator.generateMesh(Map, SquareSize);

        if (Map != null && DrawDebugMesh)
        {
            for (int i = 0; i < DebugCubes.Count; i++)
                Destroy(DebugCubes[i]);

            DebugCubes.Clear();

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(SquareSize * 0.1f, SquareSize * 0.1f, SquareSize * 0.1f);
            cube.GetComponent<Renderer>().material.color = Color.black;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    for (int z = 0; z < Depth; z++)
                    {
                        Vector3 pos = new Vector3(-Width / 2f + x + .5f, -Depth / 2f + z + .5f, -Height / 2f + y + .5f);
                        cube.transform.position = pos;

                        if (Map[x, y, z] == 1)
                            DebugCubes.Add(Instantiate(cube, transform));
                    }

            Destroy(cube);
        }
    }

    private void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        foreach (List<Coord> wallRegion in wallRegions)
            if (wallRegion.Count < WallThresholdSize)
                foreach (Coord tile in wallRegion)
                    Map[tile.tileX, tile.tileY, tile.tileZ] = 0;

        List<List<Coord>> roomRegions = GetRegions(0);
        List<Room> survivingRooms = new List<Room>();

        foreach (List<Coord> roomRegion in roomRegions)
            if (roomRegion.Count < RoomThresholdSize)
                foreach (Coord tile in roomRegion)
                    Map[tile.tileX, tile.tileY, tile.tileZ] = 1;
            else
                survivingRooms.Add(new Room(roomRegion, Map, new MapBoundaries(Width, Depth, Height)));

        survivingRooms.Sort();
        survivingRooms[0].IsMainRoom = true;
        survivingRooms[0].IsAccessibleFromMainRoom = true;

        ConnectClosestRooms(survivingRooms);
    }

    private void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom)
        {
            foreach (Room room in allRooms)
                if (room.IsAccessibleFromMainRoom)
                    roomListB.Add(room);
                else
                    roomListA.Add(room);
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int smallestDistance = 0;

        Coord closestTileA = new Coord();
        Coord closestTileB = new Coord();

        Room closestRoomA = new Room();
        Room closestRoomB = new Room();

        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.ConnectedRooms.Count > 0)
                    continue;
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                    continue;

                for (int tileIndexA = 0; tileIndexA < roomA.EdgeTiles.Count; tileIndexA++)
                    for (int tileIndexB = 0; tileIndexB < roomB.EdgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.EdgeTiles[tileIndexA];
                        Coord tileB = roomB.EdgeTiles[tileIndexB];

                        int distanceBetweenRooms = (int)Vector3.Distance(new Vector3(tileA.tileX, tileA.tileY, tileA.tileZ), new Vector3(tileB.tileX, tileB.tileY, tileB.tileZ));

                        if (distanceBetweenRooms < smallestDistance || !possibleConnectionFound)
                        {
                            smallestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;

                            closestTileA = tileA;
                            closestTileB = tileB;

                            closestRoomA = roomA;
                            closestRoomB = roomB;
                        }
                    }
            }

            if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                CreatePassage(closestRoomA, closestRoomB, closestTileA, closestTileB);
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(closestRoomA, closestRoomB, closestTileA, closestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
            ConnectClosestRooms(allRooms, true);
    }

    private void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);

        if(DrawDebugPassages)
            DebugLines.Add(new Tuple<Coord, Coord>(tileA, tileB));

        List<Coord> line = GetLine(tileA, tileB);
        foreach (Coord temp in line)
            DrawSphere(temp, PassageRadius);
    }

    private void DrawSphere(Coord temp, int radius)
    {
        for (int x = -radius; x < radius; x++)
            for (int y = -radius; y < radius; y++)
                for (int z = -radius; z < radius; z++)
                    if (x * x + y * y + z * z <= radius * radius)
                    {
                        int tempX = temp.tileX + x;
                        int tempY = temp.tileY + y;
                        int tempZ = temp.tileZ + z;

                        if (!IsInMapRange(tempX, tempY, tempZ))
                            continue;

                        Map[tempX, tempY, tempZ] = 0;
                    }
    }

    private List<Coord> GetLine(Coord start, Coord end)
    {
        List<Coord> line = new List<Coord>();

       float dx = end.tileX - start.tileX;
        float dy = end.tileY - start.tileY;
        float dz = end.tileZ - start.tileZ;

        if (dy < dx && dy < dz)
            foreach (Vector3Int tempCoord in BresenhamsAlgorithm(dy, dx, dz, start.tileY, end.tileY, start.tileX, start.tileZ))
                line.Add(new Coord(tempCoord.y, tempCoord.x, tempCoord.z));
        else if (dz < dx && dz < dy)
            foreach (Vector3Int tempCoord in BresenhamsAlgorithm(dz, dy, dx, start.tileZ, end.tileZ, start.tileY, start.tileX))
                line.Add(new Coord(tempCoord.z, tempCoord.y, tempCoord.x));
        else
            foreach (Vector3Int tempCoord in BresenhamsAlgorithm(dx, dy, dz, start.tileX, end.tileX, start.tileY, start.tileZ))
                line.Add(new Coord(tempCoord.x, tempCoord.y, tempCoord.z));

        return line;
    }

    //Algorithm that checks all tiles crossed by the line.
    private List<Vector3Int> BresenhamsAlgorithm(float deltaShortest, float deltaLongA, float deltaLongB, int startShort, int endShort, int startLongA, int startLongB)
    {
        List<Vector3Int> line = new List<Vector3Int>();

        float deltaErrorY = Mathf.Abs(deltaLongA / deltaShortest);
        float deltaErrorZ = Mathf.Abs(deltaLongB / deltaShortest);
        float errorY = 0;
        float errorZ = 0;
        int y = startLongA;
        int z = startLongB;

        for (int x = endShort; x < startShort; x++)
        {
            line.Add(new Vector3Int(x, y, z));

            errorY += deltaErrorY;

            while (errorY >= 0.5)
            {
                y += Math.Sign(deltaLongA);
                errorY--;
            }
            errorZ += deltaErrorZ;
            while (errorZ >= 0.5)
            {
                z += Math.Sign(deltaLongB);
                errorZ--;
            }
        }

        return line;
    }

    private Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-Width / 2f + .5f + tile.tileX, -Depth / 2f + .5f + tile.tileZ, -Height / 2 + .5f + tile.tileY);
    }

    private List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,,] mapFlags = new int[Width, Height, Depth];

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                    if (mapFlags[x, y, z] == 0 && Map[x, y, z] == tileType)
                    {
                        List<Coord> newRegion = GetRegionTiles(x, y, z);
                        regions.Add(newRegion);

                        foreach (Coord tile in newRegion)
                            mapFlags[tile.tileX, tile.tileY, tile.tileZ] = 1;
                    }

        return regions;
    }

    private List<Coord> GetRegionTiles(int startX, int startY, int startZ)
    {
        List<Coord> tiles = new List<Coord>();
        int[,,] mapFlags = new int[Width, Height, Depth];
        int tileType = Map[startX, startY, startZ];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY, startZ));
        mapFlags[startX, startY, startZ] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    for (int z = tile.tileZ - 1; z <= tile.tileZ + 1; z++)
                    {
                        if (!IsInMapRange(x, y, z) || (y != tile.tileY && x != tile.tileX && z != tile.tileZ))
                            continue;

                        if (mapFlags[x, y, z] == 0 && Map[x, y, z] == tileType)
                        {
                            mapFlags[x, y, z] = 1;
                            queue.Enqueue(new Coord(x, y, z));
                        }

                    }
        }

        return tiles;
    }

    private void RandomFillMap()
    {
        if (UseRandomSeed)
            Seed = Time.time.ToString();

        System.Random pseudoRandom = new System.Random(Seed.GetHashCode());

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    if (x <= 0 || x >= Width - 1 || y <= 0 || y >= Height - 1 || z <= 0 || z >= Depth - 1)
                        Map[x, y, z] = 1;
                    else
                        Map[x, y, z] = (pseudoRandom.Next(0, 100) < RandomFillPercent) ? 1 : 0;
                }
    }

    private void SmoothMap()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    int neighbourWallTiles = GetSurroundingWallCount(x, y, z);

                    if (neighbourWallTiles > SmoothingFactor)
                        NewMap[x, y, z] = 1;
                    else if (neighbourWallTiles < SmoothingFactor)
                        NewMap[x, y, z] = 0;
                    else
                        NewMap[x, y, z] = Map[x, y, z];
                }

        Map = NewMap;
    }

    private int GetSurroundingWallCount(int gridX, int gridY, int gridZ)
    {
        int wallCount = 0;

        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
                for (int neighbourZ = gridZ - 1; neighbourZ < gridZ + 1; neighbourZ++)
                {
                    if (neighbourX == gridX && neighbourY == gridY && neighbourZ == gridZ)
                        continue;

                    if (!IsInMapRange(neighbourX, neighbourY, neighbourZ))
                        wallCount++;
                    else if (Map[neighbourX, neighbourY, neighbourZ] == 1)
                        wallCount += Map[neighbourX, neighbourY, neighbourZ];
                }

        return wallCount;
    }

    private bool IsInMapRange(int x, int y, int z)
    {
        return (x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth);
    }

    private void OnDrawGizmos()
    {
        if (DrawDebugPassages)
        {
            foreach (Tuple<Coord, Coord> debugLine in DebugLines)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(CoordToWorldPoint(debugLine.Item1), CoordToWorldPoint(debugLine.Item2));
            }
        }
    }
}