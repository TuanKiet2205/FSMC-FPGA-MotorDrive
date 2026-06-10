# Bài Tập Lớn Hệ Thống Điều Khiển Nhúng: FSMC-FPGA-MotorDrive

Đây là kho lưu trữ mã nguồn cho dự án "Hệ thống điều khiển nhúng" (BTL_HTDKN). Dự án thực hiện điều khiển vị trí động cơ DC kết hợp giữa Vi điều khiển STM32, FPGA và phần mềm giám sát trên máy tính (PC) sử dụng C#. Giao tiếp giữa STM32 và FPGA được thực hiện qua chuẩn **FSMC** (Flexible Static Memory Controller).

## 🗂 Cấu trúc thư mục

Dự án được chia thành 3 phần chính tương ứng với 3 thư mục:

- 📁 **`FSMC/` (STM32 Firmware)**: Mã nguồn C cho vi điều khiển STM32 (cụ thể là STM32F407). Chứa logic điều khiển PID vị trí, giao tiếp UART với máy tính và cấu hình đọc/ghi dữ liệu vào FPGA thông qua bộ nhớ FSMC.
- 📁 **`Quartus/` (FPGA Hardware)**: Mã nguồn Verilog (project Quartus) dành cho FPGA/CPLD. Đóng vai trò là phần cứng ngoại vi xử lý tính toán xung Encoder, xuất băm xung PWM và điều hướng động cơ DC.
- 📁 **`C#/` (PC GUI)**: Ứng dụng WinForms viết bằng C#. Đây là giao diện người dùng (GUI) để kết nối UART, gửi góc quay mục tiêu (Target Angle) xuống STM32 và vẽ đồ thị đáp ứng vị trí thực tế của động cơ theo thời gian thực.

## 🌟 Các tính năng chính

1. **Điều khiển vị trí PID**: Vòng lặp PID được thực thi trên STM32 với tần số cập nhật cố định (thông qua ngắt Timer), đảm bảo động cơ bám sát góc quay mục tiêu.
2. **Giao tiếp tốc độ cao FSMC**: STM32 tương tác với FPGA/CPLD như một vùng nhớ SRAM ngoài. Địa chỉ cơ sở `0x60000000`.
   - Ghi lệnh điều khiển (Hướng & PWM) vào địa chỉ `0x0000`.
   - Đọc giá trị Encoder từ địa chỉ `0x0002`.
3. **Giám sát thời gian thực (UART DMA)**: STM32 truyền liên tục dữ liệu góc mục tiêu và góc hiện tại về PC ở baudrate `115200`. Phần mềm C# sẽ parse dữ liệu và hiển thị lên đồ thị một cách mượt mà.
4. **Phần cứng xử lý song song**: FPGA chịu trách nhiệm đọc các kênh A/B của Encoder và tạo xung PWM với độ chính xác và tần số cao, giảm tải tối đa cho vi điều khiển chính.

## 🛠 Yêu cầu phần mềm

Để mở và biên dịch dự án này, bạn cần cài đặt:
- **STM32CubeIDE**: Dành cho thư mục `FSMC`.
- **Intel Quartus Prime**: Dành cho thư mục `Quartus`.
- **Visual Studio**: Dành cho thư mục `C#` (yêu cầu .NET Framework/Core hỗ trợ WinForms).

## 🚀 Hướng dẫn sử dụng

1. **Nạp Firmware & Hardware**:
   - Mở project Quartus, tổng hợp (Compile) và nạp file `.sof`/`.pof` xuống board FPGA.
   - Mở project STM32 bằng STM32CubeIDE, biên dịch (Build) và nạp (Debug/Run) code xuống board STM32.
2. **Kết nối phần cứng**:
   - Đảm bảo các bus dữ liệu, bus địa chỉ và các chân điều khiển (WE, OE, CS) giữa STM32 (FSMC) và FPGA được kết nối đúng.
   - Kết nối module công suất (Driver) động cơ với FPGA.
   - Kết nối cổng UART của STM32 với PC (thông qua module USB-to-TTL).
3. **Khởi chạy phần mềm PC**:
   - Mở thư mục `C#` và khởi chạy ứng dụng (hoặc build từ Visual Studio).
   - Chọn đúng cổng COM của module USB-to-TTL, thiết lập Baudrate là `115200` và nhấn Kết nối.
   - Nhập góc quay mong muốn trên giao diện PC và quan sát động cơ hoạt động cũng như đồ thị đáp ứng.

## 📝 Bản Quyền & Tác Giả

Dự án này là bài tập lớn môn Hệ thống điều khiển nhúng. Toàn bộ mã nguồn do nhóm tự phát triển nhằm mục đích học tập và nghiên cứu.
