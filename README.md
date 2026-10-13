# Farm3D – Farm Restoration

Dự án game nông trại 3D làm bằng Unity. Người chơi di chuyển trong bản đồ, canh tác bí ngô, cà rốt và cà chua, chế biến nông sản, giao đơn hàng cho NPC để nhận tiền và sửa chữa một số hạng mục trong làng.

## Mở dự án

1. Cài **Unity 6.6 (6000.6.1f1)** qua Unity Hub.
2. Trong Unity Hub, chọn **Add project from disk** và trỏ tới thư mục `FarmRestoration/` (thư mục chứa `Assets`, `Packages` và `ProjectSettings`).
3. Đợi Unity nhập asset và biên dịch xong. Trong cửa sổ Project, mở `Assets/FarmRestoration/Scenes/FarmDemo.unity` bằng cách nhấp đúp, rồi bấm **Play**.

Nếu Unity mở scene trống `Untitled`, bản đồ không bị mất: hãy mở lại `FarmDemo.unity`. Trong tab Scene, chọn `Player` ở Hierarchy và nhấn **F** để đưa góc nhìn về nhân vật. Không lưu scene `Untitled` đè lên `FarmDemo`.

> `Assets/Scenes/SampleScene.unity` là scene mẫu của Unity, **không phải** bản đồ game. Khi tạo bản build, hãy chọn `FarmDemo.unity` làm scene khởi đầu trong Build Profiles/Scene List.

## Clone và tải texture bằng Git LFS

