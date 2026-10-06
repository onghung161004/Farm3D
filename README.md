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
| `E` | Tương tác với ô đất hoặc đối tượng ở gần |
| Giữ `E` gần một vườn | Lần lượt thao tác trên tối đa 9 ô đất bằng công cụ đang chọn |
| `R` | Đổi công thức ở trạm chế biến có nhiều công thức |

Vòng chơi: **cuốc đất → gieo hạt → tưới nước → chờ cây lớn → thu hoạch → chế biến hoặc giao đơn → nhận tiền → sửa chữa làng**. Thời gian trồng hiện tại là 45 giây cho cà rốt, 60 giây cho cà chua và 75 giây cho bí ngô. Tiến trình được lưu dưới dạng `FarmDemoSave.json` trong `Application.persistentDataPath` của Unity, không lưu trong repository.

## Cấu trúc chính

- `FarmRestoration/Assets/FarmRestoration/Scenes/FarmDemo.unity`: scene nông trại chính.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Farming/`: trạng thái ô đất, cây trồng và kho nông sản.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Player/`: di chuyển, camera và tương tác.
- `FarmRestoration/Assets/FarmRestoration/Scripts/Progression/`: đơn hàng, chế biến, tiền, sửa chữa và lưu game.
- `FarmRestoration/Assets/FarmRestoration/Scripts/UI/`: thanh công cụ và HUD.
- `FarmRestoration/Assets/FarmRestoration/Tests/EditMode/`: các bài kiểm thử Edit Mode.

Đồ họa sử dụng nhiều asset bên thứ ba được nhập trong `Assets/` (cây trồng, địa hình, nhà, NPC và cảnh quan). Quyền sử dụng từng asset tuân theo giấy phép của nhà phát hành tương ứng; không xem chúng là tài sản do dự án tự tạo.

## Lưu ý khi đưa lên GitHub

Chỉ cần chia sẻ các thư mục dự án như `Assets/`, `Packages/` và `ProjectSettings/`. Các thư mục Unity tự sinh (`Library/`, `Temp/`, `obj/`, `Logs/`, `Build/`) đã được liệt kê trong `.gitignore` và sẽ được tạo lại khi mở dự án. File lớn hơn giới hạn GitHub cần được xử lý riêng; không đưa cache Unity lên Git.
