# Bộ prompt thực thi Farm3D – Phase 0 đến 5

Oct 10, 2026 · @text in vietnamese

## Cách dùng

Chạy mỗi prompt trong một phiên AI riêng, tại thư mục gốc project Unity, theo thứ tự Phase 0, 1A, 1B, 2, 3, 4, 5; Phase 2 có thể chạy song song với Phase 1 trên nhánh git riêng.

1. Lưu "Khối ngữ cảnh chung" bên dưới thành `CLAUDE.md` ở thư mục gốc project (hoặc file rules tương đương nếu dùng Cursor hay Copilot). Nếu AI không tự đọc file này, dán khối đó vào đầu mỗi prompt.
2. Commit git trước khi bắt đầu mỗi phase để quay lại được.
3. Đóng Unity Editor khi AI chạy ở chế độ batchmode, vì Unity không cho hai tiến trình cùng mở một project.
4. Phase 1A dừng lại chờ bạn chọn ảnh Art Target và duyệt look-dev. Phase 1B chỉ chạy sau khi bạn duyệt.
5. Cuối mỗi phase, AI ghi báo cáo vào `Docs/PHASE_<số>_REPORT.md`. Dán báo cáo đó lại cho tôi để kiểm tra và chỉnh prompt của phase kế tiếp.

Mỗi prompt đã gắn tiêu chí hoàn thành. Nếu AI báo chưa đạt thì chưa chuyển sang phase sau.

## Khối ngữ cảnh chung

Khối này là bối cảnh cố định cho mọi phase; lưu thành `CLAUDE.md` để AI nạp mỗi lần, hoặc dán trước prompt.

```text
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
```

## Phase 0 – Baseline

Phase 0 chỉ đo và chỉ thêm công cụ đo, kết quả là `BASELINE.md` làm mốc so sánh cho mọi phase sau.

```text
Đọc CLAUDE.md (khối ngữ cảnh chung) trước khi làm.

# PHASE 0 – BASELINE (CHỈ ĐO, KHÔNG SỬA GAMEPLAY HAY ASSET)

Mục tiêu: có số đo thật về hiện trạng project để làm mốc so sánh. Hồ sơ audit trước đó chấm điểm đồ hoạ và hiệu năng khi chưa có số liệu, nên bước này là bắt buộc.

## Việc cần làm
1. Thống kê scene (Editor script, menu Tools/Farm/Baseline/Scan Scenes). Với mỗi scene trong Build Settings và mỗi scene dưới Assets/: số Renderer, tổng triangle và vertex của mesh đang bật, số material duy nhất, số shader duy nhất, số Light (realtime/baked/mixed) và kiểu shadow, số Particle System, số Collider và Rigidbody, số Terrain nếu có. Xuất Docs/Baseline/scene_stats.csv.
2. Thống kê asset (menu Tools/Farm/Baseline/Scan Assets):
   - Texture: số lượng, 20 texture lớn nhất (đường dẫn, kích thước, format import, có mipmap không, bộ nhớ ước tính) và tổng bộ nhớ ước tính.
   - Model: 20 model nặng nhất theo triangle.
   - Material: tổng số, số material dùng chung shader, các nhóm material trùng màu.
   - Audio: số clip, tổng dung lượng, load type.
   - Shader: đang dùng shader nào, có Shader Graph không.
   Xuất Docs/Baseline/asset_stats.csv và Docs/Baseline/asset_top20.md.
3. Cài đặt render hiện tại, ghi vào BASELINE.md: URP Asset đang dùng ở từng Quality level, shadow distance, số cascade, shadow resolution, render scale, MSAA, SRP Batcher, post-processing và Volume đang có, texture quality, V-Sync, target frame rate.
4. Overlay đo runtime (script PerfOverlay, phím F3 bật/tắt, chỉ chạy trong Editor và Development Build): FPS trung bình và 1% low, frame time (ms), CPU/GPU frame time nếu FrameTimingManager hỗ trợ, và ProfilerRecorder cho Draw Calls Count, SetPass Calls Count, Batches Count, Triangles Count, Vertices Count, System Used Memory, Total Used Memory. Không cấp phát bộ nhớ mỗi frame. Có tuỳ chọn ghi CSV mỗi giây vào Application.persistentDataPath/perf_log.csv.
5. Công cụ chụp ảnh (phím F12 hoặc menu): lưu screenshot 1920x1080 vào Docs/Screens/ kèm timestamp để so sánh trước và sau.
6. Compile và console: compile ở batchmode (hoặc đọc log), ghi mọi error và warning vào Docs/Baseline/compile_log.txt, kèm tóm tắt số lượng theo nhóm.
7. License sơ bộ: quét các thư mục asset bên thứ ba (PolytopeNature, CartoonCropVisuals và các pack khác) tìm LICENSE, README hoặc thông tin Asset Store. Lập bảng: pack | nguồn | license | ghi chú. Không tìm thấy thì ghi CHƯA XÁC ĐỊNH, không suy đoán.
8. Quét anti-pattern (chỉ báo cáo, không sửa, ghi file:dòng): Find, GetComponent, FindObjectOfType trong Update/FixedUpdate/LateUpdate; Instantiate/Destroy trong vòng lặp; Resources.Load; LINQ và nối chuỗi trong đường chạy nóng; event đăng ký mà không huỷ.

## Phần người dùng làm thủ công (AI không làm được)
Soạn sẵn hướng dẫn tối đa 10 dòng trong BASELINE.md: chạy scene chính trong Editor, đứng giữa nông trại 60 giây với PerfOverlay bật, ghi FPS, batches, triangles; mở Profiler (CPU, GPU, Memory) và Frame Debugger; lặp lại với bản build Windows (Development Build). Để trống các ô số liệu này cho người dùng điền.

## Sản phẩm bàn giao
- Docs/BASELINE.md: bảng tổng hợp (scene, asset, cài đặt render, anti-pattern, license), các ô người dùng cần điền, và 5 điểm nghẽn có khả năng cao nhất trên GPU Intel tích hợp (kèm bằng chứng).
- Các file trong Docs/Baseline/ và Docs/Screens/.
- Script runtime mới đặt trong Assets/FarmRestoration/Tools, script editor mới đặt trong Assets/FarmRestoration/Editor/Tools.
- Docs/PHASE_0_REPORT.md theo quy tắc số 7.

## Tiêu chí hoàn thành
- Compile sạch lỗi. Không có file gameplay hay asset nào bị sửa (git diff chỉ có file mới).
- Mọi mục trên có số liệu hoặc ghi rõ CHƯA XÁC ĐỊNH.
- BASELINE.md đủ hướng dẫn để người dùng điền các số đo thủ công trong 15 phút.
```