Một số texture núi lớn được lưu bằng **Git LFS**. Trên mỗi máy mới, hãy [cài Git LFS](https://git-lfs.com/) trước khi mở dự án trong Unity. Trong PowerShell hoặc terminal của SourceTree, chạy:

```powershell
git lfs install
git clone https://github.com/onghung161004/Farm3D.git
cd Farm3D
git lfs pull
```

Nếu đã clone dự án từ trước, mở terminal tại thư mục `Farm3D` rồi chạy:

```powershell
git lfs install
git pull
git lfs pull
```

Kiểm tra bằng `git lfs ls-files`: repository hiện có 3 texture núi được quản lý bằng LFS. Nếu file `.png` chỉ chứa vài dòng bắt đầu bằng `version https://git-lfs.github.com/spec/v1` thay vì ảnh thật, chạy lại `git lfs pull` và kiểm tra kết nối/quyền tải LFS. Sau đó mở thư mục `FarmRestoration/` bằng Unity Hub và chờ Unity nhập lại asset.

Khi bổ sung một file lớn mới, dùng `git lfs track "đường/dẫn/file"` **trước khi** `git add`; commit cả `.gitattributes`, file asset và file `.meta` đi kèm. Không đưa `Library/` hoặc các file cache Unity lên Git.

## Điều khiển và vòng chơi

| Phím | Chức năng |
| --- | --- |
| `W A S D` hoặc phím mũi tên | Di chuyển |
| Giữ chuột phải và rê | Xoay camera quanh nhân vật |
| Con lăn chuột | Phóng to/thu nhỏ camera |
| `1` / `2` / `3` / `4` | Chọn cuốc / hạt giống / bình tưới / thu hoạch |
| `8` hoặc ô Axe trên thanh công cụ | Chọn rìu chặt cây |
| `E` | Tương tác với ô đất hoặc đối tượng ở gần |
| Giữ `E` gần một vườn | Lần lượt thao tác trên tối đa 9 ô đất bằng công cụ đang chọn |
| `R` | Đổi công thức ở trạm chế biến có nhiều công thức |
| `Enter` / `Esc` | Xác nhận giao đơn trong hội thoại NPC / đóng hội thoại hoặc kho |

Vòng chơi: **cuốc đất → gieo hạt → tưới nước → chờ cây lớn → thu hoạch → chế biến hoặc giao đơn → nhận tiền → sửa chữa làng**. Thời gian trồng hiện tại là 45 giây cho cà rốt, 60 giây cho cà chua và 75 giây cho bí ngô. Tiến trình được lưu dưới dạng `FarmDemoSave.json` trong `Application.persistentDataPath` của Unity, không lưu trong repository.

Ở làng, bấm `E` gần thương nhân để xem đơn; chọn **Deliver** hoặc nhấn `Enter` mới giao hàng. Bấm `E` gần thùng **WAREHOUSE** để xem chung lượng bí ngô, cà rốt, cà chua, sản phẩm chế biến và sữa; chọn một món rồi bấm **Sell 1** để bán một đơn vị. Bán trực tiếp có giá thấp hơn giao đơn; kho không có bước gửi/rút, mọi thao tác dùng cùng số lượng đã lưu trong game.

Làng có ba cư dân đi dạo ngoài thương nhân. Đến gần Maya, Bram hoặc Lina và bấm `E` để chào hỏi; chọn một trong hai chủ đề rồi bấm **Next**/`Enter` để đọc tiếp, `Esc` để đóng. Hội thoại này không giao dịch hay thay đổi kho/tiền.
Nếu scene đang mở trước khi cập nhật mã, hãy mở lại `FarmDemo.unity` để Unity tạo bố cục 7 nhà gỗ và 3 cư dân. Có thể chạy thủ công `Tools > Farm Restoration > Build Village With Village Houses Pack` và `Add Village Progression Gameplay` rồi lưu scene.

## Chăn nuôi và ánh sáng

### Phục hồi nhà và cầu

Sửa nhà/cầu thay đổi hình ảnh trên chính công trình đang dùng, không chỉ hiện thông báo. Nhà gỗ giữ nguyên vị trí/kích thước: trước sửa gỗ bạc tối, hàng rào nghiêng và sân có ván vụn; sau sửa phục hồi màu vật liệu gốc, hoàn thiện hàng rào và thêm ghế/chậu cây. Cầu gỗ trước sửa thiếu một phần lan can; sau sửa khôi phục đúng mesh cầu gốc và dọn vật liệu hỏng ở đầu cầu. Mặt cầu, độ cong, vị trí và collider đi bộ không thay đổi. NPC đổi lời chào theo trạng thái sửa nhà/cầu và cảm ơn khi cả hai hoàn tất.

Thay đổi hiện trong Play/bản build và theo cờ sửa chữa đã lưu: save cũ đã sửa công trình được áp dụng hình ảnh mới, không cần trả tiền lần nữa. Biển tương tác đi theo công trình thực tế; nhóm nhà thay thế/hàng rào đầu cầu cũ không được bật chồng lên. Không cần chạy lại công cụ dựng làng để thấy thay đổi này.

Game luôn ở ban ngày; thời tiết mưa vẫn hoạt động và tưới các ô đã gieo.

Dắt hai bò trưởng thành về cùng chuồng, rồi bấm `E` hoặc **ACTION** để cho mỗi con ăn 1 cà rốt trong vòng 60 giây. Game sinh một bê con, hiện thông báo và lưu bê cùng tiến trình. Bê bắt đầu bằng 45% kích thước bò trưởng thành và lớn dần trong 5 phút; sau khi lớn mới có thể cho ăn để lấy sữa và sinh sản. Mỗi bố mẹ cần chờ 5 phút trước lần sinh tiếp theo. Chuồng có tối đa 8 bò đã nuôi, tính cả bê; khi đầy, cho ăn vẫn tạo sữa nhưng không sinh thêm bê. Bò trưởng thành vẫn cho 1 sữa sau 120 giây kể từ khi ăn.

## Chặt cây và dùng gỗ

Chọn **Axe** (phím `8` hoặc chạm ô rìu), đi vào rừng và đến gần cây có nhãn **WOOD TREE**. Bấm `E`/**ACTION** ba lần, mỗi nhát cách nhau ít nhất 0,55 giây: cây đổ, còn gốc và cộng **3 gỗ** vào kho. Gốc mọc lại sau **5 phút**, kể cả khi thoát game. Có tối đa 12 cây thông được đánh dấu để khai thác; các cây ăn quả giữ nguyên.

Khi cầm rìu, đường vàng dẫn tới cây chặt được gần nhất và hiện khoảng cách. Chặt xong cây thì chỉ dẫn chuyển sang cây khác; bỏ qua gốc đang chờ mọc lại. Đổi công cụ sẽ ẩn chỉ dẫn. Nếu không tìm được đường đi trên đất, đường mảnh chỉ biểu thị hướng và có cảnh báo phải tránh vật cản, qua sông bằng cầu.

Nhạc nền không lời nhẹ được phát lặp ở âm lượng thấp, giảm thêm khi câu cá để dễ nghe cá cắn câu; giai điệu gốc được tạo bằng code. Chặt cây dùng clip va vào gỗ riêng `wood_hit_01`, không dùng tiếng cuốc đất. Nhát cuối phát clip thân cây rạn, lá xào xạc và chạm đất tại vị trí cây, nhỏ dần khi đi xa. Hai clip chặt/đổ được tách từ [tree chop fall thud của kheetor (CC0)](https://opengameart.org/content/tree-chop-fall-thud); nguồn và chỉnh sửa ghi ở `Resources/FarmAudio/README.md`. Cây đổ trong 1,65 giây, âm cây đổ hạ pitch để nặng hơn. Có thể thay nhạc bằng clip `Resources/FarmAudio/farm_music`.

Gỗ bán tại kho với giá **3 coin/gỗ**, hoặc giữ để sửa làng (coin như trước và thêm **6/10 gỗ** tùy hạng mục). Biển **PEN UPGRADE** ở cạnh phía ngoài cổng chuồng dùng **20 gỗ + 30 coin** để tăng sức chứa sinh sản từ 8 lên **12 bò** và kéo chuồng dài thêm **6 m về phía cổng** (chuồng mặc định từ 8×18 m lên 8×24 m). Hàng rào gỗ, cổng và bàn nâng cấp được dời đồng bộ; phía sau chuồng giữ nguyên. Nâng cấp mua một lần và được lưu; bản lưu đã nâng cấp được mở rộng tự động, không thu tiền lại. Những hạng mục đã sửa trong bản lưu cũ vẫn giữ nguyên.

## Chuỗi nhiệm vụ khôi phục làng

Nhấn **QUESTS** trên HUD hoặc `Q` để xem bảng tiến độ; có thể đến bảng gỗ cạnh giếng làng và bấm `E`/`ACTION`. Sáu chặng dùng chính hoạt động đang có, không tạo thêm kho hay loại tiền riêng:

| Chặng | Mục tiêu | Thưởng |
|---|---|---|
| 1 | Chào cư dân/thương nhân, thu hoạch 3 cà rốt | 15 coin |
| 2 | Chế biến 1 sản phẩm, giao 1 đơn hàng | 20 coin |
| 3 | Sửa cầu gỗ hiện có | 20 coin |
| 4 | Đưa 2 bò trưởng thành vào chuồng, cho 2 con khác nhau ăn, lấy 1 sữa | 25 coin |
| 5 | Câu 3 cá, giao 2 cá tại bảng gỗ | 20 coin |
| 6 | Sửa nhà, trở về bảng gỗ nhận lời cảm ơn | 40 coin |

Hoạt động được ghi nhận tích lũy; **Claim reward** nhận thưởng theo thứ tự, không nhận lặp một chặng. **Give 1 fish** chỉ bật khi đứng gần bảng, mỗi lần trừ đúng 1 cá trong kho; cá đã bán không thể giao. Nhận thưởng chặng cuối cũng cần trở về bảng. **TRACK** bật/tắt đường vàng tới mục tiêu, tự nhường cho chỉ dẫn dắt bò/chặt cây; không tự di chuyển nhân vật. Nếu không tìm được đường, game báo phải đến đất trống và qua cầu, không vẽ đường xuyên sông.

Tiếp tế hoàn thành sẽ hiện thùng gỗ cạnh bảng; kết thúc chuỗi xuất hiện ghế gỗ, cùng thay đổi thật của cầu/nhà đã sửa. Hoàn thành không kết thúc game: vẫn làm nông, câu cá, chăn nuôi và mở rộng bình thường. Mở bảng khóa di chuyển/tương tác; đóng bằng **Close**, `Q` hoặc `Esc`.

Save hiện tại ở phiên bản **8**, giữ nguyên tiền, kho, bò, đất và công trình cũ. Save cũ chỉ được công nhận hoạt động có bằng chứng từ kho/đơn hàng/trạng thái bò; không thể suy ra toàn bộ cá/sữa đã bán trước đây. Công trình đã sửa vẫn được tính, không phải trả tiền sửa lại. Thay đổi được tạo khi chạy `FarmDemo`, không cần dựng lại scene; muốn kiểm tra trên điện thoại phải build lại APK.

### Nâng cấp chiều rộng cầu

Sửa cầu trước với giá cũ **45 coin + 10 gỗ**. Sau khi sửa, cùng bảng cạnh cầu sẽ hiển thị **UPGRADE BRIDGE**. Đến gần và bấm `E`/`ACTION` để mua cấp kế tiếp:

- Rộng **gấp 2** ban đầu: **40 coin + 15 gỗ**.
- Từ gấp 2 lên **gấp 3** ban đầu: thêm **60 coin + 25 gỗ**.

Mặt cầu, lan can và mesh va chạm mặt đi mở rộng đồng bộ quanh đường giữa cầu; chiều dài, độ dốc và vị trí cầu không đổi. **Cầu chưa sửa không có vùng chặn an toàn ở hai bên**, nhân vật có thể đi khỏi mép và rơi xuống. **Sau khi sửa, lan can có va chạm thật**, chặn người chơi ở hai cạnh nhưng không chắn hai đầu cầu. Khi nâng rộng gấp 2/gấp 3, vùng chặn dời ra theo mép mới, không để tường vô hình ở chiều rộng cũ. Trạng thái này dùng cùng cờ sửa cầu đã lưu, không thu thêm phí và không bổ sung cơ chế bơi/chết đuối.

Chỉ dẫn dắt bò/nhiệm vụ vẫn dùng cùng collider mặt cầu. Thiếu một trong hai tài nguyên hoặc không tìm được geometry hợp lệ thì không trừ tiền/gỗ. Cấp tối đa hiện **3x WIDTH**, không mua thêm; cấp nâng được lưu, tải lại không thu tiền. Save cũ mặc định rộng như ban đầu, không mất trạng thái sửa cầu. Không cần dựng lại scene, nhưng APK cũ phải build lại để có tính năng này.

Mặt đi được hiệu chỉnh khi chạy từ các tam giác sàn gỗ thật (không dùng độ rộng cố định 3,3 m của collider cũ); độ cao theo sàn cong, vùng chặn theo mặt trong lan can và có miếng gỗ nối ngắn ở hai đầu bờ. Save đã nâng 2x/3x tự dùng geometry đã hiệu chỉnh, không bị giảm cấp hoặc thu tiền lại. Sau cập nhật code, **dừng Play rồi chạy lại** để dựng lại các mesh/va chạm runtime; không chỉ tiếp tục phiên Play cũ.

### Điều chỉnh âm lượng trong game

Nhấn **AUDIO** bên phải HUD để mở popup. **Background music** điều chỉnh nhạc nền; **SFX / actions** điều chỉnh bước chân, cuốc đất/tưới/thu hoạch/chế biến/sửa chữa, câu cá, tiếng bò và cây đổ. Kéo thanh **0–100%** để nghe thay đổi ngay; **100% giữ mức phối âm ban đầu**, không làm nhạc nền to thêm vượt mức cũ.

Mỗi nhóm có **Mute / Unmute** riêng. **Mute all / Unmute all** tắt/bật cả hai nhóm mà vẫn giữ mức thanh trượt và nút tắt riêng trước đó. Kéo thanh về 0 cũng làm nhóm đó im lặng. Nhạc nền vẫn giảm nhẹ khi câu cá, không ghi đè lựa chọn âm lượng. Âm lượng không thay đổi phạm vi nghe 3D của bò/cây và không tắt rung khi cá cắn câu.

Popup dùng TextMeshPro, thanh kéo cảm ứng và **Close**/`Esc`; mở popup khóa di chuyển/ACTION/chọn công cụ, đóng trả lại đúng trạng thái điều khiển. Không mở chồng khi đang câu cá hoặc đang ở hội thoại/kho/nhiệm vụ. Cài đặt được lưu riêng trên thiết bị bằng `PlayerPrefs` key `FarmRestoration.Audio.v1` khi đóng popup hoặc ứng dụng tạm dừng/thoát, không thay đổi `FarmDemoSave.json`. Muốn kiểm tra trên điện thoại cần build lại APK.

## Cấu trúc chính

- `FarmRestoration/Assets/FarmRestoration/Scenes/FarmDemo.unity`: scene nông trại chính.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Farming/`: trạng thái ô đất, cây trồng và kho nông sản.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Player/`: di chuyển, camera và tương tác.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Progression/`: đơn hàng, chế biến, tiền, sửa chữa và lưu game.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Quests/`: chuỗi nhiệm vụ, bảng tiến độ, bảng gỗ và chỉ dẫn mục tiêu.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Audio/`: cài đặt âm lượng nhạc/SFX và kênh điều chỉnh cho từng nguồn âm.
- `FarmRestoration/Assets/FarmRestoration/Scripts/UI/`: thanh công cụ và HUD.
- `FarmRestoration/Assets/FarmRestoration/Tests/EditMode/`: các bài kiểm thử Edit Mode.

Đồ họa sử dụng nhiều asset bên thứ ba được nhập trong `Assets/` (cây trồng, địa hình, nhà, NPC và cảnh quan). Quyền sử dụng từng asset tuân theo giấy phép của nhà phát hành tương ứng; không xem chúng là tài sản do dự án tự tạo.

## Lưu ý khi đưa lên GitHub

Chỉ cần chia sẻ các thư mục dự án như `Assets/`, `Packages/` và `ProjectSettings/`. Các thư mục Unity tự sinh (`Library/`, `Temp/`, `obj/`, `Logs/`, `Build/`) đã được liệt kê trong `.gitignore` và sẽ được tạo lại khi mở dự án. File lớn hơn giới hạn GitHub cần được xử lý riêng; không đưa cache Unity lên Git.
