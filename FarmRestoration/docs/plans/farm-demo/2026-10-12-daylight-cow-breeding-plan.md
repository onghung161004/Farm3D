# Implementation Plan: Ban ngày và bò sinh bê

Spec Source: `2026-10-12-daylight-cow-breeding-spec.md`
Owner: Codex
Last Updated: 2026-10-12
Status: Done

## 1. Context

Ánh sáng hiện có ban đêm; cho ăn chỉ tạo sữa. Thêm sinh bê cần mở rộng danh sách bò và save.
Modules: World, Animals, Progression, UI, Tests. Không thay kinh tế, giá thức ăn hay bản đồ.

## 2. Constraints

Unity 6000.6.1f1/C#, MonoBehaviour và NUnit EditMode. Không thêm package. Không sửa các thay đổi vật liệu/Android hiện có. Giới hạn 8 bò/chuồng để hạn chế nhân đàn vô hạn.

## 3. Conventions

Namespace FarmRestoration (World cho ánh sáng), PascalCase API, camelCase fields, constants PascalCase. API trả false nếu hành động không hợp lệ; Debug.LogWarning cho lỗi tài nguyên. Test dựng GameObject rồi DestroyImmediate.

## 4. Contracts

- DayNightCycle.TimeOfDay luôn 0.5; SetRainBlend giữ ánh sáng ban ngày và thay đổi màu/intensity cho mưa.
- CowLifeCycle: RecordFeed(now) chỉ tạo cửa sổ 60 giây nếu trưởng thành và hết cooldown; CanBreed(now) kiểm tra tuổi/deadline; CompleteBreeding(now) xóa cửa sổ ăn và chờ 300 giây.
- CowLifeCycle.InitializeCalf/Restore/Tick: lưu tuổi UTC, scale từ 0.45 tới 1 trong 300 giây; gọi sự kiện khi trưởng thành. Restore snapshot v4 mặc định bò trưởng thành.
- CowBreeding.TryBreed(fedCow, herd, now) trả bê mới hoặc null; chỉ ghép cùng chuồng, kiểm tra sức chứa và vị trí trước khi tiêu thụ trạng thái bố mẹ.
- CowBreeding.RestoreOffspring(saved, herd) chỉ tạo bản ghi offspring chưa có ID, tìm đúng chuồng và template trưởng thành. Chạy lại không tạo trùng.
- CowSaveData thêm offspring, penId, bornUtcTicks, grownUtcTicks, breedingReadyUtcTicks, fedUntilUtcTicks. ID được giữ duy nhất.

## 5. Architecture

ACTION/E -> FarmProgression.TryFeedCow -> CowAnimal.Feed -> lifecycle.RecordFeed -> CowBreeding.TryBreed -> clone CowAnimal -> RegisterCow -> NotifyAndSave.
RestoreGame -> khôi phục bò scene -> tạo offspring còn thiếu -> đăng ký sự kiện -> sẵn sàng chơi.

## 6. Artifact Registry

| Artifact | Task |
|---|---|
| Scripts/World/DayNightCycle.cs; Editor/FarmDayNightSetup.cs | T1 |
| Scripts/Animals/CowLifeCycle.cs; CowAnimal.cs; CowPen.cs | T2 |
| Scripts/Animals/CowBreeding.cs; CowOffspringFactory.cs | T3 |
| Scripts/Progression/FarmProgression.cs; FarmSaveData.cs | T4 |
| Scripts/UI/CowBreedingHud.cs; README.md | T5 |
| Tests/EditMode/CowBreedingTests.cs; DaylightTests.cs | T6 |

## 7. Task Graph

Thực hiện trực tiếp theo yêu cầu người dùng, chia từng phần có thể kiểm tra: ánh sáng -> trạng thái/sinh bê -> lưu/hiển thị -> kiểm thử.
T1 độc lập; T2 -> T3 -> T4 -> T5; T6 xác minh T1–T5. Tái sử dụng trước phù hợp code sẵn có; dựng prefab mới hoặc thay cả hệ chăn nuôi làm chậm kiểm thử.

## 8. Tasks

- T1: bỏ vòng lặp đêm, giữ weather bridge. Input: Light hiện có. Output: ánh sáng ban ngày. TimeOfDay và NPC không chuyển đêm.
- T2: thêm lifecycle và dữ liệu v5. Input: feed/UTC. Output: tuổi, cửa sổ ghép cặp, cooldown. Bê không tạo sữa; timestamp giữ qua JSON.
- T3: tìm bạn cùng pen, chỗ trống và clone. Input T2/đàn bò. Output một bê hoặc null. Không sinh khi đầy/khác pen; không tái dùng lần ăn.
- T4: danh sách bò động và restore offspring. Input T3/save. Output snapshot có bê và restore không trùng. Save cũ vẫn có milk/crops/coin.
- T5: lời nhắc và toast TMP; ghi quy tắc vào README. Input trạng thái T2–T4. Output hướng dẫn ghép cặp/sinh bê/đầy chuồng.
- T6: NUnit cho ghép cặp, tuổi, deadline, sức chứa, JSON, restore và ánh sáng; compile runtime/editor.

Kết quả thực hiện: T1–T6 hoàn thành. 13 kiểm thử mới và 14 kiểm thử chăn nuôi/kho liên quan đều qua (27/27). Bộ đầy đủ 110/112 qua; hai ca cũ lỗi cỡ chữ HUD và cộng thời gian chuyển mưa. Code/test của hai ca đó không bị thay đổi trong công việc này. Chưa chạy smoke đồ họa hoặc bản APK trên thiết bị.

## 9. Edge Cases

| Case | Expected | Task |
|---|---|---|
| Null/không có bạn | Không sinh, không ném lỗi | T3 |
| Đúng 60 giây/hết cooldown/đủ 8 bò | Deadline và sức chứa được kiểm tra | T2,T3 |
| Save v4/ID Unicode | Trưởng thành mặc định; ID giữ nguyên | T2,T4 |
| Gọi liên tiếp/tải lại hai lần | Chỉ một bê, ID không trùng | T3,T4 |
| Bê lớn trong lúc offline | Lần tải tính tuổi từ UTC | T2,T4 |
| Mưa | Giữ ánh sáng ban ngày, tưới cây như cũ | T1 |

## 10. Risks

Clone runtime có thể mang trạng thái bố mẹ: khởi tạo lại lifecycle và identity trước đăng ký. Scene đang mở trong Unity: kiểm tra biên dịch và chạy test trong dự án kiểm tra tách biệt nếu khóa editor. Không sửa save của người dùng khi kiểm thử.

## 11. Verification

NUnit: pair success, milk retained, cooldown duplicate, expired feed, two pens, calf restrictions/growth, capacity, old snapshot, Unicode JSON, idempotent restore. Daylight with rain. Smoke trong FarmDemo: dắt hai bò vào, cho ăn, thấy bê và thông báo, thoát/mở lại giữ bê. Ghi rõ nếu chưa chạy được smoke đồ họa.

## 12. Rollout

Mở lại FarmDemo, Play, sử dụng ACTION/E như hiện tại. Không cần dựng lại bản đồ.

## 13. Future improvements

Giới tính/di truyền và model bê riêng nằm ngoài phạm vi phiên bản này.