## Phase 1A – Art Gate (trước cổng duyệt)

Phase 1A kết thúc ở cổng duyệt: AI dừng để bạn chọn ảnh Art Target và duyệt look-dev trước khi dựng hàng loạt.

```text
Đọc CLAUDE.md trước khi làm. Đọc Docs/BASELINE.md nếu đã có.

# PHASE 1A – ART GATE (TRƯỚC CỔNG DUYỆT)

Mục tiêu: chốt phong cách và chứng minh nó đẹp, nhẹ trên GPU Intel qua MỘT góc nông trại (look-dev slice) trước khi dựng hàng loạt. Ở phase này tuyệt đối KHÔNG dựng hàng loạt và KHÔNG thay material toàn project.

## Bước 1 – Art Target (cần người dùng)
Kiểm tra thư mục Assets/FarmRestoration/Art/Target/.
- Nếu CHƯA có ảnh: dừng ở bước này. Viết Docs/ART_TARGET_BRIEF.md gồm: (a) 4 prompt tạo ảnh concept bằng AI ảnh (nông trại low-poly flat-color ấm áp; ba trạng thái: héo xám, đang phục hồi, lễ hội hoàng hôn; góc nhìn 3/4 từ trên cao giống camera game; không chữ, không logo, không nhân vật có bản quyền); (b) checklist 8 tiêu chí chọn ảnh (palette đồng bộ, hình khối đơn giản dựng được bằng low-poly, ánh sáng ấm, độ tương phản, dễ đọc ở độ phân giải thấp...). Sau đó báo người dùng "Đang chờ Art Target" và kết thúc.
- Nếu ĐÃ có ảnh: đọc ảnh và làm tiếp.

## Bước 2 – Art Bible
Từ ảnh Art Target, viết Docs/ART_BIBLE.md: palette 12–16 màu (mã hex, tên, vai trò: cỏ, đất, nước, gỗ, mái, nhấn...), 3 bảng biến thể màu cho trạng thái héo, phục hồi, lễ hội; quy tắc hình khối; quy tắc ánh sáng (hướng nắng, nhiệt độ màu theo giờ); quy tắc UI (màu, font, nút phẳng). Ghi rõ phần nào là suy luận từ ảnh. Tạo ScriptableObject FarmPalette và một texture palette (ví dụ 16x4 điểm ảnh, filter Point, không nén, không mipmap).

## Bước 3 – Kiểm kê asset hiện có
Tạo Docs/ASSET_AUDIT.md: gom asset (model, material, texture) theo pack hoặc họ phong cách; đánh dấu từng nhóm HỢP / CẦN SỬA / LẠC QUẺ kèm lý do và ảnh chụp; số prefab thực sự dùng trong scene so với tổng số; đề xuất tối đa 2 họ asset để giữ. Chỉ đề xuất nguồn CC0 thay thế (Kenney, Quaternius, KayKit, Poly Pizza lọc CC0), nêu tên pack và URL, không tải nếu chưa chắc license.

## Bước 4 – Shader (viết bằng HLSL URP, không dùng Shader Graph vì AI khó tạo file .shadergraph chính xác)
Đặt trong Assets/FarmRestoration/Shaders/. Tất cả tương thích SRP Batcher (CBUFFER UnityPerMaterial) và GPU Instancing:
- FarmLitPalette: lit đơn giản, màu lấy từ texture palette theo UV hoặc vertex color, 1 directional light cộng ambient gradient, nhận bóng; không normal map, không reflection.
- FarmFoliageWind: như trên, thêm sway bằng vertex animation (biên độ, tốc độ, trọng số theo độ cao hoặc vertex color) và tham số gió toàn cục.
- FarmWater: nước phẳng đơn giản (gradient, bọt viền bằng texture noise nhỏ, chảy bằng scroll UV). KHÔNG refraction, KHÔNG depth texture, KHÔNG reflection.
- Thành phần Restore: biến toàn cục _Restore (0–1) làm giảm độ bão hoà và đẩy màu về tông xám ấm; tích hợp vào cả 3 shader qua một file include chung. Script RestoreGlobals gọi Shader.SetGlobalFloat, có slider debug trong Inspector.
Mỗi shader có Material mẫu trong Assets/FarmRestoration/Materials/Shared/.

## Bước 5 – Ánh sáng, URP, post-processing
- Tạo 3 URP Asset: FarmURP_Low (mặc định), FarmURP_Medium, FarmURP_High, gán vào Quality Settings tương ứng. Low: render scale 0.8, shadow distance 25–30, 1 cascade, shadow resolution 1024, soft shadow mức thấp, MSAA tắt, SRP Batcher bật, không SSAO. Medium: render scale 1.0, 2 cascade, shadow 40m, 2048. High: để thử, không cam kết FPS.
- Volume profile FarmPost_Default: Bloom nhẹ chất lượng thấp, Color Adjustments hoặc LUT, Tonemapping, Vignette. Không SSAO ở Low và Medium.
- Ánh sáng: 1 directional light realtime (không bake lightmap), ambient dạng gradient, fog nhẹ màu ấm, skybox gradient. Không dùng reflection probe realtime.

## Bước 6 – Look-dev slice
Tạo scene Assets/FarmRestoration/Scenes/LookDev_Corner.unity: một góc nông trại (nhà, vài luống đất, hàng rào, 3–5 loại cây, một vũng nước, vài prop, 1 con bò, nhân vật) dựng từ asset hiện có hoặc primitive tạm, tất cả dùng shader và palette mới.
- Viết công cụ Tools/Farm/Art/Unify Materials (dry-run mặc định, xuất CSV: prefab, material cũ, material mới). CHỈ áp dụng cho các prefab dùng trong look-dev slice. Gán 8–10 material dùng chung thay cho material riêng lẻ. Texture riêng của pack thì giữ, tạo một material dùng chung cho mỗi texture thay vì mỗi prefab.
- Chụp ảnh 1920x1080 ở 3 trạng thái (_Restore = 0, 0.5, 1) và 2 mức chất lượng (Low, Medium) vào Docs/Screens/lookdev_*.png. Đặt cạnh ảnh Art Target và liệt kê các khác biệt còn lại theo thứ tự ảnh hưởng.
- Đo bằng PerfOverlay (nếu có): FPS, batches, triangles ở mức Low. Ô nào chưa đo được thì để trống cho người dùng.

## Bước 7 – CREDITS
Tạo hoặc cập nhật CREDITS.md cho mọi asset ngoài đã dùng.

## Dừng ở cổng
Kết thúc bằng câu: "Cổng Art Gate: chờ duyệt look-dev." Không làm gì thêm. Ghi Docs/PHASE_1A_REPORT.md.

## Tiêu chí hoàn thành (cổng A)
- Có Art Target, ART_BIBLE.md, ASSET_AUDIT.md, 4 shader, 3 URP Asset, scene LookDev_Corner và ảnh chụp.
- Compile sạch. Scene look-dev đạt ngân sách Low (batches <= 250, triangles <= 150k) hoặc báo rõ vượt ở đâu.
- Người dùng duyệt look-dev (AI không tự duyệt).
```

