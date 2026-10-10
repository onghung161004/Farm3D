# BÁO CÁO HIỆN TRẠNG DỰ ÁN FARM3D (HỒ SƠ KIỂM TOÁN)

## A. TÓM TẮT ĐIỀU HÀNH
Dự án **Farm3D** hiện tại là một bản prototype/demo (bản thử nghiệm) của một game mô phỏng nông trại 3D (tương tự phong cách Harvest Moon / Stardew Valley). Game đã xây dựng được vòng lặp cơ bản nhất (Core loop) bao gồm: tương tác với đất (cuốc, gieo, tưới, thu hoạch) và chăn nuôi cơ bản (bò sữa), đồng thời bước đầu tích hợp phản hồi âm thanh. Tuy nhiên, dự án vẫn ở giai đoạn rất sơ khai.
**5 vấn đề lớn nhất hiện tại:**
1. Thiếu vòng lặp kinh tế (mua bán, tiền tệ) và mục tiêu chơi (Nhiệm vụ/Cốt truyện).
2. Chưa có hệ thống UI điều hướng cơ bản (Main Menu, Pause, Settings) và Save/Load.
3. Không gian thế giới thiếu tính tương tác động ngoài các ô đất (chưa có NPC, ngày/đêm).
4. Thiết kế Level đang phụ thuộc mạnh vào các file script Editor (sinh map bằng code) thay vì kéo thả trực quan, có thể gây khó cho Level Designer.
5. Thiếu VFX (hiệu ứng hình ảnh/hạt) để tăng tính thỏa mãn khi tương tác.

---

## B. THÔNG TIN KỸ THUẬT TỔNG QUAN

| Hạng mục | Chi tiết |
| :--- | :--- |
| **Engine** | Unity 6000.6.1f1 |
| **Render Pipeline** | URP (Universal Render Pipeline) 17.6.0 |
| **Scripting Backend** | IL2CPP |
| **Cấu trúc CPU Target**| ARM64 |
| **Android API** | Min: 26 (Android 8.0) - Target: Automatic (0) |
| **Gói (Packages) chính** | Input System 1.20, AI Navigation, UGUI, Visual Scripting |
| **Khối lượng tài sản** | ~1300 file meta, 238 Prefab, 229 Material, 214 Texture, 190 FBX, 86 Script |

---

## C. HIỆN TRẠNG THEO 6 BƯỚC KHẢO SÁT

### Bước 1 – Nhận diện kỹ thuật
- **[Đã có & tốt]**: Cấu hình Android rất chuẩn cho game hiện đại (IL2CPP + ARM64 giúp chống hack và tối ưu tốc độ). Min API 26 là hợp lý để dùng các tính năng đồ họa mới.
- **[Có nhưng yếu]**: Số lượng tài sản 3D khá lớn nhưng chưa thấy quy hoạch rõ ràng về nén texture (Texture compression) cho mobile.
- **[Bằng chứng]**: `ProjectSettings.asset` dòng 275 (Architecture: 2 = ARM64), dòng 780 (scriptingBackend: 1 = IL2CPP).

### Bước 2 – Game design hiện tại
- **[Có nhưng yếu]**: Gameplay đã có tương tác công cụ (`PlayerToolController.cs`), có sinh trưởng cây trồng (`FarmGrowthDemo.cs`, `FarmPlot.cs`), và AI bò sữa (`CowAnimal.cs`).
- **[Chưa có]**: Hoàn toàn thiếu cốt truyện, UI hệ thống, kinh tế, cơ chế ngày đêm, và hệ thống lưu trữ.
- **[Bằng chứng]**: Quét thư mục `Assets/FarmRestoration/Scripts` chỉ có `Animals`, `Farming`, `Player`, `Progression`, `UI`, `World`. Không có thư mục `Data`, `Save`, hay `Quests`.

### Bước 3 – Đồ hoạ & phong cách nghệ thuật
- **[Có nhưng yếu]**: Game theo phong cách Low-poly / Stylized / Cartoon. Có sử dụng một số gói tài nguyên như `PolytopeNature` và `CartoonCropVisuals`. Đã tích hợp URP.
- **[Chưa có]**: Chưa cấu hình Post-processing chuyên biệt cho mobile (bloom, color grading) để làm nổi bật đồ họa. 
- **[Bằng chứng]**: Sự tồn tại của file `CartoonCropVisuals.cs` và các thư mục `Models`, `Materials`.

