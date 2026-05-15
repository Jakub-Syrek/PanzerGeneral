using PanzerGeneral.Game;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PanzerGeneral
{
    public partial class MainWindow : Window
    {
        private const double HexSize = 20;
        private const int MapRows = 28;
        private const int MapColumns = 44;
        private const int MaxUnitsPerPlayer = 20;

        private readonly Dictionary<(int column, int row), HexTile> _map = new();
        private readonly List<Unit> _units = new();
        private readonly TurnManager _turnManager;
        private readonly Dictionary<int, Brush> _playerColors;
        private readonly Dictionary<int, int> _deployedUnitsPerPlayer = new();

        private Polygon? _selectedHex;
        private Unit? _selectedUnit;
        private bool _isDeployMode;
        private bool _aiThinking;
        private bool _emulateMode;
        private AiController _ai = null!;
        private readonly List<UIElement> _movementOverlays = new();

        // Sąsiedzi w układzie offset (parzyste/nieparzyste wiersze)
        private static readonly (int dc, int dr)[] EvenRowNeighbors =
            { (-1, 0), (1, 0), (-1, -1), (0, -1), (-1, 1), (0, 1) };
        private static readonly (int dc, int dr)[] OddRowNeighbors =
            { (-1, 0), (1, 0), (0, -1), (1, -1), (0, 1), (1, 1) };

        public MainWindow()
        {
            InitializeComponent();

            _playerColors = new Dictionary<int, Brush>
            {
                { 1, new SolidColorBrush(Color.FromRgb(62, 72, 54)) },  // Axis Feldgrau
                { 2, new SolidColorBrush(Color.FromRgb(120, 100, 45)) } // Allied khaki
            };
            _deployedUnitsPerPlayer[1] = 0;
            _deployedUnitsPerPlayer[2] = 0;

            _turnManager = new TurnManager(new[] { 1, 2 });
            _isDeployMode = false;

            GenerateMap();
            _ai = new AiController(_map, _units);
            AutoDeployAll();
            DrawMap();
            StartTurn();
            UpdateUI();
        }

        private void AutoDeployAll()
        {
            // === AXIS (Player 1) — Wehrmacht, September 1939 ===
            // Grupa Armii Północ (East Prussia) — atak na Polskę z północy
            PlaceUnit(1, UnitType.Tank,       27, 9);   // Pz.Div — Prusy Wschodnie
            PlaceUnit(1, UnitType.Mechanized, 26, 10);  // Mot.Div — Prusy Wschodnie
            PlaceUnit(1, UnitType.Infantry,   28, 10);  // Inf.Div — Prusy Wschodnie

            // Grupa Armii Centrum (Pomerania/Silesia) — główne uderzenie
            PlaceUnit(1, UnitType.Tank,       24, 12);  // Pz.Div — Pomorze
            PlaceUnit(1, UnitType.Tank,       25, 13);  // Pz.Div — Śląsk
            PlaceUnit(1, UnitType.Mechanized, 23, 13);  // Mot.Div — centrum
            PlaceUnit(1, UnitType.Infantry,   24, 14);  // Inf.Div — centrum
            PlaceUnit(1, UnitType.Infantry,   25, 14);  // Inf.Div — centrum

            // Grupa Armii Południe (Silesia/Slovakia) — oskrzydlenie od południa
            PlaceUnit(1, UnitType.Tank,       25, 16);  // Pz.Div — Śląsk płd.
            PlaceUnit(1, UnitType.Mechanized, 26, 17);  // Mot.Div — Słowacja
            PlaceUnit(1, UnitType.Infantry,   24, 17);  // Inf.Div — Śląsk płd.

            // Linia Zygfryda (front zachodni — defensywa)
            PlaceUnit(1, UnitType.Infantry,   18, 15);  // Inf.Div — Zygfryd płn.
            PlaceUnit(1, UnitType.Infantry,   18, 16);  // Inf.Div — Zygfryd cent.
            PlaceUnit(1, UnitType.Infantry,   18, 17);  // Inf.Div — Zygfryd płd.
            PlaceUnit(1, UnitType.Mechanized, 19, 14);  // Mot.Div — rezerwa

            // === ALLIES (Player 2) — wrzesień 1939 ===
            // Armia Polska — obrona granicy zachodniej
            PlaceUnit(2, UnitType.Infantry,   30, 10);  // Armia Modlin — płn.
            PlaceUnit(2, UnitType.Infantry,   31, 11);  // Armia Pomorze
            PlaceUnit(2, UnitType.Tank,       30, 12);  // Brygada Pancerna
            PlaceUnit(2, UnitType.Infantry,   31, 12);  // Armia Poznań
            PlaceUnit(2, UnitType.Mechanized, 30, 13);  // Armia Łódź
            PlaceUnit(2, UnitType.Infantry,   31, 14);  // Armia Kraków
            PlaceUnit(2, UnitType.Infantry,   30, 15);  // Armia Kraków
            PlaceUnit(2, UnitType.Infantry,   32, 15);  // Armia Karpaty
            PlaceUnit(2, UnitType.Tank,       32, 13);  // Brygada Pancerna — rezerwa

            // Armia Francuska — Linia Maginota
            PlaceUnit(2, UnitType.Infantry,   16, 16);  // Armia I — Maginot płn.
            PlaceUnit(2, UnitType.Infantry,   16, 17);  // Armia II — Maginot cent.
            PlaceUnit(2, UnitType.Infantry,   16, 18);  // Armia III — Maginot płd.
            PlaceUnit(2, UnitType.Mechanized, 15, 16);  // DCR — rezerwa mobilna

            // Brytyjskie Siły Ekspedycyjne (BEF) — Belgia/Francja płn.
            PlaceUnit(2, UnitType.Infantry,   14, 14);  // BEF — I Korpus
            PlaceUnit(2, UnitType.Mechanized, 15, 14);  // BEF — mobilna
        }

        /// <summary>
        /// Umieszcza jednostkę na mapie (lub najbliższym wolnym hexie jeśli zajęty)
        /// </summary>
        private void PlaceUnit(int playerId, UnitType type, int col, int row)
        {
            var pos = FindNearestValid(col, row, playerId);
            if (pos == null) return;
            _units.Add(UnitCatalog.Create(type, playerId, pos.Value.col, pos.Value.row));
            _deployedUnitsPerPlayer[playerId]++;
        }

        private (int col, int row)? FindNearestValid(int startCol, int startRow, int playerId)
        {
            var visited = new HashSet<(int, int)>();
            var queue = new Queue<(int col, int row)>();
            queue.Enqueue((startCol, startRow));
            visited.Add((startCol, startRow));

            while (queue.Count > 0)
            {
                var (col, row) = queue.Dequeue();
                if (_map.TryGetValue((col, row), out var tile)
                    && !tile.BlocksMovement
                    && tile.MoveCost < 99
                    && !_units.Any(u => u.Column == col && u.Row == row))
                {
                    return (col, row);
                }

                var offsets = row % 2 == 0 ? EvenRowNeighbors : OddRowNeighbors;
                foreach (var (dc, dr) in offsets)
                {
                    var next = (col + dc, row + dr);
                    if (!visited.Contains(next) && _map.ContainsKey(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Generuje mapę z predefiniowanym układem terenu
        /// </summary>
        private void GenerateMap()
        {
            _map.Clear();

            for (int row = 0; row < MapRows; row++)
            {
                for (int col = 0; col < MapColumns; col++)
                {
                    TerrainType terrain = GetTerrainForPosition(col, row);
                    _map[(col, row)] = new HexTile
                    {
                        Column = col,
                        Row = row,
                        Terrain = terrain,
                        MoveCost = TerrainCatalog.GetMoveCost(terrain),
                        DefenseBonus = TerrainCatalog.GetDefenseBonus(terrain),
                        BlocksMovement = TerrainCatalog.BlocksMovement(terrain),
                        BlocksLineOfSight = TerrainCatalog.BlocksLineOfSight(terrain),
                        IsObjective = TerrainCatalog.IsObjective(terrain)
                    };
                }
            }
        }

        /// <summary>
        /// Europa 1939 — teren dla danej pozycji na mapie (44×28 hexów, ~70km/hex)
        /// </summary>
        private TerrainType GetTerrainForPosition(int col, int row)
        {
            // ── MORZA ──────────────────────────────────────────────
            // Atlantyk (zachód)
            if (col <= 0) return TerrainType.Water;
            if (col <= 1 && (row < 4 || row > 13)) return TerrainType.Water;
            if (col == 2 && row <= 3) return TerrainType.Water;
            if (col <= 2 && row >= 20) return TerrainType.Water;

            // Morze Północne (między Wyspami a Skandynawią/Niemcami)
            if (col >= 9 && col <= 19 && row <= 7) return TerrainType.Water;
            if (col >= 7 && col <= 11 && row <= 3) return TerrainType.Water;

            // Kanał La Manche
            if (col >= 7 && col <= 14 && row >= 12 && row <= 14) return TerrainType.Water;
            if (col >= 7 && col <= 10 && row == 11) return TerrainType.Water;

            // Morze Bałtyckie
            if (col >= 21 && col <= 31 && row >= 5 && row <= 9) return TerrainType.Water;
            if (col >= 23 && col <= 27 && row == 10) return TerrainType.Water;
            if (col >= 19 && col <= 22 && row >= 6 && row <= 8) return TerrainType.Water;

            // Morze Śródziemne (południe)
            if (row >= 25) return TerrainType.Water;
            if (row >= 23 && col >= 6 && col <= 36) return TerrainType.Water;

            // Morze Adriatyckie
            if (col >= 23 && col <= 26 && row >= 21 && row <= 24) return TerrainType.Water;

            // Morze Czarne
            if (col >= 36 && col <= 43 && row >= 19 && row <= 23) return TerrainType.Water;

            // Zatoka Fińska / Bałtyk wschód
            if (col >= 30 && col <= 36 && row >= 4 && row <= 7) return TerrainType.Water;

            // ── WIELKA BRYTANIA (wyspa) ─────────────────────────────
            if (col >= 4 && col <= 9 && row >= 4 && row <= 12)
            {
                // Wody wokół wyspy (kanał wschodni)
                if (col == 9 && row >= 7) return TerrainType.Water;
                if (col == 8 && row >= 10) return TerrainType.Water;
                // Londyn
                if (col == 7 && row == 9) return TerrainType.City;
                // Manchester / Glasgow
                if ((col == 6 && row == 7) || (col == 5 && row == 5)) return TerrainType.City;
                // Szkockie wyżyny
                if (col <= 6 && row <= 6) return TerrainType.Hills;
                return TerrainType.Plain;
            }

            // ── SKANDYNAWIA ─────────────────────────────────────────
            // Norwegia (góry i lasy)
            if (col >= 18 && col <= 22 && row <= 9)
            {
                if (col == 20 && row == 4) return TerrainType.City; // Oslo
                if (col >= 19 && col <= 22 && row <= 6) return TerrainType.Mountains;
                return TerrainType.Forest;
            }
            // Szwecja / Finlandia
            if (col >= 22 && col <= 33 && row <= 9)
            {
                if (col == 24 && row == 6) return TerrainType.City; // Sztokholm
                if (col >= 30 && col <= 33 && row <= 7) return TerrainType.Forest; // Finlandia
                return TerrainType.Forest;
            }
            // Dania
            if (col >= 20 && col <= 22 && row >= 9 && row <= 11)
            {
                if (col == 21 && row == 10) return TerrainType.City; // Kopenhaga
                return TerrainType.Plain;
            }

            // ── HOLANDIA / BELGIA ────────────────────────────────────
            if (col >= 15 && col <= 18 && row >= 11 && row <= 13)
            {
                if (col == 16 && row == 11) return TerrainType.City; // Amsterdam/Rotterdam
                if (col == 17 && row == 12) return TerrainType.City; // Bruksela
                return TerrainType.Plain;
            }

            // ── FRANCJA ─────────────────────────────────────────────
            if (col >= 5 && col <= 17 && row >= 14 && row <= 22)
            {
                // Paryż
                if (col >= 10 && col <= 12 && row >= 14 && row <= 16) return TerrainType.City;
                // Lyon / Marsylia
                if ((col == 12 && row == 19) || (col == 12 && row == 21)) return TerrainType.City;
                // Bordeaux
                if (col == 8 && row == 20) return TerrainType.City;
                // Linia Maginota (fortyfikacje = City z bonusem obrony)
                if (col >= 15 && col <= 17 && row >= 16 && row <= 19) return TerrainType.City;
                // Ardeny (las)
                if (col >= 14 && col <= 17 && row >= 13 && row <= 15) return TerrainType.Forest;
                // Pireneje (góry)
                if (col >= 7 && col <= 13 && row >= 21 && row <= 22) return TerrainType.Mountains;
                return TerrainType.Plain;
            }

            // ── NIEMCY ──────────────────────────────────────────────
            if (col >= 17 && col <= 26 && row >= 9 && row <= 19)
            {
                // Berlin
                if (col >= 22 && col <= 23 && row >= 11 && row <= 12) return TerrainType.City;
                // Hamburg
                if (col == 19 && row == 10) return TerrainType.City;
                // Monachium
                if (col == 22 && row == 17) return TerrainType.City;
                // Kolonia / Frankfurt
                if ((col == 19 && row == 14) || (col == 20 && row == 15)) return TerrainType.City;
                // Linia Zygfryda (zachodni wał)
                if (col >= 17 && col <= 18 && row >= 15 && row <= 18) return TerrainType.City;
                // Lasy środkowe Niemcy
                if (col >= 19 && col <= 22 && row >= 9 && row <= 11) return TerrainType.Forest;
                if (col >= 20 && col <= 24 && row >= 16 && row <= 18) return TerrainType.Forest;
                return TerrainType.Plain;
            }
            // Prusy Wschodnie (wybrzusze)
            if (col >= 26 && col <= 29 && row >= 8 && row <= 10)
            {
                if (col == 27 && row == 9) return TerrainType.City; // Królewiec
                return TerrainType.Plain;
            }

            // ── POLSKA ──────────────────────────────────────────────
            if (col >= 26 && col <= 35 && row >= 10 && row <= 18)
            {
                // Warszawa
                if (col >= 29 && col <= 30 && row >= 11 && row <= 12) return TerrainType.City;
                // Kraków
                if (col == 29 && row == 15) return TerrainType.City;
                // Lwów
                if (col == 31 && row == 16) return TerrainType.City;
                // Łódź / Poznań
                if ((col == 28 && row == 12) || (col == 26 && row == 12)) return TerrainType.City;
                // Gdańsk
                if (col == 27 && row == 10) return TerrainType.City;
                // Lasy wschodnie
                if (col >= 32 && col <= 35 && row >= 11 && row <= 16) return TerrainType.Forest;
                return TerrainType.Plain;
            }

            // ── AUSTRIA / CZECHOSŁOWACJA ─────────────────────────────
            if (col >= 22 && col <= 28 && row >= 16 && row <= 18)
            {
                if ((col == 24 && row == 17) || (col == 25 && row == 17)) return TerrainType.City; // Wiedeń/Praga
                return TerrainType.Plain;
            }

            // ── KARPATY ─────────────────────────────────────────────
            if (col >= 27 && col <= 34 && row >= 16 && row <= 19) return TerrainType.Mountains;

            // ── ALPY ────────────────────────────────────────────────
            if (col >= 16 && col <= 23 && row >= 19 && row <= 21) return TerrainType.Mountains;

            // ── WŁOCHY ──────────────────────────────────────────────
            if (col >= 17 && col <= 23 && row >= 20 && row <= 25)
            {
                if (col == 19 && row == 22) return TerrainType.City; // Mediolan
                if (col == 20 && row == 24) return TerrainType.City; // Rzym
                // Apeniny
                if (col >= 20 && col <= 22 && row >= 21 && row <= 23) return TerrainType.Hills;
                return TerrainType.Plain;
            }

            // ── BAŁKANY ─────────────────────────────────────────────
            if (col >= 25 && col <= 36 && row >= 18 && row <= 25)
            {
                if (col == 26 && row == 19) return TerrainType.City; // Budapeszt
                if (col == 28 && row == 21) return TerrainType.City; // Belgrad
                if (col == 30 && row == 22) return TerrainType.City; // Sofia
                if (col == 32 && row == 24) return TerrainType.City; // Ateny
                if (col >= 27 && col <= 32 && row >= 19 && row <= 22) return TerrainType.Hills;
                return TerrainType.Plain;
            }

            // ── ROSJA / ZSRR ─────────────────────────────────────────
            if (col >= 34 && row >= 8 && row <= 20)
            {
                if (col >= 35 && col <= 37 && row >= 9 && row <= 11) return TerrainType.Forest; // Lasy białoruskie
                if (col >= 37 && col <= 40 && row >= 11 && row <= 14) return TerrainType.Forest; // Lasy ukraińskie
                if (col == 36 && row == 11) return TerrainType.City; // Mińsk
                if (col == 38 && row == 14) return TerrainType.City; // Kijów
                if (col == 43 && row == 12) return TerrainType.City; // Moskwa (krawędź)
                return TerrainType.Plain;
            }

            // ── UKRAINA / RUMUNIA ────────────────────────────────────
            if (col >= 34 && col <= 43 && row >= 17 && row <= 22)
            {
                if (col == 37 && row == 18) return TerrainType.City; // Odessa
                return TerrainType.Plain;
            }

            // ── IBERIA (Hiszpania/Portugalia) ────────────────────────
            if (col >= 3 && col <= 13 && row >= 20 && row <= 25)
            {
                if (col == 9 && row == 21) return TerrainType.City;  // Madryt
                if (col == 7 && row == 23) return TerrainType.City;  // Lizbona
                if (col == 11 && row == 23) return TerrainType.City; // Barcelona
                if (col >= 4 && col <= 7 && row >= 20 && row <= 22) return TerrainType.Hills;
                return TerrainType.Plain;
            }

            // Domyślnie: woda (ocean)
            return TerrainType.Water;
        }

        /// <summary>
        /// Rysuje mapę hexów
        /// </summary>
        private void DrawMap()
        {
            GameCanvas.Children.Clear();

            const double offsetX = 30;
            const double offsetY = 30;

            double hexWidth = Math.Sqrt(3) * HexSize;
            double hexHeight = 2 * HexSize;
            double horizontalSpacing = hexWidth;
            double verticalSpacing = hexHeight * 0.75;

            // Ustaw rozmiar canvasu dla ScrollViewer
            GameCanvas.Width  = offsetX * 2 + MapColumns * horizontalSpacing + hexWidth / 2;
            GameCanvas.Height = offsetY * 2 + MapRows * verticalSpacing + HexSize;

            foreach (var tile in _map.Values)
            {
                double x = offsetX + tile.Column * horizontalSpacing;
                if (tile.Row % 2 == 1)
                    x += hexWidth / 2;
                double y = offsetY + tile.Row * verticalSpacing;
                var center = new Point(x, y);

                var hex = new Polygon
                {
                    Points = HexHelper.GetHexPoints(center, HexSize),
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 100, 100, 100)),
                    StrokeThickness = 0.5,
                    Fill = TextureHelper.GetTextureBrush(tile.Terrain),
                    Tag = tile
                };

                hex.MouseEnter += Hex_MouseEnter;
                hex.MouseLeave += Hex_MouseLeave;
                hex.MouseLeftButtonDown += Hex_Click;

                GameCanvas.Children.Add(hex);
                DrawTerrainDecorations(tile.Terrain, center, HexSize);
            }

            AddCountryLabels(offsetX, offsetY, horizontalSpacing, verticalSpacing);
            DrawUnits();
        }

        private double HexCenterX(int col, int row, double offsetX)
        {
            double hexWidth = Math.Sqrt(3) * HexSize;
            return offsetX + col * hexWidth + (row % 2 == 1 ? hexWidth / 2 : 0);
        }

        private double HexCenterY(int row, double offsetY)
        {
            double verticalSpacing = 2 * HexSize * 0.75;
            return offsetY + row * verticalSpacing;
        }

        private void AddCountryLabel(string text, double cx, double cy, Color color, double fontSize = 11)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                Opacity = 0.75,
                IsHitTestVisible = false,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 3,
                    ShadowDepth = 1,
                    Opacity = 0.9
                }
            };
            tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(tb, cx - tb.DesiredSize.Width / 2);
            Canvas.SetTop(tb, cy - tb.DesiredSize.Height / 2);
            Panel.SetZIndex(tb, 2);
            GameCanvas.Children.Add(tb);
        }

        private void AddCountryLabels(double ox, double oy, double hs, double vs)
        {
            double Cx(int col, int row) => ox + col * hs + (row % 2 == 1 ? hs / 2 : 0);
            double Cy(int row) => oy + row * vs;

            var water = Color.FromRgb(180, 220, 255);
            var land  = Color.FromRgb(240, 235, 200);
            var axis  = Color.FromRgb(180, 200, 155);
            var ally  = Color.FromRgb(200, 190, 130);

            AddCountryLabel("WIELKA\nBRYTANIA",  Cx(6, 8),  Cy(8),  land, 9);
            AddCountryLabel("FRANCJA",           Cx(11,17), Cy(17), ally, 10);
            AddCountryLabel("NIEMCY",            Cx(21,13), Cy(13), axis, 11);
            AddCountryLabel("POLSKA",            Cx(31,13), Cy(13), ally, 10);
            AddCountryLabel("ROSJA",             Cx(39,14), Cy(14), land, 11);
            AddCountryLabel("SZWECJA",           Cx(24, 5), Cy(5),  land, 9);
            AddCountryLabel("NORWEGIA",          Cx(20, 6), Cy(6),  land, 8);
            AddCountryLabel("HISZPANIA",         Cx(9, 22), Cy(22), land, 9);
            AddCountryLabel("WŁOCHY",            Cx(20,22), Cy(22), land, 9);
            AddCountryLabel("BAŁKANY",           Cx(29,22), Cy(22), land, 9);
            AddCountryLabel("M. PÓŁNOCNE",       Cx(14, 4), Cy(4),  water, 8);
            AddCountryLabel("BAŁTYK",            Cx(25, 7), Cy(7),  water, 8);
            AddCountryLabel("ATLANTYK",          Cx(2, 17), Cy(17), water, 8);
            AddCountryLabel("M. ŚRÓDZIEMNE",     Cx(20,24), Cy(24), water, 8);
        }

        /// <summary>
        /// Rysuje jednostki na mapie
        /// </summary>
        private void DrawUnits()
        {
            const double offsetX = 30;
            const double offsetY = 30;

            double hexWidth = Math.Sqrt(3) * HexSize;
            double hexHeight = 2 * HexSize;

            double horizontalSpacing = hexWidth;
            double verticalSpacing = hexHeight * 0.75;

            // Usunięcie starych elementów jednostek z canvasu
            var toRemove = GameCanvas.Children
                .OfType<FrameworkElement>()
                .Where(fe => fe.Tag is Unit)
                .ToList();

            foreach (var el in toRemove)
                GameCanvas.Children.Remove(el);

            // Rysowanie nowych jednostek
            foreach (var unit in _units)
            {
                double x = offsetX + unit.Column * horizontalSpacing;
                if (unit.Row % 2 == 1)
                    x += hexWidth / 2;
                double y = offsetY + unit.Row * verticalSpacing;
                DrawUnitToken(unit, x, y);
            }
        }

        private void StartTurn()
        {
            _turnManager.StartTurn(_units);

            bool isAiTurn = _turnManager.CurrentPlayerId == 2 || _emulateMode;
            if (!_isDeployMode && isAiTurn)
                _ = ExecuteAiTurnAsync(_turnManager.CurrentPlayerId);
        }

        private async Task ExecuteAiTurnAsync(int playerId)
        {
            _aiThinking = true;
            SetUiEnabled(false);
            UpdateUI();

            await Task.Delay(500);

            var actions = _ai.PlanTurn(playerId);

            foreach (var action in actions)
            {
                if (action.MoveTo.HasValue)
                {
                    MoveUnit(action.Unit, action.MoveTo.Value.col, action.MoveTo.Value.row);
                    PlayMoveSound(action.Unit);
                    DrawUnits();
                    await Task.Delay(350);
                }

                if (action.Attack != null)
                {
                    if (!_map.TryGetValue((action.Attack.Column, action.Attack.Row), out var defTile))
                        defTile = new HexTile();

                    SoundManager.PlayAttack();
                    bool defeated = CombatResolver.Resolve(action.Unit, action.Attack, defTile);
                    if (defeated) _units.Remove(action.Attack);
                    DrawUnits();
                    await Task.Delay(300);

                    if (CheckVictory()) return;
                }

                action.Unit.HasMoved = true;
            }

            await Task.Delay(400);

            _aiThinking = false;
            SetUiEnabled(true);

            // Switch back to player 1
            _turnManager.NextTurn();
            StartTurn();
            DrawUnits();
            UpdateUI();
            _selectedUnit = null;
            ClearMovementRange();
        }

        private bool CheckVictory()
        {
            bool p1Lost = !_units.Any(u => u.OwnerId == 1);
            bool p2Lost = !_units.Any(u => u.OwnerId == 2);

            if (p1Lost || p2Lost)
            {
                _aiThinking = false;
                SetUiEnabled(false);
                string winner = p2Lost ? "Gracz 1" : "Gracz 2 (AI)";
                MessageBox.Show($"{winner} wygrywa! Wszystkie jednostki wroga zniszczone.",
                    "Koniec gry", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            return false;
        }

        private void SetUiEnabled(bool enabled)
        {
            NextTurnButton.IsEnabled     = enabled;
            EndTurnButton.IsEnabled      = enabled;
            ToggleDeployButton.IsEnabled = enabled;
            GameCanvas.IsHitTestVisible  = enabled;
            EmulateButton.IsEnabled      = true; // zawsze aktywny — można zatrzymać w każdej chwili
        }

        /// <summary>
        /// Aktualizacja interfejsu użytkownika
        /// </summary>
        private void UpdateUI()
        {
            string phaseText = _aiThinking ? "AI mysli..." : "Twoja tura";
            string playerText = $"Gracz {_turnManager.CurrentPlayerId} — Tura {_turnManager.TurnNumber}";

            Title = $"Panzer General | {playerText} | {phaseText}";

            if (PlayerLabel != null)
                PlayerLabel.Text = playerText;

            if (PhaseLabel != null)
                PhaseLabel.Text = phaseText;
        }

        /// <summary>
        /// Obsługa wejścia myszą na hex
        /// </summary>
        private void Hex_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Polygon hex && hex != _selectedHex)
            {
                hex.Stroke = Brushes.LightGreen;
                hex.StrokeThickness = 2;
            }
        }

        /// <summary>
        /// Obsługa wyjścia myszą z hexu
        /// </summary>
        private void Hex_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Polygon hex && hex != _selectedHex)
            {
                hex.Stroke = Brushes.Gray;
                hex.StrokeThickness = 1;
            }
        }

        /// <summary>
        /// Obsługa kliknięcia na hex
        /// </summary>
        private void Hex_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Polygon hex)
                return;

            // Ignoruj kliknięcia na heksagony jednostek
            if (hex.Tag is Unit)
                return;

            if (hex.Tag is not HexTile tile)
                return;

            // Odznaczenie poprzedniego hexu
            if (_selectedHex != null)
            {
                _selectedHex.Stroke = Brushes.Gray;
                _selectedHex.StrokeThickness = 1;
            }

            hex.Stroke = Brushes.Yellow;
            hex.StrokeThickness = 3;
            _selectedHex = hex;

            Title = $"Panzer General | {tile.Column},{tile.Row} | {tile.Terrain} | Move: {tile.MoveCost} | Def: {tile.DefenseBonus}";

            // Faza rozkładania - postawienie nowej jednostki
            if (_isDeployMode && IsValidDeployZone(tile.Column, _turnManager.CurrentPlayerId))
            {
                // Sprawdzenie czy pole jest wolne
                if (!_units.Any(u => u.Column == tile.Column && u.Row == tile.Row))
                {
                    // Zamiast tego: menu do wyboru jednostki lub automatyczne
                    // Na razie: wciśnij przycisk "Deploy Unit"
                }
            }
            // Faza akcji - ruch lub atak
            else if (!_isDeployMode && _selectedUnit != null)
            {
                HandleUnitAction(_selectedUnit, tile);
            }
        }

        /// <summary>
        /// Obsługa kliknięcia na jednostkę
        /// </summary>
        private void Unit_Click(object sender, MouseButtonEventArgs e)
        {
            if (_aiThinking) return;
            if (sender is not FrameworkElement el || el.Tag is not Unit unit)
                return;

            // Kliknięcie własnej jednostki — wybierz i pokaż zasięg
            if (unit.OwnerId == _turnManager.CurrentPlayerId)
            {
                _selectedUnit = unit;
                ShowMovementRange(unit);
                Title = $"Panzer General | {unit.Name} | HP: {unit.HP} | Ruch: {unit.CurrentMovePoints}/{unit.MaxMovePoints}";
                e.Handled = true;
                return;
            }

            // Kliknięcie wrogiej jednostki z wybranym atakującym — atak
            if (_selectedUnit != null && !_selectedUnit.HasMoved)
            {
                TryAttackEnemy(_selectedUnit, unit);
                e.Handled = true;
            }
        }

        private void TryAttackEnemy(Unit attacker, Unit defender)
        {
            // Sprawdź czy wróg jest bezpośrednio sąsiedni
            var nbNow = GetNeighbors(attacker.Column, attacker.Row).ToHashSet();
            if (nbNow.Contains((defender.Column, defender.Row)))
            {
                ClearMovementRange();
                Attack(attacker, defender, _map[(defender.Column, defender.Row)]);
                return;
            }

            // Znajdź pozycję startową ataku — hex sąsiedni do wroga w zasięgu ruchu
            var reachable = GetReachableHexes(attacker);
            var launchPad = GetNeighbors(defender.Column, defender.Row)
                .Where(nb => reachable.Contains(nb))
                .OrderByDescending(nb => _map.TryGetValue(nb, out var t) ? t.DefenseBonus : 0)
                .Cast<(int col, int row)?>()
                .FirstOrDefault();

            if (launchPad == null) return; // poza zasięgiem

            MoveUnit(attacker, launchPad.Value.col, launchPad.Value.row);

            PlayMoveSound(attacker);
            ClearMovementRange();
            DrawUnits();
            Attack(attacker, defender, _map[(defender.Column, defender.Row)]);
        }

        private static void PlayMoveSound(Unit unit)
        {
            if (unit.Type == UnitType.Infantry)
                SoundManager.PlayMarch();
            else
                SoundManager.PlayVehicle();
        }

        private static void MoveUnit(Unit unit, int newCol, int newRow)
        {
            if (newCol != unit.Column)
                unit.FacingLeft = newCol < unit.Column;
            unit.Column = newCol;
            unit.Row    = newRow;
        }

        /// <summary>
        /// Sprawdza czy pozycja znajduje się w strefie rozkładania dla gracza
        /// </summary>
        private bool IsValidDeployZone(int column, int playerId)
        {
            if (playerId == 1)
                return column <= 3; // Gracz 1 rozkłada na zachodzie
            else
                return column >= MapColumns - 4; // Gracz 2 rozkłada na wschodzie
        }

        /// <summary>
        /// Obsługa akcji jednostki - ruch lub atak
        /// </summary>
        private void HandleUnitAction(Unit unit, HexTile targetTile)
        {
            if (unit.HasMoved)
            {
                MessageBox.Show("This unit has already moved this turn!");
                return;
            }

            // Sprawdzenie czy pole jest zajęte
            var targetUnit = _units.FirstOrDefault(u => u.Column == targetTile.Column && u.Row == targetTile.Row);

            if (targetUnit != null)
            {
                // Atak na wrogą jednostkę
                if (targetUnit.OwnerId != unit.OwnerId)
                {
                    Attack(unit, targetUnit, targetTile);
                }
            }
            else
            {
                // Ruch do pustego pola
                int moveCost = targetTile.MoveCost;

                if (moveCost >= 99) // Zablokowane pole
                {
                    MessageBox.Show("Cannot move to this terrain!");
                    return;
                }

                MoveUnit(unit, targetTile.Column, targetTile.Row);
                unit.HasMoved = true;

                PlayMoveSound(unit);
                ClearMovementRange();
                DrawUnits();
            }
        }

        /// <summary>
        /// Atak jednostki na wrogą jednostkę
        /// </summary>
        private void Attack(Unit attacker, Unit defender, HexTile defenderTile)
        {
            SoundManager.PlayAttack();
            bool defenderDefeated = CombatResolver.Resolve(attacker, defender, defenderTile);

            string message = $"{attacker.Name} attacks {defender.Name}!\n" +
                           $"Defender HP: {defender.HP}/{defender.MaxHP}";

            if (defenderDefeated)
            {
                _units.Remove(defender);
                message += $"\n{defender.Name} has been defeated!";
            }

            attacker.HasMoved = true;
            ClearMovementRange();
            DrawUnits();
            MessageBox.Show(message);
        }

        // Axis = Wehrmacht field-gray, Allied = khaki
        private static readonly Dictionary<int, Color> PlayerBaseColors = new()
        {
            { 1, Color.FromRgb(62, 72, 54) },   // Wehrmacht Feldgrau
            { 2, Color.FromRgb(120, 100, 45) }  // Allied khaki
        };

        private void DrawUnitToken(Unit unit, double cx, double cy)
        {
            const double W = 30, H = 30;
            const double border = 2;
            double left = cx - W / 2;
            double top  = cy - H / 2;
            double alpha = unit.HasMoved ? 0.45 : 1.0;

            Color col = PlayerBaseColors.TryGetValue(unit.OwnerId, out var c) ? c : Color.FromRgb(100, 100, 100);

            // Cień
            Token(unit, new Rectangle { Width = W, Height = H,
                Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
                IsHitTestVisible = false, Opacity = alpha }, left + 2.5, top + 2.5, 9);

            // Ramka w kolorze gracza (klikalny obszar)
            var frame = new Rectangle { Width = W, Height = H,
                Fill = new SolidColorBrush(col),
                Stroke = new SolidColorBrush(Color.FromRgb(230, 225, 205)),
                StrokeThickness = 1.5,
                Tag = unit, IsHitTestVisible = true, Opacity = alpha };
            frame.MouseLeftButtonDown += Unit_Click;
            Token(unit, frame, left, top, 10);

            // Miniaturka jednostki (wypełnia wnętrze ramki)
            double imgW = W - border * 2, imgH = H - border * 2;
            var img = new Image
            {
                Width = imgW, Height = imgH,
                Source = UnitCatalog.GetCachedImage(unit.Type, unit.OwnerId),
                Stretch = Stretch.UniformToFill,
                Clip = new RectangleGeometry(new Rect(0, 0, imgW, imgH)),
                Tag = unit, IsHitTestVisible = false, Opacity = alpha,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = unit.FacingLeft
                    ? new ScaleTransform(-1, 1)
                    : Transform.Identity
            };
            Token(unit, img, left + border, top + border, 11);

            // Semi-przezroczysty pasek statystyk na dole
            const double statsH = 9;
            Token(unit, new Rectangle { Width = imgW, Height = statsH,
                Fill = new SolidColorBrush(Color.FromArgb(185, 0, 0, 0)),
                IsHitTestVisible = false, Opacity = alpha },
                left + border, top + H - border - statsH, 12);

            // Statystyki A/D
            Token(unit, new TextBlock
            {
                Text = $"{unit.Attack}/{unit.Defense}",
                FontSize = 6, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Width = imgW, TextAlignment = TextAlignment.Center,
                IsHitTestVisible = false, Opacity = alpha
            }, left + border, top + H - border - statsH + 1, 13);

            // Pasek HP
            const double hpH = 3;
            double hpRatio = (double)unit.HP / unit.MaxHP;
            Color hpColor = hpRatio > 0.6 ? Color.FromRgb(50, 200, 55)
                          : hpRatio > 0.3 ? Color.FromRgb(220, 175, 0)
                                          : Color.FromRgb(220, 40, 40);

            Token(unit, new Rectangle { Width = imgW, Height = hpH,
                Fill = new SolidColorBrush(Color.FromRgb(25, 15, 15)),
                IsHitTestVisible = false, Opacity = alpha },
                left + border, top + H - border - hpH, 13);

            Token(unit, new Rectangle { Width = imgW * hpRatio, Height = hpH,
                Fill = new SolidColorBrush(hpColor),
                IsHitTestVisible = false, Opacity = alpha },
                left + border, top + H - border - hpH, 14);
        }

        private void Token(Unit unit, FrameworkElement el, double left, double top, int zIndex)
        {
            if (el.Tag == null) el.Tag = unit;
            Canvas.SetLeft(el, left);
            Canvas.SetTop(el, top);
            Panel.SetZIndex(el, zIndex);
            GameCanvas.Children.Add(el);
        }

        private IEnumerable<(int col, int row)> GetNeighbors(int col, int row)
        {
            var offsets = row % 2 == 0 ? EvenRowNeighbors : OddRowNeighbors;
            foreach (var (dc, dr) in offsets)
            {
                int nc = col + dc, nr = row + dr;
                if (_map.ContainsKey((nc, nr)))
                    yield return (nc, nr);
            }
        }

        private HashSet<(int col, int row)> GetReachableHexes(Unit unit)
        {
            var reachable = new HashSet<(int, int)>();
            var best = new Dictionary<(int, int), int> { [(unit.Column, unit.Row)] = unit.CurrentMovePoints };
            var queue = new Queue<((int col, int row), int remaining)>();
            queue.Enqueue(((unit.Column, unit.Row), unit.CurrentMovePoints));

            while (queue.Count > 0)
            {
                var ((col, row), remaining) = queue.Dequeue();

                foreach (var nb in GetNeighbors(col, row))
                {
                    if (!_map.TryGetValue(nb, out var tile) || tile.BlocksMovement)
                        continue;
                    // Nie można wejść na żaden zajęty hex (ruch zatrzymuje się przed wrogiem)
                    if (_units.Any(u => u.Column == nb.Item1 && u.Row == nb.Item2))
                        continue;

                    int left = remaining - tile.MoveCost;
                    if (left < 0)
                        continue;

                    if (best.TryGetValue(nb, out int prev) && prev >= left)
                        continue;

                    best[nb] = left;
                    reachable.Add(nb);
                    queue.Enqueue((nb, left));
                }
            }

            return reachable;
        }

        // Wrogowie których można zaatakować z bieżącej pozycji LUB po ruchu
        private HashSet<(int col, int row)> GetAttackableEnemyPositions(Unit unit)
        {
            int enemyId = unit.OwnerId == 1 ? 2 : 1;
            var reachable = GetReachableHexes(unit);
            reachable.Add((unit.Column, unit.Row)); // bieżąca pozycja też liczy się jako "stanowisko ataku"

            var attackable = new HashSet<(int, int)>();
            foreach (var pos in reachable)
                foreach (var nb in GetNeighbors(pos.col, pos.row))
                    if (_units.Any(u => u.OwnerId == enemyId && u.Column == nb.Item1 && u.Row == nb.Item2))
                        attackable.Add(nb);
            return attackable;
        }

        private void ShowMovementRange(Unit unit)
        {
            ClearMovementRange();
            if (unit.HasMoved) return;

            const double offsetX = 30;
            const double offsetY = 30;
            double hexWidth = Math.Sqrt(3) * HexSize;
            double hexHeight = 2 * HexSize;
            double hSpacing = hexWidth;
            double vSpacing = hexHeight * 0.75;

            // Podświetlenie hexa jednostki
            double ux = offsetX + unit.Column * hSpacing + (unit.Row % 2 == 1 ? hexWidth / 2 : 0);
            double uy = offsetY + unit.Row * vSpacing;
            AddOverlay(new Point(ux, uy), Color.FromArgb(60, 255, 230, 0), Color.FromArgb(220, 255, 210, 0), 2.5);

            // Podświetlenie dostępnych hexów ruchu (zielone)
            foreach (var (col, row) in GetReachableHexes(unit))
            {
                double x = offsetX + col * hSpacing + (row % 2 == 1 ? hexWidth / 2 : 0);
                double y = offsetY + row * vSpacing;
                AddOverlay(new Point(x, y),
                    Color.FromArgb(65, 80, 210, 100),
                    Color.FromArgb(200, 60, 190, 80), 2);
            }

            // Podświetlenie wrogów do ataku (czerwone — nad zielonymi hexami)
            foreach (var (col, row) in GetAttackableEnemyPositions(unit))
            {
                double x = offsetX + col * hSpacing + (row % 2 == 1 ? hexWidth / 2 : 0);
                double y = offsetY + row * vSpacing;
                AddOverlay(new Point(x, y),
                    Color.FromArgb(80, 230, 50, 50),
                    Color.FromArgb(230, 210, 30, 30), 2.5);
            }
        }

        private void AddOverlay(Point center, Color fill, Color stroke, double strokeThickness)
        {
            var overlay = new Polygon
            {
                Points = HexHelper.GetHexPoints(center, HexSize),
                Fill = new SolidColorBrush(fill),
                Stroke = new SolidColorBrush(stroke),
                StrokeThickness = strokeThickness,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(overlay, 5);
            GameCanvas.Children.Add(overlay);
            _movementOverlays.Add(overlay);
        }

        private void ClearMovementRange()
        {
            foreach (var el in _movementOverlays)
                GameCanvas.Children.Remove(el);
            _movementOverlays.Clear();
        }

        private void DrawTerrainDecorations(TerrainType terrain, Point center, double hexSize)
        {
            double s = hexSize / 30.0;
            switch (terrain)
            {
                case TerrainType.Forest:
                    AddTree(center.X - 9 * s, center.Y - 5 * s, s);
                    AddTree(center.X + 9 * s, center.Y - 5 * s, s);
                    AddTree(center.X,          center.Y + 7 * s, s);
                    break;
                case TerrainType.City:
                    AddBuilding(center.X - 11 * s, center.Y + 10 * s, 7 * s, 10 * s, Color.FromRgb(185, 180, 195));
                    AddBuilding(center.X -  3 * s, center.Y + 10 * s, 7 * s, 16 * s, Color.FromRgb(160, 155, 175));
                    AddBuilding(center.X +  5 * s, center.Y + 10 * s, 7 * s, 12 * s, Color.FromRgb(172, 167, 182));
                    break;
                case TerrainType.Hills:
                    AddHill(center.X - 7 * s, center.Y + 5 * s, 13 * s, 7 * s, Color.FromRgb(125, 108, 58));
                    AddHill(center.X + 7 * s, center.Y + 5 * s, 13 * s, 7 * s, Color.FromRgb(115, 98, 48));
                    break;
                case TerrainType.Mountains:
                    AddMountain(center.X, center.Y, s);
                    break;
            }
        }

        private void AddTree(double cx, double cy, double s)
        {
            double c = 8 * s;
            var crown = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(cx, cy - c),
                    new Point(cx - c * 0.75, cy + c * 0.35),
                    new Point(cx + c * 0.75, cy + c * 0.35)
                },
                Fill = new SolidColorBrush(Color.FromRgb(22, 100, 22)),
                Stroke = new SolidColorBrush(Color.FromRgb(10, 60, 10)),
                StrokeThickness = 0.5,
                IsHitTestVisible = false
            };
            var trunk = new Rectangle
            {
                Width = 2.5 * s,
                Height = 3.5 * s,
                Fill = new SolidColorBrush(Color.FromRgb(95, 55, 18)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(trunk, cx - 1.25 * s);
            Canvas.SetTop(trunk, cy + c * 0.35);
            GameCanvas.Children.Add(crown);
            GameCanvas.Children.Add(trunk);
        }

        private void AddBuilding(double left, double bottom, double width, double height, Color color)
        {
            var building = new Rectangle
            {
                Width = width,
                Height = height,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Color.FromRgb(55, 50, 65)),
                StrokeThickness = 0.7,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(building, left);
            Canvas.SetTop(building, bottom - height);
            GameCanvas.Children.Add(building);
        }

        private void AddHill(double cx, double cy, double rx, double ry, Color color)
        {
            var ellipse = new Ellipse
            {
                Width = rx * 2,
                Height = ry * 2,
                Fill = new SolidColorBrush(color),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(ellipse, cx - rx);
            Canvas.SetTop(ellipse, cy - ry);
            GameCanvas.Children.Add(ellipse);
        }

        private void AddMountain(double cx, double cy, double s)
        {
            var mountain = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(cx,           cy - 20 * s),
                    new Point(cx - 14 * s,  cy +  8 * s),
                    new Point(cx + 14 * s,  cy +  8 * s)
                },
                Fill = new SolidColorBrush(Color.FromRgb(95, 80, 65)),
                Stroke = new SolidColorBrush(Color.FromRgb(55, 45, 35)),
                StrokeThickness = 0.5,
                IsHitTestVisible = false
            };
            var snow = new Polygon
            {
                Points = new PointCollection
                {
                    new Point(cx,          cy - 20 * s),
                    new Point(cx - 5 * s,  cy - 11 * s),
                    new Point(cx + 5 * s,  cy - 11 * s)
                },
                Fill = new SolidColorBrush(Color.FromRgb(225, 228, 238)),
                IsHitTestVisible = false
            };
            GameCanvas.Children.Add(mountain);
            GameCanvas.Children.Add(snow);
        }

        /// <summary>
        /// Oblicza dystans między dwoma hexami (uproszczenie)
        /// </summary>
        private int CalculateHexDistance(int col1, int row1, int col2, int row2)
        {
            int dCol = Math.Abs(col2 - col1);
            int dRow = Math.Abs(row2 - row1);
            return Math.Max(dCol, dRow);
        }

        /// <summary>
        /// Przycisk: następna tura
        /// </summary>
        private void NextTurnButton_Click(object sender, RoutedEventArgs e)
        {
            if (_aiThinking) return;

            ClearMovementRange();
            _selectedUnit = null;
            _turnManager.NextTurn();
            StartTurn();
            DrawUnits();
            UpdateUI();
        }

        private void ToggleDeployButton_Click(object sender, RoutedEventArgs e) { }

        private void EndTurnButton_Click(object sender, RoutedEventArgs e)
            => NextTurnButton_Click(sender, e);

        private void EmulateButton_Click(object sender, RoutedEventArgs e)
        {
            _emulateMode = !_emulateMode;
            EmulateButton.Content    = _emulateMode ? "Stop Emulate" : "Emulate";
            EmulateButton.Background = _emulateMode
                ? new SolidColorBrush(Color.FromRgb(30, 120, 30))
                : new SolidColorBrush(Color.FromRgb(68, 68, 68));

            // Jeśli włączono w trakcie tury gracza 1 — uruchom AI od razu
            if (_emulateMode && !_aiThinking && _turnManager.CurrentPlayerId == 1)
            {
                SetUiEnabled(false);
                _ = ExecuteAiTurnAsync(1);
            }
        }
    }
}