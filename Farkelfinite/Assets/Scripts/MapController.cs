using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public class MapGenerator : MonoBehaviour
{
    public Camera mainCamera;
    public GameObject player;

    [Header("Map Settings")]
    public int totalColumns = 7;
    public int minNodesPerColumn = 2;
    public int maxNodesPerColumn = 6;
    public int minPaths = 2;
    public int maxPaths = 5;
    public float columnSpacing = 200f;
    public float nodeSpacing = 200f;
    public float SidePadding = 100f;
    public bool debugMode = false;

    public int stage = 1;
    public int Level = 0;
     

    [Header("Prefabs")]
    public GameObject nodePrefab;

    public List<Sprite> iconSprites;

    private List<List<MapNode>> columns = new List<List<MapNode>>();
    private List<MapNode> allNodes = new List<MapNode>();

    [Header("Node Type Limits")]
    public int maxShops = 3;
    public int maxMoney = 4;
    public int maxEliteEnemies = 2;

    private void Start()
    {
        GenerateMap();
        DontDestroyOnLoad(player);
    }

    [ContextMenu("Debug Test")]
    private void DebugThisShit()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        columns = new List<List<MapNode>>();
        allNodes = new List<MapNode>();
        GenerateMap();
    }

    public void GenerateMap()
    {
        GenerateNodes();
        GenerateMapConnections();
        AssignNodeTypes();
        CreateVisuals();
    }

    void GenerateNodes()
    {
        RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float xPadding = canvasWidth * SidePadding;
        float yPadding = canvasHeight * SidePadding;

        float screenWidth = canvasWidth - (xPadding * 2);
        float screenHeight = canvasHeight - (yPadding * 2);

        for (int col = 0; col < totalColumns; col++)
        {
            List<MapNode> column = new List<MapNode>();
            int nodeCount = Random.Range(minNodesPerColumn, maxNodesPerColumn + 1);
            if (col == 0) nodeCount = 1;
            if (col == totalColumns - 1) nodeCount = 1;

            for (int row = 0; row < nodeCount; row++)
            {
                float xPos = (screenWidth / (totalColumns - 1)) * col - (Screen.width / 2) + SidePadding;

                float yPos;
                if (nodeCount == 1)
                {
                    yPos = 0;
                }
                else
                {              
                    float ySpacing = screenHeight / (nodeCount - 1);
                    yPos = (ySpacing * row) - (Screen.height / 2) + SidePadding;
                }

                MapNode node = new MapNode
                {
                    position = new Vector2(col * 200, row * 200),
                    connections = new List<MapNode>(),
                    columnIndex = col,
                    isAvailable = (col == 0),
                    isCompleted = false,
                    type = NodeType.Start
                };
                column.Add(node);
                allNodes.Add(node);
            }
            columns.Add(column);
        }

        Debug.Log("Nodes generated");
    }

    public void GenerateMapConnections()
    {
        int pathCount = Random.Range(minPaths,maxPaths);
        HashSet<MapNode> usedNodes = new HashSet<MapNode>();
        List<(MapNode from, MapNode to)> allConnections = new List<(MapNode, MapNode)>();

        for (int pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            MapNode currentNode = columns[0][Random.Range(0, columns[0].Count)];
            usedNodes.Add(currentNode);

            for (int col = 0; col < totalColumns - 1; col++)
            {
                MapNode nextNode = GetClosestNonCrossingNode(currentNode, columns[col + 1], allConnections);

                if (nextNode != null)
                {
                    if (!currentNode.connections.Contains(nextNode))
                    {
                        currentNode.connections.Add(nextNode);
                    }
                    allConnections.Add((currentNode, nextNode));
                    usedNodes.Add(nextNode);
                    currentNode = nextNode;
                }
                else
                {
                    break;
                }
            }
        }

        for (int col = 0; col < totalColumns; col++)
        {
            columns[col].RemoveAll(node => !usedNodes.Contains(node));
        }
        allNodes.RemoveAll(node => !usedNodes.Contains(node));

        Debug.Log($"Map generation complete. Total nodes: {usedNodes.Count}");
    }

    MapNode GetClosestNonCrossingNode(MapNode from, List<MapNode> targets, List<(MapNode from, MapNode to)> existingConnections)
    {
        if (from.columnIndex == 0)
        {
            var spreadTargets = targets
                .OrderBy(t => t.position.y)
                .ToList();

            for (int i = 0; i < spreadTargets.Count; i++)
            {
                int randomIndex = Random.Range(i, spreadTargets.Count);
                var temp = spreadTargets[i];
                spreadTargets[i] = spreadTargets[randomIndex];
                spreadTargets[randomIndex] = temp;
            }

            foreach (var target in spreadTargets)
            {
                if (!WouldCross(from, target, existingConnections))
                {
                    return target;
                }
            }

            return null;
        }

        var closest = targets
            .OrderBy(t => Mathf.Abs(t.position.y - from.position.y))
            .Take(maxPaths)
            .ToList();

        for (int i = 0; i < closest.Count; i++)
        {
            int randomIndex = Random.Range(i, closest.Count);
            var temp = closest[i];
            closest[i] = closest[randomIndex];
            closest[randomIndex] = temp;
        }

        foreach (var target in closest)
        {
            if (!WouldCross(from, target, existingConnections))
            {
                return target;
            }
        }

        return null;
    }

    bool WouldCross(MapNode from, MapNode to, List<(MapNode from, MapNode to)> existingConnections)
    {
        foreach (var existing in existingConnections)
        {
            if (existing.from == from && existing.to == to) continue;
            if (existing.from == from || existing.to == to) continue;

            if (LineSegmentsIntersect(from.position, to.position,
                                      existing.from.position, existing.to.position))
            {
                return true;
            }
        }
        return false;
    }

    bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float denominator = ((p4.y - p3.y) * (p2.x - p1.x)) - ((p4.x - p3.x) * (p2.y - p1.y));

        if (Mathf.Abs(denominator) < 0.0001f) return false;

        float ua = (((p4.x - p3.x) * (p1.y - p3.y)) - ((p4.y - p3.y) * (p1.x - p3.x))) / denominator;
        float ub = (((p2.x - p1.x) * (p1.y - p3.y)) - ((p2.y - p1.y) * (p1.x - p3.x))) / denominator;

        return (ua > 0 && ua < 1 && ub > 0 && ub < 1);
    }

    void AssignNodeTypes()
    {
        int shopCount = 0;
        int moneyCount = 0;
        int eliteCount = 0;

        foreach (var node in columns[0])
        {
            node.type = NodeType.Start;
        }

        if (columns.Count > 0)
        {
            foreach (var node in columns[columns.Count - 1])
            {
                node.type = NodeType.Boss;
            }
            foreach (var node in columns[columns.Count - 2])
            {
                node.type = NodeType.Shop;
            }
            if(totalColumns > 5)
            foreach (var node in columns[totalColumns/2])
            {
                node.type = NodeType.Shop;
            }
        }

        List<MapNode> middleNodes = new List<MapNode>();
        for (int col = 1; col < totalColumns - 2; col++)
        {
            middleNodes.AddRange(columns[col]);
        }

        for (int i = middleNodes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = middleNodes[i];
            middleNodes[i] = middleNodes[j];
            middleNodes[j] = temp;
        }



        foreach (var node in middleNodes)
        {
            float rand = Random.value;
            if (node.type != NodeType.Start) continue;
            if (rand < 0.5f)
            {
                node.type = NodeType.Enemy;
            }
            else if (rand < 0.7f && shopCount < maxShops)
            {
                node.type = NodeType.Enemy;
                shopCount++;
            }
            else if (rand < 0.85f && moneyCount < maxMoney)
            {
                node.type = NodeType.Money;
                moneyCount++;
            }
            else if (eliteCount < maxEliteEnemies)
            {
                node.type = NodeType.EliteEnemy;
                eliteCount++;
            }
            else
            {
                node.type = NodeType.Enemy;
            }
        }

        Debug.Log($"Map types assigned - Shops: {shopCount}/{maxShops}, Money: {moneyCount}/{maxMoney}, Elites: {eliteCount}/{maxEliteEnemies}");
    }

    void CreateVisuals()
    {
        foreach (var node in allNodes)
        {
            CreateNodeVisual(node);
        }

        DrawConnections();
    }

    void CreateNodeVisual(MapNode node)
    {
        GameObject nodeObj = Instantiate(nodePrefab, transform);

        Vector2 Position = node.position;

        if (!debugMode)
        {
            RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;

            float xPadding = canvasWidth * SidePadding;
            float yPadding = canvasHeight * SidePadding;

            float screenWidth = canvasWidth - (xPadding * 2);
            float screenHeight = canvasHeight - (yPadding * 2);

            float xPos = (screenWidth / (totalColumns - 1)) * node.columnIndex - (canvasWidth / 2) + xPadding;
            int nodcount = columns[node.columnIndex].Count;

            float yPos;
            if (columns[node.columnIndex].Count == 1)
            {
                yPos = 0;
            }
            else
            {
                float ySpacing = screenHeight / (nodcount + 1);
                yPos = (ySpacing * (columns[node.columnIndex].IndexOf(node) + 1)) - (canvasHeight / 2) + yPadding;
            }
            Position = new Vector2(xPos, yPos);
            node.position = Position;
        }

        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = Position;

        node.nodeObject = nodeObj;

        Button button = nodeObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnNodeClicked(node));
        }

        Image image = nodeObj.GetComponent<Image>();
        if (image != null)
        {
            image.color = GetColorForNodeType(node.type);
            image.sprite = iconSprites[(int)node.type];
        }

        UpdateNodeVisual(node);
    }

    void DrawConnections()
    {
        foreach (MapNode node in allNodes)
        {
            foreach (MapNode connectedNode in node.connections)
            {
                DrawConnectionLine(node.position, connectedNode.position);
            }
        }
    }

    void DrawConnectionLine(Vector2 start, Vector2 end)
    {
        GameObject lineObj = new GameObject("Connection");
        lineObj.transform.SetParent(transform, false);

        RectTransform rectTransform = lineObj.AddComponent<RectTransform>();
        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = new Color(1f, 1f, 1f, 0.3f);

        Vector2 direction = end - start;
        float distance = direction.magnitude;

        rectTransform.anchoredPosition = start + direction / 2;
        rectTransform.sizeDelta = new Vector2(distance, 3f);
        rectTransform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        lineObj.transform.SetAsFirstSibling();
    }

    void OnNodeClicked(MapNode node)
    {
        if (!node.isAvailable)
        {
            Debug.Log("Node not available yet!");
            return;
        }

        Debug.Log($"Clicked {node.type} node at column {node.columnIndex}");

        node.isCompleted = true;

        foreach (MapNode nonConnectNode in columns[node.columnIndex]) 
        {

            nonConnectNode.isAvailable = false;
            UpdateNodeVisual(nonConnectNode);
        }

        foreach (MapNode connectedNode in node.connections)
        {
            connectedNode.isAvailable = true;
            UpdateNodeVisual(connectedNode);
        }


        UpdateNodeVisual(node);

        
        LoadScene(node.type);
    }

    void UpdateNodeVisual(MapNode node)
    {
        Image image = node.nodeObject.GetComponent<Image>();
        if (image == null) return;

        if (node.isCompleted)
        {
            image.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        else if (node.isAvailable)
        {
            image.color = GetColorForNodeType(node.type);
        }
        else
        {
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }
    }

    Color GetColorForNodeType(NodeType type)
    {
        switch (type)
        {
            case NodeType.Start: return Color.green;
            case NodeType.Shop: return Color.yellow;
            case NodeType.Money: return Color.cyan;
            case NodeType.Enemy: return Color.red;
            case NodeType.EliteEnemy: return new Color(1f, 0.3f, 0.3f);
            case NodeType.Boss: return new Color(0.5f, 0f, 0.5f);
            default: return Color.white;
        }
    }

    void LoadScene(NodeType node)
    {

        if (node == NodeType.Start) return;
        Level += 1;

        switch (node)
        {
            case NodeType.Start:
                Debug.Log("uhhhhhhhhhh");
                break;
            case NodeType.Shop:
                SceneManager.LoadScene("ShopScene", LoadSceneMode.Additive);
                setUpShop();
                break;
            case NodeType.Money:
                break;
            case NodeType.Enemy:
                SceneManager.LoadScene("FightScene", LoadSceneMode.Additive);
                break;
            case NodeType.EliteEnemy:
                break;
            case NodeType.Boss: 
                break;
        }
        for (int x = 0; x < this.transform.childCount; x++) this.transform.GetChild(x).gameObject.SetActive(false);
        updateCanvases();
    }

    public void setUpShop() 
    { 
        
    }

    public void updateCanvases() 
    {
        List<Canvas> canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).ToList();
        foreach (var can in canvases) 
        { 
            can.worldCamera = mainCamera;
        }
        List<CanvasScaler> scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None).ToList();
        foreach (var scaler in scalers)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }
    }

    void NextStage() 
    {
        stage += 1;
        Level = 0;
    }
}

public enum NodeType
{
    Start,
    Enemy,
    EliteEnemy,
    Shop,
    Money,
    Boss
}

[System.Serializable]
public class MapNode
{
    public Vector2 position;
    public Vector2 visualPos;
    public List<MapNode> connections;
    public NodeType type;
    public GameObject nodeObject;
    public bool isAvailable;
    public bool isCompleted;
    public int columnIndex;
}