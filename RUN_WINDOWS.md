# Chạy Mechra 0.2.0-dev.7 trên Windows

Bản này sửa lỗi chặn Part vì thư mục Equations (`EqnFolder`), kiểm tra số phương trình thực tế và khôi phục chữ gợi ý trong ô nhập. Giữ giao diện native màu đen, bố cục gọn và bản sửa CS1513 trước đó. Chưa tích hợp LLM; không cần API key.

Cần cập nhật cả add-in C# lẫn agent Python bằng gói đầy đủ này.

## 1. Giải nén và cài bản mới

Lưu công việc, đóng SOLIDWORKS. Giải nén `Mechra-v0.2.0-dev.7.zip` vào `C:\AI_project`. Mở **Windows PowerShell → Run as Administrator** và chạy:

```powershell
cd C:\AI_project\Mechra-v0.2.0-dev.7
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\install.ps1 -PreviousProjectRoot C:\AI_project\Mechra-v0.2.0-dev.6
```

Nếu agent đang chạy từ thư mục khác, thay `PreviousProjectRoot` bằng đúng thư mục đó. Nếu không có bản cũ, bỏ tham số này. Script chỉ dừng listener được xác minh thuộc thư mục được chỉ định: kiểm tra đường dẫn Python, lệnh Uvicorn, PID/thời điểm tạo; hỗ trợ tiến trình con của Windows venv. Không dừng cả nhóm Python và không sửa Part đang mở.

Nếu chạy lại bộ cài trong cùng thư mục dev.7 và agent dev.7 đã khởi động, dùng `./scripts/install.ps1` không kèm `PreviousProjectRoot`.

Bộ cài sẽ:

1. Kiểm tra cú pháp PowerShell và chạy các bài kiểm tra nhận diện tiến trình bằng dữ liệu giả lập.
2. Kiểm tra quyền Administrator, trạng thái đóng SOLIDWORKS và công cụ build.
3. Dừng agent cũ được xác minh, chuẩn bị Python 3.11+, chạy toàn bộ test và kiểm tra HTTP.
4. Build add-in C# x64/.NET Framework 4.8, chạy bộ test C# thuần.
5. Chạy test bố cục WinForms native (không gọi CAD); nếu có lỗi sẽ dừng trước khi đăng ký.
6. Đăng ký COM rồi đọc lại đường dẫn DLL và phiên bản để đối chiếu.
7. Lưu log cài đặt và báo cáo chẩn đoán ngay cả khi một bước thất bại.

Chỉ mở SOLIDWORKS khi thấy **Add-in registration verified**. Nếu dùng `-SkipRegister`, đó chỉ là build; cần đăng ký riêng bằng Administrator.

Nếu SOLIDWORKS nằm ngoài vị trí thông thường:

```powershell
.\scripts\install.ps1 -PreviousProjectRoot C:\AI_project\Mechra-v0.2.0-dev.6 -SolidWorksApiDir 'D:\SOLIDWORKS\api\redist'
```

## 2. Thử tạo plate

Mở SOLIDWORKS → **Tools → Add-Ins → Mechra**. Giao diện mới có chữ **Mechra**, biểu tượng khối kỹ thuật, nhãn **NATIVE CAD · LOCAL PLANNER** và phiên bản **0.2.0-dev.7** ở footer lúc bắt đầu. Nút **+** tạo cuộc trò chuyện mới; **Check Part / Save log** nằm trên ô nhập.

1. Chọn **File → New → Part** để mở một Part trống, một configuration. Có thể thử lại Part1 đang trống trong ảnh mới nhất. Markups, Design Binder, Lights/Cameras/Scene và thư mục Equations được nhận diện riêng với hình học; không cần xóa các thư mục này. Part phải không có phương trình/biến toàn cục thực tế hoặc liên kết tệp phương trình.
2. Bấm **Check Part**. Với Part trống hợp lệ, thẻ **PART CHECK** báo **Sẵn sàng tạo plate mới**. Mở **Chi tiết Part** để xem số solid/surface bodies, cả hai phải bằng 0. Equations và Disabled equations phải bằng 0; Equation file linked phải là Không. Sau đó gửi `Tạo plate 100 x 60 x 5 mm`.
3. Xem **CAD PLAN** rồi bấm **Áp dụng kế hoạch**.
4. Kiểm tra có `Mechra-Plate-Sketch`, `Mechra-Plate-Extrude` và kết quả **VERIFIED**, thể tích **30.000 mm³**.
5. Gửi `Đổi chiều dày thành 8 mm`, xem kế hoạch rồi **Áp dụng kế hoạch**.
6. Kiểm tra chiều dày **8 mm**, thể tích **48.000 mm³**; feature Extrude hiện tại được chỉnh sửa.
7. Bấm **Save log** và lưu Part bằng SOLIDWORKS khi kết quả đúng.