## Phase 1B – Dựng hàng loạt sau khi duyệt

Chỉ chạy Phase 1B sau khi bạn đã duyệt look-dev; prompt này áp phong cách đã chốt cho toàn bộ cảnh chính mà vẫn giữ ngân sách hiệu năng.

```text
Đọc CLAUDE.md, Docs/ART_BIBLE.md, Docs/ASSET_AUDIT.md trước khi làm. Chỉ chạy khi người dùng đã xác nhận trong chat là đã duyệt look-dev. Nếu người dùng có yêu cầu chỉnh look-dev thì làm các chỉnh đó trước.

# PHASE 1B – DỰNG HÀNG LOẠT THEO ART GATE ĐÃ DUYỆT

Mục tiêu: áp phong cách đã duyệt cho toàn bộ nông trại (scene chính của demo), giữ nguyên ngân sách hiệu năng.

## Việc cần làm
1. Xác định scene chính của demo (hỏi người dùng nếu có nhiều scene). Làm việc trên bản sao Farm_Main.unity, giữ nguyên scene gốc. Nếu cần layout nền, chạy các script Setup.cs hiện có đúng MỘT lần rồi lưu kết quả vào scene hoặc prefab; sau khi đã chỉnh tay thì không sinh lại.
2. Unify Materials toàn project: chạy công cụ ở chế độ dry-run, xuất báo cáo, tóm tắt cho người dùng (số material trước và sau, số prefab bị đổi). Sau đó áp dụng trên bản sao prefab hoặc bằng override trong scene Farm_Main, không sửa asset gốc của pack. Mục tiêu: scene Farm_Main còn khoảng 8–15 loại material dùng chung (ngoài các material có texture riêng).
3. Xử lý asset LẠC QUẺ theo ASSET_AUDIT.md: ưu tiên loại khỏi scene hơn là tải mới. Asset mới chỉ lấy từ nguồn CC0. Không tải được thì đặt placeholder (primitive cùng kích thước, đúng palette) và ghi vào mục "Asset cần người dùng bổ sung" trong báo cáo, kèm từ khoá tìm kiếm và URL pack gợi ý.
4. Công cụ scatter (Tools/Farm/Art/Scatter): rải cây, đá, cỏ, hoa, bụi theo vùng (BoxVolume hoặc mask đơn giản), seed cố định, mật độ chỉnh được, bỏ qua vùng đã có vật thể (kiểm tra overlap), tránh ruộng và đường đi. Mỗi lần rải tạo một ScatterGroup_* riêng để xoá hoặc chạy lại từng nhóm. Vật thể lặp bật GPU Instancing, vật thể tĩnh đánh dấu Static. Cỏ và hoa không đổ bóng.
5. Môi trường: skybox gradient kèm mây, đồi viền ngoài không collider, nước dùng FarmWater, ánh sáng và Volume theo ART_BIBLE. Tạo ScriptableObject TimeOfDayProfile cho 3 trạng thái (sáng, hoàng hôn, đêm): gradient ambient, màu nắng, cường độ, fog. Chỉ tạo dữ liệu; hệ thống ngày/đêm làm ở Phase 2.
6. Đặt các điểm cố định cho 5 ngày game với tên GameObject chuẩn: nhà, chuồng bò, khu ruộng, hàng rào, đường mòn, cối xay gió (placeholder đúng kích thước nếu chưa có model), bảng hiệu, chỗ cho quầy bán hàng. Chia nông trại thành ít nhất 5 RestoreZone (GameObject rỗng, trigger collider, danh sách prop sẽ bật khi phục hồi). Chỉ tạo khung, logic làm ở Phase 3.
7. Tối ưu: Static batching, occlusion culling nếu đo thấy có lợi (đo trước và sau), LOD đơn giản cho prop nặng, tắt shadow casting cho vật nhỏ, texture <= 512px (prop) và <= 1024px (nền).
8. Đo lại ở mức Low bằng PerfOverlay: FPS, batches, triangles, bộ nhớ. Chụp ảnh ở 3 trạng thái _Restore và 3 trạng thái ánh sáng. So với Art Target và BASELINE.md.

## Sản phẩm bàn giao
- Farm_Main.unity hoàn chỉnh về hình ảnh, ảnh chụp ở Docs/Screens/farm_*.png.
- Báo cáo trước và sau: số material, batches, triangles, bộ nhớ texture, FPS.
- CREDITS.md cập nhật, danh sách "Asset cần người dùng bổ sung".
- Docs/PHASE_1B_REPORT.md.

## Tiêu chí hoàn thành
- Cảnh chính thống nhất với Art Target ở cả 3 trạng thái _Restore; không còn asset LẠC QUẺ nhìn thấy được.
- Mức Low: batches <= 250, triangles <= 150k, bộ nhớ texture < 300MB, hoặc báo rõ vượt ở đâu và vì sao.
- Compile sạch, console không lỗi khi chạy scene. Các tương tác hiện có (cuốc, gieo, tưới, thu hoạch, chăm bò) vẫn hoạt động.
```

