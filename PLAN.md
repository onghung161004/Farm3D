Kế hoạch nâng cấp Farm3D – Bản demo
10 thg 10, 2026 · @text in vietnamese
Yêu cầu đã chốt và tác động lên plan
Demo chỉ cần chạy mượt trên laptop chỉ có card đồ hoạ Intel tích hợp và không cần build APK, nên plan chọn đồ hoạ nhẹ nhưng đẹp nhờ palette, shader, ánh sáng và nội dung gọn trong 5 ngày game.
Yêu cầu
Tác động lên plan
Map nhỏ, demo ngắn
Bỏ Addressables và streaming. Đổi Resources.Load trong FarmAudioFeedback.cs sang tham chiếu trực tiếp.
Laptop chỉ có card Intel tích hợp (Inspiron 7620 Plus, cấu hình CPU/RAM cần xác nhận)
Ngân sách hiệu năng hạ xuống mức Low làm mặc định: bỏ SSAO, bóng 1 cascade, có tuỳ chọn giảm render scale. Đo thật ở Phase 0.
Không cần APK
Bỏ hoàn toàn build và tối ưu Android. Chỉ chạy trong Unity Editor và build Windows.
Không có Level Designer
Giữ các script Setup.cs. Chạy một lần lấy layout nền, lưu thành scene/prefab rồi chỉnh tay, không sinh lại.
Không có ngân sách asset
Dùng asset CC0, AI hỗ trợ và tự làm. Ghi nguồn vào CREDITS.md.
Chỉ hoàn thiện demo
Không làm quảng cáo, mua trong ứng dụng hay online.
Concept: Farm Restoration – Màu sắc trở lại
Bạn thừa kế nông trại hoang tàn và xám xịt của ông, và mỗi mục tiêu hoàn thành sẽ "trả màu" cho nông trại, nên tiến trình hiện ra ngay trên màn hình thay vì chỉ là con số.
• Cốt truyện: lá thư ông để lại, cùng linh vật nhỏ tên Sprout dẫn dắt qua từng nhiệm vụ.
• Điểm độc đáo: cỏ mọc, hoa nở, bướm bay, đèn sáng theo từng mốc. Kỹ thuật rẻ: một biến shader toàn cục _Restore, kết hợp trọng số Volume và bật prop theo mốc.
• Độ dài: 5 ngày game, mỗi ngày 2–3 phút thực, tổng khoảng 10–15 phút (giả định, cần xác nhận).
Ngày
Mục tiêu
Điểm nhấn
1
Dọn cỏ và đá, cuốc, gieo, tưới
Tutorial do Sprout hướng dẫn
2
Chăm bò, vắt sữa
Chăn nuôi
3
Bán hàng, kiếm tiền, mua hạt giống, sửa hàng rào
Kinh tế cơ bản
4
Sửa cối xay gió (mục tiêu lớn), nhận đơn hàng có giới hạn thời gian
Thời tiết thử thách: hạn hán hoặc mưa
5
Lễ hội thu hoạch
Cảnh kết
Phương án đồ hoạ và quy trình Art Gate
Phong cách nhất quán đến từ palette, shader, ánh sáng và post-processing chứ không phải từ model, nên có thể trộn nguồn model nếu ép tất cả qua cùng một pipeline, và pipeline đó phải đủ nhẹ cho card Intel tích hợp.
Nguồn
Điểm mạnh
Điểm yếu
Dùng cho
Pack CC0 low-poly (Kenney, Quaternius, KayKit; Poly Pizza lọc CC0)
Sạch, nhẹ, nhân vật và động vật có rig, animation sẵn
Cần chọn 1–2 họ để đồng bộ
Khoảng 80%: cây, nhà, hàng rào, đồ nông trại, nhân vật
AI image-to-3D (Tripo, Meshy, Hunyuan3D, TRELLIS)
Ra nhanh các prop riêng biệt
Topology bẩn, không rig, texture lệch style, cần retexture
Vài prop đặc thù: cối xay, bảng hiệu, miếu nhỏ
AI ảnh 2D
Nhanh, đẹp
Cần kiểm tra điều khoản sử dụng
Moodboard, icon UI, skybox, texture
Tự làm trong Blender
Đồng bộ nhất
Tốn thời gian
Sửa nhỏ, prop còn thiếu
Khuyến nghị: phương án lai. Dùng CC0 làm nền, AI cho concept, ảnh 2D và vài prop đặc biệt. Không dùng AI để tạo nhân vật có animation. Cần thêm clip thì dùng Mixamo nếu còn khả dụng. Âm thanh lấy từ Freesound (lọc CC0), Kenney Audio, Pixabay. Kiểm tra điều khoản từng nguồn trước khi dùng và ghi hết vào CREDITS.md.
Art Gate: qua cổng này mới dựng hàng loạt
1. Art Target: dùng AI tạo 3–4 ảnh concept (nông trại low-poly ấm áp, hoàng hôn), chọn một ảnh. Rút ra palette 12–16 màu và 3 trạng thái: héo, phục hồi, lễ hội.
2. Chốt tối đa 2 họ asset (gợi ý Quaternius và Kenney vì cùng kiểu flat-color). Loại các asset hiện có bị lạc quẻ.
3. Look-dev slice: dựng một góc nông trại (nhà, ruộng, hàng rào, cây, nước) với đủ ánh sáng, post và shader, rồi so với Art Target. Đạt mới đi tiếp.
4. Công cụ Unify: script Editor gán lại toàn bộ model về khoảng 8–10 material URP dùng chung palette. Việc này thay cho 229 material hiện có, vừa đồng bộ màu vừa ít batch.
5. Dựng hàng loạt bằng script scatter (seed cố định), rồi chụp ảnh đối chiếu với Art Target.
Thông số để trông đẹp mà vẫn nhẹ trên GPU Intel
• Ánh sáng: một directional light realtime, bóng mềm, 1 cascade khoảng 25–30m, shadow map 1024. Ambient dạng gradient, sương mù nhẹ màu ấm. Ngày/đêm đổi gradient và nhiệt độ màu. Không bake lightmap vì xung đột với chu kỳ ngày đêm.
• Post (URP Volume): Bloom nhẹ chất lượng thấp, Color Adjustments hoặc LUT, Vignette, Tonemapping. Không dùng SSAO ở mức Low và Medium. Khử răng cưa bằng SMAA hoặc FXAA, kèm tuỳ chọn render scale khoảng 0,8 ở mức Low.
• Shader Graph (khoảng 4 shader, ít lần lấy mẫu texture): Lit theo palette, gió lay cho cỏ, cây và lúa bằng vertex animation, nước đơn giản (gradient, bọt, chảy, tránh refraction và depth texture), và "Restore" (lerp độ bão hoà theo _Restore).
• Môi trường: skybox gradient kèm mây, đồi viền ngoài không collider, tắt reflection probe realtime, bật SRP Batcher và GPU Instancing cho cây trồng.
Roadmap theo phase
Phase 1 và 2 chạy song song sau Phase 0 rồi hợp lại ở Phase 3, tổng khoảng 3–4 tuần cho một người có AI hỗ trợ (ước lượng).
• Phase 0: đo bằng Game view Stats, Profiler và Frame Debugger (batches, triangles, FPS), chụp screenshot, ghi compile warnings. Bắt buộc vì điểm đồ hoạ và hiệu năng trong audit chưa có số đo.
• Phase 1: chi tiết ở phần Art Gate phía trên. Hoàn thành khi ảnh look-dev đạt Art Target và bạn duyệt.
• Phase 2: hệ thống gồm chu kỳ ngày/đêm, event bus, inventory, tiền và bán hàng, quest bằng ScriptableObject, save/load JSON. UI gồm Main Menu, HUD, quest tracker, Pause, Settings, thông báo. Thêm hội thoại với Sprout và một NPC bán hàng. Hoàn thành khi chơi được từ menu đến hết ngày 5.
• Phase 3: thiết kế 5 ngày chơi, các vùng phục hồi, đơn hàng, thử thách thời tiết, tutorial, cảnh kết.
• Phase 4: VFX có object pooling (bụi đất, nước bắn, lấp lánh, thu hoạch, lá bay, bướm, mưa, floating text). Animation: vung công cụ, cây nảy khi mọc, bò phản ứng. Âm thanh: SFX theo từng sự kiện, nhạc nền ngày/đêm, ambience, tiếng bước chân, AudioMixer.
• Phase 5: 3 mức chất lượng Low, Medium, High, rà soát null-safety, test save hỏng, bug bash, build Windows, quay video demo.
Ngân sách hiệu năng cho laptop Intel tích hợp
Mức Low phải giữ được 30 FPS ổn định trên card Intel tích hợp. Các con số dưới đây là đề xuất, Phase 0 sẽ đo thực tế rồi chỉnh.
Chỉ số
Mục tiêu
FPS trong Unity Editor (Game view tối đa 1280×720)
≥ 30
FPS bản build Windows 1080p, mức Low (mặc định)
≥ 30, render scale khoảng 0,8
Mức Medium và High
Chỉ để thử, không cam kết FPS
Batches
≤ 250 (bật SRP Batcher, GPU Instancing cho cây trồng)
Triangles hiển thị
≤ 150k
Texture
Prop ≤ 512px, nền và skybox ≤ 1024px, tổng bộ nhớ texture < 300MB
Particle đồng thời
≤ 200, hạt nhỏ, ít lớp chồng nhau
Bóng đổ
Chỉ cho nhân vật, nhà, cây lớn; cỏ và lúa không đổ bóng
Post-processing ở mức Low
Tối đa 3 hiệu ứng: Bloom nhẹ, Color Adjustments kèm Tonemapping, Vignette
Thời gian load scene
< 5 giây
Rủi ro và lưu ý
Cần xử lý bốn việc trước khi dựng hàng loạt: license asset, số đo thực tế, GPU Intel và prop do AI tạo.
• License: xác nhận nguồn gốc PolytopeNature và các pack hiện có (mua, free hay lấy từ nơi khác) trước khi demo công khai.
• Thiếu số đo: điểm đồ hoạ và hiệu năng trong audit chưa dựa trên số liệu thật, nên Phase 0 là bắt buộc.
• GPU Intel: overdraw (cỏ, tán cây, particle) và bóng đổ là phần dễ vượt ngân sách nhất. Kiểm tra bằng Frame Debugger sau mỗi phase.
• Prop do AI tạo: phải retexture về palette chung và giảm poly, nếu không sẽ phá tính nhất quán và làm nặng cảnh.
Câu hỏi còn lại
Bốn câu dưới đây đều có mặc định, nên chỉ cần sửa chỗ nào khác ý bạn.
Câu hỏi
Mặc định
CPU và RAM của laptop là gì?
16GB RAM, CPU Intel Core i7 (theo trí nhớ, cần xác nhận)
Thời lượng demo?
10–15 phút, 5 ngày game
Giữ concept "Màu sắc trở lại" và cốt truyện thừa kế nông trại?
Giữ
Có cho phép bỏ hoặc thay các asset hiện có không hợp phong cách?
Có