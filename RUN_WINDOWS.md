# Chạy Mechra 0.2.0-dev.3 trên Windows

Bản này sửa luồng nâng cấp từ Mechra-clean/dev.2 và tiếp tục mốc tạo/sửa plate native. Chưa tích hợp LLM; không cần API key.

## 1. Giải nén và cài bản mới

Lưu công việc, đóng SOLIDWORKS. Giải nén `Mechra-v0.2.0-dev.3.zip` vào `C:\AI_project`. Mở **Windows PowerShell → Run as Administrator** và chạy:

```powershell
cd C:\AI_project\Mechra-v0.2.0-dev.3
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\install.ps1 -PreviousProjectRoot C:\AI_project\Mechra-clean
```

Nếu agent đang chạy từ thư mục khác, thay `PreviousProjectRoot` bằng đúng thư mục đó. Nếu không có bản cũ, bỏ tham số này. Script chỉ dừng listener được xác minh thuộc thư mục được chỉ định: kiểm tra đường dẫn Python, lệnh Uvicorn, PID/thời điểm tạo; hỗ trợ tiến trình con của Windows venv. Không dừng cả nhóm Python và không sửa Part đang mở.

Bộ cài sẽ:

1. Kiểm tra cú pháp PowerShell và chạy các bài kiểm tra nhận diện tiến trình bằng dữ liệu giả lập.
2. Kiểm tra quyền Administrator, trạng thái đóng SOLIDWORKS và công cụ build.
3. Dừng agent cũ được xác minh, chuẩn bị Python 3.11+, chạy toàn bộ test và kiểm tra HTTP.
4. Build add-in C# x64/.NET Framework 4.8, chạy bộ test C# thuần.
5. Đăng ký COM rồi đọc lại đường dẫn DLL và phiên bản để đối chiếu.
6. Lưu log cài đặt và báo cáo chẩn đoán ngay cả khi một bước thất bại.

Chỉ mở SOLIDWORKS khi thấy **Add-in registration verified**. Nếu dùng `-SkipRegister`, đó chỉ là build; cần đăng ký riêng bằng Administrator.

Nếu SOLIDWORKS nằm ngoài vị trí thông thường:

```powershell
.\scripts\install.ps1 -PreviousProjectRoot C:\AI_project\Mechra-clean -SolidWorksApiDir 'D:\SOLIDWORKS\api\redist'
```

## 2. Thử tạo plate

Mở SOLIDWORKS → **Tools → Add-Ins → Mechra**. Giao diện mới có **Mechra / Native CAD**, log chào **MECHRA v0.2.0-dev.3** và các nút **Model / New chat / Save log**.

1. Chọn **File → New → Part** để mở một Part trống, một configuration. `tesst.SLDPRT` trong ảnh đã có `Boss-Extrude1`, vì vậy không đáp ứng điều kiện tạo plate mới.
2. Gửi `Tạo plate 100 x 60 x 5 mm`.
3. Xem **REVIEW PLAN** rồi bấm **Apply plan**.
4. Kiểm tra có `Mechra-Plate-Sketch`, `Mechra-Plate-Extrude` và kết quả **VERIFIED**, thể tích **30.000 mm³**.
5. Gửi `Đổi chiều dày thành 8 mm`, xem kế hoạch rồi **Apply plan**.
6. Kiểm tra chiều dày **8 mm**, thể tích **48.000 mm³**; feature Extrude hiện tại được chỉnh sửa.
7. Bấm **Save log** và lưu Part bằng SOLIDWORKS khi kết quả đúng.

Nếu Part có feature ngoài phạm vi, Mechra trả lời cách mở Part mới ngay từ bước nhận lệnh. Bộ thực thi C# vẫn kiểm tra mô hình thực tế tại thời điểm Apply.

Thử hỏi lại kích thước trên một Part mới: `Tạo plate 100 x 60 mm`, rồi trả lời `5 mm`.

## 3. Nếu vẫn lỗi hoặc thấy giao diện cũ

Bộ cài tạo hai loại file trong thư mục `.runtime` của bản mới:

- `install-YYYYMMDD-HHMMSS.log`: toàn bộ log cài/build/đăng ký.
- `diagnostics.json`: phiên bản dự kiến, agent health, listener/tiến trình cha, DLL đã build và đăng ký COM 64-bit.

Gửi hai file này để xác định lỗi. Có thể tạo lại báo cáo chỉ đọc bằng:

```powershell
.\scripts\doctor.ps1 -PreviousProjectRoot C:\AI_project\Mechra-clean
```

Báo cáo đăng ký đúng không chứng minh DLL đã được SOLIDWORKS nạp. Đối chiếu phiên bản trong lời chào Task Pane và **Save log**; log này chứa đường dẫn DLL thực sự đang chạy.

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

Giữ repo `C:\AI_project\Mechra-clean` trong lúc thử. Sau khi các bài kiểm tra thực tế đạt yêu cầu, chuyển source vào nhánh `dev`; không đưa môi trường Python, log runtime, `bin`, `obj` hoặc Interop DLL vào Git. Chưa gắn tag `v0.2.0` khi [các bước kiểm thử SOLIDWORKS](docs/V02_TEST_PLAN.md) còn chưa đạt.
