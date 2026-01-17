using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("Spacing")]
    public float horizontalSpacing = 300f;
    public float verticalSpacing = 200f;

    [Header("Map Size")]
    public int maxNodesPerColumn = 4;
    public int totalColumns = 5;

    [Header("Shop Settings")]
    public int minShopCount = 1;
    public int maxShopCount = 2;

    [Header("Prefabs")]
    public GameObject nodePrefab;
    public GameObject connectionLinePrefab;

    private List<MapNode> allNodes = new List<MapNode>();
    private List<List<MapNode>> columns = new List<List<MapNode>>();

    private RectTransform canvasRect;

    enum NodeType
    {
        Start,
        Shop,
        Money,
        Enemy,
        EliteEnemy,
        Boss,
    }

    [System.Serializable]
    class MapNode
    {
        public NodeType type;
        public Vector2 position;
        public GameObject nodeObject;
        public List<MapNode> connections = new List<MapNode>();
        public int columnIndex;
        public int rowIndex;
        public bool isCompleted = false;
        public bool isAvailable = false;
    }

    private void Start()
    {
        canvasRect = GetComponent<RectTransform>();
        GenerateMap();
        DrawConnections();
    }

    void GenerateMap()
    {
        for (int i = 0; i < totalColumns; i++)
        {
            columns.Add(new List<MapNode>());
        }

        for (int col = 0; col < totalColumns; col++)
        {
            NodeType columnType = DetermineColumnType(col);
            int nodesInColumn = DetermineNodesInColumn(col, columnType);

            for (int row = 0; row < nodesInColumn; row++)
            {
                MapNode node = new MapNode
                {
                    type = columnType,
                    columnIndex = col,
                    rowIndex = row,
                    position = CalculateNodePosition(col, row, nodesInColumn)
                };

                if (col == 0)
                {
                    node.isAvailable = true;
                }

                CreateNodeVisual(node);
                columns[col].Add(node);
                allNodes.Add(node);
            }
        }

        CreateConnections();
    }

    NodeType DetermineColumnType(int columnIndex)
    {
        if (columnIndex == 0) return NodeType.Start;
        if (columnIndex == totalColumns - 1) return NodeType.Boss;

        if (Random.value < 0.3f) return NodeType.Shop;
        if (Random.value < 0.2f) return NodeType.EliteEnemy;
        if (Random.value < 0.3f) return NodeType.Money;

        return NodeType.Enemy;
    }

    int DetermineNodesInColumn(int columnIndex, NodeType type)
    {
        if (type == NodeType.Start || type == NodeType.Boss)
            return 1;

        return Random.Range(2, maxNodesPerColumn + 1);
    }

    Vector2 CalculateNodePosition(int column, int row, int totalRowsInColumn)
    {
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float startX = -canvasWidth / 2 + 100f;
        float x = startX + (column * horizontalSpacing);

        float totalHeight = (totalRowsInColumn - 1) * verticalSpacing;
        float startY = totalHeight / 2;
        float y = startY - (row * verticalSpacing);

        return new Vector2(x, y);
    }

    void CreateNodeVisual(MapNode node)
    {
        GameObject nodeObj = Instantiate(nodePrefab, transform);
        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = node.position;

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
        }

        UpdateNodeVisual(node);
    }

    void CreateConnections()
    {
        for (int col = 0; col < totalColumns - 1; col++)
        {
            List<MapNode> currentColumn = columns[col];
            List<MapNode> nextColumn = columns[col + 1];

            foreach (MapNode currentNode in currentColumn)
            {
                int connectionCount = Random.Range(1, Mathf.Min(3, nextColumn.Count + 1));

                List<MapNode> availableTargets = new List<MapNode>(nextColumn);

                for (int i = 0; i < connectionCount && availableTargets.Count > 0; i++)
                {
                    MapNode target = availableTargets[Random.Range(0, availableTargets.Count)];
                    currentNode.connections.Add(target);
                    availableTargets.Remove(target);
                }
            }
        }
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

        foreach (MapNode connectedNode in node.connections)
        {
            connectedNode.isAvailable = true;
            UpdateNodeVisual(connectedNode);
        }

        UpdateNodeVisual(node);

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
}