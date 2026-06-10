using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports; // Thư viện quan trọng nhất để dùng SerialPort

namespace BTL_HTDKN
{
    public class UART : IDisposable
    {
        private SerialPort _serialPort;
        private bool _disposed = false;

        // Sự kiện này sẽ bắn dữ liệu thô (chuỗi) sang cho DataParser
        public event Action<string> OnRawDataReceived;

        // Sự kiện thông báo lỗi (để Form hiển thị, tránh dùng MessageBox trong class logic)
        public event Action<string> OnError;

        public UART()
        {
            _serialPort = new SerialPort();
            // Cấu hình sự kiện tự động gọi khi có dữ liệu từ phần cứng gửi về
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        /// <summary>
        /// Lấy danh sách các cổng COM hiện có trên máy tính
        /// </summary>
        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Mở kết nối UART với cổng COM và baudrate chỉ định
        /// </summary>
        public bool Connect(string portName, int baudRate)
        {
            try
            {
                if (_serialPort.IsOpen) _serialPort.Close();

                _serialPort.PortName = portName;
                _serialPort.BaudRate = baudRate;
                // Cấu hình đầy đủ thông số truyền (phải khớp với STM32)
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
                _serialPort.Parity = Parity.None;
                _serialPort.Handshake = Handshake.None;
                // Ký tự kết thúc dòng khi dùng ReadLine()
                _serialPort.NewLine = "\n";
                // Timeout đọc (tránh treo nếu STM32 không gửi)
                _serialPort.ReadTimeout = 1000;
                _serialPort.WriteTimeout = 1000;
                // Kích thước buffer nhận
                _serialPort.ReadBufferSize = 4096;

                _serialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Không thể kết nối cổng COM: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Ngắt kết nối UART
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.DiscardInBuffer();  // Xóa buffer đầu vào
                    _serialPort.DiscardOutBuffer(); // Xóa buffer đầu ra
                    _serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi khi ngắt kết nối: " + ex.Message);
            }
        }

        /// <summary>
        /// Hàm tự động chạy khi cổng Serial nhận được dữ liệu (chạy trên thread riêng)
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    // Đọc đến khi gặp ký tự xuống dòng (\n)
                    // Lưu ý: ReadLine() sẽ block cho đến khi nhận được \n hoặc timeout
                    string data = _serialPort.ReadLine();

                    // Gửi chuỗi vừa đọc được ra bên ngoài (cho DataParser xử lý)
                    OnRawDataReceived?.Invoke(data);
                }
            }
            catch (TimeoutException)
            {
                // Timeout đọc - bỏ qua, đợi lần nhận tiếp
                System.Diagnostics.Debug.WriteLine("UART ReadLine timeout");
            }
            catch (Exception ex)
            {
                // Tránh crash khi rút dây đột ngột hoặc đóng port
                System.Diagnostics.Debug.WriteLine("Lỗi đọc UART: " + ex.Message);
            }
        }

        /// <summary>
        /// Gửi lệnh xuống STM32 (dùng khi cần điều khiển motor từ PC)
        /// </summary>
        public void SendData(string command)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Write(command);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Lỗi gửi dữ liệu UART: " + ex.Message);
            }
        }

        /// <summary>
        /// Gửi lệnh kèm ký tự xuống dòng
        /// </summary>
        public void SendLine(string command)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.WriteLine(command);
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke("Lỗi gửi dữ liệu UART: " + ex.Message);
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái kết nối
        /// </summary>
        public bool IsConnected
        {
            get { return _serialPort != null && _serialPort.IsOpen; }
        }

        /// <summary>
        /// Giải phóng tài nguyên SerialPort đúng cách
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                    if (_serialPort != null)
                    {
                        _serialPort.Dispose();
                        _serialPort = null;
                    }
                }
                _disposed = true;
            }
        }

        ~UART()
        {
            Dispose(false);
        }
    }
}
