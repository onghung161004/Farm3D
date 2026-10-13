# Code Review Report

**Scope:** 12 file code/test: DayNightCycle.cs, FarmDayNightSetup.cs, CowAnimal.cs, CowPen.cs, CowLifeCycle.cs, CowBreeding.cs, CowOffspringFactory.cs, FarmProgression.cs, FarmSaveData.cs, CowBreedingHud.cs, CowBreedingTests.cs, DaylightTests.cs.
**Date:** 2026-10-12
**Verdict:** GREEN — Approved (phần thay đổi của task).

## Summary

Tái sử dụng model bò đã nhập và input tương tác hiện có. Bê được đăng ký vào danh sách bò động, tạo snapshot và khôi phục theo ID; sinh sản xóa cửa sổ ăn của cả bố mẹ để tránh sinh trùng. Ánh sáng cố định vẫn giữ API thời tiết và lịch NPC.

## Checklist Summary

| Category | Verdict | Issues |
|---|---|---|
| Security | PASS | 0; không thêm mạng/tài khoản/secret |
| Business Logic | PASS | 0; ghép cùng pen, deadline UTC, cooldown, sức chứa và restore |
| Performance | PASS | 0; kiểm tra vị trí chỉ khi cho ăn, giới hạn grid theo pen; tái sử dụng mảng collider |
| Clean Code | PASS | 0; lifecycle, sinh sản, clone và HUD tách riêng; giao dịch reset trong finally |
| Conventions | PASS | 0; C#/namespace/NUnit/EditMode theo dự án |

## Verification

- Unity 6000.6.1f1 biên dịch runtime và tests trong một dự án tạm, dùng các package đã có trên máy.
- 12 CowBreedingTests + 1 DaylightTests mới: 13/13 qua.
- Kiểm tra cuối sau rà code: 27/27 qua (12 sinh bê + 1 daylight + 11 CowHusbandry + 3 FarmWarehouse).
- Chạy cả suite: 110/112 qua. Hai ca lỗi có sẵn trong code/test không đổi so với HEAD: FarmHudPresentationTests.ReadabilityStyle_UsesLargeTypeAndTwoLineControls (32 so với ngưỡng 34), FarmWeatherControllerTests.RainTransition_WatersSeededPlotOnlyOnce (biên số thực 1.9 + 0.1).
- Kiểm tra whitespace diff của task: qua. Thay đổi material/Android có sẵn của người dùng được giữ.
- dotnet build csproj Unity không dùng được do máy thiếu .NET Framework 4.7.1 targeting pack; biên dịch Unity/NUnit ở trên đã thực hiện thành công.
- Chưa chạy kiểm tra hình ảnh Game View hoặc APK thật. Cần Play FarmDemo để xem bê thực tế trên map.

## Kiểm thử thủ công

1. Mở FarmDemo, Play: map giữ ban ngày, vẫn đổi trời quang/mưa.
2. Dắt hai bò về chuồng; có ít nhất 2 cà rốt. Cho mỗi con ăn bằng E/ACTION trong 60 giây: có một bê nhỏ và thông báo.
3. Chờ 120 giây: vẫn vắt được sữa của bố mẹ. Chờ 5 phút: bê lớn và bố mẹ hết cooldown; lần sinh sau cần các lần ăn mới.
4. Thoát Play và mở lại: bê giữ ID/tuổi, không nhân đôi, tiền/cá/cây/đơn vẫn còn.
5. Khi có 8 bò trong chuồng hoặc các điểm sinh đều bị vật cản: không sinh thêm; thức ăn vẫn phục vụ tạo sữa.