## Phase 2 – Lõi game

Phase 2 làm hệ thống để chơi được từ Main Menu đến hết ngày 5 với nội dung tạm; nội dung chi tiết để Phase 3.

```text
Đọc CLAUDE.md trước khi làm. Đọc code hiện có ở Assets/FarmRestoration/Scripts (PlayerToolController, FarmPlot, CowAnimal, FarmAudioFeedback, FarmGrowthDemo, các thư mục Progression, UI, World) trước khi thiết kế.

# PHASE 2 – LÕI GAME

Mục tiêu: chơi được vòng lặp từ Main Menu đến hết ngày 5 với nội dung tạm: có thời gian, kinh tế, nhiệm vụ, lưu và tải, UI hoàn chỉnh. Phase này làm hệ thống; nội dung chi tiết làm ở Phase 3.
Nếu chạy song song với Phase 1: làm trên nhánh git riêng, không đụng vào shader, material hay scene hình ảnh; chỉ thêm scene và prefab mới.

## Nguyên tắc kiến trúc
- Tái sử dụng module có sẵn, thêm lớp mỏng nối vào chúng (ví dụ FarmPlot phát sự kiện khi cuốc, gieo, tưới, thu hoạch thay vì để hệ thống khác đọc trực tiếp).
- Thư mục mới: Scripts/Core, Scripts/Economy, Scripts/Quests, Scripts/Save, Scripts/Dialogue; mở rộng Scripts/UI. Namespace FarmRestoration.*.
- EventBus nhẹ (event hoặc struct event, không cấp phát mỗi lần phát). Mọi đăng ký event phải huỷ trong OnDisable.
- Dữ liệu trong ScriptableObject: ItemDefinition, CropDefinition (nối với CropVisuals hiện có), QuestDefinition, OrderDefinition, DialogueDefinition, DayDefinition (khung rỗng, điền ở Phase 3), TimeOfDayProfile (dùng bản của Phase 1B nếu đã có, chưa có thì tạo bản tối thiểu).
- Singleton chỉ cho dịch vụ toàn cục (GameServices), không phụ thuộc chéo vòng.
- Input: dùng Input System 1.20 với Input Actions asset: Move, Look, Interact, UseTool, NextTool, PrevTool, Pause, Inventory, DialogueNext. Hỗ trợ bàn phím, chuột và tay cầm.

## Hệ thống cần làm
1. GameClock và chu kỳ ngày/đêm: 5 ngày, độ dài mỗi ngày chỉnh được (mặc định 150 giây thực), giờ trong game, sự kiện OnDayStart, OnDayEnd, OnHourChanged. Xoay directional light và đổi màu, ambient, fog theo TimeOfDayProfile. Cuối ngày hiện màn hình tổng kết (tiền kiếm được, nhiệm vụ xong) rồi sang ngày mới. Tạm dừng khi mở menu hoặc hội thoại.
2. Inventory và Hotbar: ô chứa có số lượng, stack, thêm/bớt, hotbar chọn công cụ và hạt giống (nối với PlayerToolController), giới hạn nhỏ (ví dụ 12 ô).
3. Economy: Wallet, ShopStall (mua hạt giống, bán nông sản), SellBox (thả đồ vào, bán tức thì hoặc cuối ngày), giá nằm trong ItemDefinition, thông báo khi tiền thay đổi. Không để tiền âm, không nhân đôi vật phẩm.
4. Quest: QuestDefinition chứa danh sách Objective với các loại PlantCount, WaterCount, HarvestCount (theo loại cây), MilkCow, EarnMoney, DeliverOrder, RepairObject, TalkTo. QuestManager lắng nghe EventBus, cập nhật tiến độ, hoàn thành thì trao thưởng và mở quest kế tiếp (chuỗi). Quest tracker trên HUD.
5. Order Board (khung): đơn hàng giới hạn thời gian, yêu cầu vật phẩm, tiền thưởng, dữ liệu OrderDefinition. Nội dung điền ở Phase 3.
6. Dialogue: DialogueDefinition (người nói, dòng thoại, tối đa 2 lựa chọn, sự kiện kích hoạt), DialogueRunner hiển thị hộp thoại, phím để tiếp. Hai nhân vật: Sprout (linh vật) và một NPC bán hàng; dùng placeholder nếu chưa có model.
7. Save/Load: JSON có version, lưu ngày/giờ, tiền, inventory, trạng thái ô đất (loại cây, giai đoạn, độ ẩm), vị trí và trạng thái bò, quest, RestoreProgress (khung), cài đặt. Ghi an toàn: ghi ra file tạm rồi đổi tên, giữ một bản .bak, kiểm tra schema version. File hỏng thì thử .bak, vẫn hỏng thì báo người dùng và bắt đầu game mới, không crash. Tự lưu cuối ngày. Main Menu có nút Continue.
8. UI bằng UGUI (theo ART_BIBLE nếu đã có, chưa có thì dùng style phẳng trung tính, dễ đổi): Main Menu (New Game, Continue, Settings, Quit) ở scene riêng MainMenu.unity; HUD (giờ, ngày, tiền, hotbar, quest tracker, gợi ý phím); Pause (Resume, Settings, Về menu); Settings (âm lượng Master/Music/SFX, chất lượng Low/Medium/High, toàn màn hình, độ phân giải, lưu bền); Toast thông báo; màn hình tổng kết ngày; màn hình kết thúc demo. Canvas Scaler kiểu Scale With Screen Size, chuẩn 16:9 và không vỡ ở 16:10.
9. Luồng scene: MainMenu, Farm_Main (hoặc scene chính hiện tại), EndDemo, chuyển bằng SceneManager.LoadSceneAsync kèm màn hình loading đơn giản. Thêm tất cả vào Build Settings.
10. Không thêm Resources.Load mới. Resources.Load cũ trong FarmAudioFeedback.cs để Phase 4 xử lý.

## Kiểm thử
- Unity Test Framework (EditMode và PlayMode) cho: Economy (mua bán, không âm tiền), Inventory (stack, đầy ô), Quest (hoàn thành, chuỗi), Save/Load (khứ hồi; file rỗng; file cắt cụt; version sai; thiếu trường), GameClock (qua ngày). Chạy ở batchmode và ghi kết quả.
- Một kịch bản chơi thử tự động (PlayMode): New Game, cuốc/gieo/tưới/thu hoạch 1 ô, bán, hoàn thành 1 quest, lưu, thoát, Continue, kiểm tra trạng thái giống nhau.

## Sản phẩm bàn giao
- Code, ScriptableObject mẫu, 2–3 quest và 1–2 đơn hàng mẫu để thử, MainMenu.unity và các prefab UI.
- Docs/CORE_SYSTEMS.md: sơ đồ ngắn các hệ thống và cách thêm một vật phẩm, quest, đơn hàng, đoạn thoại mới trong 5 bước (viết cho người không biết code).
- Docs/PHASE_2_REPORT.md.

## Tiêu chí hoàn thành
- Chơi liền mạch từ Main Menu đến hết ngày 5 với nội dung tạm; lưu và tải đúng trạng thái.
- Bộ test tự động chạy xanh; file save hỏng không làm crash game.
- Hệ thống mới không có Find/GetComponent trong Update và không cấp phát bộ nhớ mỗi frame (kiểm tra bằng Profiler hoặc rà code).
- Compile sạch, console không lỗi.
```

