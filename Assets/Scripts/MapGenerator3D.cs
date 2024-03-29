using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UIElements;
using Unity.VisualScripting;

public class MapGenerator3D : MonoBehaviour
{
    public struct Coord
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

    private MapBoundaries Boundaries = new MapBoundaries();

    [SerializeField]
    private ComputeShader BresenhamShader = null;
    [SerializeField]
    private ComputeShader CellularAutomataShader = null;

    [SerializeField]
    private bool UseRandomSeed = false;
    [SerializeField]
    private string Seed = "Bo De Keersmaeker";

    [SerializeField, Range(0, 1000)]
    private float WallThresholdSize = 50;
    [SerializeField, Range(0, 1000)]
    private float RoomThresholdSize = 50;
    [SerializeField, Range(0.001f, 1000f)]
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
    bool SpawnPlayer = true;
    [SerializeField]
    GameObject Player = null;
    Coord PlayerSpawn = new Coord();

    //Debug variables.
    private List<GameObject> DebugMeshCubes = new List<GameObject>();
    List<Tuple<Coord, Coord>> DebugPassageLines = new List<Tuple<Coord, Coord>>();
    List<Tuple<Coord, Coord>> DebugPassage2Lines = new List<Tuple<Coord, Coord>>();
    [SerializeField]
    private float DebugCubeSize = 0.75f;
    [SerializeField]
    private bool DrawDebugPassages = false;
    [SerializeField]
    private bool DrawDebugMesh = false;
    [SerializeField]
    private bool DrawMiningDebug = false;

    private void Start()
    {
        GenerateMap();
        print("Startup took: " + Time.realtimeSinceStartup.ToString() + " seconds.");
    }

    private void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //    GenerateMap();

        //if (Input.GetMouseButtonDown(0))
        //{
        //    float tempTime = Time.realtimeSinceStartup;

        //    MeshGenerator3D meshGenerator = GetComponent<MeshGenerator3D>();
        //    int[,,] temp = GenerateBorderedMap(ref Map);
        //    meshGenerator.generateMesh(ref temp, SquareSize);

        //    tempTime = Time.realtimeSinceStartup - tempTime;
        //    print("Mesh generation took: " + tempTime);
        //}

        //if (Input.GetMouseButtonDown(1))
        //{
        //    List<List<Coord>> wallRegions = GetRegions(1);

        //    foreach (List<Coord> wallRegion in wallRegions)
        //        if (wallRegion.Count < WallThresholdSize)
        //            foreach (Coord tile in wallRegion)
        //                Map[tile.tileX, tile.tileY, tile.tileZ] = 0;

        //    List<List<Coord>> roomRegions = GetRegions(0);
        //    List<Room> survivingRooms = new List<Room>();

        //    foreach (List<Coord> roomRegion in roomRegions)
        //        if (roomRegion.Count < RoomThresholdSize)
        //            foreach (Coord tile in roomRegion)
        //                Map[tile.tileX, tile.tileY, tile.tileZ] = 1;
        //        else
        //            survivingRooms.Add(new Room(roomRegion, Map, new MapBoundaries(Width, Depth, Height)));

        //    survivingRooms.Sort();
        //    survivingRooms[0].IsMainRoom = true;
        //    survivingRooms[0].IsAccessibleFromMainRoom = true;

