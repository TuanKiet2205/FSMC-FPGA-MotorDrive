using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization; // Cần thiết để xử lý dấu chấm thập phân chuẩn quốc tế

namespace BTL_HTDKN
{
    public class DataParser
    {
        // Sự kiện này sẽ "bắn" dữ liệu đã xử lý xong về Form1 để hiển thị
        public event Action<MotorData> OnDataParsed;

        // Sự kiện thông báo khi parse lỗi (để debug)
        public event Action<string> OnParseError;

        /// <summary>
        /// Hàm xử lý chuỗi nhận được từ UART
        /// Định dạng STM32 gửi lên: "target_deg, current_deg\n"
        /// Ví dụ: "90,45\n" hoặc "target_deg:90, current_deg:45\n"
        /// </summary>
        public void Parse(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData)) return;

            try
            {
                // 1. Làm sạch chuỗi (xóa khoảng trắng, \r, \n dư thừa)
                string cleanData = rawData.Trim().Replace("\r", "").Replace("\n", "");

                // 2. Trích xuất tất cả các con số có trong chuỗi
                var matches = System.Text.RegularExpressions.Regex.Matches(cleanData, @"-?\d*\.?\d+");
                if (matches.Count < 2)
                {
                    OnParseError?.Invoke($"Dữ liệu không chứa đủ 2 số: '{cleanData}'");
                    return;
                }

                // 3. Trích xuất target_deg (số đầu tiên)
                double targetDeg;
                if (!double.TryParse(matches[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out targetDeg))
                {
                    OnParseError?.Invoke($"Không thể parse target_deg từ: '{matches[0].Value}'");
                    return;
                }

                // 4. Trích xuất current_deg (số thứ hai)
                double currentDeg;
                if (!double.TryParse(matches[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out currentDeg))
                {
                    OnParseError?.Invoke($"Không thể parse current_deg từ: '{matches[1].Value}'");
                    return;
                }

                // 5. Tạo đối tượng MotorData mới
                MotorData motorData = new MotorData(currentDeg, targetDeg);

                // 6. Gửi dữ liệu đã parse cho subscriber (Form1)
                OnDataParsed?.Invoke(motorData);
            }
            catch (Exception ex)
            {
                OnParseError?.Invoke("Lỗi phân tích dữ liệu: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Lỗi phân tích dữ liệu: " + ex.Message);
            }
        }
    }
}
