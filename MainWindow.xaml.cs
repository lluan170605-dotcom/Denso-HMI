
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;

namespace AgvHmiApp
{
    public class RackViewModel : INotifyPropertyChanged
    {
        private int _occupied;
        private string _name = "";
        private string _statusText = "";
        private Brush _statusColor = Brushes.Green;
        private string _restStatus = "⚡ Sẵn sàng";
        private Brush _restStatusColor = Brushes.ForestGreen;
        private string _lastServicedText = "Vừa khởi động";

        public string Id { get; set; } = "";
        public string PartName { get; set; } = "";
        public Point StopPointOnTrack { get; set; }

        public DateTime LastServicedTime { get; set; } = DateTime.Now;
        public double TargetRestSeconds { get; set; } = 15.0;
        public double RestSecondsRemaining { get; set; } = 0.0;

        public int Occupied
        {
            get => _occupied;
            set { _occupied = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public Brush StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        public string RestStatus
        {
            get => _restStatus;
            set { _restStatus = value; OnPropertyChanged(); }
        }

        public Brush RestStatusColor
        {
            get => _restStatusColor;
            set { _restStatusColor = value; OnPropertyChanged(); }
        }

        public string LastServicedText
        {
            get => _lastServicedText;
            set { _lastServicedText = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class AiMatrixRow : INotifyPropertyChanged
    {
        private string _slotDisplay = "";
        private string _occupancyRate = "";
        private string _lstmProb = "";
        private string _xgbProb = "";
        private string _maxProb = "";
        private string _winner = "";
        private string _statusText = "";

        public string Id { get; set; } = "";
        public string PartName { get; set; } = "";

        public string SlotDisplay
        {
            get => _slotDisplay;
            set { _slotDisplay = value; OnPropertyChanged(); }
        }

        public string OccupancyRate
        {
            get => _occupancyRate;
            set { _occupancyRate = value; OnPropertyChanged(); }
        }

        public string LstmProb
        {
            get => _lstmProb;
            set { _lstmProb = value; OnPropertyChanged(); }
        }

        public string XgbProb
        {
            get => _xgbProb;
            set { _xgbProb = value; OnPropertyChanged(); }
        }

        public string MaxProb
        {
            get => _maxProb;
            set { _maxProb = value; OnPropertyChanged(); }
        }

        public string Winner
        {
            get => _winner;
            set { _winner = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class MiniSlotModel : INotifyPropertyChanged
    {
        private Brush _color = Brushes.White;
        public string TagId { get; set; } = "";

        public Brush Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class WmsRackGroup : INotifyPropertyChanged
    {
        private string _countText = "";
        public string RackId { get; set; } = "";
        public string HeaderText { get; set; } = "";

        public string CountText
        {
            get => _countText;
            set { _countText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MiniSlotModel> MiniSlots { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class CsvRecordModel
    {
        public string Timestamp { get; set; } = "";
        public DateTime ParsedTime { get; set; }
        public string RackId { get; set; } = "";
        public string PartName { get; set; } = "";
        public int Occupied { get; set; }
        public int TotalSlots { get; set; } = 20;
        public string OccupancyRate { get; set; } = "";
        public string LstmProb { get; set; } = "";
        public string XgbProb { get; set; } = "";
        public string MaxProb { get; set; } = "";
        public string Winner { get; set; } = "";
        public string RiskLabel { get; set; } = "";
    }

    public class OrderQueueItem
    {
        public string RackId { get; set; } = "";
        public int TargetQuantity { get; set; }
    }

    public class AgvHmiEntity
    {
        public string Id { get; set; } = "";
        public string Zone { get; set; } = "";
        public List<string> ManagedRackIds { get; set; } = new();
        public Point[] Route { get; set; } = Array.Empty<Point>();
        public double[] SegLengths { get; set; } = Array.Empty<double>();
        public double TotalLength { get; set; }
        public double Distance { get; set; }
        public double CurrentSpeed { get; set; } = 48.0;
        public double X { get; set; }
        public double Y { get; set; }
        public int Battery { get; set; } = 95;
        public Brush Color { get; set; } = Brushes.Blue;
        public string Heading { get; set; } = "RIGHT";

        public string MissionStatus { get; set; } = "Patrolling";
        public string TargetAction { get; set; } = "";
        public string AssignedRackId { get; set; } = "";
        public string CarryingPartName { get; set; } = "";
        public double OperationTimer { get; set; } = 0.0;

        public List<Point> ActiveWaypoints { get; set; } = new();
    }

    public partial class MainWindow : Window
    {
        private const string IOT_SECRET_KEY = "DENSO_FPT_SCADA_SECRET_KEY";

        private readonly int[] TRACK_COLS = { 90, 290, 490, 690 };
        private readonly int[] TRACK_ROWS = { 60, 160, 260, 360, 460 };
        private const double BASE_SPEED = 48.0;

        private readonly Point DEPOT_LOCATION = new(90, 260);

        private readonly Dictionary<string, AgvHmiEntity> _agvs = new();
        private readonly ObservableCollection<RackViewModel> _rackCards = new();
        private readonly ObservableCollection<AiMatrixRow> _aiMatrixRows = new();
        private readonly ObservableCollection<WmsRackGroup> _wmsGroups = new();

        private readonly ObservableCollection<CsvRecordModel> _displayRecords = new();
        private readonly List<CsvRecordModel> _recordsCache = new();
        private readonly object _csvLock = new();

        private readonly DispatcherTimer _renderTimer = new();
        private readonly DispatcherTimer _aiDispatcherTimer = new();
        private readonly DispatcherTimer _rackRestTimer = new();
        private DateTime _lastTick = DateTime.Now;

        private string _selectedAgvId = "AGV-01";
        private bool _isManualMode = false;
        private bool _isEStopped = false;
        private IMqttClient? _mqttClient;
        private MqttServer? _embeddedMqttServer;
        private string? _currentModalRackId;
        private string _csvFilePath = "";
        private bool _isFilterInitialized = false;

        private readonly List<OrderQueueItem> _orderQueue = new();
        private bool _isExecutingQueue = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ItemsRackCards.ItemsSource = _rackCards;
            GridAiMatrix.ItemsSource = _aiMatrixRows;
            ItemsWmsGrid.ItemsSource = _wmsGroups;
            GridCsvData.ItemsSource = _displayRecords;

            InitFleetRoutes();
            InitWarehouseData();
            InitFilterControls();
            InitDatasetFile();
            BindUiEvents();

            AddLog("HMI Controller sẵn sàng. Tải dữ liệu toàn bộ từ tháng 09/2026 thành công.");

            DrawHmiWorld();

            Task.Run(InitDirectMqttsAsync);

            _renderTimer.Interval = TimeSpan.FromMilliseconds(16);
            _renderTimer.Tick += RenderLoop;
            _renderTimer.Start();

            _aiDispatcherTimer.Interval = TimeSpan.FromSeconds(1.2);
            _aiDispatcherTimer.Tick += AiMissionDispatchLoop;
            _aiDispatcherTimer.Start();

            _rackRestTimer.Interval = TimeSpan.FromSeconds(1.0);
            _rackRestTimer.Tick += RackRestTimerTick;
            _rackRestTimer.Start();
        }

        private void BindUiEvents()
        {
            this.KeyDown += Window_KeyDown;

            TabBtnScada.Click += SwitchTab_Click;
            TabBtnAi.Click += SwitchTab_Click;
            TabBtnWms.Click += SwitchTab_Click;
            TabBtnSecurity.Click += SwitchTab_Click;

            BtnAddOrder.Click += BtnAddOrder_Click;
            BtnExecuteOrders.Click += BtnExecuteOrders_Click;
            BtnClearOrders.Click += BtnClearOrders_Click;

            BtnAgv1.Click += BtnSelectAgv_Click;
            BtnAgv2.Click += BtnSelectAgv_Click;
            BtnAgv3.Click += BtnSelectAgv_Click;
            BtnAgv4.Click += BtnSelectAgv_Click;
            BtnAgv5.Click += BtnSelectAgv_Click;

            BtnMoveUp.Click += BtnManualMove_Click;
            BtnMoveLeft.Click += BtnManualMove_Click;
            BtnMoveStop.Click += BtnManualMove_Click;
            BtnMoveRight.Click += BtnManualMove_Click;
            BtnMoveDown.Click += BtnManualMove_Click;
            BtnManualCargo.Click += BtnManualCargo_Click;

            BtnToggleMode.Click += BtnToggleMode_Click;
            BtnEStop.Click += BtnEStop_Click;

            CboFilterRack.SelectionChanged += FilterSelectionChanged;
            CboFilterHour.SelectionChanged += FilterSelectionChanged;
            DpFromDate.SelectedDateChanged += DatePicker_SelectedDateChanged;
            DpToDate.SelectedDateChanged += DatePicker_SelectedDateChanged;
            BtnSetToday.Click += BtnSetToday_Click;
            BtnReloadCsv.Click += BtnReloadCsv_Click;
            BtnExportCsv.Click += BtnExportCsv_Click;

            BtnCloseModalTop.Click += CloseModal_Click;
            BtnCloseModalBottom.Click += CloseModal_Click;
        }

        private void AddLog(string msg)
        {
            LstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            if (LstLogs.Items.Count > 25) LstLogs.Items.RemoveAt(LstLogs.Items.Count - 1);
        }

        private void RackRestTimerTick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            foreach (var rack in _rackCards)
            {
                bool isBeingServiced = _agvs.Values.Any(a => a.AssignedRackId == rack.Id && (a.MissionStatus == "Dropping_At_Rack" || a.MissionStatus == "Picking_At_Rack"));
                if (isBeingServiced)
                {
                    rack.RestStatus = "⏳ Đang bốc dỡ...";
                    rack.RestStatusColor = Brushes.MediumVioletRed;
                    continue;
                }

                if (rack.RestSecondsRemaining > 0)
                {
                    rack.RestSecondsRemaining -= 1.0;
                    rack.RestStatus = $"⏳ Nghỉ: {Math.Ceiling(rack.RestSecondsRemaining)}s";
                    rack.RestStatusColor = Brushes.DarkOrange;
                }
                else
                {
                    rack.RestStatus = "⚡ Sẵn sàng";
                    rack.RestStatusColor = Brushes.ForestGreen;
                }

                var elapsed = now - rack.LastServicedTime;
                if (elapsed.TotalSeconds < 60)
                    rack.LastServicedText = $"{Math.Round(elapsed.TotalSeconds)}s trước";
                else
                    rack.LastServicedText = $"{Math.Round(elapsed.TotalMinutes)}p trước";
            }
        }

        private void InitFilterControls()
        {
            CboFilterRack.Items.Clear();
            CboOrderRack.Items.Clear();
            for (int i = 1; i <= 12; i++)
            {
                string rId = $"RACK-{i:00}";
                CboFilterRack.Items.Add(rId);
                CboOrderRack.Items.Add(rId);
            }
            CboFilterRack.SelectedIndex = 0;
            CboOrderRack.SelectedIndex = 0;

            // Mặc định lọc từ đầu tháng 9/2026 đến nay
            DpFromDate.SelectedDate = new DateTime(2026, 9, 1);
            DpToDate.SelectedDate = DateTime.Today;

            _isFilterInitialized = true;
        }

        public void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            string rackId = CboOrderRack.SelectedItem?.ToString() ?? "RACK-01";
            if (!int.TryParse(TxtOrderQuantity.Text.Trim(), out int qty) || qty < 0 || qty > 20)
            {
                MessageBox.Show("Vui lòng nhập số lượng khay hợp lệ (0-20)!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var card = _rackCards.FirstOrDefault(r => r.Id == rackId);
            if (card == null) return;

            var existing = _orderQueue.FirstOrDefault(o => o.RackId == rackId);
            if (existing != null)
            {
                existing.TargetQuantity = qty;
            }
            else
            {
                _orderQueue.Add(new OrderQueueItem { RackId = rackId, TargetQuantity = qty });
            }

            UpdateOrderQueueUi();
            AddLog($"➕ ĐÃ THÊM LỆNH: {rackId} cần {qty}/20 khay (Hàng đợi: {_orderQueue.Count} lệnh). Bấm 'Phát Lệnh' để xe thi hành.");
        }

        public void BtnExecuteOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_orderQueue.Count == 0)
            {
                MessageBox.Show("Hàng đợi đang trống! Hãy thêm ít nhất 1 lệnh trước khi phát lệnh.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isExecutingQueue = true;
            foreach (var item in _orderQueue)
            {
                var card = _rackCards.FirstOrDefault(r => r.Id == item.RackId);
                if (card != null) card.RestSecondsRemaining = 0;
            }

            UpdateOrderQueueUi();
            AddLog($"🚀 ĐÃ PHÁT LỆNH: Bắt đầu thực hiện {_orderQueue.Count} lệnh theo thứ tự ưu tiên FIFO...");

            AiMissionDispatchLoop(null, EventArgs.Empty);
        }

        public void BtnClearOrders_Click(object sender, RoutedEventArgs e)
        {
            _orderQueue.Clear();
            _isExecutingQueue = false;
            UpdateOrderQueueUi();
            AddLog("Đã xóa hết hàng đợi. Hệ thống quay về chế độ tự động thường.");
        }

        private void UpdateOrderQueueUi()
        {
            if (_orderQueue.Count == 0)
            {
                TxtActiveOrderCount.Text = "0 mục tiêu đang chờ";
                TxtQueueSummary.Text = "Chế độ thường: Tự động ưu tiên nạp đầy khay (20/20) cho các kệ trống";
                TxtQueueSummary.Foreground = new SolidColorBrush(Color.FromRgb(2, 132, 199));
            }
            else
            {
                string statusText = _isExecutingQueue ? "Đang thi hành: " : "Đang chờ phát lệnh: ";
                TxtActiveOrderCount.Text = $"{_orderQueue.Count} lệnh trong hàng đợi";
                var summaryList = _orderQueue.Select(o => $"{o.RackId}➔{o.TargetQuantity}").ToList();
                TxtQueueSummary.Text = statusText + string.Join(" ➔ ", summaryList);
                TxtQueueSummary.Foreground = _isExecutingQueue ? new SolidColorBrush(Color.FromRgb(22, 163, 74)) : new SolidColorBrush(Color.FromRgb(217, 119, 6));
            }
        }

        private async Task InitDirectMqttsAsync()
        {
            try
            {
                var mqttServerOptions = new MqttServerOptionsBuilder()
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointPort(8883)
                    .Build();

                var serverFactory = new MqttFactory();
                _embeddedMqttServer = serverFactory.CreateMqttServer(mqttServerOptions);
                await _embeddedMqttServer.StartAsync();

                Dispatcher.Invoke(() => AddLog("Đã khởi chạy Embedded MQTT Broker nội bộ (Port 8883)."));
            }
            catch { }

            try
            {
                var factory = new MqttFactory();
                _mqttClient = factory.CreateMqttClient();

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost", 8883)
                    .WithTlsOptions(o => { o.UseTls(); o.WithCertificateValidationHandler(_ => true); })
                    .WithCleanSession()
                    .Build();

                _mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    string topic = e.ApplicationMessage.Topic;
                    string payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                    Dispatcher.Invoke(() =>
                    {
                        if (topic.StartsWith("agv/") && topic.EndsWith("/telemetry"))
                        {
                            ProcessSecureAgvTelemetry(payload);
                        }
                    });

                    return Task.CompletedTask;
                };

                await _mqttClient.ConnectAsync(options);
                await _mqttClient.SubscribeAsync("agv/+/telemetry");

                Dispatcher.Invoke(() =>
                {
                    TxtConnStatus.Text = "MQTTS Bảo Mật (Port 8883 Online)";
                    AddLog("HMI đã kết nối MQTTS Port 8883 thành công.");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    TxtConnStatus.Text = "HMI Độc Lập (Mô Phỏng Trực Tiếp)";
                    AddLog($"Chế độ mô phỏng HMI: {ex.Message}");
                });
            }
        }

        private void ProcessSecureAgvTelemetry(string rawJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                string agvId = root.GetProperty("id").GetString() ?? "";
                double x = root.GetProperty("x").GetDouble();
                double y = root.GetProperty("y").GetDouble();
                long timestamp = root.GetProperty("timestamp").GetInt64();

                long currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (Math.Abs(currentUnix - timestamp) > 6) return;

                if (x < 80 || x > 700 || y < 50 || y > 470) return;

                if (_agvs.ContainsKey(agvId) && !_isManualMode)
                {
                    _agvs[agvId].X = x;
                    _agvs[agvId].Y = y;
                }
            }
            catch { }
        }

        private async Task SendMqttCommandAsync(string agvId, string action)
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    string dataToSign = $"{agvId}:{action}:{ts}";

                    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(IOT_SECRET_KEY));
                    string signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign)));

                    var payloadObj = new { target = agvId, action = action, timestamp = ts, signature = signature };

                    var msg = new MqttApplicationMessageBuilder()
                        .WithTopic($"agv/{agvId}/control")
                        .WithPayload(JsonSerializer.Serialize(payloadObj))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await _mqttClient.PublishAsync(msg);
                }
                catch { }
            }
        }

        private bool IsOnVerticalTrack(double x) => TRACK_COLS.Any(col => Math.Abs(x - col) <= 3.0);
        private bool IsOnHorizontalTrack(double y) => TRACK_ROWS.Any(row => Math.Abs(y - row) <= 3.0);

        private Point FindClosestGridIntersection(Point p)
        {
            Point best = new Point(TRACK_COLS[0], TRACK_ROWS[0]);
            double minD = double.MaxValue;

            foreach (var x in TRACK_COLS)
            {
                foreach (var y in TRACK_ROWS)
                {
                    double d = Math.Pow(p.X - x, 2) + Math.Pow(p.Y - y, 2);
                    if (d < minD)
                    {
                        minD = d;
                        best = new Point(x, y);
                    }
                }
            }
            return best;
        }

        private List<Point> FindGridPath(Point start, Point goal)
        {
            Point startNode = FindClosestGridIntersection(start);
            Point goalNode = FindClosestGridIntersection(goal);

            if (startNode == goalNode) return new List<Point> { startNode };

            var queue = new Queue<Point>();
            var parent = new Dictionary<Point, Point>();
            var visited = new HashSet<Point>();

            queue.Enqueue(startNode);
            visited.Add(startNode);

            bool found = false;
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (Math.Abs(cur.X - goalNode.X) < 1 && Math.Abs(cur.Y - goalNode.Y) < 1)
                {
                    goalNode = cur;
                    found = true;
                    break;
                }

                int colIdx = Array.IndexOf(TRACK_COLS, (int)cur.X);
                int rowIdx = Array.IndexOf(TRACK_ROWS, (int)cur.Y);

                var neighbors = new List<Point>();
                if (colIdx > 0) neighbors.Add(new Point(TRACK_COLS[colIdx - 1], cur.Y));
                if (colIdx < TRACK_COLS.Length - 1) neighbors.Add(new Point(TRACK_COLS[colIdx + 1], cur.Y));
                if (rowIdx > 0) neighbors.Add(new Point(cur.X, TRACK_ROWS[rowIdx - 1]));
                if (rowIdx < TRACK_ROWS.Length - 1) neighbors.Add(new Point(cur.X, TRACK_ROWS[rowIdx + 1]));

                foreach (var next in neighbors)
                {
                    if (!visited.Contains(next))
                    {
                        visited.Add(next);
                        parent[next] = cur;
                        queue.Enqueue(next);
                    }
                }
            }

            var path = new List<Point>();
            if (found)
            {
                var curr = goalNode;
                while (curr != startNode)
                {
                    path.Add(curr);
                    curr = parent[curr];
                }
                path.Add(startNode);
                path.Reverse();
            }
            else
            {
                path.Add(startNode);
                path.Add(goalNode);
            }

            return path;
        }

        private List<Point> BuildForwardPathToDepot(AgvHmiEntity car)
        {
            var path = new List<Point>();

            if (car.Zone == "Zone_Top")
            {
                if (car.Y <= 100)
                {
                    path.Add(new Point(690, 60));
                    path.Add(new Point(690, 160));
                    path.Add(new Point(90, 160));
                    path.Add(DEPOT_LOCATION);
                }
                else
                {
                    path.Add(new Point(90, 160));
                    path.Add(DEPOT_LOCATION);
                }
            }
            else if (car.Zone == "Zone_Bottom")
            {
                if (car.Y <= 400)
                {
                    path.Add(new Point(690, 360));
                    path.Add(new Point(690, 460));
                    path.Add(new Point(90, 460));
                    path.Add(new Point(90, 360));
                    path.Add(DEPOT_LOCATION);
                }
                else
                {
                    path.Add(new Point(90, 460));
                    path.Add(new Point(90, 360));
                    path.Add(DEPOT_LOCATION);
                }
            }
            return path;
        }

        private List<Point> BuildForwardPathFromDepotToRack(AgvHmiEntity car, Point rackStopPoint)
        {
            var path = new List<Point> { DEPOT_LOCATION };

            if (car.Zone == "Zone_Top")
            {
                path.Add(new Point(90, 160));
                path.Add(new Point(90, 60));
                if (rackStopPoint.Y == 60)
                {
                    path.Add(rackStopPoint);
                }
                else
                {
                    path.Add(new Point(690, 60));
                    path.Add(new Point(690, 160));
                    path.Add(rackStopPoint);
                }
            }
            else if (car.Zone == "Zone_Bottom")
            {
                path.Add(new Point(90, 360));
                if (rackStopPoint.Y == 360)
                {
                    path.Add(rackStopPoint);
                }
                else
                {
                    path.Add(new Point(690, 360));
                    path.Add(new Point(690, 460));
                    path.Add(rackStopPoint);
                }
            }
            return path;
        }

        private List<Point> BuildPathBackToPatrol(AgvHmiEntity car)
        {
            var path = new List<Point>();
            if (car.Zone == "Zone_Top")
            {
                if (Math.Abs(car.Y - 60) <= 5)
                {
                    path.Add(new Point(690, 60));
                    path.Add(new Point(690, 160));
                    path.Add(new Point(90, 160));
                }
                else
                {
                    path.Add(new Point(90, 160));
                    path.Add(new Point(90, 60));
                }
            }
            else if (car.Zone == "Zone_Bottom")
            {
                if (Math.Abs(car.Y - 360) <= 5)
                {
                    path.Add(new Point(690, 360));
                    path.Add(new Point(690, 460));
                    path.Add(new Point(90, 460));
                }
                else
                {
                    path.Add(new Point(90, 460));
                    path.Add(new Point(90, 360));
                }
            }
            return path;
        }

        private double CalculateForwardDistanceToDepot(AgvHmiEntity car)
        {
            var testPath = BuildForwardPathToDepot(car);
            double total = 0;
            Point current = new Point(car.X, car.Y);
            for (int i = 0; i < testPath.Count; i++)
            {
                total += Math.Sqrt(Math.Pow(testPath[i].X - current.X, 2) + Math.Pow(testPath[i].Y - current.Y, 2));
                current = testPath[i];
            }
            return total;
        }

        private Point FindClosestNodeOnZoneRoute(AgvHmiEntity car, Point currentPoint)
        {
            Point bestNode = car.Route[0];
            double minD = double.MaxValue;

            foreach (var routeNode in car.Route)
            {
                double d = Math.Pow(currentPoint.X - routeNode.X, 2) + Math.Pow(currentPoint.Y - routeNode.Y, 2);
                if (d < minD)
                {
                    minD = d;
                    bestNode = routeNode;
                }
            }
            return bestNode;
        }

        private void ExecuteManualMove(string action)
        {
            if (!_isManualMode || _isEStopped) return;
            if (!_agvs.ContainsKey(_selectedAgvId)) return;

            var car = _agvs[_selectedAgvId];
            double targetX = car.X;
            double targetY = car.Y;
            string targetHeading = car.Heading;

            if (action == "LEFT" || action == "RIGHT")
            {
                if (!IsOnHorizontalTrack(car.Y))
                {
                    AddLog($"⚠️ Xe {_selectedAgvId} đang ở ray dọc, chỉ được rẽ ngang tại các nút giao ray!");
                    return;
                }
                targetX = action == "LEFT" ? Math.Max(TRACK_COLS[0], car.X - 20) : Math.Min(TRACK_COLS[^1], car.X + 20);
                targetHeading = action;
            }
            else if (action == "UP" || action == "DOWN")
            {
                if (!IsOnVerticalTrack(car.X))
                {
                    AddLog($"⚠️ Xe {_selectedAgvId} đang ở ray ngang, chỉ được rẽ dọc tại các nút giao ray!");
                    return;
                }
                targetY = action == "UP" ? Math.Max(TRACK_ROWS[0], car.Y - 20) : Math.Min(TRACK_ROWS[^1], car.Y + 20);
                targetHeading = action;
            }
            else if (action == "STOP")
            {
                AddLog($"DỪNG THỦ CÔNG XE {car.Id}!");
                _ = SendMqttCommandAsync(_selectedAgvId, "STOP");
                return;
            }

            car.Heading = targetHeading;

            bool blocked = false;
            string blockerId = "";
            foreach (var kvp in _agvs)
            {
                if (kvp.Key == car.Id) continue;
                var other = kvp.Value;

                double distToOther = Math.Sqrt(Math.Pow(targetX - other.X, 2) + Math.Pow(targetY - other.Y, 2));
                if (distToOther < 32.0)
                {
                    blocked = true;
                    blockerId = other.Id;
                    break;
                }
            }

            if (blocked)
            {
                AddLog($"⚠️ [CHỐNG VA CHẠM] Phía trước mặt bị chắn sát bởi {blockerId} -> Phanh dừng!");
                _ = SendMqttCommandAsync(_selectedAgvId, "STOP");
                return;
            }

            car.X = targetX;
            car.Y = targetY;
            _ = SendMqttCommandAsync(_selectedAgvId, action);
        }

        private void ExecuteManualCargoOperation()
        {
            if (!_isManualMode || _isEStopped) return;
            if (!_agvs.ContainsKey(_selectedAgvId)) return;

            var car = _agvs[_selectedAgvId];
            Point currentPos = new Point(car.X, car.Y);

            double distToDepot = Math.Sqrt(Math.Pow(currentPos.X - DEPOT_LOCATION.X, 2) + Math.Pow(currentPos.Y - DEPOT_LOCATION.Y, 2));
            if (distToDepot <= 36.0)
            {
                if (string.IsNullOrEmpty(car.CarryingPartName))
                {
                    car.CarryingPartName = "Linh kiện Depot";
                    AddLog($"📦 [THỦ CÔNG] {car.Id} đã bốc hàng từ Trạm Cấp Hàng lên xe!");
                }
                else
                {
                    car.CarryingPartName = "";
                    AddLog($"📦 [THỦ CÔNG] {car.Id} đã dỡ bỏ hàng trả về Trạm Cấp Hàng (Xe rỗng).");
                }
                return;
            }

            RackViewModel? nearestRack = null;
            double minRackDist = double.MaxValue;
            foreach (var rack in _rackCards)
            {
                double d = Math.Sqrt(Math.Pow(currentPos.X - rack.StopPointOnTrack.X, 2) + Math.Pow(currentPos.Y - rack.StopPointOnTrack.Y, 2));
                if (d < minRackDist)
                {
                    minRackDist = d;
                    nearestRack = rack;
                }
            }

            if (nearestRack != null && minRackDist <= 36.0)
            {
                var wmsGroup = _wmsGroups.FirstOrDefault(g => g.RackId == nearestRack.Id);

                if (!string.IsNullOrEmpty(car.CarryingPartName))
                {
                    if (nearestRack.Occupied < 20 && wmsGroup != null)
                    {
                        var emptySlot = wmsGroup.MiniSlots.FirstOrDefault(s => !s.Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase));
                        if (emptySlot != null) emptySlot.Color = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                        int newOcc = nearestRack.Occupied + 1;
                        wmsGroup.CountText = $"{newOcc}/20 Khay";
                        RecalculateAiPrediction(nearestRack.Id, newOcc);
                        AddLog($"✅ [THỦ CÔNG] {car.Id} đã dỡ bỏ khay hàng vào {nearestRack.Id} (Tồn mới: {newOcc}/20).");
                        car.CarryingPartName = "";
                    }
                    else
                    {
                        AddLog($"⚠️ [THỦ CÔNG] {nearestRack.Id} đã đầy, không thể dỡ thêm!");
                    }
                }
                else
                {
                    if (nearestRack.Occupied > 0 && wmsGroup != null)
                    {
                        var occSlot = wmsGroup.MiniSlots.LastOrDefault(s => s.Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase));
                        if (occSlot != null) occSlot.Color = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                        int newOcc = nearestRack.Occupied - 1;
                        wmsGroup.CountText = $"{newOcc}/20 Khay";
                        RecalculateAiPrediction(nearestRack.Id, newOcc);
                        car.CarryingPartName = nearestRack.PartName;
                        AddLog($"📦 [THỦ CÔNG] {car.Id} đã bốc khay [{nearestRack.PartName}] từ {nearestRack.Id} lên xe.");
                    }
                    else
                    {
                        AddLog($"⚠️ [THỦ CÔNG] {nearestRack.Id} đang hết khay để bốc!");
                    }
                }
                return;
            }

            AddLog($"⚠️ [THỦ CÔNG] {car.Id} đang không đỗ trước Trạm Cấp Hàng hoặc Kệ hàng để bốc/dỡ!");
        }