## Phase 3 – Nội dung 5 ngày và Restoration

Phase 3 biến lõi game thành một demo có cốt truyện, nhịp độ và cảm giác "màu sắc trở lại", với mọi nội dung nằm trong ScriptableObject để người không biết code chỉnh được.

```text
Đọc CLAUDE.md, Docs/ART_BIBLE.md, Docs/CORE_SYSTEMS.md trước khi làm. Chỉ chạy sau khi Phase 1B và Phase 2 hoàn thành; nếu thiếu thì báo rõ phần nào bị chặn.

# PHASE 3 – NỘI DUNG 5 NGÀY VÀ HỆ THỐNG RESTORATION

Mục tiêu: biến lõi game thành demo có cốt truyện, nhịp độ và cảm giác "màu sắc trở lại". Mọi nội dung nằm trong ScriptableObject để người không biết code chỉnh được.

## Cốt truyện (giữ ý, có thể viết thoại hay hơn)
Người chơi thừa kế nông trại hoang tàn, xám xịt của ông. Linh vật Sprout dẫn dắt qua lá thư ông để lại. Mỗi mục tiêu hoàn thành "trả màu" cho nông trại: cỏ mọc, hoa nở, bướm bay, đèn sáng. Giọng văn ấm, ngắn, tiếng Việt tự nhiên; mỗi lượt thoại tối đa 140 ký tự.

## Hệ thống Restoration
1. RestoreController: giá trị RestoreProgress 0–1 (lưu trong save), đẩy xuống shader bằng Shader.SetGlobalFloat("_Restore"), chuyển mượt, đồng bộ trọng số Volume (tông màu, bloom) với FarmPost_Default.
2. RestoreZone (khung đã có từ Phase 1B): mỗi vùng có ngưỡng kích hoạt. Khi đạt thì bật nhóm prop (hoa, cỏ cao, bướm, đèn lồng, hàng rào đã sửa...) bằng hiệu ứng nảy lên (scale từ 0 theo animation curve) và ghi vào save. Có công cụ Inspector với slider mô phỏng tiến độ để xem trước mà không cần chơi.
3. Gắn tiến độ Restore vào quest hoàn thành, mốc ngày và mục tiêu lớn (cối xay gió).

## Nội dung 5 ngày (DayDefinition, QuestDefinition, DialogueDefinition, OrderDefinition)
- Ngày 1: dọn cỏ và đá (vật thể tương tác nhặt hoặc phá được), cuốc, gieo, tưới. Tutorial theo ngữ cảnh do Sprout hướng dẫn: gợi ý hiện đúng lúc, tự biến mất khi người chơi làm đúng, không chặn thao tác. Cuối ngày: vùng 1 phục hồi.
- Ngày 2: chăm bò, vắt sữa. Cuối ngày: vùng chuồng bò phục hồi.
- Ngày 3: bán hàng, kiếm tiền, mua hạt giống mới, sửa hàng rào (RepairObject). Giới thiệu NPC bán hàng. Cuối ngày: vùng đường mòn và hàng rào phục hồi.
- Ngày 4: sửa cối xay gió (mục tiêu lớn) chia 3 bước (gom vật liệu, sửa, khởi động) với trạng thái hình ảnh từng bước; đơn hàng có giới hạn thời gian; một thử thách thời tiết (mưa hoặc hạn hán). Cuối ngày: vùng cối xay phục hồi.
- Ngày 5: lễ hội thu hoạch: nông trại phục hồi hoàn toàn, hoàng hôn, đèn sáng, NPC tụ họp; nhiệm vụ cuối ngắn; cảnh kết (camera lướt qua nông trại, thư của ông, lời cảm ơn); màn hình EndDemo.
Mỗi ngày có 2–4 nhiệm vụ, thời lượng thực 2–3 phút, độ khó tăng dần; tổng demo 10–15 phút.

## Thời tiết (nhẹ)
WeatherSystem với các trạng thái Clear, Rain, Drought theo DayDefinition. Rain: ô đất tự được tưới, đổi ambient và fog. Drought: ô đất khô nhanh hơn, người chơi phải tưới nhiều hơn. Hạt mưa để Phase 4 làm; phase này chỉ cần sự kiện OnWeatherChanged và thay đổi ánh sáng.

## Thế giới tương tác và sinh động (chọn việc rẻ, hiệu quả)
- Sprout đi theo người chơi (NavMesh hoặc cách đơn giản), tự đưa gợi ý khi người chơi đứng yên quá 15 giây, phản ứng khi hoàn thành quest.
- Vật thể tương tác: cỏ dại và đá dọn được, hòm thư để đọc thư, bảng thông báo xem quest, thùng bán hàng.
- Môi trường có sự sống: bò đi lại và phản ứng khi người chơi lại gần; chim hoặc bướm bay bằng animation đơn giản (vertex animation hoặc spline), chỉ bật ở vùng đã phục hồi.
- Mọi vật thể tương tác hiện gợi ý phím khi lại gần và có highlight nhẹ.

## Công cụ cho người không phải Level Designer
- Tools/Farm/Content/Validate: kiểm tra mọi DayDefinition, Quest, Dialogue, Order không thiếu tham chiếu, không có vòng lặp quest, không trỏ tới vật phẩm không tồn tại; xuất danh sách lỗi.
- Tools/Farm/Content/Simulate: ước tính thời lượng từng nhiệm vụ bằng tham số (thời gian trung bình mỗi hành động) thay vì chơi thật, cho biết ngày nào quá chặt so với độ dài ngày.
- Docs/CONTENT_GUIDE.md: cách chỉnh nội dung ngày, thoại, đơn hàng, vùng phục hồi.

## Sản phẩm bàn giao
- Dữ liệu 5 ngày, RestoreController, WeatherSystem, thoại, cảnh kết, công cụ Validate và Simulate.
- Docs/CONTENT_GUIDE.md và Docs/PHASE_3_REPORT.md (kèm bảng: ngày | nhiệm vụ | thời lượng ước tính | vùng phục hồi).

## Tiêu chí hoàn thành
- Một lượt chơi liên tục từ New Game đến EndDemo mất 10–15 phút (dựa trên Simulate và một lượt chạy thử).
- Validate không có lỗi; Simulate cho thấy cả 5 ngày hoàn thành được.
- Trạng thái Restore và các vùng được lưu và tải đúng.
- Hiệu năng mức Low vẫn trong ngân sách (batches <= 250, triangles <= 150k).
- Compile sạch, console không lỗi.
```