Trước khi hiện CAD PLAN, add-in kiểm tra trực tiếp điều kiện của Part: chế độ sửa sketch, quyền ghi, số configuration, phương trình/liên kết tệp, feature, solid/surface bodies và hình học plate khi sửa chiều dày. Áp dụng kế hoạch vẫn kiểm tra lại. **Check Part** chỉ đọc và báo điều kiện; chưa phải kết quả VERIFIED của một thao tác tạo/sửa.

Nếu vẫn bị chặn ở một Part mới, bấm **Check Part → Save log** và gửi file `Mechra-session-YYYYMMDD-HHMMSS.txt`. Log có tên và API type của các feature, số bodies, trạng thái phương trình và đường dẫn DLL đang chạy.

Thử hỏi lại kích thước trên một Part mới: `Tạo plate 100 x 60 mm`, rồi trả lời `5 mm`.

## 3. Nếu vẫn lỗi hoặc thấy giao diện cũ

Bộ cài tạo hai loại file trong thư mục `.runtime` của bản mới:

- `install-YYYYMMDD-HHMMSS.log`: toàn bộ log cài/build/đăng ký.
- `diagnostics.json`: phiên bản dự kiến, agent health, listener/tiến trình cha, DLL đã build và đăng ký COM 64-bit.

Gửi hai file này để xác định lỗi. Có thể tạo lại báo cáo chỉ đọc bằng:

```powershell
.\scripts\doctor.ps1 -PreviousProjectRoot C:\AI_project\Mechra-v0.2.0-dev.6
```

Báo cáo đăng ký đúng không chứng minh DLL đã được SOLIDWORKS nạp. Đối chiếu phiên bản ở footer lúc bắt đầu và **Save log**; log này chứa đường dẫn DLL thực sự đang chạy.

Nếu báo port 8765 không xác minh được, script giữ nguyên tiến trình. Chạy `doctor.ps1` bằng Administrator để lấy thông tin listener trước khi xử lý tiếp. Không dùng `Stop-Process -Name python` hoặc dừng PID chỉ dựa vào cổng.

## 4. Các lệnh riêng

```powershell
# Chỉ chạy agent/test HTTP, không build CAD
.\scripts\install.ps1 -AgentOnly

# Kiểm tra cú pháp và nhận diện tiến trình bằng dữ liệu giả lập
.\scripts\test-powershell.ps1

# Đăng ký lại DLL đã build, khi SOLIDWORKS đã đóng; cần Administrator
.\scripts\register-addin.ps1

# Dừng đúng agent của thư mục hiện tại
.\scripts\stop-agent.ps1

# Chạy lại agent
.\scripts\run-agent.ps1
```

Kiểm tra bố cục hẹp/rộng, DPI, cuộn và bàn phím theo [UI_DEV6.md](docs/UI_DEV6.md), cùng các bước mới trong [VALIDATION_DEV7.md](docs/VALIDATION_DEV7.md). Chạy `.\scripts\test-ui.ps1 -CaptureScreenshots` để tạo ảnh control native ở `.runtime\ui-captures`. Ảnh này là test harness; vẫn cần kiểm tra Task Pane được host trong SOLIDWORKS.

Giữ repo `C:\AI_project\Mechra-clean` và thư mục dev.6 trong lúc thử dev.7. Sau khi các bài kiểm tra thực tế đạt yêu cầu, chuyển source vào nhánh `dev`; không đưa môi trường Python, log runtime, `bin`, `obj` hoặc Interop DLL vào Git. Chưa gắn tag `v0.2.0` khi [các bước kiểm thử SOLIDWORKS](docs/V02_TEST_PLAN.md) còn chưa đạt.