### Bước 4 – Animation, VFX, vật lý, âm thanh
- **[Có nhưng yếu]**: Có phản hồi âm thanh (audio feedback) thông qua script `FarmAudioFeedback.cs`. Có animation cơ bản cho người và bò (`CowAnimal.cs`).
- **[Chưa có]**: Hoàn toàn thiếu Particle System (VFX văng đất, giọt nước, bụi...) để tạo "Game feel". 
- **[Bằng chứng]**: Tìm kiếm Instantiate trả về rất ít kết quả liên quan đến VFX. Có 17 file âm thanh (`.wav`, `.ogg`).

### Bước 5 – Hiệu năng, ổn định, Android
- **[Đã có & tốt]**: Mã nguồn (Source code) rất sạch. Các lỗi anti-pattern kinh điển (như dùng `Find` hoặc `GetComponent` trong hàm `Update`) không xuất hiện trong các đoạn script core.
- **[Có nhưng yếu]**: Sử dụng `Resources.Load` đồng bộ để nạp âm thanh, có thể gây giật lag (spike) nhỏ khi tải lần đầu. Cần cảnh giác với overdraw khi trồng quá nhiều cây.
- **[Bằng chứng]**: `FarmAudioFeedback.cs` dòng 158 sử dụng `Resources.Load<AudioClip>`. 

### Bước 6 – Kiến trúc & nợ kỹ thuật
- **[Đã có & tốt]**: Kiến trúc phân rã thành các module hợp lý. Dễ mở rộng các loại hạt giống mới hay công cụ mới.
- **[Có nhưng yếu]**: Việc sinh map/địa hình có vẻ đang bị hard-code trong các script Editor khổng lồ (ví dụ: `ReferenceLandscapeSetup.cs` nặng tới 38KB). Điều này là nợ kỹ thuật lớn nếu muốn thiết kế level thủ công linh hoạt.
- **[Bằng chứng]**: Top 10 file `.cs` lớn nhất đa số nằm trong thư mục `Editor` chứa chữ `Setup`.

---

## D. BẢNG CHẤM ĐIỂM HIỆN TRẠNG (0-10)

| Mục tiêu | Điểm | Lý do & Bằng chứng | Độ tin cậy |
| :--- | :---: | :--- | :---: |
| 1. Giải trí & thú vị | **3/10** | Mới có cơ chế chặt/cuốc/tưới cơ bản. Thiếu mục tiêu chơi, kinh tế, NPC. (*Bằng chứng: Thiếu script Quest, NPC*) | Cao |
| 2. Đồ hoạ đẹp | **5/10** | Dùng URP, asset Low-poly có phong cách, nhưng thiếu ánh sáng/VFX ấn tượng. (*Bằng chứng: Thiếu shader phức tạp*) | Vừa |
| 3. Nhất quán nghệ thuật | **6/10** | Các asset có vẻ đồng bộ theo phong cách Cartoon. (*Bằng chứng: Tên các script/prefab thống nhất*) | Vừa |
| 4. Hiệu ứng (Audio/VFX) | **4/10** | Đã có âm thanh, nhưng thiếu trầm trọng VFX hạt (Particle). (*Bằng chứng: Thiếu code spawn VFX trong Farming*) | Cao |
| 5. Hoàn thiện (Hiệu năng) | **7/10** | Code sạch, config IL2CPP/ARM64 chuẩn Android. Ít Anti-pattern. (*Bằng chứng: Kết quả quét Regex source code*) | Cao |

---

## E. GAP ANALYSIS (PHÂN TÍCH KHOẢNG TRỐNG)