## Phase 4 – Game feel

Phase 4 làm cho mỗi hành động có phản hồi nhìn, nghe và cảm nhận được, trong ngân sách hiệu năng của GPU Intel tích hợp.

```text
Đọc CLAUDE.md, Docs/ART_BIBLE.md, Docs/CORE_SYSTEMS.md, Docs/CONTENT_GUIDE.md trước khi làm. Chỉ chạy sau Phase 3.

# PHASE 4 – GAME FEEL: VFX, ANIMATION, ÂM THANH

Mục tiêu: mỗi hành động của người chơi đều có phản hồi nhìn thấy, nghe thấy, cảm nhận được, trong ngân sách hiệu năng của GPU Intel tích hợp.

## 1. Hạ tầng
- PoolManager tổng quát (prefab -> hàng đợi), Get/Release, prewarm theo cấu hình, không Instantiate/Destroy lúc chơi, tự thu hồi sau thời gian sống.
- VfxService.Play(VfxId, position, rotation, scale) dùng PoolManager. VfxLibrary (ScriptableObject) ánh xạ VfxId -> prefab. Giới hạn tổng số particle đồng thời <= 200, bỏ qua hiệu ứng ưu tiên thấp khi vượt.
- Để sẵn tham số ParticleMultiplier mà QualityManager (Phase 5) sẽ dùng để nhân số hạt theo mức chất lượng.

## 2. VFX
Dùng Particle System, material Particles/Unlit hoặc shader đơn giản dùng chung một texture atlas <= 512px; hạt nhỏ, ít lớp chồng nhau để tránh overdraw. Gắn vào sự kiện qua EventBus:
- Cuốc đất: bụi và mảnh đất văng. Gieo: vài hạt nảy. Tưới: giọt nước bắn, ô đất tối màu dần. Thu hoạch: lấp lánh và lá bay, vật phẩm bay vào túi.
- Cây sang giai đoạn mới: vòng sáng nhẹ hoặc lá xoay, cây nảy (scale curve).
- Bán hàng: tiền bay lên kèm ánh sáng. Hoàn thành quest và Restore: ánh sáng nhẹ, hoa nở, bướm.
- Môi trường: mưa (hạt nhỏ bám theo camera, mật độ thấp), bụi nắng, lá rơi, đom đóm ban đêm (tắt ở Low nếu cần), khói ống khói.
- Floating text (số tiền, +1 vật phẩm) bằng pool, chọn cách rẻ hơn giữa TextMeshPro và Canvas world-space.
- Phản hồi va chạm và vật lý: tiếng và bụi khi chân chạm đất, bò đụng hàng rào, vật phẩm thả xuống có nảy, camera shake nhẹ (có hệ số giảm và tắt được trong Settings), hit stop cực ngắn cho thao tác mạnh nếu phù hợp.

## 3. Animation
- Người chơi: idle, đi, chạy, cuốc, tưới, gieo, thu hoạch, nhặt, bán. Dùng clip có sẵn trong asset đã chọn, retarget Humanoid nếu cần, blend tree cho di chuyển, layer riêng cho thao tác công cụ để vừa đi vừa vung. Thiếu clip thì liệt kê, đề xuất nguồn (Mixamo nếu còn khả dụng, tự kiểm tra điều khoản) hoặc dùng animation thủ tục (tween) tạm.
- Bò: idle, đi, gặm cỏ, nhìn người chơi, phản ứng khi vắt sữa.
- Sprout: lơ lửng và nhún (procedural), biểu cảm khi nói và khi vui.
- Vật thể: cây nảy khi mọc, cửa và rào mở, cối xay quay (tốc độ theo trạng thái sửa), cờ và vải đung đưa (vertex animation), nước chảy bằng shader.
- UI: nảy nhẹ, fade, số tiền chạy số.
- Chỉ dùng Animator hoặc tween nhẹ; không dùng Cloth thật hay IK tốn CPU nếu không cần.

## 4. Âm thanh
- AudioManager: pool AudioSource, kênh SFX, Music, Ambience; AudioMixer với nhóm Master, Music, SFX, Ambience; mở tham số âm lượng cho Settings (đổi slider tuyến tính sang dB), lưu cài đặt.
- AudioLibrary (ScriptableObject) giữ tham chiếu trực tiếp tới AudioClip, hỗ trợ biến thể ngẫu nhiên (pitch và volume lệch nhẹ, 2–4 biến thể cho sự kiện quan trọng) và cooldown chống chồng tiếng. THAY toàn bộ Resources.Load trong FarmAudioFeedback.cs (khoảng dòng 158) bằng AudioLibrary, giữ nguyên hành vi cũ.
- SFX theo sự kiện: rà mọi sự kiện trong EventBus, không để sự kiện nào bị "câm". Tối thiểu: cuốc, gieo, tưới, thu hoạch, bước chân (theo bề mặt đơn giản bằng tag), nhặt, bán, tiền, mua, hoàn thành quest, restore, UI (click, hover, mở, đóng), bò kêu, mưa, gió, cối xay, cửa và rào.
- Nhạc nền: 1 bản ban ngày, 1 bản hoàng hôn/đêm, 1 bản lễ hội và cảnh kết, 1 bản menu. Crossfade theo giờ trong ngày. Ambience nền (chim ban ngày, dế ban đêm, gió). Giảm nhạc nhẹ (ducking) khi hội thoại.
- Âm thanh 3D chỉ cho nguồn có vị trí (bò, cối xay, nước), phần còn lại 2D. Giới hạn số voice đồng thời.
- Nguồn: chỉ CC0 hoặc license cho phép dùng có ghi công (Freesound lọc CC0, Kenney Audio, Pixabay; kiểm tra điều khoản từng file). Không tải được thì tạo đủ slot trống trong AudioLibrary và ghi Docs/AUDIO_TODO.md: sự kiện | từ khoá tìm | nguồn gợi ý | thời lượng mong muốn. Ghi CREDITS.md cho mọi file đã dùng.
- Định dạng: SFX ngắn dùng Vorbis hoặc ADPCM, Load In Background; nhạc dùng Streaming; SFX 3D dùng mono.

## Kiểm tra
- Đo bằng PerfOverlay ở cảnh đông hiệu ứng nhất (mưa, thu hoạch, restore cùng lúc): phải trong ngân sách; particle đồng thời không vượt 200; GC alloc khi phát hiệu ứng bằng 0 (xác nhận bằng Profiler).
- Lập bảng sự kiện | VFX | SFX | animation, tìm ô trống và lấp, hoặc ghi lý do chưa lấp.

## Sản phẩm bàn giao
- PoolManager, VfxService, VfxLibrary, AudioManager, AudioLibrary, AudioMixer, các prefab VFX, các Animator controller.
- Docs/FEEL_MATRIX.md, Docs/AUDIO_TODO.md (nếu còn thiếu), CREDITS.md cập nhật.
- Docs/PHASE_4_REPORT.md.

## Tiêu chí hoàn thành
- Mỗi hành động chính (cuốc, gieo, tưới, thu hoạch, bán, hoàn thành quest, restore) có đủ VFX, SFX và animation.
- Particle đồng thời <= 200; không Instantiate/Destroy khi chơi; hệ thống mới không GC alloc mỗi frame.
- Không còn Resources.Load trong FarmAudioFeedback.cs; nhạc nền đổi mượt theo thời gian trong ngày.
- Mức Low vẫn trong ngân sách; compile sạch, console không lỗi.
```

