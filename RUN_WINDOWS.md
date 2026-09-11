# Chạy Mechra 0.2.0-dev.2 trên Windows

Bản này tiếp tục mốc v0.2: **tạo plate native, sửa chiều dày, duyệt kế hoạch và kiểm chứng**. Không cần API key.

## 1. Mở bộ source mới

Lưu công việc rồi đóng SOLIDWORKS. Giải nén `Mechra-v0.2.0-dev.2.zip` tại `C:\AI_project` để có:

```text
C:\AI_project\Mechra-v0.2.0-dev.2\README.md
C:\AI_project\Mechra-v0.2.0-dev.2\scripts\setup-and-run.ps1
```

Mở PowerShell **Run as Administrator** để build và đăng ký add-in trong một lần. Đi vào thư mục mới:

```powershell
cd C:\AI_project\Mechra-v0.2.0-dev.2
Set-ExecutionPolicy -Scope Process Bypass
```

Nếu agent cũ vẫn đang chạy từ `Mechra-clean`, dùng script mới để dừng đúng tiến trình đó:

```powershell
.\scripts\stop-agent.ps1 -ProjectRoot C:\AI_project\Mechra-clean
```

Script đối chiếu đường dẫn Python và dòng lệnh trước khi dừng. Nếu báo tiến trình không xác minh được, đóng cửa sổ agent cũ rồi chạy lại. Không dùng lệnh dừng toàn bộ Python.

## 2. Chạy kiểm thử, build và đăng ký

```powershell
.\scripts\setup-and-run.ps1
```

Script sẽ kiểm tra Python 3.11+, tạo môi trường Python của máy này, cài dependencies, chạy bộ test, kiểm tra agent qua HTTP, build C#, chạy các kiểm tra C# thuần và đăng ký add-in khi PowerShell có quyền Administrator.

Nếu SOLIDWORKS nằm ngoài đường dẫn thông thường:

```powershell
.\scripts\setup-and-run.ps1 -SolidWorksApiDir 'D:\SOLIDWORKS\api\redist'
```

Nếu chỉ muốn thử agent trước:

```powershell
.\scripts\setup-and-run.ps1 -AgentOnly
```

`-AgentOnly` không build hoặc chạy SolidWorks. Không dùng kết quả này làm bằng chứng đã tạo CAD thành công.

## 3. Thử trong SOLIDWORKS

Mở SOLIDWORKS → **Tools → Add-Ins → Mechra**. Tạo một **Part trống, một configuration**.

1. Gửi `Tạo plate 100 x 60 x 5 mm`.
2. Xem thẻ **REVIEW PLAN**. Lúc này chưa tạo hình.
3. Bấm **Apply plan**.
4. Kiểm tra `Mechra-Plate-Sketch` và `Mechra-Plate-Extrude`, cùng trạng thái **VERIFIED** trong chat.
5. Gửi `Đổi chiều dày thành 8 mm`, xem kế hoạch rồi bấm **Apply plan**.
6. Thể tích mục tiêu đổi từ **30.000 mm³** sang **48.000 mm³**; feature Extrude hiện tại được chỉnh sửa.
7. Dùng **Save log** để lưu kết quả nếu cần kiểm tra lỗi. Sau khi xác nhận đúng, lưu Part bằng SOLIDWORKS.

Thử thêm trên Part mới: `Tạo plate 100 x 60 mm`, rồi trả lời `5 mm`. Nếu bạn đổi Part hoặc chỉnh mô hình trước khi Apply, hãy lập kế hoạch mới.

## 4. Dừng và mở lại agent

```powershell
.\scripts\stop-agent.ps1
.\scripts\run-agent.ps1
```

Chạy agent hiện log trong cửa sổ hiện tại (khi port 8765 trống):

```powershell
.\scripts\run-agent.ps1 -Foreground
```

Nếu agent không lên, đọc `.runtime\agent-error.log`. Nếu build lỗi, giữ nguyên thông báo MSBuild/C# đầy đủ. Không sửa kiểu đoán tên API trong một dòng.

## 5. Đưa vào repo sau khi thử

Giữ repo `C:\AI_project\Mechra-clean` trong lúc thử bản mới. Khi kiểm tra thực tế đạt yêu cầu, chuyển source thay đổi vào nhánh `dev`, giữ nguyên `.git`; không đưa `.venv`, `.runtime`, `bin`, `obj` hay Interop DLL vào Git. Không gắn tag `v0.2.0` trước khi hoàn thành [bài kiểm tra thực tế](docs/V02_TEST_PLAN.md).
