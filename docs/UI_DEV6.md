# Mechra dev.6 — sửa lỗi giao diện từ ảnh chạy thật

Ảnh người dùng gửi xác nhận dev.5 đã được nạp vào SOLIDWORKS. Bản này sửa các vấn đề quan sát được trong ảnh; không dùng SVG để chứng minh giao diện native đã chạy đúng.

| Vấn đề dev.5 | Thay đổi dev.6 |
|---|---|
| Tiêu đề lớn xuống 3 dòng, biểu tượng chiếm nhiều chỗ | Header giảm chiều cao; bỏ biểu tượng lặp trong lời chào; tiêu đề ngắn 14 pt, lệnh mẫu nằm gần nội dung. |
| Thanh cuộn hội thoại nền trắng rộng | `ConversationViewport` quản lý offset và thanh cuộn tối riêng; không bật AutoScroll/native scrollbar. Hỗ trợ con lăn, kéo thumb, PageUp/PageDown, Home/End. |
| Ô nhập có mũi tên trắng dù chưa nhập; nút Gửi lệch/cắt | `PromptComposer` bố trí editor và hàng hành động bằng tọa độ tách biệt. Bỏ native scrollbar; ô nhập tăng từ 2 đến tối đa 4 dòng, văn bản dài tiếp tục cuộn theo caret. |
| Vùng hội thoại, toolbar và footer dễ bị ép lệch | `PaneRegions` tính các vùng theo kích thước host/DPI, các vùng không chồng lên nhau. Cửa sổ quá thấp vẫn phải cuộn hội thoại. |
| Mở chi tiết tạo một thẻ rất dài, lỗi bị lặp lại | Preview chi tiết giới hạn 136 đơn vị ở DPI 96; có Sao chép đầy đủ và Save log. Gộp điều kiện bị lặp giữa tạo và sửa. |
| Nội dung mới kéo người dùng khỏi chỗ đang đọc | Giữ thẻ đang đọc khi resize/mở chi tiết; hiện nút Tin mới khi trả lời đến lúc đang đọc lịch sử. |
| Part trống bị chặn bởi Markups | Nhận diện đúng `InkMarkupFolder` ở Python và C#. Vẫn kiểm tra bodies/sketch/configuration và không bỏ qua loại feature lạ. |

Số nhỏ hiển thị với định dạng có 6 chữ số có nghĩa để không biến kích thước nhỏ thành 0 do định dạng 3 chữ số thập phân.

## Kiểm tra Windows được bổ sung

`tests/UiLayoutTests.cs` dùng trực tiếp các control production: welcome factory, conversation viewport, composer, detail preview và phép chia vùng pane. Không gọi SOLIDWORKS COM. `scripts/test-ui.ps1` biên dịch/runs test, được bộ cài gọi sau build/C# contract tests và trước COM registration.

Harness có manifest tương thích Windows 10 và cấu hình PerMonitorV2 riêng theo [hướng dẫn .NET Framework](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/high-dpi-support-in-windows-forms?view=netframeworkdesktop-4.8). Không sửa cấu hình DPI của tiến trình SOLIDWORKS.

Test kiểm tra:

- 108 tổ hợp tính vùng (kích thước và DPI giả lập), không tràn/chồng vùng.
- Control native tại DPI hiện tại của Windows: bề rộng logic 320/380/520, chiều cao 480/760.
- Nhập trống/ngắn/12 dòng, editor không đè nút Gửi; không có style WS_VSCROLL mặc định.
- Welcome production, lệnh mẫu chỉ điền draft, card nằm trong viewport.
- Giữ vị trí đọc khi thêm reply; nút tin mới; scroll clamp; mở detail không làm nhảy thẻ đầu; clear không giữ offset cũ.
- Giới hạn preview chi tiết và không chồng các thẻ hội thoại.

Có thể chạy riêng và tạo ảnh chụp control thật tại máy Windows:

```powershell
.\scripts\test-ui.ps1 -CaptureScreenshots
```

Ảnh lưu vào `.runtime\ui-captures`. Đây là ảnh của control trong test harness, không phải ảnh toàn Task Pane được host trong SOLIDWORKS. Không có ảnh dev.6 native nào được tạo trong môi trường assistant.

## Các bước còn cần thử trong SOLIDWORKS

1. Đối chiếu footer lúc mở là `0.2.0-dev.6`. Xem Save log để kiểm tra đường dẫn DLL.
2. Thử pane rộng khoảng 320, 380, 520 px và kéo đổi chiều cao. Kiểm tra editor, nút Gửi, các thông số kế hoạch, kết quả và footer.
3. Gõ dài, Shift+Enter nhiều dòng, xóa hết, gửi bằng Enter và nút. Đảm bảo lấy được toàn bộ nội dung và không gửi hai lần.
4. Mở/thu gọn chi tiết; đọc đoạn cũ khi reply về; cuộn bằng con lăn, kéo thumb, bàn phím và bấm Tin mới.
5. Thử Windows scale 100%, 150%, 200% và chuyển màn hình. Phép tính DPI giả lập không thay thế thử rendering tại từng DPI.
6. Part trống có Markups → Check Part → tạo 100×60×5 mm → xem kế hoạch → áp dụng → kiểm chứng 30000 mm³. Sửa dày 8 mm → kiểm chứng 48000 mm³.
7. Hủy kế hoạch, đổi Part sau review, mất kết nối agent, New chat, đóng/mở Task Pane. Không áp dụng kế hoạch cũ hoặc báo Verified khi thất bại.

## Căn cứ cho Markups

Ảnh chạy thật của người dùng hiển thị tên `Markups`, type `InkMarkupFolder`, solid/surface bodies đều bằng 0. [SOLIDWORKS 2025 Markups](https://help.solidworks.com/2025/english/SolidWorks/sldworks/r_markups.htm) mô tả Markups là ghi chú có thể tạo/xem/sửa/xuất trên Part, Assembly và Drawing. Tên type chính xác ở bản cài của người dùng được lấy từ ảnh; không suy đoán một allowlist rộng cho mọi loại folder.