## Phase 5 – Polish và ổn định

Phase 5 không thêm tính năng mới, chỉ sửa lỗi, tối ưu theo số đo và đóng gói bản build Windows để demo trên laptop Intel tích hợp.

```text
Đọc CLAUDE.md và mọi file Docs/PHASE_*_REPORT.md trước khi làm. Chỉ chạy sau Phase 4.

# PHASE 5 – POLISH, ỔN ĐỊNH, BUILD DEMO

Mục tiêu: demo chạy mượt, không crash, sẵn sàng quay video và trình diễn trên laptop Intel tích hợp. KHÔNG thêm tính năng mới; chỉ sửa lỗi, tối ưu, hoàn thiện. Không làm gì cho Android/APK.

## 1. Quality settings
- QualityManager áp dụng mức Low, Medium, High: URP Asset tương ứng (từ Phase 1A), shadow distance, render scale, post-processing, ParticleMultiplier, mật độ cỏ và prop nhỏ (bật hoặc tắt nhóm Detail). Mặc định Low. Tuỳ chọn Auto: đo FPS 5 giây đầu rồi đề xuất mức phù hợp (chỉ hạ, không tự nâng).
- Settings UI lưu bền và áp dụng ngay, không cần khởi động lại.
- Target frame rate hợp lý (ví dụ 60), V-Sync tuỳ chọn; ở Low cho phép khoá 30 để máy ít nóng.

## 2. Kiểm toán ổn định
- Rà null-safety ở code mới và code cũ hay bị chạm: GetComponent không kiểm tra null, truy cập singleton khi scene đang chuyển, event gọi sau khi object bị huỷ, coroutine không dừng khi object tắt. Sửa các điểm nguy hiểm và ghi file:dòng vào báo cáo.
- Không để try/catch nuốt lỗi âm thầm: log rõ và có hướng xử lý.
- Vòng đời ứng dụng: mất focus, Alt+Tab, thu nhỏ cửa sổ, đổi độ phân giải, đổi màn hình. Game không crash và không mất tiến độ.
- Save hỏng: thử file cắt cụt, file rỗng, version sai, thiếu trường, JSON sai cú pháp, thư mục không ghi được. Phải báo lỗi dễ hiểu và vào game mới, không crash.
- Chạy toàn bộ bộ test từ Phase 2 và thêm test cho từng lỗi đã sửa.
- Soak test 20–30 phút (tự động bằng PlayMode hoặc thủ công): qua nhiều ngày, mở và đóng UI, chuyển scene Menu <-> Farm 5 lần. Theo dõi bộ nhớ (không tăng đều), số object và log lỗi.
- Quét lại anti-pattern như Phase 0 và sửa phần còn tồn.

## 3. Tối ưu theo số đo
- Đo lại bằng PerfOverlay và Profiler ở mức Low tại 3 cảnh nặng nhất: giữa nông trại ban ngày; ban đêm có đom đóm và đèn; mưa kèm thu hoạch. So với BASELINE.md.
- Dùng Frame Debugger tìm batch bị vỡ, overdraw lớn (cỏ, particle, tán cây), shadow caster thừa. Sửa theo thứ tự ảnh hưởng, mỗi thay đổi ghi lại số trước và sau.
- Ngân sách: batches <= 250, triangles <= 150k, texture < 300MB, particle <= 200, load scene < 5 giây, FPS >= 30 ở Low (Editor và build). Mục nào chưa đạt thì ghi rõ lý do và phương án.
- Thời gian load: kiểm tra load scene bất đồng bộ, tránh khựng khi vào Farm, prewarm pool trong màn hình loading.
- Dọn project: asset không dùng chỉ được LIỆT KÊ, chỉ xoá khi người dùng xác nhận. Ghi dung lượng build.

## 4. Hoàn thiện hình ảnh
- Chụp ảnh cảnh chính ở 3 trạng thái _Restore và 3 trạng thái ánh sáng, đối chiếu với Art Target, sửa chỗ lệch phong cách còn thấy (palette, bóng, bloom quá tay, chữ UI khó đọc).
- Kiểm tra UI ở 1366x768, 1920x1080 và 2560x1600: không vỡ, không chữ bị cắt.
- Camera: không xuyên tường, không giật; va chạm đúng.

## 5. Build và bàn giao
- Tạo menu Tools/Farm/Build/Build Windows: build Windows 64-bit (tắt Development Build) ra Builds/Windows/ với scene đúng thứ tự (MainMenu, Farm_Main, EndDemo). Nếu IL2CPP trên Windows cần cài thêm công cụ C++ thì dùng Mono cho bản Windows và nói rõ trong báo cáo. Không đụng cài đặt Android.
- Chạy thử build một lượt đầy đủ từ New Game đến EndDemo; ghi lỗi trong Player.log nếu có.
- Docs/DEMO_CHECKLIST.md: danh sách kiểm tra trước khi demo (cấu hình máy, độ phân giải, quality Low, âm lượng, file save sạch); kịch bản trình diễn 10–15 phút kèm điểm nhấn từng phút; lỗi đã biết và cách tránh khi demo; hướng dẫn quay video; phím tắt (F3 overlay, F12 chụp ảnh).
- CREDITS.md hoàn chỉnh: mọi asset, âm thanh, font ngoài, bảng license; đánh dấu mục CHƯA XÁC ĐỊNH (ví dụ PolytopeNature) để người dùng xác minh trước khi công khai.
- README ngắn: cách mở project, cách chạy demo, cấu trúc thư mục, cách thêm nội dung.

## Sản phẩm bàn giao
- Bản build Windows, DEMO_CHECKLIST.md, CREDITS.md, README, báo cáo so sánh BASELINE với hiện tại, Docs/PHASE_5_REPORT.md.

## Tiêu chí hoàn thành
- Một lượt chơi đầy đủ không crash ở cả Editor và build Windows; soak test không rò bộ nhớ đáng kể.
- Ngân sách mức Low đạt, hoặc ghi rõ ngoại lệ có lý do. FPS >= 30 trên laptop Intel tích hợp (số đo do người dùng xác nhận).
- Bộ test chạy xanh; file save hỏng không gây crash.
- Console sạch lỗi; số warning không tăng so với BASELINE.
- Có đủ tài liệu demo và credits.
```
