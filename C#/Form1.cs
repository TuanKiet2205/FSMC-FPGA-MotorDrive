using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Concurrent;

namespace BTL_HTDKN
{
    public partial class Form1 : Form
    {
        private UART myUart;
        private DataParser myParser;

        // === Hàng đợi dữ liệu để chống đơ UI ===
        private ConcurrentQueue<MotorData> dataQueue = new ConcurrentQueue<MotorData>();
        private System.Windows.Forms.Timer uiUpdateTimer;

        // === Biến giả lập ===
        private Random rand = new Random();
        private double fakeCurrentPos = 0;
        private double fakeTargetPos = 90;
        private bool isSimulating = false;

        // Số điểm tối đa hiển thị trên chart (cuộn khi vượt)
        private const int MAX_CHART_POINTS = 5000;

        public Form1()
        {
            InitializeComponent();

            myUart = new UART();
            myParser = new DataParser();

            // === Kết nối các thành phần: UART → DataParser → Form ===

            myUart.OnRawDataReceived += (rawString) => {
                myParser.Parse(rawString);
            };

            myUart.OnError += (errorMsg) => {
                if (this.InvokeRequired)
                    this.Invoke(new Action(() => MessageBox.Show(errorMsg, "Lỗi UART", MessageBoxButtons.OK, MessageBoxIcon.Error)));
                else
                    MessageBox.Show(errorMsg, "Lỗi UART", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            myParser.OnDataParsed += (data) => {
                UpdateMotorUI(data);
            };

            myParser.OnParseError += (errorMsg) => {
                System.Diagnostics.Debug.WriteLine("PARSE ERROR: " + errorMsg);
            };

            // === Cấu hình Chart ===
            SetupChart();

            // === Cấu hình Timer giả lập ===
            testTimer.Interval = 100;
            testTimer.Tick += testTimer_Tick;

            // === Cấu hình ComboBox COM Port ===
            LoadComPorts();

            // === Cấu hình UI Update Timer ===
            uiUpdateTimer = new System.Windows.Forms.Timer();
            uiUpdateTimer.Interval = 50; // Update UI ~20 FPS để tránh đơ
            uiUpdateTimer.Tick += UiUpdateTimer_Tick;
            uiUpdateTimer.Start();
        }

        /// <summary>
        /// Nạp danh sách COM port hiện có vào ComboBox
        /// </summary>
        private void LoadComPorts()
        {
            cboComPort.Items.Clear();
            cboComPort.Items.Add("Simulate");
            cboComPort.Items.Add("COM8"); // Thêm cứng thủ công COM9

            string[] ports = UART.GetAvailablePorts();
            foreach (string port in ports)
            {
                if (port != "COM8") // Tránh thêm 2 lần nếu máy tự nhận ra COM9
                    cboComPort.Items.Add(port);
            }

            cboComPort.SelectedIndex = 0;
        }

        /// <summary>
        /// Cấu hình Chart với 2 đường:
        ///   - current_deg (trục Y trái, xanh dương)
        ///   - target_deg  (trục Y trái, xanh lá)
        /// </summary>
        private void SetupChart()
        {
            chart1.Series.Clear();
            chart1.ChartAreas[0].AxisX.Title = "Sample";

            // --- Series 1: Current Position ---
            var seriesCurrent = new Series("Current_Position")
            {
                ChartArea = "ChartArea1",
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.DodgerBlue
            };
            chart1.Series.Add(seriesCurrent);

            // --- Series 2: Target Position ---
            var seriesTarget = new Series("Target_Position")
            {
                ChartArea = "ChartArea1",
                ChartType = SeriesChartType.Line,
                BorderWidth = 2,
                Color = Color.MediumSeaGreen,
                BorderDashStyle = ChartDashStyle.Dash  // Đường đứt để phân biệt
            };
            chart1.Series.Add(seriesTarget);

            // Trục Y trái: vị trí (độ)
            chart1.ChartAreas[0].AxisY.Title = "Position (deg)";
            chart1.ChartAreas[0].AxisY.TitleForeColor = Color.DodgerBlue;
            chart1.ChartAreas[0].AxisY.IsStartedFromZero = false; // Bỏ giới hạn trục Y ở gốc 0 để tự động scale mượt mà cả giá trị âm/dương

            chart1.AntiAliasing = AntiAliasingStyles.All;
        }

        /// <summary>
        /// Timer giả lập: tạo dữ liệu giả target_deg và current_deg
        /// current_deg "đuổi theo" target_deg theo từng bước nhỏ
        /// </summary>
        private void testTimer_Tick(object sender, EventArgs e)
        {
            // Target thay đổi nhảy 90° mỗi 3 giây (30 tick × 100ms = 3s)
            if (rand.Next(30) == 0)
                fakeTargetPos = rand.Next(0, 4) * 90.0; // 0, 90, 180, 270

            // Current tiến dần về Target (mô phỏng hệ thống điều khiển)
            double error = fakeTargetPos - fakeCurrentPos;
            fakeCurrentPos += error * 0.15; // Hệ số kP giả lập

            MotorData data = new MotorData(fakeCurrentPos, fakeTargetPos);
            UpdateMotorUI(data);
        }

        /// <summary>
        /// NÚT KẾT NỐI / NGẮT KẾT NỐI
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            if (myUart.IsConnected || isSimulating)
            {
                testTimer.Stop();
                isSimulating = false;
                myUart.Disconnect();

                button1.Text = "Kết nối";
                button1.BackColor = SystemColors.Control;
                cboComPort.Enabled = true;
                return;
            }

            string selectedPort = cboComPort.SelectedItem?.ToString();

            if (string.IsNullOrEmpty(selectedPort))
            {
                MessageBox.Show("Vui lòng chọn COM port!", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (selectedPort == "Simulate")
            {
                isSimulating = true;
                testTimer.Start();
                button1.Text = "Dừng (Sim)";
                button1.BackColor = Color.LightGreen;
                cboComPort.Enabled = false;
            }
            else
            {
                int baudRate = 115200;
                bool success = myUart.Connect(selectedPort, baudRate);

                if (success)
                {
                    button1.Text = "Ngắt kết nối";
                    button1.BackColor = Color.LightGreen;
                    cboComPort.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Cập nhật giao diện khi nhận được dữ liệu motor mới.
        /// Đẩy dữ liệu vào hàng đợi để xử lý sau, tránh làm đơ ứng dụng.
        /// </summary>
        private void UpdateMotorUI(MotorData data)
        {
            dataQueue.Enqueue(data);
        }

        private void UiUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (dataQueue.IsEmpty) return;

            // Dừng vẽ đồ thị để thêm điểm hàng loạt (giúp đồ thị không giật lag)
            chart1.Series["Current_Position"].Points.SuspendUpdates();
            chart1.Series["Target_Position"].Points.SuspendUpdates();

            MotorData lastData = null;

            // Lấy tất cả dữ liệu nhận được kể từ lần vẽ trước
            while (dataQueue.TryDequeue(out MotorData data))
            {
                chart1.Series["Current_Position"].Points.AddY(data.CurrentDeg);
                chart1.Series["Target_Position"].Points.AddY(data.TargetDeg);
                lastData = data;
            }

            // Xoá bớt các điểm cũ ở đầu nếu vượt quá giới hạn
            while (chart1.Series["Current_Position"].Points.Count > MAX_CHART_POINTS)
            {
                chart1.Series["Current_Position"].Points.RemoveAt(0);
                chart1.Series["Target_Position"].Points.RemoveAt(0);
            }

            // Tiếp tục vẽ đồ thị
            chart1.Series["Current_Position"].Points.ResumeUpdates();
            chart1.Series["Target_Position"].Points.ResumeUpdates();

            // Cập nhật Labels bằng dữ liệu mới nhất
            if (lastData != null)
            {
                double posError = lastData.TargetDeg - lastData.CurrentDeg;

                label1.Text  = $"Current Pos: {lastData.CurrentDeg:F1} °";
                label2.Text  = $"Target Pos:  {lastData.TargetDeg:F1} °";
                label3.Text  = $"Error:       {posError:F1} °";

                if (Math.Abs(posError) < 1.0)
                    label3.ForeColor = Color.LimeGreen;
                else if (Math.Abs(posError) < 10.0)
                    label3.ForeColor = Color.Orange;
                else
                    label3.ForeColor = Color.Tomato;
            }
        }

        /// <summary>
        /// NÚT THOÁT - Đóng ứng dụng an toàn
        /// </summary>
        private void button2_Click_1(object sender, EventArgs e)
        {
            testTimer.Stop();
            isSimulating = false;

            if (myUart.IsConnected) myUart.Disconnect();
            myUart.Dispose();

            Close();
        }

        /// <summary>
        /// NÚT GỬI - Gửi góc mong muốn xuống STM32 hoặc cập nhật mô phỏng
        /// </summary>
        private void btnSend_Click(object sender, EventArgs e)
        {
            double targetAngle = (double)numTargetAngle.Value;

            if (isSimulating)
            {
                fakeTargetPos = targetAngle;
            }
            else if (myUart != null && myUart.IsConnected)
            {
                // Gửi giá trị góc mong muốn xuống STM32 (kết thúc bằng ký tự \n nhờ hàm SendLine)
                string command = targetAngle.ToString(CultureInfo.InvariantCulture);
                myUart.SendLine(command);
            }
            else
            {
                MessageBox.Show("Vui lòng kết nối UART hoặc chế độ Simulate trước khi gửi!", "Chưa kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Xử lý khi form đang đóng (nhấn nút X)
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            testTimer.Stop();
            if (uiUpdateTimer != null) uiUpdateTimer.Stop();
            isSimulating = false;

            if (myUart != null)
            {
                myUart.Disconnect();
                myUart.Dispose();
            }

            base.OnFormClosing(e);
        }
    }
}