        public void BtnManualCargo_Click(object sender, RoutedEventArgs e)
        {
            ExecuteManualCargoOperation();
            this.Focus();
        }

        public void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_isManualMode || _isEStopped) return;

            string action = "";
            switch (e.Key)
            {
                case Key.Up: case Key.W: action = "UP"; break;
                case Key.Down: case Key.S: action = "DOWN"; break;
                case Key.Left: case Key.A: action = "LEFT"; break;
                case Key.Right: case Key.D: action = "RIGHT"; break;
                case Key.Space: action = "STOP"; break;
                case Key.E: ExecuteManualCargoOperation(); e.Handled = true; return;
            }

            if (!string.IsNullOrEmpty(action))
            {
                e.Handled = true;
                ExecuteManualMove(action);
            }
        }

        public void BtnManualMove_Click(object sender, RoutedEventArgs e)
        {
            if (!_isManualMode || _isEStopped) return;
            string act = (sender as Button)?.Tag?.ToString() ?? "";
            if (!string.IsNullOrEmpty(act))
            {
                ExecuteManualMove(act);
            }
            this.Focus();
        }

        public async void BtnToggleMode_Click(object sender, RoutedEventArgs e)
        {
            if (_isEStopped) { RecoverFromEStop(); return; }

            _isManualMode = !_isManualMode;
            if (_isManualMode)
            {
                TxtCurrentMode.Text = "CHẾ ĐỘ: THỦ CÔNG";
                TxtCurrentMode.Foreground = Brushes.DarkOrange;
                BtnToggleMode.Content = "TRẢ VỀ TỰ ĐỘNG";
                AddLog($"Chuyển {_selectedAgvId} sang THỦ CÔNG (Được lái tự do trên toàn bộ ray & bốc dỡ hàng).");
                this.Focus();
            }
            else
            {
                TxtCurrentMode.Text = "CHẾ ĐỘ: TỰ ĐỘNG";
                TxtCurrentMode.Foreground = Brushes.Green;
                BtnToggleMode.Content = "LÁI THỦ CÔNG";

                if (_agvs.ContainsKey(_selectedAgvId))
                {
                    var car = _agvs[_selectedAgvId];
                    Point currentPos = new Point(car.X, car.Y);

                    Point closestZoneNode = FindClosestNodeOnZoneRoute(car, currentPos);
                    double distToZone = Math.Sqrt(Math.Pow(currentPos.X - closestZoneNode.X, 2) + Math.Pow(currentPos.Y - closestZoneNode.Y, 2));

                    if (distToZone <= 15.0)
                    {
                        car.MissionStatus = "Patrolling";
                        car.ActiveWaypoints.Clear();
                        car.Distance = ProjectPositionToRouteDistance(car, currentPos);
                        var nextP = CalculatePoint(car, car.Distance + 2.0);
                        car.Heading = nextP.Heading;
                        AddLog($"✅ {car.Id} đang ở trong phân vùng -> Hòa làn tuần tra theo chiều thuận.");
                    }
                    else
                    {
                        car.MissionStatus = "Returning_To_Patrol";
                        car.ActiveWaypoints = FindGridPath(currentPos, closestZoneNode);
                        AddLog($"🔄 {car.Id} tự tìm đường ngắn nhất theo ray về ({closestZoneNode.X}, {closestZoneNode.Y}) của phân vùng mình để nhập làn đi luôn!");
                    }
                }
            }

            await SendMqttCommandAsync(_selectedAgvId, _isManualMode ? "MODE_MANUAL" : "MODE_AUTO");
        }

        private double ProjectPositionToRouteDistance(AgvHmiEntity car, Point p)
        {
            double bestDistOnRoute = 0;
            double minDistanceSquared = double.MaxValue;
            double accumDist = 0;

            for (int i = 0; i < car.Route.Length - 1; i++)
            {
                Point a = car.Route[i];
                Point b = car.Route[i + 1];
                double segLen = car.SegLengths[i];
                if (segLen <= 0.001) continue;

                double dx = b.X - a.X;
                double dy = b.Y - a.Y;
                double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy), 0.0, 1.0);

                double projX = a.X + t * dx;
                double projY = a.Y + t * dy;
                double distSq = (p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY);

                if (distSq < minDistanceSquared)
                {
                    minDistanceSquared = distSq;
                    bestDistOnRoute = accumDist + t * segLen;
                }
                accumDist += segLen;
            }

            return bestDistOnRoute;
        }

        private async void AiMissionDispatchLoop(object? sender, EventArgs e)
        {
            if (_isEStopped || _isManualMode) return;

            bool isDepotLocked = _agvs.Values.Any(a => 
                a.MissionStatus == "Going_To_Depot" || 
                a.MissionStatus == "Loading_At_Depot");

            if (_isExecutingQueue && _orderQueue.Count > 0)
            {
                var currentOrder = _orderQueue[0];
                var rack = _rackCards.FirstOrDefault(r => r.Id == currentOrder.RackId);

                if (rack != null)
                {
                    if (rack.Occupied == currentOrder.TargetQuantity)
                    {
                        _orderQueue.RemoveAt(0);
                        UpdateOrderQueueUi();
                        AddLog($"🎉 LỆNH FIFO HOÀN THÀNH: {rack.Id} đã đạt đúng {currentOrder.TargetQuantity}/20 khay!");

                        if (_orderQueue.Count == 0)
                        {
                            _isExecutingQueue = false;
                            UpdateOrderQueueUi();
                            AddLog("✅ ĐÃ HOÀN THÀNH TOÀN BỘ CÁC LỆNH TRONG HÀNG ĐỢI!");
                        }
                        return;
                    }

                    if (rack.RestSecondsRemaining <= 0 && !_agvs.Values.Any(a => a.AssignedRackId == rack.Id))
                    {
                        var availableCars = _agvs.Values
                            .Where(a => a.ManagedRackIds.Contains(rack.Id) && a.MissionStatus == "Patrolling" && a.ActiveWaypoints.Count == 0)
                            .ToList();

                        if (availableCars.Any() && !isDepotLocked)
                        {
                            var bestCar = availableCars.OrderBy(c => CalculateForwardDistanceToDepot(c)).First();

                            bestCar.AssignedRackId = rack.Id;
                            bestCar.MissionStatus = "Going_To_Depot";
                            bestCar.TargetAction = rack.Occupied < currentOrder.TargetQuantity ? "ADD" : "REMOVE";
                            bestCar.CarryingPartName = "";
                            bestCar.ActiveWaypoints = BuildForwardPathToDepot(bestCar);

                            AddLog($"FIFO DISPATCH: Chọn {bestCar.Id} phụ trách {rack.Id} ra Depot lấy hàng.");
                            await SendMqttCommandAsync(bestCar.Id, "GOTO_DEPOT");
                            return;
                        }
                    }
                }
                return;
            }

            if (!_isExecutingQueue && !isDepotLocked)
            {
                foreach (var car in _agvs.Values.Where(a => a.Id != "AGV-03" && a.MissionStatus == "Patrolling" && a.ActiveWaypoints.Count == 0))
                {
                    var targetRack = _rackCards
                        .Where(r => car.ManagedRackIds.Contains(r.Id) && r.Occupied < 20 && r.RestSecondsRemaining <= 0 && !_agvs.Values.Any(a => a.AssignedRackId == r.Id))
                        .OrderBy(r => r.Occupied)
                        .FirstOrDefault();

                    if (targetRack != null)
                    {
                        car.AssignedRackId = targetRack.Id;
                        car.MissionStatus = "Going_To_Depot";
                        car.TargetAction = "ADD";
                        car.CarryingPartName = "";
                        car.ActiveWaypoints = BuildForwardPathToDepot(car);

                        AddLog($"NẠP ĐẦY: {car.Id} ra Depot lấy hàng nạp cho {targetRack.Id} ({targetRack.Occupied}/20).");
                        await SendMqttCommandAsync(car.Id, "GOTO_DEPOT");
                        return;
                    }
                }
            }

            var car3 = _agvs["AGV-03"];
            if (car3.MissionStatus == "Patrolling" && car3.ActiveWaypoints.Count == 0)
            {
                var r5 = _rackCards.First(r => r.Id == "RACK-05");
                var r8 = _rackCards.First(r => r.Id == "RACK-08");

                if (Math.Abs(r5.Occupied - r8.Occupied) >= 4)
                {
                    car3.MissionStatus = "Transferring_Cargo";
                    AddLog("TRUNG CHUYỂN: AGV-03 đang luân chuyển linh kiện giữa Kệ 05 và Kệ 08.");
                }
            }
        }

        private void RenderLoop(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            double dt = (now - _lastTick).TotalSeconds;
            _lastTick = now;
            if (dt <= 0 || dt > 0.1) dt = 0.016;

            var keys = _agvs.Keys.ToList();

            foreach (var id in keys)
            {
                var car = _agvs[id];

                if (double.IsNaN(car.X) || double.IsNaN(car.Y) || car.X < 85 || car.X > 695 || car.Y < 55 || car.Y > 465)
                {
                    Point safePoint = FindClosestGridIntersection(new Point(Math.Clamp(car.X, 90, 690), Math.Clamp(car.Y, 60, 460)));
                    car.X = safePoint.X;
                    car.Y = safePoint.Y;
                    car.Distance = ProjectPositionToRouteDistance(car, safePoint);
                }

                if ((_isManualMode && id == _selectedAgvId) || _isEStopped) continue;

                if (car.ActiveWaypoints.Count > 0)
                {
                    Point targetNode = car.ActiveWaypoints[0];
                    double dx = targetNode.X - car.X;
                    double dy = targetNode.Y - car.Y;
                    double distToNode = Math.Sqrt(dx * dx + dy * dy);
                    double step = BASE_SPEED * dt;

                    bool mustYield = CheckYieldAndCollision(car, keys);
                    if (mustYield) { car.CurrentSpeed = 0; continue; }

                    car.CurrentSpeed = BASE_SPEED;
                    if (distToNode <= step)
                    {
                        car.X = targetNode.X;
                        car.Y = targetNode.Y;
                        car.ActiveWaypoints.RemoveAt(0);

                        if (car.ActiveWaypoints.Count == 0)
                        {
                            OnReachedWaypointDestination(car);
                            continue;
                        }
                    }
                    else
                    {
                        car.X += (dx / distToNode) * step;
                        car.Y += (dy / distToNode) * step;
                        car.Heading = Math.Abs(dx) >= Math.Abs(dy) ? (dx > 0 ? "RIGHT" : "LEFT") : (dy > 0 ? "DOWN" : "UP");
                    }
                    continue;
                }

                if (car.MissionStatus == "Loading_At_Depot" || car.MissionStatus == "Dropping_At_Rack")
                {
                    car.CurrentSpeed = 0;
                    car.OperationTimer -= dt;

                    if (car.OperationTimer <= 0)
                    {
                        OnOperationTimerFinished(car);
                    }
                    continue;
                }

                bool needStop = CheckYieldAndCollision(car, keys);
                if (needStop)
                {
                    car.CurrentSpeed = 0;
                }
                else
                {
                    car.CurrentSpeed = BASE_SPEED;
                    double nextDist = car.Distance + BASE_SPEED * dt;
                    var nextPos = CalculatePoint(car, nextDist);

                    car.Distance = nextDist;
                    car.X = nextPos.X;
                    car.Y = nextPos.Y;
                    car.Heading = nextPos.Heading;
                }
            }

            DrawHmiWorld();
        }

        private void OnReachedWaypointDestination(AgvHmiEntity car)
        {
            if (car.MissionStatus == "Going_To_Depot")
            {
                car.X = DEPOT_LOCATION.X;
                car.Y = DEPOT_LOCATION.Y;
                car.CurrentSpeed = 0;
                car.MissionStatus = "Loading_At_Depot";
                car.OperationTimer = 2.5;

                var targetRack = _rackCards.FirstOrDefault(r => r.Id == car.AssignedRackId);
                string partName = targetRack != null ? targetRack.PartName : "Linh kiện";
                AddLog($"📦 [TẠI DEPOT] {car.Id} dừng tại Trạm Cấp Hàng: Đang bốc khay [{partName}] lên xe (2.5s)...");
            }
            else if (car.MissionStatus == "Carrying_To_Rack")
            {
                var targetRack = _rackCards.FirstOrDefault(r => r.Id == car.AssignedRackId);
                if (targetRack != null)
                {
                    car.X = targetRack.StopPointOnTrack.X;
                    car.Y = targetRack.StopPointOnTrack.Y;
                    car.CurrentSpeed = 0;
                    car.MissionStatus = "Dropping_At_Rack";
                    car.OperationTimer = 2.5;
                    targetRack.RestStatus = $"⏳ Đang nạp [{car.CarryingPartName}]...";
                    targetRack.RestStatusColor = Brushes.MediumVioletRed;
                    AddLog($"🛑 [DỪNG TRƯỚC KỆ] {car.Id} dừng trước {targetRack.Id}: Đang hạ khay [{car.CarryingPartName}] vào ô trống...");
                }
            }
            else if (car.MissionStatus == "Returning_To_Patrol")
            {
                car.MissionStatus = "Patrolling";
                car.AssignedRackId = "";
                car.CarryingPartName = "";
                car.Distance = ProjectPositionToRouteDistance(car, new Point(car.X, car.Y));
                var nextP = CalculatePoint(car, car.Distance + 2.0);
                car.Heading = nextP.Heading;
                AddLog($"✅ {car.Id} đã nhập làn an toàn tại ({car.X}, {car.Y}) và tiếp tục hành trình tuần tra.");
            }
        }

        private void OnOperationTimerFinished(AgvHmiEntity car)
        {
            if (car.MissionStatus == "Loading_At_Depot")
            {
                var targetRack = _rackCards.FirstOrDefault(r => r.Id == car.AssignedRackId);
                if (targetRack != null)
                {
                    car.CarryingPartName = targetRack.PartName;
                    car.MissionStatus = "Carrying_To_Rack";
                    car.ActiveWaypoints = BuildForwardPathFromDepotToRack(car, targetRack.StopPointOnTrack);
                    AddLog($"🚚 {car.Id} đã bốc xong [{car.CarryingPartName}] ➔ Đang chở về {targetRack.Id}...");
                }
            }
            else if (car.MissionStatus == "Dropping_At_Rack")
            {
                var targetRack = _rackCards.FirstOrDefault(r => r.Id == car.AssignedRackId);
                if (targetRack != null)
                {
                    var wmsGroup = _wmsGroups.FirstOrDefault(g => g.RackId == targetRack.Id);
                    if (targetRack.Occupied < 20 && wmsGroup != null)
                    {
                        var emptySlot = wmsGroup.MiniSlots.FirstOrDefault(s => !s.Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase));
                        if (emptySlot != null) emptySlot.Color = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                        int newOcc = targetRack.Occupied + 1;
                        wmsGroup.CountText = $"{newOcc}/20 Khay";
                        RecalculateAiPrediction(targetRack.Id, newOcc);
                        AddLog($"✅ HOÀN TẤT NẠP: {car.Id} nạp khay [{car.CarryingPartName}] vào {targetRack.Id} (Tồn mới: {newOcc}/20).");
                    }

                    targetRack.LastServicedTime = DateTime.Now;
                    targetRack.RestSecondsRemaining = targetRack.TargetRestSeconds;
                }

                car.CarryingPartName = "";
                car.MissionStatus = "Returning_To_Patrol";
                car.ActiveWaypoints = BuildPathBackToPatrol(car);
            }
        }

        private bool CheckYieldAndCollision(AgvHmiEntity car, List<string> keys)
        {
            foreach (var otherId in keys)
            {
                if (otherId == car.Id) continue;
                var other = _agvs[otherId];

                double dx = other.X - car.X;
                double dy = other.Y - car.Y;
                double distToOther = Math.Sqrt(dx * dx + dy * dy);

                if (distToOther > 45.0) continue;

                bool onSameLine = (Math.Abs(car.X - other.X) <= 3.0 && IsOnVerticalTrack(car.X)) ||
                                  (Math.Abs(car.Y - other.Y) <= 3.0 && IsOnHorizontalTrack(car.Y));

                if (onSameLine)
                {
                    bool isOtherDirectlyAhead = false;
                    switch (car.Heading)
                    {
                        case "LEFT":  if (dx < 0 && Math.Abs(dx) <= 32.0 && Math.Abs(dy) <= 8.0) isOtherDirectlyAhead = true; break;
                        case "RIGHT": if (dx > 0 && Math.Abs(dx) <= 32.0 && Math.Abs(dy) <= 8.0) isOtherDirectlyAhead = true; break;
                        case "UP":    if (dy < 0 && Math.Abs(dy) <= 32.0 && Math.Abs(dx) <= 8.0) isOtherDirectlyAhead = true; break;
                        case "DOWN":  if (dy > 0 && Math.Abs(dy) <= 32.0 && Math.Abs(dx) <= 8.0) isOtherDirectlyAhead = true; break;
                    }

                    if (isOtherDirectlyAhead) return true;
                    else continue;
                }

                Point myIntersection = FindClosestGridIntersection(new Point(car.X, car.Y));
                Point otherIntersection = FindClosestGridIntersection(new Point(other.X, other.Y));

                if (Math.Abs(myIntersection.X - otherIntersection.X) <= 3.0 && Math.Abs(myIntersection.Y - otherIntersection.Y) <= 3.0)
                {
                    double myDistToJunction = Math.Sqrt(Math.Pow(car.X - myIntersection.X, 2) + Math.Pow(car.Y - myIntersection.Y, 2));
                    double otherDistToJunction = Math.Sqrt(Math.Pow(other.X - otherIntersection.X, 2) + Math.Pow(other.Y - otherIntersection.Y, 2));

                    if (Math.Abs(myDistToJunction - otherDistToJunction) > 2.0)
                    {
                        if (myDistToJunction > otherDistToJunction)
                        {
                            return true;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    int GetPriority(AgvHmiEntity c)
                    {
                        if (c.MissionStatus == "Going_To_Depot" || c.MissionStatus == "Carrying_To_Rack") return 3;
                        if (c.MissionStatus == "Returning_To_Patrol") return 2;
                        return 1;
                    }

                    int myPrio = GetPriority(car);
                    int otherPrio = GetPriority(other);

                    if (myPrio < otherPrio) return true;
                    if (myPrio > otherPrio) continue;

                    if (string.Compare(car.Id, other.Id, StringComparison.Ordinal) > 0) return true;
                    else continue;
                }

                bool isBlockedAhead = false;
                switch (car.Heading)
                {
                    case "LEFT":  if (dx < 0 && Math.Abs(dx) <= 32.0 && Math.Abs(dy) <= 12.0) isBlockedAhead = true; break;
                    case "RIGHT": if (dx > 0 && Math.Abs(dx) <= 32.0 && Math.Abs(dy) <= 12.0) isBlockedAhead = true; break;
                    case "UP":    if (dy < 0 && Math.Abs(dy) <= 32.0 && Math.Abs(dx) <= 12.0) isBlockedAhead = true; break;
                    case "DOWN":  if (dy > 0 && Math.Abs(dy) <= 32.0 && Math.Abs(dx) <= 12.0) isBlockedAhead = true; break;
                }

                if (isBlockedAhead) return true;
            }

            return false;
        }

        private (double X, double Y, string Heading) CalculatePoint(AgvHmiEntity car, double distTraveled)
        {
            if (car.TotalLength <= 0.001) return (car.Route[0].X, car.Route[0].Y, "RIGHT");

            double d = distTraveled % car.TotalLength;
            if (d < 0) d += car.TotalLength;
            double accum = 0;

            for (int i = 0; i < car.SegLengths.Length; i++)
            {
                if (accum + car.SegLengths[i] >= d)
                {
                    double segLen = car.SegLengths[i];
                    double ratio = segLen > 0.001 ? (d - accum) / segLen : 0;
                    double curX = car.Route[i].X + (car.Route[i + 1].X - car.Route[i].X) * ratio;
                    double curY = car.Route[i].Y + (car.Route[i + 1].Y - car.Route[i].Y) * ratio;
                    double dx = car.Route[i + 1].X - car.Route[i].X;
                    double dy = car.Route[i + 1].Y - car.Route[i].Y;

                    string h = Math.Abs(dx) >= Math.Abs(dy) ? (dx > 0 ? "RIGHT" : "LEFT") : (dy > 0 ? "DOWN" : "UP");
                    return (curX, curY, h);
                }
                accum += car.SegLengths[i];
            }
            return (car.Route[0].X, car.Route[0].Y, "RIGHT");
        }

        private void InitFleetRoutes()
        {
            var rTop = new Point[] { new(90, 60), new(690, 60), new(690, 160), new(90, 160), new(90, 60) };
            var rBottom = new Point[] { new(90, 360), new(690, 360), new(690, 460), new(90, 460), new(90, 360) };
            var rCenter = new Point[] { new(290, 60), new(490, 60), new(490, 460), new(290, 460), new(290, 60) };

            RegisterAgv("AGV-01", "Zone_Top", new List<string> { "RACK-01", "RACK-02", "RACK-03" }, rTop, 0.0, Brushes.Gold);
            RegisterAgv("AGV-02", "Zone_Top", new List<string> { "RACK-04", "RACK-05", "RACK-06" }, rTop, 0.5, Brushes.Goldenrod);

            RegisterAgv("AGV-04", "Zone_Bottom", new List<string> { "RACK-07", "RACK-08", "RACK-09" }, rBottom, 0.0, Brushes.DeepSkyBlue);
            RegisterAgv("AGV-05", "Zone_Bottom", new List<string> { "RACK-10", "RACK-11", "RACK-12" }, rBottom, 0.5, Brushes.DarkCyan);

            RegisterAgv("AGV-03", "Zone_Center", new List<string> { "RACK-05", "RACK-08" }, rCenter, 0.25, Brushes.MediumOrchid);
        }

        private void RegisterAgv(string id, string zone, List<string> managedRacks, Point[] route, double startRatio, Brush color)
        {
            var segs = new double[route.Length - 1];
            double total = 0;
            for (int i = 0; i < route.Length - 1; i++)
            {
                segs[i] = Math.Sqrt(Math.Pow(route[i + 1].X - route[i].X, 2) + Math.Pow(route[i + 1].Y - route[i].Y, 2));
                total += segs[i];
            }

            _agvs[id] = new AgvHmiEntity
            {
                Id = id,
                Zone = zone,
                ManagedRackIds = managedRacks,
                Route = route,
                SegLengths = segs,
                TotalLength = total,
                Distance = total * startRatio,
                Color = color
            };

            var p = CalculatePoint(_agvs[id], _agvs[id].Distance);
            _agvs[id].X = p.X;
            _agvs[id].Y = p.Y;
            _agvs[id].Heading = p.Heading;
        }

        private void InitWarehouseData()
        {
            string[] names = {
                "STM32F407", "Sensor E3Z", "Trở 10k 0805", "Nguồn 5V-3A",
                "ESP32-WROOM", "Tụ nhôm 470uF", "Relay 12VDC", "Driver TB6600",
                "Encoder 600P", "Step Motor 57", "IC ULN2003", "Cầu chì 5A"
            };

            Point[] trackStopPoints = {
                new(190, 60),
                new(290, 110),
                new(590, 60),
                new(190, 160),
                new(290, 210),
                new(590, 160),
                new(190, 360),
                new(290, 310),
                new(590, 360),
                new(190, 460),
                new(290, 410),
                new(590, 460)
            };

            _rackCards.Clear();
            _aiMatrixRows.Clear();
            _wmsGroups.Clear();

            int count = 0;
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    count++;
                    string id = $"RACK-{count:00}";
                    string part = names[count - 1];
                    int occ = (count % 3 == 0) ? 18 : ((count % 3 == 1) ? 4 : 11);

                    _rackCards.Add(new RackViewModel
                    {
                        Id = id, PartName = part,
                        Name = $"Kệ {count:00}: {part} ({occ}/20)",
                        Occupied = occ,
                        StopPointOnTrack = trackStopPoints[count - 1]
                    });

                    _aiMatrixRows.Add(new AiMatrixRow
                    {
                        Id = id, PartName = part,
                        SlotDisplay = $"{occ} / 20",
                        OccupancyRate = $"{(occ * 100 / 20)}%"
                    });

                    var wmsGroup = new WmsRackGroup
                    {
                        RackId = id, HeaderText = $"{id} - {part}", CountText = $"{occ}/20 Khay"
                    };

                    for (int slotIdx = 0; slotIdx < 20; slotIdx++)
                    {
                        bool isOcc = slotIdx < occ;
                        wmsGroup.MiniSlots.Add(new MiniSlotModel
                        {
                            TagId = $"{id}:{slotIdx}",
                            Color = isOcc ? new SolidColorBrush(Color.FromRgb(22, 163, 74)) : new SolidColorBrush(Color.FromRgb(241, 245, 249))
                        });
                    }
                    _wmsGroups.Add(wmsGroup);

                    RecalculateAiPrediction(id, occ);
                }
            }
        }

        private void DrawHmiWorld()
        {
            HmiCanvas.Children.Clear();

            foreach (var y in TRACK_ROWS)
            {
                var line = new Line { X1 = TRACK_COLS[0], Y1 = y, X2 = TRACK_COLS[^1], Y2 = y, Stroke = Brushes.White, StrokeThickness = 5 };
                HmiCanvas.Children.Add(line);
            }
            foreach (var x in TRACK_COLS)
            {
                var line = new Line { X1 = x, Y1 = TRACK_ROWS[0], X2 = x, Y2 = TRACK_ROWS[^1], Stroke = Brushes.White, StrokeThickness = 5 };
                HmiCanvas.Children.Add(line);
            }

            foreach (var x in TRACK_COLS)
            {
                foreach (var y in TRACK_ROWS)
                {
                    var dot = new Ellipse { Width = 8, Height = 8, Fill = Brushes.White };
                    Canvas.SetLeft(dot, x - 4);
                    Canvas.SetTop(dot, y - 4);
                    HmiCanvas.Children.Add(dot);
                }
            }

            var depotBorder = new Rectangle
            {
                Width = 72, Height = 44,
                Fill = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Stroke = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                StrokeThickness = 2.5,
                RadiusX = 6, RadiusY = 6
            };
            Canvas.SetLeft(depotBorder, DEPOT_LOCATION.X - 36);
            Canvas.SetTop(depotBorder, DEPOT_LOCATION.Y - 22);
            HmiCanvas.Children.Add(depotBorder);

            var txtDepotIcon = new TextBlock
            {
                Text = "🏭 DEPOT (50 items)",
                FontSize = 7.5, FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                TextAlignment = TextAlignment.Center, Width = 70
            };
            Canvas.SetLeft(txtDepotIcon, DEPOT_LOCATION.X - 35);
            Canvas.SetTop(txtDepotIcon, DEPOT_LOCATION.Y - 16);
            HmiCanvas.Children.Add(txtDepotIcon);

            var txtDepotSub = new TextBlock
            {
                Text = "TRẠM CẤP HÀNG",
                FontSize = 7.0, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center, Width = 70
            };
            Canvas.SetLeft(txtDepotSub, DEPOT_LOCATION.X - 35);
            Canvas.SetTop(txtDepotSub, DEPOT_LOCATION.Y + 2);
            HmiCanvas.Children.Add(txtDepotSub);

            DrawZoneBorder(new Point(80, 50), 620, 120, Color.FromArgb(70, 234, 179, 8));
            DrawZoneBorder(new Point(80, 350), 620, 120, Color.FromArgb(70, 6, 182, 212));
            DrawZoneBorder(new Point(280, 50), 220, 420, Color.FromArgb(100, 168, 85, 247));

            int count = 0;
            int[] colCenters = { 190, 390, 590 };
            int[] rowCenters = { 110, 210, 310, 410 };

            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    count++;
                    var card = _rackCards[count - 1];

                    Brush fillBrush = card.Occupied >= 16 ? new SolidColorBrush(Color.FromRgb(220, 38, 38)) :
                                     (card.Occupied <= 5 ? new SolidColorBrush(Color.FromRgb(217, 119, 6)) :
                                                           new SolidColorBrush(Color.FromRgb(22, 163, 74)));

                    double rx = colCenters[c] - 70;
                    double ry = rowCenters[r] - 30;

                    var rect = new Rectangle
                    {
                        Width = 140, Height = 60,
                        Fill = fillBrush,
                        Stroke = Brushes.White,
                        StrokeThickness = 1.5,
                        RadiusX = 4, RadiusY = 4,
                        Cursor = Cursors.Hand,
                        Tag = card.Id
                    };
                    rect.MouseDown += (s, ev) => OpenModalForRack(((Rectangle)s).Tag.ToString()!);

                    Canvas.SetLeft(rect, rx);
                    Canvas.SetTop(rect, ry);
                    HmiCanvas.Children.Add(rect);

                    var txtTitle = new TextBlock
                    {
                        Text = card.Id + ": " + card.PartName,
                        FontSize = 10.5, FontWeight = FontWeights.Bold, Foreground = Brushes.White,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(txtTitle, rx + 8);
                    Canvas.SetTop(txtTitle, ry + 8);
                    HmiCanvas.Children.Add(txtTitle);

                    var txtSub = new TextBlock
                    {
                        Text = $"Khay: {card.Occupied}/20 | {card.RestStatus}",
                        FontSize = 9.0, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(txtSub, rx + 8);
                    Canvas.SetTop(txtSub, ry + 28);
                    HmiCanvas.Children.Add(txtSub);

                    var txtTime = new TextBlock
                    {
                        Text = $"PV: {card.LastServicedText}",
                        FontSize = 8.5, Foreground = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(txtTime, rx + 8);
                    Canvas.SetTop(txtTime, ry + 44);
                    HmiCanvas.Children.Add(txtTime);
                }
            }

            foreach (var kvp in _agvs)
            {
                var car = kvp.Value;
                bool isSel = (car.Id == _selectedAgvId);
                bool hasCargo = !string.IsNullOrEmpty(car.CarryingPartName);

                if (isSel)
                {
                    var halo = new Ellipse { Width = 36, Height = 36, Stroke = Brushes.DeepSkyBlue, StrokeThickness = 2.5 };
                    Canvas.SetLeft(halo, car.X - 18);
                    Canvas.SetTop(halo, car.Y - 18);
                    HmiCanvas.Children.Add(halo);
                }

                var agvBody = new Ellipse
                {
                    Width = 26, Height = 26,
                    Fill = car.Color,
                    Stroke = hasCargo ? Brushes.Gold : Brushes.White,
                    StrokeThickness = hasCargo ? 3 : 2
                };
                Canvas.SetLeft(agvBody, car.X - 13);
                Canvas.SetTop(agvBody, car.Y - 13);
                HmiCanvas.Children.Add(agvBody);

                string arrow = car.Heading switch { "LEFT" => "◀", "DOWN" => "▼", "UP" => "▲", _ => "▶" };
                var txtArrow = new TextBlock { Text = arrow, FontSize = 10, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.Black, IsHitTestVisible = false };
                Canvas.SetLeft(txtArrow, car.X - 4);
                Canvas.SetTop(txtArrow, car.Y - 7);
                HmiCanvas.Children.Add(txtArrow);

                string labelText = $"{car.Id}\n[{car.MissionStatus}]";
                if (hasCargo)
                {
                    labelText = $"📦 {car.Id}\n[{car.CarryingPartName}]";
                }
                else if (car.MissionStatus == "Going_To_Depot")
                {
                    labelText = $"{car.Id}\n[Ra Depot lấy]";
                }
                else if (car.MissionStatus == "Transferring_Cargo")
                {
                    labelText = $"{car.Id}\n[Trung chuyển]";
                }

                var txtId = new TextBlock
                {
                    Text = labelText,
                    FontSize = 8.5, FontWeight = FontWeights.Bold,
                    Foreground = hasCargo ? Brushes.Yellow : Brushes.White,
                    IsHitTestVisible = false,
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetLeft(txtId, car.X - 45);
                Canvas.SetTop(txtId, car.Y - 32);
                HmiCanvas.Children.Add(txtId);
            }
        }

        private void DrawZoneBorder(Point p, double w, double h, Color col)
        {
            var r = new Rectangle
            {
                Width = w, Height = h,
                Stroke = new SolidColorBrush(col), StrokeThickness = 2.5,
                Fill = new SolidColorBrush(Color.FromArgb(15, col.R, col.G, col.B)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(r, p.X);
            Canvas.SetTop(r, p.Y);
            HmiCanvas.Children.Add(r);
        }

        public void RackCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag != null) OpenModalForRack(el.Tag.ToString()!);
        }

        private void OpenModalForRack(string rackId)
        {
            var group = _wmsGroups.FirstOrDefault(g => g.RackId == rackId);
            var card = _rackCards.FirstOrDefault(c => c.Id == rackId);
            if (group == null || card == null) return;

            _currentModalRackId = rackId;
            TxtModalTitle.Text = $"CHI TIẾT: {card.Name} (20 KHAY)";
            TxtModalDesc.Text = $"Số khay đang chứa: {card.Occupied}/20. Bấm vào ô để điều chỉnh số lượng khay:";

            GridModalSlots.Children.Clear();
            for (int i = 0; i < 20; i++)
            {
                int slotIdx = i;
                bool isOcc = group.MiniSlots[i].Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase);

                var slotBtn = new Button
                {
                    Content = $"Slot #{i + 1:00}\n{(isOcc ? "CHỨA" : "TRỐNG")}",
                    FontSize = 9.5, FontWeight = FontWeights.Bold, Margin = new Thickness(3),
                    Background = isOcc ? new SolidColorBrush(Color.FromRgb(22, 163, 74)) : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Foreground = isOcc ? Brushes.White : Brushes.DimGray,
                    BorderBrush = Brushes.Black, BorderThickness = new Thickness(1.5)
                };

                slotBtn.Click += (s, args) =>
                {
                    ToggleSlotState(rackId, slotIdx);
                    OpenModalForRack(rackId);
                };

                GridModalSlots.Children.Add(slotBtn);
            }

            ModalRackDetail.Visibility = Visibility.Visible;
        }

        public void BtnWmsDirectClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag == null) return;
            string[] parts = btn.Tag.ToString()!.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int slotIdx))
            {
                ToggleSlotState(parts[0], slotIdx);
            }
        }

        private void ToggleSlotState(string rackId, int slotIdx)
        {
            var group = _wmsGroups.FirstOrDefault(g => g.RackId == rackId);
            if (group == null) return;

            var slot = group.MiniSlots[slotIdx];
            bool currentlyOcc = slot.Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase);

            slot.Color = currentlyOcc ? new SolidColorBrush(Color.FromRgb(241, 245, 249)) : new SolidColorBrush(Color.FromRgb(22, 163, 74));
            int newOcc = group.MiniSlots.Count(s => s.Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase));
            group.CountText = $"{newOcc}/20 Khay";

            RecalculateAiPrediction(rackId, newOcc);
            AddLog($"WMS: {rackId} - Slot #{slotIdx + 1:00} -> {(currentlyOcc ? "Bỏ khay" : "Thêm khay")} (Tồn mới: {newOcc}/20)");
        }

        public void CloseModal_Click(object sender, RoutedEventArgs e)
        {
            ModalRackDetail.Visibility = Visibility.Collapsed;
        }

        public async void BtnEStop_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEStopped)
            {
                _isEStopped = true;
                _isManualMode = true;
                foreach (var kvp in _agvs) kvp.Value.CurrentSpeed = 0;

                TxtCurrentMode.Text = "E-STOPPED (ĐANG DỪNG)";
                TxtCurrentMode.Foreground = Brushes.Red;
                BtnEStop.Content = "KHÔI PHỤC VẬN HÀNH";
                BtnEStop.Background = Brushes.ForestGreen;
                BtnEStop.BorderBrush = Brushes.DarkGreen;
                AddLog("⚠️ ĐÃ KÍCH HOẠT DỪNG KHẨN CẤP TOÀN HỆ THỐNG!");

                await SendMqttCommandAsync("ALL", "STOP");
            }
            else
            {
                RecoverFromEStop();
            }
        }

        private async void RecoverFromEStop()
        {
            _isEStopped = false;
            _isManualMode = false;
            foreach (var kvp in _agvs) kvp.Value.CurrentSpeed = BASE_SPEED;

            TxtCurrentMode.Text = "CHẾ ĐỘ: TỰ ĐỘNG";
            TxtCurrentMode.Foreground = Brushes.Green;
            BtnToggleMode.Content = "LÁI THỦ CÔNG";
            BtnEStop.Content = "DỪNG KHẨN CẤP";
            BtnEStop.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38));
            BtnEStop.BorderBrush = new SolidColorBrush(Color.FromRgb(185, 28, 28));
            AddLog("✅ HỆ THỐNG ĐÃ KHÔI PHỤC HOẠT ĐỘNG BÌNH THƯỜNG!");

            await SendMqttCommandAsync("ALL", "RESUME");
        }

        public void BtnSelectAgv_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                _selectedAgvId = b.Content.ToString()!;
                AddLog($"Đã chọn xe {_selectedAgvId}");

                BtnAgv1.Background = BtnAgv2.Background = BtnAgv3.Background = BtnAgv4.Background = BtnAgv5.Background = Brushes.White;
                BtnAgv1.Foreground = BtnAgv2.Foreground = BtnAgv3.Foreground = BtnAgv4.Foreground = BtnAgv5.Foreground = Brushes.Black;
                b.Background = Brushes.Black;
                b.Foreground = Brushes.White;

                this.Focus();
            }
        }

        public void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = _csvFilePath, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File lưu tại:\n{_csvFilePath}\nLỗi: {ex.Message}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void SwitchTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string tab = btn.Tag?.ToString() ?? "scada";

            TabBtnScada.Background = TabBtnAi.Background = TabBtnWms.Background = TabBtnSecurity.Background = Brushes.White;
            TabBtnScada.Foreground = TabBtnAi.Foreground = TabBtnWms.Foreground = TabBtnSecurity.Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105));
            TabBtnScada.BorderBrush = TabBtnAi.BorderBrush = TabBtnWms.BorderBrush = TabBtnSecurity.BorderBrush = Brushes.Transparent;
            TabBtnScada.Margin = TabBtnAi.Margin = TabBtnWms.Margin = TabBtnSecurity.Margin = new Thickness(0, 0, 4, 0);

            btn.Background = new SolidColorBrush(Color.FromRgb(241, 245, 249));
            btn.Foreground = Brushes.Black;
            btn.BorderBrush = Brushes.Black;
            btn.Margin = new Thickness(0, 0, 4, -2);

            ViewScada.Visibility = tab == "scada" ? Visibility.Visible : Visibility.Collapsed;
            ViewAi.Visibility = tab == "ai" ? Visibility.Visible : Visibility.Collapsed;
            ViewWms.Visibility = tab == "wms" ? Visibility.Visible : Visibility.Collapsed;
            ViewSecurity.Visibility = tab == "security" ? Visibility.Visible : Visibility.Collapsed;

            if (tab == "security") UpdateChartByFilter();
        }

        public void FilterSelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateChartByFilter(); }
        public void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) { UpdateChartByFilter(); }
        public void BtnSetToday_Click(object sender, RoutedEventArgs e)
        {
            DpFromDate.SelectedDate = DateTime.Today;
            DpToDate.SelectedDate = DateTime.Today;
            CboFilterHour.SelectedIndex = 0;
            UpdateChartByFilter();
        }
        public void BtnReloadCsv_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(LoadCsvToMemoryAsync);
            AddLog("Đã nạp lại dữ liệu CSV.");
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateChartByFilter();
        }

        // =========================================================================
        // KHỞI TẠO VÀ LƯU CSV CỐ ĐỊNH TỪ THÁNG 09/2026 ĐẾN NAY (KHÔNG BỊ MẤT KHI REBUILD)
        // =========================================================================
        private void InitDatasetFile()
        {
            // Lưu ra thư mục cha (Root Folder của Source Code) để không bị dotnet clean xóa mất
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo? dirInfo = new DirectoryInfo(currentDir);
            while (dirInfo != null && !File.Exists(System.IO.Path.Combine(dirInfo.FullName, "AgvHmiApp.csproj")))
            {
                dirInfo = dirInfo.Parent;
            }
            string projectRoot = dirInfo != null ? dirInfo.FullName : currentDir;
            _csvFilePath = System.IO.Path.Combine(projectRoot, "RUNTIME_TRAINING_DATA.csv");

            lock (_csvLock)
            {
                if (!File.Exists(_csvFilePath))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("timestamp,rack_id,part_name,occupied,total_slots,occupancy_rate,lstm_prob,xgb_prob,max_prob,winner,risk_label");

                    var startDate = new DateTime(2026, 9, 1, 8, 0, 0);
                    var endDate = DateTime.Now;
                    var rand = new Random(42);

                    // Sinh dữ liệu liên tục từ ngày 01/09/2026 đến nay mỗi 6 giờ
                    for (var dt = startDate; dt <= endDate; dt = dt.AddHours(6))
                    {
                        string timeStr = dt.ToString("yyyy-MM-dd HH:mm:ss");
                        foreach (var rack in _rackCards)
                        {
                            int occ = rand.Next(3, 20);
                            double rate = (occ * 100.0) / 20.0;
                            double lstm = Math.Min(98.5, Math.Max(15.0, 30.0 + (occ % 7) * 9.5 + rand.NextDouble() * 8));
                            double xgb = Math.Min(99.0, Math.Max(12.0, 28.0 + (occ % 6) * 10.2 + rand.NextDouble() * 7));
                            double maxProb = Math.Max(lstm, xgb);
                            string win = lstm >= xgb ? "LSTM" : "XGBoost";
                            string risk = occ >= 16 ? "OVERLOAD_RISK" : (occ <= 5 ? "DEPLETION_RISK" : "SAFE");

                            sb.AppendLine($"{timeStr},{rack.Id},{rack.PartName},{occ},20,{rate:F1}%,{lstm:F1}%,{xgb:F1}%,{maxProb:F1}%,{win},{risk}");
                        }
                    }

                    File.WriteAllText(_csvFilePath, sb.ToString(), Encoding.UTF8);
                }
            }

            Task.Run(LoadCsvToMemoryAsync);
        }

        private async Task LoadCsvToMemoryAsync()
        {
            if (!File.Exists(_csvFilePath)) return;

            var list = new List<CsvRecordModel>();
            try
            {
                string[] lines;
                lock (_csvLock) { lines = File.ReadAllLines(_csvFilePath); }

                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] cols = line.Split(',');
                    if (cols.Length < 11) continue;

                    DateTime dt;
                    if (!DateTime.TryParseExact(cols[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        if (!DateTime.TryParse(cols[0], out dt)) dt = DateTime.Now;
                    }

                    list.Add(new CsvRecordModel
                    {
                        Timestamp = cols[0],
                        ParsedTime = dt,
                        RackId = cols[1],
                        PartName = cols[2],
                        Occupied = int.TryParse(cols[3], out int occ) ? occ : 0,
                        TotalSlots = 20,
                        OccupancyRate = cols[5],
                        LstmProb = cols[6],
                        XgbProb = cols[7],
                        MaxProb = cols[8],
                        Winner = cols[9],
                        RiskLabel = cols[10]
                    });
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    _recordsCache.Clear();
                    _recordsCache.AddRange(list);

                    _displayRecords.Clear();
                    foreach (var item in _recordsCache.TakeLast(300).Reverse())
                    {
                        _displayRecords.Add(item);
                    }

                    TxtRecordCount.Text = $"{_recordsCache.Count} Bản ghi";
                    UpdateChartByFilter();
                });
            }
            catch { }
        }

        private void UpdateChartByFilter()
        {
            if (!_isFilterInitialized) return;

            string targetRack = CboFilterRack.SelectedItem?.ToString() ?? "RACK-01";
            string hourFilter = (CboFilterHour.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Cả ngày (00h - 24h)";

            DateTime fromDate = DpFromDate.SelectedDate?.Date ?? new DateTime(2026, 9, 1);
            DateTime toDate = (DpToDate.SelectedDate?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);
            DateTime now = DateTime.Now;

            var query = _recordsCache.Where(r => r.RackId == targetRack && r.ParsedTime >= fromDate && r.ParsedTime <= toDate);

            if (hourFilter.Contains("Ca Sáng")) query = query.Where(r => r.ParsedTime.Hour >= 6 && r.ParsedTime.Hour < 14);
            else if (hourFilter.Contains("Ca Chiều")) query = query.Where(r => r.ParsedTime.Hour >= 14 && r.ParsedTime.Hour < 22);
            else if (hourFilter.Contains("Ca Đêm")) query = query.Where(r => r.ParsedTime.Hour >= 22 || r.ParsedTime.Hour < 6);
            else if (hourFilter.Contains("1 Giờ")) query = query.Where(r => r.ParsedTime >= now.AddHours(-1));
            else if (hourFilter.Contains("4 Giờ")) query = query.Where(r => r.ParsedTime >= now.AddHours(-4));

            var rackRecords = query.OrderBy(r => r.ParsedTime).ToList();

            string fromStr = fromDate.ToString("dd/MM/yyyy");
            string toStr = (DpToDate.SelectedDate ?? DateTime.Today).ToString("dd/MM/yyyy");
            TxtSlotFilterSummary.Text = $"{targetRack} | {fromStr} ➔ {toStr}";
            TxtChartTitle.Text = $"📈 BIỂU ĐỒ {targetRack}: THỰC TẾ vs DỰ ĐOÁN ENSEMBLE ({rackRecords.Count} Mốc)";

            var actualPoints = new List<double>();
            var ensemblePoints = new List<double>();
            var timeLabels = new List<string>();

            // Lấy 30 mốc gần nhất trong khoảng lọc để hiển thị trên biểu đồ to
            var displayList = rackRecords.TakeLast(30).ToList();
            foreach (var r in displayList)
            {
                if (double.TryParse(r.OccupancyRate.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out double actVal))
                    actualPoints.Add(actVal);
                else
                    actualPoints.Add(0);

                if (double.TryParse(r.MaxProb.Replace("%", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out double ensVal))
                    ensemblePoints.Add(ensVal);
                else
                    ensemblePoints.Add(0);

                timeLabels.Add(r.ParsedTime.ToString("dd/MM HH:mm"));
            }

            DrawLargeDualLineChart(actualPoints, ensemblePoints, timeLabels);
        }

        // =========================================================================
        // THUẬT TOÁN VẼ BIỂU ĐỒ ĐƯỜNG TO (2 ĐƯỜNG: THỰC TẾ VS ENSEMBLE)
        // =========================================================================
        private void DrawLargeDualLineChart(List<double> actualList, List<double> ensembleList, List<string> timeLabels)
        {
            ChartCanvas.Children.Clear();
            if (actualList.Count < 2) return;

            double canvasW = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 850;
            double canvasH = ChartCanvas.ActualHeight > 0 ? ChartCanvas.ActualHeight : 250;
            double leftMargin = 45.0;
            double rightMargin = 20.0;
            double bottomMargin = 30.0;
            double topMargin = 20.0;

            double plotW = canvasW - leftMargin - rightMargin;
            double plotH = canvasH - topMargin - bottomMargin;

            // 1. Vẽ các đường lưới ngang và nhãn trục Y (%)
            for (int p = 0; p <= 100; p += 20)
            {
                double y = topMargin + plotH - (p / 100.0 * plotH);

                var gridLine = new Line
                {
                    X1 = leftMargin,
                    Y1 = y,
                    X2 = leftMargin + plotW,
                    Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 2 }
                };
                ChartCanvas.Children.Add(gridLine);

                var txtY = new TextBlock
                {
                    Text = $"{p}%",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255))
                };
                Canvas.SetLeft(txtY, 8);
                Canvas.SetTop(txtY, y - 8);
                ChartCanvas.Children.Add(txtY);
            }

            double stepX = plotW / (actualList.Count - 1);

            // Đường 1: Thực Tế (Màu Xanh Lá Cây - Forest Green)
            var actualPoly = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                StrokeThickness = 3.0
            };

            // Đường 2: Dự Đoán Ensemble Max(LSTM, XGB) (Màu Tím Sáng - Purple)
            var ensemblePoly = new Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(168, 85, 247)),
                StrokeThickness = 3.0
            };

            for (int i = 0; i < actualList.Count; i++)
            {
                double x = leftMargin + i * stepX;
                double yActual = topMargin + plotH - (Math.Clamp(actualList[i], 0, 100) / 100.0 * plotH);
                double yEnsemble = topMargin + plotH - (Math.Clamp(ensembleList[i], 0, 100) / 100.0 * plotH);

                actualPoly.Points.Add(new Point(x, yActual));
                ensemblePoly.Points.Add(new Point(x, yEnsemble));

                // Điểm nút tròn cho Thực Tế
                var dotAct = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.LimeGreen,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(dotAct, x - 3);
                Canvas.SetTop(dotAct, yActual - 3);
                ChartCanvas.Children.Add(dotAct);

                // Điểm nút tròn cho Ensemble
                var dotEns = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = Brushes.MediumPurple,
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(dotEns, x - 3);
                Canvas.SetTop(dotEns, yEnsemble - 3);
                ChartCanvas.Children.Add(dotEns);

                // Nhãn mốc thời gian trục X
                if (i % Math.Max(1, actualList.Count / 6) == 0 && i < timeLabels.Count)
                {
                    var txtTime = new TextBlock
                    {
                        Text = timeLabels[i],
                        FontSize = 9.0,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255))
                    };
                    Canvas.SetLeft(txtTime, Math.Max(leftMargin, x - 25));
                    Canvas.SetTop(txtTime, canvasH - bottomMargin + 8);
                    ChartCanvas.Children.Add(txtTime);
                }
            }

            ChartCanvas.Children.Add(actualPoly);
            ChartCanvas.Children.Add(ensemblePoly);
        }

        private void RecalculateAiPrediction(string rackId, int newOccupied)
        {
            var card = _rackCards.FirstOrDefault(c => c.Id == rackId);
            var aiRow = _aiMatrixRows.FirstOrDefault(r => r.Id == rackId);
            if (card == null || aiRow == null) return;

            card.Occupied = newOccupied;
            card.Name = $"Kệ {rackId.Replace("RACK-", "")}: {card.PartName} ({newOccupied}/20)";

            double occRate = (newOccupied * 100.0) / 20.0;
            double lstmProb, xgbProb;
            string winner, riskLabel;

            if (newOccupied >= 16)
            {
                xgbProb = Math.Min(99.4, 85.0 + (newOccupied - 16) * 3.6);
                lstmProb = Math.Min(95.0, 78.0 + (newOccupied - 16) * 4.2);
                winner = "XGBoost";
                riskLabel = "OVERLOAD_RISK";
                card.StatusText = $"🔴 QUÁ TẢI ({xgbProb:F1}%)";
                card.StatusColor = Brushes.Crimson;
                card.TargetRestSeconds = 10.0;
                card.RestSecondsRemaining = 5.0;
            }
            else if (newOccupied <= 5)
            {
                lstmProb = Math.Min(98.8, 80.0 + (5 - newOccupied) * 3.7);
                xgbProb = Math.Min(91.0, 74.0 + (5 - newOccupied) * 3.4);
                winner = "LSTM";
                riskLabel = "DEPLETION_RISK";
                card.StatusText = $"🟡 CẠN KHAY ({lstmProb:F1}%)";
                card.StatusColor = Brushes.Goldenrod;
                card.TargetRestSeconds = 10.0;
                card.RestSecondsRemaining = 5.0;
            }
            else
            {
                lstmProb = Math.Round(20.0 + (newOccupied % 5) * 2.5, 1);
                xgbProb = Math.Round(18.0 + (newOccupied % 4) * 2.8, 1);
                winner = lstmProb >= xgbProb ? "LSTM" : "XGBoost";
                riskLabel = "SAFE";
                card.StatusText = $"🟢 AN TOÀN ({Math.Max(lstmProb, xgbProb):F1}%)";
                card.StatusColor = Brushes.ForestGreen;
                card.TargetRestSeconds = 20.0;
                card.RestSecondsRemaining = 12.0;
            }

            double maxProb = Math.Max(lstmProb, xgbProb);

            aiRow.SlotDisplay = $"{newOccupied} / 20";
            aiRow.OccupancyRate = $"{occRate:F0}%";
            aiRow.LstmProb = $"{lstmProb:F1}%";
            aiRow.XgbProb = $"{xgbProb:F1}%";
            aiRow.MaxProb = $"{maxProb:F1}%";
            aiRow.Winner = winner;
            aiRow.StatusText = newOccupied >= 16 ? "QUÁ TẢI (RỦI RO)" : (newOccupied <= 5 ? "CẠN KHAY (CẢNH BÁO)" : "AN TOÀN");

            TxtAiOverload.Text = $"{_rackCards.Count(r => r.Occupied >= 16)} Kệ";
            TxtAiDeplete.Text = $"{_rackCards.Count(r => r.Occupied <= 5)} Kệ";
            TxtAiSafe.Text = $"{_rackCards.Count(r => r.Occupied > 5 && r.Occupied < 16)} Kệ";

            var now = DateTime.Now;
            card.LastServicedTime = now;

            var newRecord = new CsvRecordModel
            {
                Timestamp = now.ToString("yyyy-MM-dd HH:mm:ss"),
                ParsedTime = now,
                RackId = rackId,
                PartName = card.PartName,
                Occupied = newOccupied,
                TotalSlots = 20,
                OccupancyRate = $"{occRate:F1}%",
                LstmProb = $"{lstmProb:F1}%",
                XgbProb = $"{xgbProb:F1}%",
                MaxProb = $"{maxProb:F1}%",
                Winner = winner,
                RiskLabel = riskLabel
            };

            _recordsCache.Add(newRecord);
            _displayRecords.Insert(0, newRecord);
            TxtRecordCount.Text = $"{_recordsCache.Count} Bản ghi";

            Task.Run(() =>
            {
                try
                {
                    string line = $"{newRecord.Timestamp},{rackId},{card.PartName},{newOccupied},20,{occRate:F1}%,{lstmProb:F1}%,{xgbProb:F1}%,{maxProb:F1}%,{winner},{riskLabel}";
                    lock (_csvLock)
                    {
                        File.AppendAllText(_csvFilePath, line + Environment.NewLine, Encoding.UTF8);
                    }
                }
                catch { }
            });
        }
    }
}
