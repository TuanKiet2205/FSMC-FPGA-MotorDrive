using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTL_HTDKN
{
    public class MotorData
    {
        // Vị trí hiện tại của Motor (đơn vị: độ)
        public double CurrentDeg { get; set; }

        // Vị trí mục tiêu của Motor (đơn vị: độ)
        public double TargetDeg { get; set; }

        // Thời gian nhận dữ liệu
        public DateTime Timestamp { get; set; }

        // Constructor đầy đủ
        public MotorData(double currentDeg, double targetDeg)
        {
            this.CurrentDeg = currentDeg;
            this.TargetDeg = targetDeg;
            this.Timestamp = DateTime.Now;
        }

        // Constructor mặc định
        public MotorData()
        {
            this.Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Hiển thị dữ liệu dạng chuỗi để debug
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] Current={CurrentDeg:F1}°, Target={TargetDeg:F1}°";
        }
    }
}
