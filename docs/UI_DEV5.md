# Mechra dev.5 — giao diện Task Pane

Triển khai bằng WinForms trong add-in .NET Framework 4.8 hiện tại. Người dùng yêu cầu giữ màu đen, bố cục hiện đại theo tinh thần Grok/OpenRouter và thiên về AI/kỹ thuật. Thiết kế sử dụng thương hiệu Mechra riêng, nền đen, phân cấp chữ rõ, nút chính trắng và biểu tượng khối kỹ thuật.

## Bố cục và tương tác

| Vùng | Nội dung và hành vi |
|---|---|
| Đầu trang | Mechra, biểu tượng khối, nhãn Native CAD / Local planner, nút `+` tạo cuộc trò chuyện mới. |
| Ngữ cảnh | Tên tài liệu và configuration tại lần đọc gần nhất; tooltip nói rõ trạng thái này. Đọc lại khi Check Part, gửi yêu cầu và thực thi. |
| Bắt đầu | Hai lệnh mẫu điền vào ô nhập; người dùng tự gửi. Ghi rõ phạm vi plate và chưa kết nối mô hình AI. |
| Hội thoại | Thẻ riêng cho người dùng, Mechra và lỗi; tự xuống dòng theo bề rộng Task Pane. |
| CAD Plan | Thông số có đơn vị, Part đích, mặt phẳng mặc định, giả định mm, mô tả bước kiểm chứng; Áp dụng/Hủy ngay trong thẻ. |
| Kết quả | Verified/Stopped, số đo và thể tích đọc lại; lỗi có bước dừng. Chi tiết kỹ thuật mở/thu gọn. |
| Soạn lệnh | Ô nhập cố định phía dưới; Enter gửi, Shift+Enter xuống dòng; nút gửi chỉ bật khi có nội dung. |
| Công cụ | Check Part và Save log luôn ở gần ô nhập; khóa khi có yêu cầu đang chạy. |

Plan cũ bị vô hiệu hóa khi gửi yêu cầu mới, Check Part, New chat hoặc Apply. Tài liệu vẫn được kiểm tra lại trong executor trước khi sửa CAD. Mở chi tiết kết quả không gọi COM và không sửa Part. New chat đặt lại ngữ cảnh planner và màn hình; log chẩn đoán vẫn giữ lịch sử trong phiên Task Pane để xuất.

Khi chạy CAD đồng bộ, footer được cập nhật theo bước. Không dùng Task.Run cho COM và không gọi Application.DoEvents. Không hiển thị kết nối LLM, upload ảnh hay tính năng khác chưa có.

## Hệ màu

| Vai trò | Màu |
|---|---|
| Nền | `#090A0C` |
| Thẻ | `#111317` |
| Ô thông số/nút phụ | `#191C21` |
| Viền | `#2B2F37` |
| Chữ chính / nút chính | `#EDF0F4` |
| Chữ phụ | `#97A0AF` |
| Điểm nhấn kỹ thuật | `#A6CBDC` |
| Đã kiểm chứng | `#73D8AE` |
| Cần kiểm tra | `#F2B589` |

Font Segoe UI, kích thước 7.5–20 pt, nút custom có trạng thái hover/disabled/focus. Nhãn tự xuống dòng, log dài thu gọn, cards cuộn trong vùng hội thoại. Không ép tiến trình SOLIDWORKS đổi chế độ DPI toàn cục.

## Bản xem trước và kiểm tra thực tế

`ui/mechra-dev5-preview.png` và `.svg` là minh họa bố cục với dữ liệu giả lập, **không phải ảnh chụp WinForms đang chạy**. Mã native nằm ở `UI/CopilotPanel.cs` và `UI/ProductControls.cs`. Script `scripts/render-ui-preview.py` tái tạo SVG, cần Pillow để đo chữ; không phải dependency của add-in hoặc agent.

Trên Windows cần kiểm tra các tổ hợp:

- Task Pane rộng khoảng 320, 400 và 600 px; scale Windows 100%, 150%, 200%. Kiểm tra riêng trường hợp thay màn hình có DPI khác.
- Hội thoại dài, tên Part dài, thông số nhỏ/lớn, tiếng Việt và mở/đóng log kỹ thuật. Không mất nút Apply, không chồng footer/ô nhập.
- Tab/Shift+Tab, focus bằng bàn phím, Enter gửi, Shift+Enter xuống dòng; nút gửi không thực thi hai lần khi busy.
- Hủy, yêu cầu mới, đổi Part sau review và New chat: kế hoạch cũ không áp dụng được.
- Agent không chạy: hiển thị lỗi rõ và bật lại ô nhập; không treo hoặc ghi trạng thái Verified giả.
- Save log chứa phiên bản DLL, đường dẫn DLL, bước thực thi, thời gian và kết quả kiểm chứng.

Các kiểm tra native này đang chờ máy Windows/SOLIDWORKS; không suy ra chúng đã đạt từ bản minh họa SVG.
