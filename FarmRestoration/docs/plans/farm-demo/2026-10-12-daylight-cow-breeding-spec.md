# Ban ngày cố định và bò sinh bê

Nguồn nghiệp vụ: README.md, CowAnimal, FarmProgression, CowPen và DayNightCycle.
Quy mô M: ánh sáng, trạng thái bò, sinh vật thể lúc chạy và lưu/tải JSON.
Người dùng yêu cầu bỏ trời tối và cho hai bò được ăn sinh bò con; đã cho phép dùng code/README làm căn cứ.

## Thiết kế

- Giữ DayNightCycle làm component tương thích với thời tiết và NPC, nhưng giờ luôn là 0.5 (ban ngày). Mưa vẫn đổi màu ánh sáng nhẹ và tưới cây.
- Mỗi bò trưởng thành trong chuồng ăn 1 cà rốt theo cơ chế hiện tại và vẫn có sữa sau 120 giây.
- Khi hai bò trưởng thành cùng chuồng đều được cho ăn trong 60 giây, sinh ngay một bê. Không yêu cầu phân biệt giới tính trong bản mô phỏng này.
- Hai bố mẹ chờ 300 giây mới có thể sinh tiếp; một lần cho ăn không được dùng cho hai lần sinh.
- Bê xuất hiện ở vị trí trống trong chuồng, dùng bản sao model/vật liệu/animation hiện tại, bắt đầu ở 45% kích thước trưởng thành và lớn dần trong 300 giây.
- Bê chưa cho sữa, ăn để tạo sữa hoặc sinh sản cho tới khi lớn. Lời nhắc hiển thị thời gian lớn còn lại.
- Chuồng tối đa 8 bò đã nuôi (bao gồm bê). Nếu đầy/không có chỗ trống, vẫn cho bò ăn để tạo sữa và thông báo chưa thể sinh bê.
- Hiển thị thông báo khi bò chờ bạn ghép cặp, khi sinh bê và khi chuồng đầy.
- Lưu định danh, loại bò sinh trong game, chuồng, tuổi, thời điểm sẵn sàng sinh và lần ăn của bò. Save v1–v4 vẫn đọc được; save mới là v5.

## Quyết định

Tái sử dụng CowAnimal/CowPen và clone model bò đã có để giữ đúng material URP. Tạo riêng lifecycle và bộ điều phối sinh sản để không trộn luật ghép cặp vào di chuyển. Không cần model mới hoặc gói ngoài. Phương án prefab mới cần thêm asset; phương án viết lại toàn bộ đàn bò tạo nhiều thay đổi không cần thiết.

## Kiểm tra

Hai bò hợp lệ sinh đúng một bê; một bò, hai chuồng khác nhau, bê, lần ăn quá 60 giây và bố mẹ trong cooldown không sinh. Tải lại phục hồi bê và không nhân đôi. Timestamps/ID Unicode qua JSON không mất. Đủ 8 bò chặn sinh mới. Milk/feed và thời tiết vẫn hoạt động.