- **G1 (Giải trí):** *Khoảng trống:* Hoàn toàn thiếu Meta-game (hệ thống nâng cấp, bán hàng, cốt truyện). *Mức độ ảnh hưởng:* **CAOT**.
- **G2 (Đồ hoạ):** *Khoảng trống:* Ánh sáng chưa được bake chuẩn, thiếu post-processing. *Mức độ ảnh hưởng:* **VỪA**.
- **G3 (Nghệ thuật):** *Khoảng trống:* Cần một UI/UX designer thống nhất giao diện phẳng (flat) hợp với low-poly. *Mức độ ảnh hưởng:* **VỪA**.
- **G4 (Hiệu ứng):** *Khoảng trống:* Thiếu hệ thống Object Pooling cho VFX. *Mức độ ảnh hưởng:* **CAO**.
- **G5 (Hiệu năng):** *Khoảng trống:* Cần đo lường trên máy RAM thấp thực tế, chưa có hệ thống Quality Settings động. *Mức độ ảnh hưởng:* **VỪA**.

---

## F. DANH SÁCH RỦI RO (Xếp theo độ nghiêm trọng)
1. **Thiết kế Cấp độ (Level Design):** Các file `Setup.cs` khổng lồ trong Editor gây khó khăn cho designer non-code.
2. **Nợ kỹ thuật:** `Resources.Load` trong `FarmAudioFeedback.cs` cần chuyển sang `Addressables` hoặc gán cứng (direct reference) / Object Pooling để tránh giật lag.
3. **Hiệu năng Android:** Quá nhiều object cây trồng độc lập có thể làm tăng Draw Calls nếu chưa thiết lập GPU Instancing.

---

## G. CÁC "QUICK WIN" (Thắng lợi nhanh)
1. Bổ sung ngay các `ParticleSystem` (Hiệu ứng bụi, nước) vào `FarmPlot.cs` mỗi khi tương tác (Rất dễ làm, tăng 50% "game feel").
2. Chuyển `Resources.Load` sang một `AudioDictionary` load sẵn ở màn hình loading.
3. Thêm một Component điều khiển chu kỳ Ngày/Đêm (xoay Directional Light) để thế giới sinh động hơn lập tức.

---

## H. DANH SÁCH FILE/MODULE THEN CHỐT CẦN ĐỌC KỸ
1. `Assets/FarmRestoration/Scripts/Player/PlayerToolController.cs`: Trái tim của cơ chế điều khiển và tương tác.
2. `Assets/FarmRestoration/Scripts/Farming/FarmPlot.cs`: State machine quản lý sinh trưởng và đất đai.
3. `Assets/FarmRestoration/Scripts/Animals/CowAnimal.cs`: Mẫu AI đầu tiên của game.
4. Các file `Setup.cs` trong `Editor`: Để hiểu cách game đang sinh ra thế giới.

---

## I. CÂU HỎI CẦN TÔI TRẢ LỜI (ĐỂ LẬP PLAN NÂNG CẤP)

**Về định hướng Game:**
1. Game sẽ đi theo hướng Sandbox vô tận (như Minecraft) hay có Cốt truyện tuyến tính có kết thúc?
2. Anh/Chị muốn tích hợp hệ thống kiếm tiền (Monetization: Ads, IAP) ngay trong giai đoạn này hay chỉ tập trung hoàn thiện Demo?

**Về Nguồn lực & Tài nguyên:**
3. Chúng ta có ngân sách để mua thêm Asset Store (VFX, UI Pack) không, hay sẽ sử dụng đồ tự làm/AI generate?
4. Đội ngũ hiện tại có Level Designer không? (Nếu có, tôi sẽ đề xuất đập bỏ hệ thống gen map bằng Code để chuyển sang tool kéo thả thân thiện hơn).

**Về Kỹ thuật:**
5. Target FPS mong muốn trên Android là 30 hay 60? Cấu hình máy Android thấp nhất (Min Spec) mà anh/chị nhắm tới là máy gì (ví dụ: Ram 3GB, Chip Snapdragon 450...)?

---

## J. THÔNG TIN KHÔNG LẤY ĐƯỢC VÀ CẦN CHẠY THỦ CÔNG
- **Dung lượng APK/AAB và Thời gian Build:** Vui lòng thực hiện build 1 bản Android và ghi nhận dung lượng file xuất ra.
- **Profiler Data:** Cần build game lên một thiết bị Android thật (hoặc giả lập), kết nối với **Unity Profiler** để đo lường chính xác lượng RAM tiêu thụ và số lượng Draw Calls / Batches trung bình khi đứng giữa nông trại.
- **Logcat:** Chạy game trên máy thật và xem có quăng lỗi ngầm (Exception) nào không.
