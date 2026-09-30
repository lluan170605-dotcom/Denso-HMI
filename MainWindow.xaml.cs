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
        public string TargetAction { get; set; } = ""; // "ADD" hoặc "REMOVE"
        public string AssignedRackId { get; set; } = "";
        public double OperationTimer { get; set; } = 0.0;

        public List<Point> ReturnWaypoints { get; set; } = new();
        public bool IsReturningHome { get; set; } = false;
    }

    public partial class MainWindow : Window
    {
        private const string IOT_SECRET_KEY = "DENSO_FPT_SCADA_SECRET_KEY";

        private readonly int[] TRACK_COLS = { 90, 290, 490, 690 };
        private readonly int[] TRACK_ROWS = { 60, 160, 260, 360, 460 };
        private const double BASE_SPEED = 48.0;

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

        // BIẾN QUẢN LÝ LỆNH TỒN KHO MỤC TIÊU CỦA NGƯỜI DÙNG
        private string? _targetOrderRackId = null;
        private int _targetOrderQuantity = -1;

        public MainWindow()
        {
            InitializeComponent();

            ItemsRackCards.ItemsSource = _rackCards;
            GridAiMatrix.ItemsSource = _aiMatrixRows;
            ItemsWmsGrid.ItemsSource = _wmsGroups;
            GridCsvData.ItemsSource = _displayRecords;

            InitFleetRoutes();
            InitWarehouseData();
            InitFilterControls();
            InitDatasetFile();

            AddLog("HMI Edge Controller sẵn sàng. MQTTS Port 8883 & Hệ thống điều phối theo lệnh kích hoạt.");

            Task.Run(InitDirectMqttsAsync);

            _renderTimer.Interval = TimeSpan.FromMilliseconds(16);
            _renderTimer.Tick += RenderLoop;
            _renderTimer.Start();

            // Vòng lặp tính toán giao việc bốc/dỡ khay
            _aiDispatcherTimer.Interval = TimeSpan.FromSeconds(1.5);
            _aiDispatcherTimer.Tick += AiMissionDispatchLoop;
            _aiDispatcherTimer.Start();

            _rackRestTimer.Interval = TimeSpan.FromSeconds(1.0);
            _rackRestTimer.Tick += RackRestTimerTick;
            _rackRestTimer.Start();
        }

        private void RackRestTimerTick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            foreach (var rack in _rackCards)
            {
                bool isBeingServiced = _agvs.Values.Any(a => a.AssignedRackId == rack.Id && a.MissionStatus == "Processing_At_Rack");
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

            DpFromDate.SelectedDate = DateTime.Today.AddDays(-7);
            DpToDate.SelectedDate = DateTime.Today;

            _isFilterInitialized = true;
        }

        // =========================================================================
        // XỬ LÝ NÚT PHÁT LỆNH & HỦY LỆNH TỒN KHO MỤC TIÊU
        // =========================================================================
        private void BtnSendOrder_Click(object sender, RoutedEventArgs e)
        {
            string rackId = CboOrderRack.SelectedItem?.ToString() ?? "RACK-01";
            if (!int.TryParse(TxtOrderQuantity.Text.Trim(), out int qty) || qty < 0 || qty > 20)
            {
                MessageBox.Show("Vui lòng nhập số lượng khay hợp lệ (từ 0 đến 20)!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var card = _rackCards.FirstOrDefault(r => r.Id == rackId);
            if (card == null) return;

            _targetOrderRackId = rackId;
            _targetOrderQuantity = qty;

            if (card.Occupied == qty)
            {
                TxtActiveOrderInfo.Text = $"✅ {rackId} đã đủ {qty}/20 khay!";
                AddLog($"LỆNH ĐIỀU PHỐI: {rackId} hiện đã có đúng {qty}/20 khay, không cần điều chỉnh.");
                _targetOrderRackId = null;
                return;
            }

            string actionType = card.Occupied < qty ? $"CẤP THÊM {qty - card.Occupied} KHAY" : $"RÚT BỚT {card.Occupied - qty} KHAY";
            TxtActiveOrderInfo.Text = $"🎯 {rackId} cần {qty}/20 (Hiện có: {card.Occupied})";
            AddLog($"🚨 PHÁT LỆNH ĐIỀU PHỐI: Kệ {rackId} yêu cầu mục tiêu: {qty}/20 khay [{actionType}]. Hệ thống đang phân công AGV...");
        }

        private void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (_targetOrderRackId != null)
            {
                AddLog($"Đã hủy lệnh mục tiêu cho {_targetOrderRackId}. Trở về chế độ cân bằng tự động.");
                _targetOrderRackId = null;
                _targetOrderQuantity = -1;
                TxtActiveOrderInfo.Text = "Chưa có lệnh";
            }
        }

        // =========================================================================
        // 1. EMBEDDED MQTT BROKER & KẾT NỐI MQTTS PORT 8883
        // =========================================================================
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
                    .WithTlsOptions(o =>
                    {
                        o.UseTls();
                        o.WithCertificateValidationHandler(_ => true);
                    })
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
                    AddLog($"Chế độ mô phỏng tự hành HMI kích hoạt: {ex.Message}");
                });
            }
        }

        // =========================================================================
        // 2. BỘ KIỂM TRA BẢO MẬT: ANTI-REPLAY + HMAC-SHA256 + BOUNDARY CHECK
        // =========================================================================
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
                string receivedSig = root.TryGetProperty("signature", out var sProp) ? (sProp.GetString() ?? "") : "";

                long currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (Math.Abs(currentUnix - timestamp) > 6)
                {
                    AddLog($"⚠️ [BẢO MẬT] Từ chối gói tin từ {agvId}: Lệch thời gian > 6s (Anti-Replay)!");
                    return;
                }

                if (!string.IsNullOrEmpty(receivedSig))
                {
                    string dataToVerify = $"{agvId}:{x:F1}:{y:F1}:{timestamp}";
                    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(IOT_SECRET_KEY));
                    string computedSig = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToVerify)));

                    if (computedSig != receivedSig)
                    {
                        AddLog($"🚨 [BÁO ĐỘNG] Chữ ký số gói tin {agvId} không khớp! Nghi vấn giả mạo.");
                        return;
                    }
                }

                if (x < 80 || x > 700 || y < 50 || y > 470)
                {
                    AddLog($"⚠️ [BIÊN DỮ LIỆU] Tọa độ của {agvId} vượt biên an toàn ({x}, {y}) -> Hủy gói.");
                    return;
                }

                if (_agvs.ContainsKey(agvId) && !_isManualMode)
                {
                    _agvs[agvId].X = x;
                    _agvs[agvId].Y = y;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Lỗi kiểm tra gói tin: {ex.Message}");
            }
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

                    var payloadObj = new
                    {
                        target = agvId,
                        action = action,
                        timestamp = ts,
                        signature = signature
                    };

                    var msg = new MqttApplicationMessageBuilder()
                        .WithTopic($"agv/{agvId}/control")
                        .WithPayload(JsonSerializer.Serialize(payloadObj))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .Build();

                    await _mqttClient.PublishAsync(msg);
                    AddLog($"[MQTTS 8883] Đã ký HMAC-SHA256 & phát lệnh '{action}' tới {agvId}");
                }
                catch (Exception ex)
                {
                    AddLog($"[Lỗi Gửi MQTTS] {ex.Message}");
                }
            }
        }

        // =========================================================================
        // 3. THUẬT TOÁN ĐIỀU HƯỚNG THEO RAY
        // =========================================================================
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

        private List<Point> BuildReturnPath(Point startNode, Point targetNode)
        {
            var path = new List<Point> { startNode };
            Point cur = startNode;

            while (Math.Abs(cur.Y - targetNode.Y) > 1.0)
            {
                int nextY = cur.Y < targetNode.Y
                    ? TRACK_ROWS.First(r => r > cur.Y)
                    : TRACK_ROWS.Last(r => r < cur.Y);
                cur = new Point(cur.X, nextY);
                path.Add(cur);
            }

            while (Math.Abs(cur.X - targetNode.X) > 1.0)
            {
                int nextX = cur.X < targetNode.X
                    ? TRACK_COLS.First(c => c > cur.X)
                    : TRACK_COLS.Last(c => c < cur.X);
                cur = new Point(nextX, cur.Y);
                path.Add(cur);
            }

            return path;
        }

        private double DistanceToRoute(Point[] route, Point p)
        {
            double minD = double.MaxValue;
            for (int i = 0; i < route.Length - 1; i++)
            {
                Point a = route[i];
                Point b = route[i + 1];
                double dx = b.X - a.X;
                double dy = b.Y - a.Y;
                double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
                Point proj = new Point(a.X + t * dx, a.Y + t * dy);
                double d = Math.Sqrt(Math.Pow(p.X - proj.X, 2) + Math.Pow(p.Y - proj.Y, 2));
                if (d < minD) minD = d;
            }
            return minD;
        }

        // =========================================================================
        // 4. CHUYỂN CHẾ ĐỘ THỦ CÔNG / TỰ ĐỘNG
        // =========================================================================
        private async void BtnToggleMode_Click(object sender, RoutedEventArgs e)
        {
            if (_isEStopped) { RecoverFromEStop(); return; }

            _isManualMode = !_isManualMode;
            if (_isManualMode)
            {
                TxtCurrentMode.Text = "CHẾ ĐỘ: THỦ CÔNG";
                TxtCurrentMode.Foreground = Brushes.DarkOrange;
                BtnToggleMode.Content = "TRẢ VỀ TỰ ĐỘNG";
                AddLog($"Chuyển {_selectedAgvId} sang THỦ CÔNG");
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

                    Point snappedNode = FindClosestGridIntersection(currentPos);
                    car.X = snappedNode.X;
                    car.Y = snappedNode.Y;

                    double distToZone = DistanceToRoute(car.Route, snappedNode);

                    if (distToZone <= 20.0)
                    {
                        car.IsReturningHome = false;
                        car.MissionStatus = "Patrolling";
                        car.Distance = ProjectPositionToRouteDistance(car, snappedNode);

                        var nextP = CalculatePoint(car, car.Distance + 2.0);
                        car.Heading = nextP.Heading;

                        AddLog($"✅ {car.Id} đã nằm trên vòng đi -> Tiếp tục tuần tra tự động ngay tại ({car.X}, {car.Y}).");
                    }
                    else
                    {
                        Point closestEntryNode = FindClosestNodeOnZoneRoute(car, snappedNode);

                        car.IsReturningHome = true;
                        car.MissionStatus = "Returning Home";
                        car.ReturnWaypoints = BuildReturnPath(snappedNode, closestEntryNode);

                        AddLog($"Xe {car.Id} tự tìm đường ngắn nhất tới điểm nhập vòng ({closestEntryNode.X}, {closestEntryNode.Y}) để đi tiếp!");
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
                double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
                t = Math.Clamp(t, 0.0, 1.0);

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

        // =========================================================================
        // 5. NẠP DỮ LIỆU ASYNC & TRUY XUẤT THEO NGÀY/THÁNG/NĂM/GIỜ & KỆ
        // =========================================================================
        private void InitDatasetFile()
        {
            _csvFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RUNTIME_TRAINING_DATA.csv");
            lock (_csvLock)
            {
                if (!File.Exists(_csvFilePath))
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("timestamp,rack_id,part_name,occupied,total_slots,occupancy_rate,lstm_prob,xgb_prob,max_prob,winner,risk_label");

                    var now = DateTime.Now;
                    foreach (var rack in _rackCards)
                    {
                        double rate = (rack.Occupied * 100.0) / 20.0;
                        string risk = rack.Occupied >= 16 ? "OVERLOAD_RISK" : (rack.Occupied <= 5 ? "DEPLETION_RISK" : "SAFE");

                        for (int k = 15; k >= 0; k--)
                        {
                            string timeStr = now.AddHours(-k * 3).ToString("yyyy-MM-dd HH:mm:ss");
                            double lstm = Math.Min(98.0, 42.0 + (rack.Occupied % 6) * 7.5);
                            double xgb = Math.Min(99.0, 40.0 + (rack.Occupied % 5) * 8.4);
                            double mx = Math.Max(lstm, xgb);
                            string win = lstm >= xgb ? "LSTM" : "XGBoost";
                            sb.AppendLine($"{timeStr},{rack.Id},{rack.PartName},{rack.Occupied},20,{rate:F1}%,{lstm:F1}%,{xgb:F1}%,{mx:F1}%,{win},{risk}");
                        }
                    }
                    File.WriteAllText(_csvFilePath, sb.ToString());
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
                lock (_csvLock)
                {
                    lines = File.ReadAllLines(_csvFilePath);
                }

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

                    if (cols.Length >= 13)
                    {
                        list.Add(new CsvRecordModel
                        {
                            Timestamp = cols[0],
                            ParsedTime = dt,
                            RackId = cols[2],
                            PartName = cols[4],
                            Occupied = int.TryParse(cols[6], out int occ) ? occ : 0,
                            TotalSlots = 20,
                            OccupancyRate = $"{((int.TryParse(cols[6], out int o) ? o : 0) * 100 / 20)}%",
                            LstmProb = cols[8],
                            XgbProb = cols[9],
                            MaxProb = cols[10],
                            Winner = cols[11],
                            RiskLabel = cols[12]
                        });
                    }
                    else
                    {
                        list.Add(new CsvRecordModel
                        {
                            Timestamp = cols[0],
                            ParsedTime = dt,
                            RackId = cols[1],
                            PartName = cols[2],
                            Occupied = int.TryParse(cols[3], out int occ) ? occ : 0,
                            TotalSlots = int.TryParse(cols[4], out int tot) ? tot : 20,
                            OccupancyRate = cols[5],
                            LstmProb = cols[6],
                            XgbProb = cols[7],
                            MaxProb = cols[8],
                            Winner = cols[9],
                            RiskLabel = cols[10]
                        });
                    }
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

            DateTime fromDate = DpFromDate.SelectedDate?.Date ?? DateTime.Today.AddDays(-30);
            DateTime toDate = (DpToDate.SelectedDate?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);

            DateTime now = DateTime.Now;

            var query = _recordsCache.Where(r => r.RackId == targetRack && r.ParsedTime >= fromDate && r.ParsedTime <= toDate);

            if (hourFilter.Contains("Ca Sáng"))
            {
                query = query.Where(r => r.ParsedTime.Hour >= 6 && r.ParsedTime.Hour < 14);
            }
            else if (hourFilter.Contains("Ca Chiều"))
            {
                query = query.Where(r => r.ParsedTime.Hour >= 14 && r.ParsedTime.Hour < 22);
            }
            else if (hourFilter.Contains("Ca Đêm"))
            {
                query = query.Where(r => r.ParsedTime.Hour >= 22 || r.ParsedTime.Hour < 6);
            }
            else if (hourFilter.Contains("1 Giờ"))
            {
                query = query.Where(r => r.ParsedTime >= now.AddHours(-1));
            }
            else if (hourFilter.Contains("4 Giờ"))
            {
                query = query.Where(r => r.ParsedTime >= now.AddHours(-4));
            }

            var rackRecords = query.OrderBy(r => r.ParsedTime).ToList();

            string fromStr = fromDate.ToString("dd/MM/yyyy");
            string toStr = (DpToDate.SelectedDate ?? DateTime.Today).ToString("dd/MM/yyyy");
            TxtSlotFilterSummary.Text = $"{targetRack} | {fromStr} ➔ {toStr} ({hourFilter})";
            TxtChartTitle.Text = $"📈 BIỂU ĐỒ AI CỦA {targetRack} TỪ {fromStr} ĐẾN {toStr} ({rackRecords.Count} Mốc)";

            var lstmData = new List<double>();
            var xgbData = new List<double>();
            var timeLabels = new List<string>();

            var displayPoints = rackRecords.TakeLast(25).ToList();

            foreach (var r in displayPoints)
            {
                if (double.TryParse(r.LstmProb.Replace("%", ""), out double lVal)) lstmData.Add(lVal);
                if (double.TryParse(r.XgbProb.Replace("%", ""), out double xVal)) xgbData.Add(xVal);

                if (fromDate.Date == toDate.Date || (displayPoints.Count > 0 && displayPoints.First().ParsedTime.Date == displayPoints.Last().ParsedTime.Date))
                {
                    timeLabels.Add(r.ParsedTime.ToString("HH:mm"));
                }
                else
                {
                    timeLabels.Add(r.ParsedTime.ToString("dd/MM HH:mm"));
                }
            }

            DrawAiTrendChartWithTime(lstmData, xgbData, timeLabels);
        }

        private void DrawAiTrendChartWithTime(List<double> lstmPoints, List<double> xgbPoints, List<string> timeLabels)
        {
            ChartCanvas.Children.Clear();
            if (lstmPoints.Count < 2) return;

            double canvasW = ChartCanvas.ActualWidth > 0 ? ChartCanvas.ActualWidth : 850;
            double canvasH = 130.0;
            double stepX = canvasW / (Math.Max(lstmPoints.Count - 1, 1));

            for (int p = 20; p <= 100; p += 20)
            {
                double y = canvasH - (p / 100.0 * 95.0 + 20.0);
                var gridLine = new Line { X1 = 0, Y1 = y, X2 = canvasW, Y2 = y, Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), StrokeThickness = 1 };
                ChartCanvas.Children.Add(gridLine);

                var txtPercent = new TextBlock { Text = $"{p}%", FontSize = 8.5, Foreground = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)) };
                Canvas.SetLeft(txtPercent, 4); Canvas.SetTop(txtPercent, y - 10);
                ChartCanvas.Children.Add(txtPercent);
            }

            var lstmPoly = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)), StrokeThickness = 2.5 };
            var xgbPoly = new Polyline { Stroke = new SolidColorBrush(Color.FromRgb(220, 38, 38)), StrokeThickness = 2.5 };

            for (int i = 0; i < lstmPoints.Count; i++)
            {
                double x = i * stepX;
                double yLstm = canvasH - (lstmPoints[i] / 100.0 * 95.0 + 20.0);
                double yXgb = canvasH - (xgbPoints[i] / 100.0 * 95.0 + 20.0);

                lstmPoly.Points.Add(new Point(x, yLstm));
                xgbPoly.Points.Add(new Point(x, yXgb));

                var dotLstm = new Ellipse { Width = 5, Height = 5, Fill = Brushes.DeepSkyBlue };
                Canvas.SetLeft(dotLstm, x - 2.5); Canvas.SetTop(dotLstm, yLstm - 2.5);
                ChartCanvas.Children.Add(dotLstm);

                var dotXgb = new Ellipse { Width = 5, Height = 5, Fill = Brushes.Tomato };
                Canvas.SetLeft(dotXgb, x - 2.5); Canvas.SetTop(dotXgb, yXgb - 2.5);
                ChartCanvas.Children.Add(dotXgb);

                if (i % 3 == 0 && i < timeLabels.Count)
                {
                    var txtTime = new TextBlock
                    {
                        Text = timeLabels[i],
                        FontSize = 8.5,
                        Foreground = new SolidColorBrush(Color.FromArgb(140, 255, 255, 255))
                    };
                    Canvas.SetLeft(txtTime, Math.Max(0, x - 22));
                    Canvas.SetTop(txtTime, canvasH - 16);
                    ChartCanvas.Children.Add(txtTime);
                }
            }

            ChartCanvas.Children.Add(lstmPoly);
            ChartCanvas.Children.Add(xgbPoly);
        }

        private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateChartByFilter();
        }

        private void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            UpdateChartByFilter();
        }

        private void BtnSetToday_Click(object sender, RoutedEventArgs e)
        {
            DpFromDate.SelectedDate = DateTime.Today;
            DpToDate.SelectedDate = DateTime.Today;
            CboFilterHour.SelectedIndex = 0;
            UpdateChartByFilter();
        }

        private void BtnReloadCsv_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(LoadCsvToMemoryAsync);
            AddLog("Đã nạp lại toàn bộ dữ liệu từ RUNTIME_TRAINING_DATA.csv.");
        }

        // =========================================================================
        // 6. THUẬT TOÁN SUY LUẬN AI DỰ BÁO TỨC THỜI
        // =========================================================================
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

            string curFilterRack = CboFilterRack.SelectedItem?.ToString() ?? "";
            if (curFilterRack == rackId)
            {
                UpdateChartByFilter();
            }

            Task.Run(() =>
            {
                try
                {
                    string line = $"{newRecord.Timestamp},{rackId},{card.PartName},{newOccupied},20,{occRate:F1}%,{lstmProb:F1}%,{xgbProb:F1}%,{maxProb:F1}%,{winner},{riskLabel}";
                    lock (_csvLock)
                    {
                        File.AppendAllText(_csvFilePath, line + Environment.NewLine);
                    }
                }
                catch { }
            });
        }

        // =========================================================================
        // 7. BỘ TÍNH TOÁN THEO LỆNH NGƯỜI DÙNG & TỰ CÂN BẰNG TẢI
        // =========================================================================
        private async void AiMissionDispatchLoop(object? sender, EventArgs e)
        {
            if (_isEStopped || _isManualMode) return;

            // =====================================================================
            // CHIẾN LƯỢC 1: NẾU NGƯỜI VẬN HÀNH ĐÃ PHÁT LỆNH MỤC TIÊU CHO MỘT KỆ CỤ THỂ
            // =====================================================================
            if (!string.IsNullOrEmpty(_targetOrderRackId) && _targetOrderQuantity >= 0)
            {
                var targetRack = _rackCards.FirstOrDefault(r => r.Id == _targetOrderRackId);
                if (targetRack != null)
                {
                    // Đã đạt mục tiêu -> Hoàn tất lệnh
                    if (targetRack.Occupied == _targetOrderQuantity)
                    {
                        TxtActiveOrderInfo.Text = $"✅ Hoàn thành: {targetRack.Id} đạt {_targetOrderQuantity}/20";
                        AddLog($"🎉 LỆNH HOÀN THÀNH: {targetRack.Id} đã đạt chính xác số lượng mục tiêu: {_targetOrderQuantity}/20 khay!");
                        _targetOrderRackId = null;
                        _targetOrderQuantity = -1;
                        return;
                    }

                    // Kệ đang trong thời gian nghỉ của lần thao tác trước -> Chờ hết nghỉ
                    if (targetRack.RestSecondsRemaining > 0) return;

                    // Nếu chưa có xe nào được phân công tới kệ này
                    if (!_agvs.Values.Any(a => a.AssignedRackId == targetRack.Id))
                    {
                        // Tìm xe nào quản lý kệ này và đang rảnh
                        var candidate = _agvs.Values.FirstOrDefault(a => a.ManagedRackIds.Contains(targetRack.Id) && a.MissionStatus == "Patrolling" && !a.IsReturningHome);
                        if (candidate != null)
                        {
                            candidate.AssignedRackId = targetRack.Id;
                            if (targetRack.Occupied < _targetOrderQuantity)
                            {
                                candidate.MissionStatus = "Inbound_Carrying";
                                candidate.TargetAction = "ADD";
                                AddLog($"ĐIỀU PHỐI THEO LỆNH: {candidate.Id} nhận khay từ ngoài kho ➔ Mang tới [BỎ VÀO] {targetRack.Id} (Cần: {_targetOrderQuantity}, Hiện có: {targetRack.Occupied})");
                                await SendMqttCommandAsync(candidate.Id, $"STORE_{targetRack.Id}");
                            }
                            else
                            {
                                candidate.MissionStatus = "Outbound_Retrieving";
                                candidate.TargetAction = "REMOVE";
                                AddLog($"ĐIỀU PHỐI THEO LỆNH: {candidate.Id} tới {targetRack.Id} [RÚT BỚT KHAY] (Cần: {_targetOrderQuantity}, Hiện có: {targetRack.Occupied})");
                                await SendMqttCommandAsync(candidate.Id, $"RETRIEVE_{targetRack.Id}");
                            }
                            return;
                        }
                    }
                    return; // Đang ưu tiên thực hiện lệnh mục tiêu
                }
            }

            // =====================================================================
            // CHIẾN LƯỢC 2: CÂN BẰNG TẢI TỰ ĐỘNG KHI KHÔNG CÓ LỆNH RIÊNG
            // =====================================================================
            foreach (var kvp in _agvs)
            {
                var car = kvp.Value;
                if (car.MissionStatus != "Patrolling" || car.IsReturningHome) continue;

                var myRacks = _rackCards.Where(r => car.ManagedRackIds.Contains(r.Id)).ToList();
                if (!myRacks.Any()) continue;

                // 1. ƯU TIÊN KỆ QUÁ TẢI (>= 16 khay) -> Lấy bớt khay ra
                var criticalOverload = myRacks
                    .Where(r => r.Occupied >= 16 && r.RestSecondsRemaining <= 0 && !_agvs.Values.Any(a => a.AssignedRackId == r.Id))
                    .OrderByDescending(r => r.Occupied)
                    .FirstOrDefault();

                if (criticalOverload != null)
                {
                    car.AssignedRackId = criticalOverload.Id;
                    car.MissionStatus = "Outbound_Retrieving";
                    car.TargetAction = "REMOVE";
                    AddLog($"AI QUÁ TẢI: {car.Id} di chuyển tới [LẤY BỚT KHAY] tại {criticalOverload.Id} ({criticalOverload.Occupied}/20)");
                    await SendMqttCommandAsync(car.Id, $"RETRIEVE_{criticalOverload.Id}");
                    continue;
                }

                // 2. ƯU TIÊN KỆ CẠN KHAY (<= 5 khay) -> Bỏ thêm khay vào
                var criticalDeplete = myRacks
                    .Where(r => r.Occupied <= 5 && r.RestSecondsRemaining <= 0 && !_agvs.Values.Any(a => a.AssignedRackId == r.Id))
                    .OrderBy(r => r.Occupied)
                    .FirstOrDefault();

                if (criticalDeplete != null)
                {
                    car.AssignedRackId = criticalDeplete.Id;
                    car.MissionStatus = "Inbound_Carrying";
                    car.TargetAction = "ADD";
                    AddLog($"AI CẠN KHAY: {car.Id} nhận khay ➔ Di chuyển tới [BỎ THÊM KHAY] {criticalDeplete.Id} ({criticalDeplete.Occupied}/20)");
                    await SendMqttCommandAsync(car.Id, $"STORE_{criticalDeplete.Id}");
                    continue;
                }

                // 3. TỰ ĐỘNG CÂN BẰNG TẢI TRONG PHÂN VÙNG (Nếu chênh lệch giữa các kệ >= 4 khay)
                var maxRack = myRacks.Where(r => r.RestSecondsRemaining <= 0 && !_agvs.Values.Any(a => a.AssignedRackId == r.Id)).OrderByDescending(r => r.Occupied).FirstOrDefault();
                var minRack = myRacks.Where(r => r.RestSecondsRemaining <= 0 && !_agvs.Values.Any(a => a.AssignedRackId == r.Id)).OrderBy(r => r.Occupied).FirstOrDefault();

                if (maxRack != null && minRack != null && (maxRack.Occupied - minRack.Occupied) >= 4)
                {
                    if (maxRack.Occupied > 10)
                    {
                        car.AssignedRackId = maxRack.Id;
                        car.MissionStatus = "Outbound_Retrieving";
                        car.TargetAction = "REMOVE";
                        AddLog($"CÂN BẰNG TẢI: {car.Id} tới lấy bớt khay ở {maxRack.Id} ({maxRack.Occupied}/20).");
                        await SendMqttCommandAsync(car.Id, $"RETRIEVE_{maxRack.Id}");
                        continue;
                    }
                    else if (minRack.Occupied < 10)
                    {
                        car.AssignedRackId = minRack.Id;
                        car.MissionStatus = "Inbound_Carrying";
                        car.TargetAction = "ADD";
                        AddLog($"CÂN BẰNG TẢI: {car.Id} chở khay tới nạp cho {minRack.Id} ({minRack.Occupied}/20).");
                        await SendMqttCommandAsync(car.Id, $"STORE_{minRack.Id}");
                        continue;
                    }
                }
            }
        }

        // =========================================================================
        // 8. THAO TÁC SỬA KHAY HÀNG TRÊN MÀN HÌNH CẢM ỨNG
        // =========================================================================
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

        private void RackCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag != null)
            {
                OpenModalForRack(el.Tag.ToString()!);
            }
        }

        private void OpenModalForRack(string rackId)
        {
            var group = _wmsGroups.FirstOrDefault(g => g.RackId == rackId);
            var card = _rackCards.FirstOrDefault(c => c.Id == rackId);
            if (group == null || card == null) return;

            _currentModalRackId = rackId;
            TxtModalTitle.Text = $"CHI TIẾT: {card.Name} (20 KHAY)";
            TxtModalDesc.Text = $"Số khay đang chứa: {card.Occupied}/20. Thời gian nghỉ: {card.RestStatus}. Bấm vào từng ô để thêm hoặc bỏ khay linh kiện:";

            GridModalSlots.Children.Clear();
            for (int i = 0; i < 20; i++)
            {
                int slotIdx = i;
                bool isOcc = group.MiniSlots[i].Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase);

                var slotBtn = new Button
                {
                    Content = $"Slot #{i + 1:00}\n{(isOcc ? "CHỨA" : "TRỐNG")}",
                    FontSize = 9.5,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(3),
                    Background = isOcc ? new SolidColorBrush(Color.FromRgb(22, 163, 74)) : new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                    Foreground = isOcc ? Brushes.White : Brushes.DimGray,
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(1.5)
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

        private void CloseModal_Click(object sender, RoutedEventArgs e)
        {
            ModalRackDetail.Visibility = Visibility.Collapsed;
        }

        private void BtnToggleWmsSlot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag == null) return;
            string[] parts = btn.Tag.ToString()!.Split(':');
            ToggleSlotState(parts[0], int.Parse(parts[1]));
        }

        // =========================================================================
        // 9. ĐIỀU KHIỂN THỦ CÔNG: TỰ ĐỘNG PHANH DỪNG KHI GẶP XE KHÁC
        // =========================================================================
        private async void ExecuteManualMove(string action)
        {
            if (!_isManualMode || _isEStopped) return;
            if (!_agvs.ContainsKey(_selectedAgvId)) return;

            var car = _agvs[_selectedAgvId];
            double targetX = car.X;
            double targetY = car.Y;
            string targetHeading = car.Heading;

            switch (action)
            {
                case "UP": targetY = Math.Max(60, car.Y - 20); targetHeading = "UP"; break;
                case "DOWN": targetY = Math.Min(460, car.Y + 20); targetHeading = "DOWN"; break;
                case "LEFT": targetX = Math.Max(90, car.X - 20); targetHeading = "LEFT"; break;
                case "RIGHT": targetX = Math.Min(690, car.X + 20); targetHeading = "RIGHT"; break;
                case "STOP":
                    AddLog($"DỪNG THỦ CÔNG XE {car.Id}!");
                    await SendMqttCommandAsync(_selectedAgvId, "STOP");
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
                if (distToOther < 42.0)
                {
                    blocked = true;
                    blockerId = other.Id;
                    break;
                }
            }

            if (blocked)
            {
                AddLog($"⚠️ [CHỐNG VA CHẠM] Không thể di chuyển {car.Id}: {blockerId} đang chắn phía trước!");
                await SendMqttCommandAsync(_selectedAgvId, "STOP");
                return;
            }

            car.X = targetX;
            car.Y = targetY;
            await SendMqttCommandAsync(_selectedAgvId, action);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
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
            }

            if (!string.IsNullOrEmpty(action))
            {
                e.Handled = true;
                ExecuteManualMove(action);
            }
        }

        private void BtnManualMove_Click(object sender, RoutedEventArgs e)
        {
            if (!_isManualMode || _isEStopped) return;
            string act = (sender as Button)?.Tag?.ToString() ?? "";
            if (!string.IsNullOrEmpty(act))
            {
                ExecuteManualMove(act);
            }
            this.Focus();
        }

        private async void BtnEStop_Click(object sender, RoutedEventArgs e)
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

        private void BtnSelectAgv_Click(object sender, RoutedEventArgs e)
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

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _csvFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File lưu tại:\n{_csvFilePath}\nLỗi mở file: {ex.Message}", "Dataset Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SwitchTab_Click(object sender, RoutedEventArgs e)
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

            if (tab == "security")
            {
                UpdateChartByFilter();
            }
        }

        // =========================================================================
        // 10. KHỞI TẠO TUYẾN ĐƯỜNG & PHÂN CÔNG PHẠM VI KỆ
        // =========================================================================
        private void InitFleetRoutes()
        {
            var r123 = new Point[] { new(90, 60), new(690, 60), new(690, 160), new(90, 160), new(90, 60) };
            var r101112 = new Point[] { new(90, 360), new(690, 360), new(690, 460), new(90, 460), new(90, 360) };
            var r25811 = new Point[] { new(290, 60), new(490, 60), new(490, 460), new(290, 460), new(290, 60) };

            RegisterAgv("AGV-01", "Rack 1-2-3", new List<string> { "RACK-01", "RACK-02", "RACK-03", "RACK-04", "RACK-06" }, r123, 0.0, Brushes.Gold);
            RegisterAgv("AGV-02", "Rack 1-2-3", new List<string> { "RACK-01", "RACK-02", "RACK-03", "RACK-04", "RACK-06" }, r123, 0.5, Brushes.Goldenrod);

            RegisterAgv("AGV-04", "Rack 10-11-12", new List<string> { "RACK-10", "RACK-11", "RACK-12", "RACK-07", "RACK-09" }, r101112, 0.0, Brushes.DeepSkyBlue);
            RegisterAgv("AGV-05", "Rack 10-11-12", new List<string> { "RACK-10", "RACK-11", "RACK-12", "RACK-07", "RACK-09" }, r101112, 0.5, Brushes.DarkCyan);

            RegisterAgv("AGV-03", "Rack 2-5-8-11", new List<string> { "RACK-02", "RACK-05", "RACK-08", "RACK-11" }, r25811, 0.25, Brushes.MediumOrchid);
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
        }

        private void InitWarehouseData()
        {
            string[] names = {
                "STM32F407", "Sensor E3Z", "Trở 10k 0805", "Nguồn 5V-3A",
                "ESP32-WROOM", "Tụ nhôm 470uF", "Relay 12VDC", "Driver TB6600",
                "Encoder 600P", "Step Motor 57", "IC ULN2003", "Cầu chì 5A"
            };

            Point[] trackStopPoints = {
                new(190, 60),   // RACK-01: dừng tại ray Y=60
                new(290, 110),  // RACK-02: dừng tại ray X=290
                new(590, 60),   // RACK-03: dừng tại ray Y=60
                new(190, 160),  // RACK-04: dừng tại ray Y=160
                new(290, 210),  // RACK-05: dừng tại ray X=290
                new(590, 160),  // RACK-06: dừng tại ray Y=160
                new(190, 360),  // RACK-07: dừng tại ray Y=360
                new(290, 310),  // RACK-08: dừng tại ray X=290
                new(590, 360),  // RACK-09: dừng tại ray Y=360
                new(190, 460),  // RACK-10: dừng tại ray Y=460
                new(290, 410),  // RACK-11: dừng tại ray X=290
                new(590, 460)   // RACK-12: dừng tại ray Y=460
            };

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

        // =========================================================================
        // 11. DỪNG ĐÚNG 1 LẦN TRƯỚC MẶT KỆ ĐỂ TỰ ĐỘNG TĂNG/GIẢM KHAY
        // =========================================================================
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

                if ((_isManualMode && id == _selectedAgvId) || _isEStopped) continue;

                if (car.IsReturningHome)
                {
                    if (car.ReturnWaypoints.Count > 1)
                    {
                        Point targetNode = car.ReturnWaypoints[1];
                        double dx = targetNode.X - car.X;
                        double dy = targetNode.Y - car.Y;
                        double distToNode = Math.Sqrt(dx * dx + dy * dy);
                        double step = BASE_SPEED * dt;

                        bool blocked = CheckObstacleAhead(car, keys);
                        if (blocked) { car.CurrentSpeed = 0; continue; }

                        car.CurrentSpeed = BASE_SPEED;
                        if (distToNode <= step)
                        {
                            car.X = targetNode.X;
                            car.Y = targetNode.Y;
                            car.ReturnWaypoints.RemoveAt(0);

                            double distToLoop = DistanceToRoute(car.Route, new Point(car.X, car.Y));
                            if (distToLoop <= 2.0)
                            {
                                car.IsReturningHome = false;
                                car.MissionStatus = "Patrolling";
                                car.ReturnWaypoints.Clear();
                                car.Distance = ProjectPositionToRouteDistance(car, new Point(car.X, car.Y));
                                var nextHead = CalculatePoint(car, car.Distance + 2.0);
                                car.Heading = nextHead.Heading;
                                AddLog($"✅ {car.Id} đã chạm vào vòng đi -> Tiếp tục tuần tra và chạy tiếp luôn!");
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
                    else
                    {
                        car.IsReturningHome = false;
                        car.MissionStatus = "Patrolling";
                        car.Distance = ProjectPositionToRouteDistance(car, new Point(car.X, car.Y));
                        var nextP = CalculatePoint(car, car.Distance + 2.0);
                        car.Heading = nextP.Heading;
                    }
                }

                // XỬ LÝ NHIỆM VỤ TIẾP CẬN KỆ: DỪNG TRƯỚC MẶT KỆ ĐỂ TĂNG/GIẢM KHAY
                if (!string.IsNullOrEmpty(car.AssignedRackId))
                {
                    var targetRack = _rackCards.FirstOrDefault(r => r.Id == car.AssignedRackId);
                    if (targetRack != null)
                    {
                        double distToStopPoint = Math.Sqrt(Math.Pow(car.X - targetRack.StopPointOnTrack.X, 2) + Math.Pow(car.Y - targetRack.StopPointOnTrack.Y, 2));

                        if (distToStopPoint <= 14.0 && car.MissionStatus != "Processing_At_Rack")
                        {
                            car.X = targetRack.StopPointOnTrack.X;
                            car.Y = targetRack.StopPointOnTrack.Y;
                            car.CurrentSpeed = 0;
                            car.MissionStatus = "Processing_At_Rack";
                            car.OperationTimer = 2.5;

                            string actDesc = car.TargetAction == "ADD" ? "bỏ thêm khay (+1 Khay)" : "lấy bớt khay (-1 Khay)";
                            targetRack.RestStatus = $"⏳ Đang {actDesc}...";
                            targetRack.RestStatusColor = Brushes.MediumVioletRed;
                            AddLog($"🛑 [DỪNG TRƯỚC MẶT KỆ] {car.Id} dừng tại {targetRack.Id}: Đang thực hiện {actDesc}...");
                        }

                        if (car.MissionStatus == "Processing_At_Rack")
                        {
                            car.CurrentSpeed = 0;
                            car.OperationTimer -= dt;

                            if (car.OperationTimer <= 0)
                            {
                                var wmsGroup = _wmsGroups.FirstOrDefault(g => g.RackId == targetRack.Id);
                                if (car.TargetAction == "ADD")
                                {
                                    if (targetRack.Occupied < 20 && wmsGroup != null)
                                    {
                                        var emptySlot = wmsGroup.MiniSlots.FirstOrDefault(s => !s.Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase));
                                        if (emptySlot != null) emptySlot.Color = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                                        int newOcc = targetRack.Occupied + 1;
                                        wmsGroup.CountText = $"{newOcc}/20 Khay";
                                        RecalculateAiPrediction(targetRack.Id, newOcc);
                                        AddLog($"✅ ĐÃ BỎ KHAY: {car.Id} bỏ khay vào {targetRack.Id} thành công (Tồn mới: {newOcc}/20) ➔ Tiếp tục tuần tra.");
                                    }
                                }
                                else if (car.TargetAction == "REMOVE")
                                {
                                    if (targetRack.Occupied > 0 && wmsGroup != null)
                                    {
                                        var occSlot = wmsGroup.MiniSlots.LastOrDefault(s => s.Color.ToString().Contains("16A34A", StringComparison.OrdinalIgnoreCase));
                                        if (occSlot != null) occSlot.Color = new SolidColorBrush(Color.FromRgb(241, 245, 249));
                                        int newOcc = targetRack.Occupied - 1;
                                        wmsGroup.CountText = $"{newOcc}/20 Khay";
                                        RecalculateAiPrediction(targetRack.Id, newOcc);
                                        AddLog($"✅ ĐÃ LẤY KHAY: {car.Id} lấy khay từ {targetRack.Id} mang ra ngoài thành công (Tồn mới: {newOcc}/20) ➔ Tiếp tục tuần tra.");
                                    }
                                }

                                targetRack.LastServicedTime = DateTime.Now;
                                targetRack.RestSecondsRemaining = targetRack.TargetRestSeconds;

                                car.AssignedRackId = "";
                                car.TargetAction = "";
                                car.MissionStatus = "Patrolling";
                                car.CurrentSpeed = BASE_SPEED;
                            }
                            continue;
                        }
                    }
                }

                // DI CHUYỂN TUẦN TRA VÀ QUÉT CHƯỚNG NGẠI PHÍA TRƯỚC
                bool needStop = CheckObstacleAhead(car, keys);

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

        private bool CheckObstacleAhead(AgvHmiEntity car, List<string> keys)
        {
            foreach (var otherId in keys)
            {
                if (otherId == car.Id) continue;
                var other = _agvs[otherId];

                if (car.Zone == other.Zone && !car.IsReturningHome && !other.IsReturningHome && !(_isManualMode && other.Id == _selectedAgvId))
                {
                    double relDist = (other.Distance - car.Distance + car.TotalLength) % car.TotalLength;
                    if (relDist > 0 && relDist < 60.0)
                    {
                        return true;
                    }
                    continue;
                }

                double dx = other.X - car.X;
                double dy = other.Y - car.Y;

                switch (car.Heading)
                {
                    case "DOWN":
                        if (dy > 0 && dy < 48.0 && Math.Abs(dx) < 20.0) return true;
                        break;
                    case "UP":
                        if (dy < 0 && dy > -48.0 && Math.Abs(dx) < 20.0) return true;
                        break;
                    case "RIGHT":
                        if (dx > 0 && dx < 48.0 && Math.Abs(dy) < 20.0) return true;
                        break;
                    case "LEFT":
                        if (dx < 0 && dx > -48.0 && Math.Abs(dy) < 20.0) return true;
                        break;
                }
            }

            return false;
        }

        private (double X, double Y, string Heading) CalculatePoint(AgvHmiEntity car, double distTraveled)
        {
            double d = distTraveled % car.TotalLength;
            if (d < 0) d += car.TotalLength;
            double accum = 0;

            for (int i = 0; i < car.SegLengths.Length; i++)
            {
                if (accum + car.SegLengths[i] >= d)
                {
                    double ratio = (d - accum) / car.SegLengths[i];
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

        private void DrawHmiWorld()
        {
            HmiCanvas.Children.Clear();

            // 1. Line ray màu trắng
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

            // 2. Nút giao ray màu trắng
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

            // 3. Phân vùng ray
            DrawZoneBorder(new Point(80, 50), 620, 120, Color.FromArgb(70, 234, 179, 8));
            DrawZoneBorder(new Point(80, 350), 620, 120, Color.FromArgb(70, 6, 182, 212));
            DrawZoneBorder(new Point(280, 50), 220, 420, Color.FromArgb(100, 168, 85, 247));

            // 4. Vẽ 12 Kệ hàng Andon 3 màu
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

            // 5. Vẽ 5 xe AGV
            foreach (var kvp in _agvs)
            {
                var car = kvp.Value;
                bool isSel = (car.Id == _selectedAgvId);

                if (isSel)
                {
                    var halo = new Ellipse { Width = 36, Height = 36, Stroke = Brushes.DeepSkyBlue, StrokeThickness = 2.5 };
                    Canvas.SetLeft(halo, car.X - 18);
                    Canvas.SetTop(halo, car.Y - 18);
                    HmiCanvas.Children.Add(halo);
                }

                var agvBody = new Ellipse { Width = 26, Height = 26, Fill = car.Color, Stroke = Brushes.White, StrokeThickness = 2 };
                Canvas.SetLeft(agvBody, car.X - 13);
                Canvas.SetTop(agvBody, car.Y - 13);
                HmiCanvas.Children.Add(agvBody);

                string arrow = car.Heading switch { "LEFT" => "◀", "DOWN" => "▼", "UP" => "▲", _ => "▶" };
                var txtArrow = new TextBlock { Text = arrow, FontSize = 10, FontWeight = FontWeights.ExtraBold, Foreground = Brushes.Black, IsHitTestVisible = false };
                Canvas.SetLeft(txtArrow, car.X - 4);
                Canvas.SetTop(txtArrow, car.Y - 7);
                HmiCanvas.Children.Add(txtArrow);

                var txtId = new TextBlock { Text = $"{car.Id}\n[{car.MissionStatus}]", FontSize = 9, FontWeight = FontWeights.Bold, Foreground = Brushes.White, IsHitTestVisible = false, TextAlignment = TextAlignment.Center };
                Canvas.SetLeft(txtId, car.X - 35);
                Canvas.SetTop(txtId, car.Y - 30);
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

        private void AddLog(string msg)
        {
            LstLogs.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
            if (LstLogs.Items.Count > 25) LstLogs.Items.RemoveAt(LstLogs.Items.Count - 1);
        }
    }
}