        //    List<Room> tempRooms = new List<Room> { survivingRooms[0], survivingRooms[1] };
        //    if (survivingRooms.Count >= 2)
        //        ConnectClosestRooms(ref tempRooms);
        //}
    }

    private void GenerateMap()
    {
        DebugPassageLines.Clear();
        Map = new int[Width, Height, Depth];
        NewMap = new int[Width, Height, Depth];

        Boundaries = new MapBoundaries(Width, Height, Depth);

        RandomFillMap();

        for (int i = 0; i < IterationAmount; i++)
            SmoothMap();

        ProcessMap();

        MeshGenerator3D meshGenerator = GetComponent<MeshGenerator3D>();
        int[,,] temp = GenerateBorderedMap(ref Map);
        meshGenerator.generateMesh(ref temp, SquareSize);

        SpawnCamera();

        DrawDebug();
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

        if (survivingRooms.Count >= 1)
        {
            survivingRooms.Sort();
            survivingRooms[0].IsMainRoom = true;
            survivingRooms[0].IsAccessibleFromMainRoom = true;

            ConnectClosestRooms(ref survivingRooms);
        }
    }

    private void ConnectClosestRooms(ref List<Room>  allRooms, bool forceAccessibilityFromMainRoom = false)
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
                CreatePassage(ref closestRoomA, ref closestRoomB, closestTileA, closestTileB);
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(ref closestRoomA, ref closestRoomB, closestTileA, closestTileB);
            ConnectClosestRooms(ref allRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
            ConnectClosestRooms(ref allRooms, true);
    }

    private void CreatePassage(ref Room roomA, ref Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);

        if(DrawDebugPassages)
            DebugPassageLines.Add(new Tuple<Coord, Coord>(tileA, tileB));

        List<Coord> line = GetLine(tileA, tileB);
        foreach (Coord temp in line)
            DrawSphere(temp, PassageRadius);
    }

    private void DrawSphere(Coord temp, int radius, int drawValue = 0)
    {
        for (int x = -radius; x < radius; x++)
            for (int y = -radius; y < radius; y++)
                for (int z = -radius; z < radius; z++)
                    if (x * x + y * y + z * z <= radius * radius)
                    {
                        int tempX = temp.tileX + x;
                        int tempY = temp.tileY + y;
                        int tempZ = temp.tileZ + z;

                        if ( !Boundaries.IsInMapRange(tempX, tempY, tempZ))
                            continue;

                        Map[tempX, tempY, tempZ] = drawValue;
                    }
    }

    private List<Coord> GetLineUsingComputeShader(Coord start, Coord end)
    {
        List<Coord> line = new List<Coord>();
        int stride = sizeof(int) * 3;
        
        Coord[] tempCoord = new Coord[1];
        ComputeBuffer lineBuffer = new ComputeBuffer(tempCoord.Length, stride);
        lineBuffer.SetData(tempCoord);
        BresenhamShader.SetBuffer(0, "LineBuffer", lineBuffer);

        Coord[] startEnd = new Coord[2] { start, end };
        stride = sizeof(int) * 3;
        ComputeBuffer startEndBuffer = new ComputeBuffer(startEnd.Length, stride);
        startEndBuffer.SetData(startEnd);
        BresenhamShader.SetBuffer(0, "StartEnd", startEndBuffer);

        //broke shit
        BresenhamShader.Dispatch(0, 10, 1, 1);

        lineBuffer.GetData(tempCoord);

        foreach (Coord coord in tempCoord)
            if(coord.tileX != -1 || coord.tileY != -1 || coord.tileZ != -1)
                line.Add(new Coord(coord.tileX, coord.tileY, coord.tileZ));

        lineBuffer.Dispose();

        return line;
    }

    private List<Coord> GetLine(Coord start, Coord end)
    {
        List<Vector2Int> tempListXY = BresenhamsAlgorithm(new Vector2Int(start.tileX, start.tileY), new Vector2Int(end.tileX, end.tileY));
        List<Vector2Int> tempListXZ = BresenhamsAlgorithm(new Vector2Int(start.tileX, start.tileZ), new Vector2Int(end.tileX, end.tileZ));

        List<Coord> line = new List<Coord>();

        if (tempListXY.Count > tempListXZ.Count)
            for (int i = 0; i < tempListXY.Count; i++)
            {
                if (i >= tempListXZ.Count)
                    line.Add(new Coord(tempListXY[i].x, tempListXY[i].y, tempListXZ[tempListXZ.Count - 1].y));
                else
                    line.Add(new Coord(tempListXY[i].x, tempListXY[i].y, tempListXZ[i].y));

                if (DrawDebugPassages && line.Count > 1)
                    DebugPassage2Lines.Add(new Tuple<Coord, Coord>(line[line.Count - 2], line[line.Count - 1]));
            }
        else
            for (int i = 0; i < tempListXZ.Count; i++)
            {
                if (i >= tempListXY.Count)
                    line.Add(new Coord(tempListXY[tempListXY.Count - 1].x, tempListXY[tempListXY.Count - 1].y, tempListXZ[i].y));
                else
                    line.Add(new Coord(tempListXY[i].x, tempListXY[i].y, tempListXZ[i].y));

                if (DrawDebugPassages && line.Count > 1)
                    DebugPassage2Lines.Add(new Tuple<Coord, Coord>(line[line.Count - 2], line[line.Count - 1]));
            }

        line.Add(end);

        if (DrawDebugPassages)
            DebugPassage2Lines.Add(new Tuple<Coord, Coord>(line[line.Count - 2], line[line.Count - 1]));

        PlayerSpawn = line[0];

        return line;
    }

    private List<Vector2Int> BresenhamsAlgorithm(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> tempList = new List<Vector2Int>();

        int x = start.x;
        int y = start.y;

        int dx = end.x - start.x;
        int dy = end.y - start.y;

        bool inverted = false;

        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        //longest and shortest because in the case that dy is longer than dx the algorithm would break.
        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradinetAccumulation = longest / 2;

        for (int i = 0; i < longest; i++)
        {
            tempList.Add(new Vector2Int(x, y));

            if (inverted)
                y += step;
            else
                x += step;

            gradinetAccumulation += shortest;

            if (gradinetAccumulation >= longest)
            {
                if (inverted)
                    x += gradientStep;
                else
                    y += gradientStep;

                gradinetAccumulation -= longest;
            }
        }

        return tempList;
    }

    private List<Coord> GetLineOld(Coord start, Coord end)
    {
        List<Coord> line = new List<Coord>();

        float dx = end.tileX - start.tileX;
        float dy = end.tileY - start.tileY;
        float dz = end.tileZ - start.tileZ;

        print("x: " + dx.ToString() + " y: " + dy.ToString() + " z: " + dz.ToString());

        if (dy < dx && dy < dz)
            foreach (Vector3Int tempCoord in BresenhamsAlgorithmOld(dy, dx, dz, start.tileY, end.tileY, start.tileX, start.tileZ))
                line.Add(new Coord(tempCoord.y, tempCoord.z, tempCoord.x));
        else if (dz < dx && dz < dy)
            foreach (Vector3Int tempCoord in BresenhamsAlgorithmOld(dz, dy, dx, start.tileZ, end.tileZ, start.tileY, start.tileX))
                line.Add(new Coord(tempCoord.z, tempCoord.y, tempCoord.x));
        else
            foreach (Vector3Int tempCoord in BresenhamsAlgorithmOld(dx, dy, dz, start.tileX, end.tileX, start.tileY, start.tileZ))
                line.Add(new Coord(tempCoord.x, tempCoord.y, tempCoord.z));

        print(line.Count);

        if (DrawDebugPassages && line.Count > 1)
            DebugPassage2Lines.Add(new Tuple<Coord, Coord>(line[0], line[line.Count-1 ]));


        return line;
    }

    //Algorithm that checks all tiles crossed by the line.
    private List<Vector3Int> BresenhamsAlgorithmOld(float deltaShortest, float deltaLongA, float deltaLongB, int startShort, int endShort, int startLongA, int startLongB)
    {
        List<Vector3Int> line = new List<Vector3Int>();

        float deltaErrorY = Mathf.Abs(deltaLongA / deltaShortest);
        float deltaErrorZ = Mathf.Abs(deltaLongB / deltaShortest);
        float errorY = 0;
        float errorZ = 0;
        int y = startLongA;
        int z = startLongB;

        if(endShort < startShort)
        {
            int tempInt = startShort;
            startShort = endShort;
            endShort = tempInt;
        }

        for (int x = startShort; x < endShort; x++)
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

    public Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3((-Width / 2f + .5f + tile.tileX) * SquareSize, (-Depth / 2f + .5f + tile.tileZ) * SquareSize, (-Height / 2 + .5f + tile.tileY) * SquareSize);
    }

    public Coord WorldPointToCoord(Vector3 position)
    {
        return new Coord((int)((position.x / SquareSize) - transform.position.x + Width / 2.5f), (int)((position.z / SquareSize) - transform.position.z + Depth / 2.5f), (int)((position.y / SquareSize) - transform.position.y + Height / 2.5f));
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
                        if (!Boundaries.IsInMapRange(x, y, z) || (y != tile.tileY && x != tile.tileX && z != tile.tileZ))
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

    private void CellularAutomateUsingShader()
    {
        int[] Map1D = new int[Width * Height * Depth];

        if (UseRandomSeed)
            Seed = Time.time.ToString();

        System.Random pseudoRandom = new System.Random(Seed.GetHashCode());

        for (int i = 0; i < Map1D.Length; i++)
            Map1D[i] = (pseudoRandom.Next(0, 100) < RandomFillPercent) ? 1 : 0;

        int stride = sizeof(int);
        ComputeBuffer mapBuffer = new ComputeBuffer(Map1D.Length, stride);
        mapBuffer.SetData(Map1D);
        CellularAutomataShader.SetBuffer(0, "Map", mapBuffer);

        CellularAutomataShader.SetInt("Width", Width);
        CellularAutomataShader.SetInt("Height", Height);
        CellularAutomataShader.SetInt("Depth", Depth);
        CellularAutomataShader.SetInt("SmoothingFactor", SmoothingFactor);
        CellularAutomataShader.SetInt("IterationAmount", IterationAmount);
        CellularAutomataShader.SetInt("RandomFillPercent", RandomFillPercent);

        CellularAutomataShader.Dispatch(0, 10, 1, 1);

        mapBuffer.GetData(Map1D);
        mapBuffer.Dispose();

        int index = 0;

        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                for (int z = 0; z < Depth; z++)
                {
                    Map[x, y, z] = Map1D[index];
                    index++;
                }
    }

    private int[,,] GenerateBorderedMap(ref int[,,] tempMap)
    {
        int[,,] borderedMap = new int[Width + BorderSize * 2, Height + BorderSize * 2, Depth + BorderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
            for (int y = 0; y < borderedMap.GetLength(1); y++)
                for (int z = 0; z < borderedMap.GetLength(2); z++)
                {
                    if (x >= BorderSize && x < Width + BorderSize && y >= BorderSize && y < Height + BorderSize && z >= BorderSize && z < Depth + BorderSize)
                        borderedMap[x, y, z] = tempMap[x - BorderSize, y - BorderSize, z - BorderSize];
                    else
                        borderedMap[x, y, z] = 1;
                }

        return borderedMap;
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

                    if (!Boundaries.IsInMapRange(neighbourX, neighbourY, neighbourZ))
                        wallCount++;
                    else if (Map[neighbourX, neighbourY, neighbourZ] == 1)
                        wallCount += Map[neighbourX, neighbourY, neighbourZ];
                }

        return wallCount;
    }

    public void MineBlock(Vector3 worldPos, int miningRadius, Vector3 direction)
    {
        //Extend the hitposition a little bit deeper to not hit exactly on the edge between nodes.
        worldPos += (direction * 0.3f ) * SquareSize;

        Coord tempCoord = WorldPointToCoord(worldPos);
        tempCoord.tileX -= (1 + miningRadius / 2);
        tempCoord.tileY -= (2 + miningRadius / 2);
        tempCoord.tileZ -= (1 + miningRadius / 2);

        if (Boundaries.IsInMapRange(tempCoord.tileX, tempCoord.tileY, tempCoord.tileZ))
        {
            DrawSphere(tempCoord, miningRadius, 0);
            MeshGenerator3D meshGenerator = GetComponent<MeshGenerator3D>();
            int[,,] tempMap = GenerateBorderedMap(ref Map);
            meshGenerator.generateMesh(ref tempMap, SquareSize);

            if (DrawMiningDebug)
            {
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = new Vector3(SquareSize * DebugCubeSize, SquareSize * DebugCubeSize, SquareSize * DebugCubeSize);
                cube.GetComponent<Renderer>().material.color = Color.yellow;

                Vector3 pos = CoordToWorldPoint(tempCoord);
                cube.transform.position = pos;
                DebugMeshCubes.Add(Instantiate(cube, transform));
            }
        }
    }

    public void SpawnCamera()
    {
        if (SpawnPlayer)
        {
            Vector3 spawnPosition = CoordToWorldPoint(PlayerSpawn);
            Quaternion spawnRotation = Quaternion.identity;
            Instantiate(Player, spawnPosition, spawnRotation);
        }
        else
        {
            GameObject cameraObject = new GameObject("CameraObject");
            cameraObject.AddComponent<Camera>();
            Vector3 cameraPosition = new Vector3(0f, 27f, 0f);
            Quaternion cameraRotation = new Quaternion(0.707106829f, 0f, 0f, 0.707106829f);

            cameraObject.transform.localPosition = cameraPosition;
            cameraObject.transform.localRotation = cameraRotation;
        }
    }

    private void DrawDebug()
    {
        if (Map != null && DrawDebugMesh)
        {
            for (int i = 0; i < DebugMeshCubes.Count; i++)
                Destroy(DebugMeshCubes[i]);

            DebugMeshCubes.Clear();

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(SquareSize * DebugCubeSize, SquareSize * DebugCubeSize, SquareSize * DebugCubeSize);
            cube.GetComponent<Renderer>().material.color = Color.black;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    for (int z = 0; z < Depth; z++)
                    {
                        Vector3 pos = CoordToWorldPoint(new Coord(x, y, z));
                        cube.transform.position = pos;

                        if (Map[x, y, z] == 0)
                            DebugMeshCubes.Add(Instantiate(cube, transform));
                    }

            Destroy(cube);
        }
    }

    private void OnDrawGizmos()
    {
        if (DrawDebugPassages)
        {
            foreach (Tuple<Coord, Coord> debugLine in DebugPassageLines)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(CoordToWorldPoint(debugLine.Item1), CoordToWorldPoint(debugLine.Item2));
            }

            foreach (Tuple<Coord, Coord> debugLine in DebugPassage2Lines)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(CoordToWorldPoint(debugLine.Item1), CoordToWorldPoint(debugLine.Item2));
            }
        }
    }
}