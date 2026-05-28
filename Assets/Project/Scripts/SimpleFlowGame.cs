using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Connect.Core
{
    public class SimpleFlowGame : MonoBehaviour
    {
        private const string MainMenuScene = "MainMenu";
        private const string GameplayScene = "Gameplay";
        private const int BoardSize = 5;

        private static readonly Color[] FlowColors =
        {
            new Color(0.94f, 0.18f, 0.18f),
            new Color(0.12f, 0.48f, 0.95f),
            new Color(0.16f, 0.72f, 0.28f),
            new Color(0.98f, 0.72f, 0.12f),
            new Color(0.72f, 0.24f, 0.88f)
        };

        private static readonly LevelDefinition[] Levels =
        {
            new LevelDefinition(new[] { "A...A", ".B.B.", "..C..", "..C..", "D...D" }),
            new LevelDefinition(new[] { "A...B", "D....", "C...B", "C....", "A...D" }),
            new LevelDefinition(new[] { "A.B..", ".....", "A.C..", "..C..", "D.B.D" }),
            new LevelDefinition(new[] { "A...B", ".C...", ".C...", "D...B", "D...A" }),
            new LevelDefinition(new[] { "A.B.C", ".....", "..D..", "..D..", "C.B.A" })
        };

        private readonly Dictionary<Vector2Int, CellView> cells = new Dictionary<Vector2Int, CellView>();
        private readonly Dictionary<char, List<Vector2Int>> paths = new Dictionary<char, List<Vector2Int>>();
        private readonly Dictionary<char, Color> colorsByPair = new Dictionary<char, Color>();
        private readonly Dictionary<char, List<Vector2Int>> endpointsByPair = new Dictionary<char, List<Vector2Int>>();

        private Camera mainCamera;
        private LevelDefinition currentLevel;
        private char activePair;
        private bool isDragging;
        private bool hasWon;
        private Text statusText;

        public static int CurrentLevelIndex
        {
            get => PlayerPrefs.GetInt("SimpleFlow.CurrentLevel", 0);
            private set => PlayerPrefs.SetInt("SimpleFlow.CurrentLevel", Mathf.Clamp(value, 0, Levels.Length - 1));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            EnsureInstance();
        }

        private static void EnsureInstance()
        {
            if (FindAnyObjectByType<SimpleFlowGame>() != null)
            {
                return;
            }

            var gameObject = new GameObject(nameof(SimpleFlowGame));
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<SimpleFlowGame>();
        }

        private void Awake()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == GameplayScene)
            {
                BuildGameplay();
                return;
            }

            BuildMainMenu();
        }

        private void BuildMainMenu()
        {
            ClearSceneObjects();
            mainCamera = EnsureCamera();
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.95f, 0.92f, 0.85f);

            var canvas = CreateCanvas("Flow Menu");
            CreateText(canvas.transform, "FLOW FREE", 72, new Vector2(0, 230), new Vector2(760, 120), FontStyle.Bold);
            CreateText(canvas.transform, "Choose one of 5 levels", 30, new Vector2(0, 145), new Vector2(760, 60), FontStyle.Normal);

            for (int i = 0; i < Levels.Length; i++)
            {
                int levelIndex = i;
                var button = CreateButton(canvas.transform, $"LEVEL {i + 1}", new Vector2(0, 55 - i * 72), new Vector2(360, 56));
                button.onClick.AddListener(() =>
                {
                    CurrentLevelIndex = levelIndex;
                    SceneManager.LoadScene(GameplayScene);
                });
            }
        }

        private void BuildGameplay()
        {
            ClearSceneObjects();
            mainCamera = EnsureCamera();
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.white;
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 4.25f;
            mainCamera.transform.position = new Vector3(BoardSize / 2f, BoardSize / 2f, -10f);

            cells.Clear();
            paths.Clear();
            colorsByPair.Clear();
            endpointsByPair.Clear();
            activePair = '\0';
            isDragging = false;
            hasWon = false;
            currentLevel = Levels[CurrentLevelIndex];

            CreateBoard();
            CreateGameplayUi();
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != GameplayScene || hasWon)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                BeginDrag();
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                ContinueDrag();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                activePair = '\0';
            }
        }

        private void CreateBoard()
        {
            var board = new GameObject("Board");
            board.transform.position = new Vector3(BoardSize / 2f, BoardSize / 2f, 0.15f);
            var boardSprite = board.AddComponent<SpriteRenderer>();
            boardSprite.sprite = LoadSprite("Board");
            boardSprite.drawMode = SpriteDrawMode.Sliced;
            boardSprite.size = new Vector2(BoardSize + 0.1f, BoardSize + 0.1f);
            boardSprite.color = new Color(0.88f, 0.82f, 0.68f);
            boardSprite.sortingOrder = -10;

            int colorIndex = 0;
            foreach (char pair in currentLevel.Pairs)
            {
                colorsByPair[pair] = FlowColors[colorIndex % FlowColors.Length];
                endpointsByPair[pair] = currentLevel.GetEndpoints(pair);
                colorIndex++;
            }

            for (int y = 0; y < BoardSize; y++)
            {
                for (int x = 0; x < BoardSize; x++)
                {
                    var pos = new Vector2Int(x, y);
                    cells[pos] = CreateCell(pos);
                }
            }
        }

        private CellView CreateCell(Vector2Int position)
        {
            var root = new GameObject($"Cell {position.x},{position.y}");
            root.transform.position = new Vector3(position.x + 0.5f, position.y + 0.5f, 0f);

            var background = root.AddComponent<SpriteRenderer>();
            background.sprite = LoadSprite("BGCell");
            background.color = new Color(0.95f, 0.9f, 0.8f);
            background.sortingOrder = -8;

            var hitbox = root.AddComponent<BoxCollider2D>();
            hitbox.size = Vector2.one;

            var lineObject = new GameObject("Line");
            lineObject.transform.SetParent(root.transform, false);
            var line = lineObject.AddComponent<SpriteRenderer>();
            line.sprite = LoadSprite("Pixel");
            line.drawMode = SpriteDrawMode.Sliced;
            line.size = new Vector2(0.72f, 0.72f);
            line.color = Color.clear;
            line.sortingOrder = -2;

            var pointObject = new GameObject("Point");
            pointObject.transform.SetParent(root.transform, false);
            var point = pointObject.AddComponent<SpriteRenderer>();
            point.sprite = LoadSprite("circle");
            point.transform.localScale = Vector3.one * 0.72f;
            point.sortingOrder = 2;

            char pair = currentLevel.GetPairAt(position);
            bool isEndpoint = pair != '.';
            pointObject.SetActive(isEndpoint);
            if (isEndpoint)
            {
                point.color = colorsByPair[pair];
            }

            return new CellView(position, isEndpoint ? pair : '\0', line);
        }

        private void BeginDrag()
        {
            CellView cell = GetCellUnderMouse();
            if (cell == null)
            {
                return;
            }

            char pair = cell.EndpointPair != '\0' ? cell.EndpointPair : cell.OccupiedBy;
            if (pair == '\0')
            {
                return;
            }

            activePair = pair;
            isDragging = true;
            ClearPath(activePair);
            AddCellToPath(cell);
        }

        private void ContinueDrag()
        {
            CellView cell = GetCellUnderMouse();
            if (cell == null)
            {
                return;
            }

            var path = paths[activePair];
            var last = path[path.Count - 1];
            if (cell.Position == last)
            {
                return;
            }

            if (path.Count > 1 && cell.Position == path[path.Count - 2])
            {
                RemoveLastPathCell();
                return;
            }

            if (Mathf.Abs(cell.Position.x - last.x) + Mathf.Abs(cell.Position.y - last.y) != 1)
            {
                return;
            }

            if (cell.EndpointPair != '\0' && cell.EndpointPair != activePair)
            {
                return;
            }

            if (cell.OccupiedBy != '\0' && cell.OccupiedBy != activePair)
            {
                ClearPath(cell.OccupiedBy);
            }

            if (!path.Contains(cell.Position))
            {
                AddCellToPath(cell);
                CheckWin();
            }
        }

        private void AddCellToPath(CellView cell)
        {
            if (!paths.ContainsKey(activePair))
            {
                paths[activePair] = new List<Vector2Int>();
            }

            paths[activePair].Add(cell.Position);
            cell.Occupy(activePair, colorsByPair[activePair]);
        }

        private void RemoveLastPathCell()
        {
            var path = paths[activePair];
            if (path.Count <= 1)
            {
                return;
            }

            var lastCell = cells[path[path.Count - 1]];
            path.RemoveAt(path.Count - 1);
            if (lastCell.EndpointPair == '\0')
            {
                lastCell.Clear();
            }
        }

        private void ClearPath(char pair)
        {
            if (!paths.TryGetValue(pair, out var path))
            {
                paths[pair] = new List<Vector2Int>();
                return;
            }

            foreach (var position in path)
            {
                var cell = cells[position];
                if (cell.EndpointPair == '\0')
                {
                    cell.Clear();
                }
            }

            path.Clear();
        }

        private void CheckWin()
        {
            foreach (char pair in currentLevel.Pairs)
            {
                if (!paths.TryGetValue(pair, out var path) || path.Count < 2)
                {
                    return;
                }

                var endpoints = endpointsByPair[pair];
                bool connectsForward = path[0] == endpoints[0] && path[path.Count - 1] == endpoints[1];
                bool connectsBackward = path[0] == endpoints[1] && path[path.Count - 1] == endpoints[0];
                if (!connectsForward && !connectsBackward)
                {
                    return;
                }
            }

            hasWon = true;
            statusText.text = "LEVEL COMPLETE";
            PlayerPrefs.SetInt($"SimpleFlow.Unlocked.{Mathf.Min(CurrentLevelIndex + 1, Levels.Length - 1)}", 1);
        }

        private CellView GetCellUnderMouse()
        {
            Vector3 world = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var grid = new Vector2Int(Mathf.FloorToInt(world.x), Mathf.FloorToInt(world.y));
            return cells.TryGetValue(grid, out var cell) ? cell : null;
        }

        private void CreateGameplayUi()
        {
            var canvas = CreateCanvas("Flow HUD");
            statusText = CreateText(canvas.transform, $"LEVEL {CurrentLevelIndex + 1}", 34, new Vector2(0, 300), new Vector2(460, 52), FontStyle.Bold);

            var menuButton = CreateButton(canvas.transform, "MENU", new Vector2(-260, -300), new Vector2(140, 48));
            menuButton.onClick.AddListener(() => SceneManager.LoadScene(MainMenuScene));

            var restartButton = CreateButton(canvas.transform, "RESTART", new Vector2(0, -300), new Vector2(170, 48));
            restartButton.onClick.AddListener(() => SceneManager.LoadScene(GameplayScene));

            var nextButton = CreateButton(canvas.transform, "NEXT", new Vector2(260, -300), new Vector2(140, 48));
            nextButton.onClick.AddListener(() =>
            {
                if (!hasWon)
                {
                    return;
                }

                if (CurrentLevelIndex < Levels.Length - 1)
                {
                    CurrentLevelIndex++;
                    SceneManager.LoadScene(GameplayScene);
                }
                else
                {
                    SceneManager.LoadScene(MainMenuScene);
                }
            });
        }

        private static void ClearSceneObjects()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.GetComponent<SimpleFlowGame>() == null)
                {
                    root.SetActive(false);
                }
            }
        }

        private static Camera EnsureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            return camera;
        }

        private static Canvas CreateCanvas(string name)
        {
            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            return canvas;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size)
        {
            var buttonObject = new GameObject(label);
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = buttonObject.AddComponent<Image>();
            image.sprite = LoadSprite("SquareSliced32");
            image.type = Image.Type.Sliced;
            image.color = new Color(0.36f, 0.79f, 0.9f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            CreateText(buttonObject.transform, label, 26, Vector2.zero, size, FontStyle.Bold);
            return button;
        }

        private static Text CreateText(Transform parent, string text, int fontSize, Vector2 position, Vector2 size, FontStyle style)
        {
            var textObject = new GameObject(text);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = textObject.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, fontSize);
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = new Color(0.18f, 0.18f, 0.18f);
            return label;
        }

        private static Sprite LoadSprite(string spriteName)
        {
            return Resources.Load<Sprite>($"Sprites/{spriteName}");
        }

        private sealed class CellView
        {
            private readonly SpriteRenderer line;

            public CellView(Vector2Int position, char endpointPair, SpriteRenderer line)
            {
                Position = position;
                EndpointPair = endpointPair;
                OccupiedBy = endpointPair;
                this.line = line;

                if (endpointPair != '\0')
                {
                    line.color = Color.clear;
                }
            }

            public Vector2Int Position { get; }
            public char EndpointPair { get; }
            public char OccupiedBy { get; private set; }

            public void Occupy(char pair, Color color)
            {
                OccupiedBy = pair;
                line.color = new Color(color.r, color.g, color.b, 0.72f);
            }

            public void Clear()
            {
                OccupiedBy = '\0';
                line.color = Color.clear;
            }
        }

        private sealed class LevelDefinition
        {
            private readonly string[] rows;
            private readonly List<char> pairs = new List<char>();

            public LevelDefinition(string[] rows)
            {
                this.rows = rows;
                var seen = new HashSet<char>();
                foreach (string row in rows)
                {
                    foreach (char value in row)
                    {
                        if (value != '.' && seen.Add(value))
                        {
                            pairs.Add(value);
                        }
                    }
                }

                foreach (char pair in pairs)
                {
                    if (GetEndpoints(pair).Count != 2)
                    {
                        throw new ArgumentException($"Level pair '{pair}' must have exactly two endpoints.");
                    }
                }
            }

            public IEnumerable<char> Pairs => pairs;

            public char GetPairAt(Vector2Int position)
            {
                return rows[BoardSize - 1 - position.y][position.x];
            }

            public List<Vector2Int> GetEndpoints(char pair)
            {
                var endpoints = new List<Vector2Int>();
                for (int y = 0; y < BoardSize; y++)
                {
                    for (int x = 0; x < BoardSize; x++)
                    {
                        var position = new Vector2Int(x, y);
                        if (GetPairAt(position) == pair)
                        {
                            endpoints.Add(position);
                        }
                    }
                }

                return endpoints;
            }
        }
    }
}
