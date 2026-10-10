# NGỮ CẢNH DỰ ÁN: Farm3D (concept: "Farm Restoration – Màu sắc trở lại")

## Dự án
- Game mô phỏng nông trại 3D (kiểu Harvest Moon / Stardew Valley), bản DEMO 10–15 phút, 5 ngày game.
- Unity 6000.6.1f1, URP 17.6.0, Input System 1.20, AI Navigation, UGUI. Code chính ở Assets/FarmRestoration/Scripts (Animals, Farming, Player, Progression, UI, World). Có các script Editor dạng *Setup.cs dùng để sinh map.
- Không có Level Designer: mọi thứ phải chỉnh được bằng Inspector, ScriptableObject hoặc công cụ Editor có menu. Không bắt người dùng sửa code.

## Mục tiêu chấm điểm
Giải trí và thú vị; đồ hoạ đẹp; nhất quán phong cách; có animation, VFX, vật lý, âm thanh, nhạc nền; độ hoàn thiện cao (không crash, hiệu năng ổn định, demo mượt).

## Ràng buộc cứng
- Nền tảng demo: Unity Editor và bản build Windows trên laptop CHỈ có card đồ hoạ Intel tích hợp (Dell Inspiron 7620 Plus). KHÔNG làm gì cho Android/APK và KHÔNG sửa cài đặt Android hiện có.
- Ngân sách hiệu năng (đề xuất, Phase 0 đo lại): mức Low mặc định >= 30 FPS; batches <= 250; triangles hiển thị <= 150k; 1 directional light realtime, bóng 1 cascade khoảng 25–30m, shadow map 1024; không SSAO ở Low/Medium; texture prop <= 512px, nền và skybox <= 1024px, tổng bộ nhớ texture < 300MB; particle đồng thời <= 200; load scene < 5 giây.
- Không có ngân sách mua asset. Chỉ dùng asset CC0 hoặc miễn phí có license rõ ràng, đồ tự làm, hoặc nội dung do AI tạo. Mọi nguồn ngoài phải ghi vào CREDITS.md (tên, tác giả, URL, license). Không dùng asset không rõ nguồn.
- Phong cách: low-poly stylized ấm áp, flat-color theo bảng palette 12–16 màu (định nghĩa ở Docs/ART_BIBLE.md sau Phase 1A). Sự nhất quán đến từ palette, shader, ánh sáng và post-processing, không đến từ model.
- Chỉ làm DEMO: không monetization, không online.

## Quy tắc làm việc
1. Đọc trước, sửa sau. Tái sử dụng module hiện có (PlayerToolController, FarmPlot, CowAnimal, FarmAudioFeedback...), không viết lại từ đầu nếu không cần.
2. Không xoá hoặc ghi đè asset gốc. Công cụ hàng loạt phải có chế độ dry-run (mặc định) và log chi tiết; chỉ ghi khi người dùng bật apply.
3. Code thân thiện GC: không Find/GetComponent trong Update, không cấp phát mỗi frame, pooling cho đối tượng sinh liên tục, huỷ đăng ký event trong OnDisable/OnDestroy, dùng Input System (không dùng Input cũ).
4. Dữ liệu nội dung đặt trong ScriptableObject dưới Assets/FarmRestoration/Data. Công cụ Editor có menu Tools/Farm/...
5. Không giả định. Điều gì không biết hoặc không làm được (không tải được file, không chạy được Unity...) thì ghi rõ "CHƯA XÁC ĐỊNH" kèm cách kiểm tra. Không bịa số liệu.
6. Sau khi sửa: compile phải sạch lỗi, ghi lại warning mới nếu có. Commit nhỏ, mỗi bước một commit với thông điệp rõ.
7. Cuối phase: ghi Docs/PHASE_<số>_REPORT.md gồm: đã làm gì (kèm đường dẫn file), kết quả đo, tiêu chí hoàn thành đạt hay chưa đạt, vấn đề còn mở, đề xuất bước tiếp. Trong chat chỉ trả lời ngắn, chi tiết nằm trong file.
## Tài liệu tham chiếu (đọc trước khi làm)
- PLAN.md (gốc repo): kế hoạch tổng thể gồm concept, Art Gate, roadmap, ngân sách hiệu năng. Đọc để hiểu lý do. Nếu PLAN.md mâu thuẫn với prompt phase mà người dùng giao thì làm theo prompt phase và ghi mâu thuẫn vào báo cáo.
- FarmRestoration/AUDIT_REPORT.md: hồ sơ hiện trạng ban đầu.
- docs/BASELINE.md, ART_BIBLE.md, ASSET_AUDIT.md, CORE_SYSTEMS.md, CONTENT_GUIDE.md, PHASE_*_REPORT.md: sinh ra dần qua từng phase, đọc những file đã tồn tại.
- KHÔNG đọc và KHÔNG thực hiện file "Bộ prompt thực thi ...md" (hoặc PROMPTS.md). Chỉ làm đúng phase mà người dùng giao trong chat.

## Cấu trúc thư mục và đường dẫn
- Gốc repo git: Farm3D/. Gốc Unity project: Farm3D/FarmRestoration/. Mọi đường dẫn Assets/..., ProjectSettings/..., Packages/..., docs/... trong prompt tính từ gốc Unity project.
- "Docs/" trong prompt nghĩa là thư mục docs/ viết thường có sẵn (FarmRestoration/docs/). Không tạo thư mục Docs viết hoa.
- CLAUDE.md, PLAN.md, README.md, CREDITS.md nằm ở gốc repo. README.md đã tồn tại: cập nhật, không ghi đè.
- Code game ở Assets/FarmRestoration/. Script generate_audio*.py ở gốc Unity project dùng để tạo âm thanh: đọc trước Phase 4 và ưu tiên tái sử dụng.

## Chạy Unity ở batchmode
- Đường dẫn Unity.exe: <điền, ví dụ C:\Program Files\Unity\Hub\Editor\6000.6.1f1\Editor\Unity.exe, sửa theo máy bạn>
- Lệnh mẫu: Unity.exe -batchmode -quit -projectPath <đường dẫn tới FarmRestoration> -logFile docs/unity_batch.log -executeMethod <Class.Method>
- Không chạy batchmode khi Unity Editor đang mở cùng project (project bị khoá). Nếu bị khoá thì dừng và nhờ người dùng đóng Editor.
- Máy chạy Windows: dùng PowerShell hoặc cmd, tránh cú pháp chỉ có trên bash.