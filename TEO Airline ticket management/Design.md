# TEO Airlines --- UI/UX Design Brief

## 1. Mục tiêu

Thiết kế website đặt và quản lý vé máy bay trực tuyến cho thương hiệu
giả lập **TEO Airlines**, triển khai bằng **ASP.NET MVC** và **SQL
Server**.

Bộ thiết kế cần hỗ trợ ba vai trò: - **Hành khách:** tìm chuyến, đặt vé,
quản lý đặt chỗ và thực hiện các dịch vụ liên quan. - **Nhân viên
quầy:** bán vé trực tiếp, thu/xác nhận thanh toán, phát hành vé và xử lý
yêu cầu đổi/hủy. - **Quản trị viên:** quản lý dữ liệu nghiệp vụ vé máy
bay và xem báo cáo.

Phong cách cần hiện đại, rõ ràng, thân thiện, có tính nhất quán giữa các
vai trò. Mockup là bản định hướng UI, chưa phải ứng dụng đã kết nối cơ
sở dữ liệu.

## 2. Nhận diện thương hiệu

### Tên thương hiệu

**TEO Airlines**

### Ý tưởng logo hiện tại

Biểu tượng máy bay/cánh bay cách điệu kết hợp chữ **TEO** và dòng chữ
nhỏ **airlines**. Có thể tinh chỉnh nét vẽ, khoảng cách và khả năng hiển
thị ở kích thước nhỏ trong các vòng thiết kế tiếp theo.

### Bảng màu đã thống nhất

  -----------------------------------------------------------------------
  Vai trò màu             Mã màu                  Cách sử dụng
  ----------------------- ----------------------- -----------------------
  Xanh teal chủ đạo       `#0F766E`               Nút hành động chính,
                                                  liên kết nổi bật, trạng
                                                  thái đang chọn

  Xanh teal đậm           `#115E59`               Trạng thái hover của
                                                  nút và liên kết

  Xanh teal nhạt          `#E6F4F1`               Nền nhẹ cho biểu tượng
                                                  và trạng thái đang chọn

  Cam nhấn                `#FFB703`               Ưu đãi, cảnh báo nhẹ,
                                                  chi tiết cần thu hút
                                                  chú ý

  Than đậm                `#1F2937`               Tiêu đề, nội dung
                                                  chính, thanh điều hướng
                                                  đậm

  Nền sáng                `#F3F4F6`               Nền trang, vùng nội
                                                  dung phụ

  Xanh ngọc thành công    `#10B981`               Thành công, đã xác
                                                  nhận, đã thanh toán

  Trắng                   `#FFFFFF`               Thẻ, biểu mẫu, vùng nội
                                                  dung chính

  Xanh navy phụ           `#082B59`               Header, sidebar hoặc
                                                  vùng điều hướng cần độ
                                                  tương phản cao
  -----------------------------------------------------------------------

**Nguyên tắc sử dụng màu** - Xanh teal là màu nhận diện và dành cho hành
động chính. - Cam dùng có chọn lọc, không dùng cho mọi nút. - Xanh ngọc
dành cho trạng thái thành công; không dùng thay màu hành động chính. - Dùng
nền sáng và thẻ trắng để tăng khả năng đọc. - Trạng thái lỗi cần màu đỏ
dễ nhận biết; trạng thái chờ dùng màu trung tính hoặc vàng nhạt. - Kiểm
tra độ tương phản của chữ và nút trước khi đưa vào giao diện thực tế.

### Font chữ đề xuất

-   **Segoe UI** hoặc **Roboto**.
-   Dùng hệ thống phân cấp rõ ràng: tiêu đề trang, tiêu đề khối, nhãn
    trường nhập liệu, nội dung, chú thích.

## 3. Kiến trúc điều hướng

Nên chia website thành các khu vực riêng theo vai trò, dùng chung nhận
diện thương hiệu nhưng có bố cục phù hợp từng công việc.

### Hành khách

Header công khai: logo, Đặt vé, Tra cứu đặt chỗ, Ưu đãi, Hỗ trợ, Đăng
nhập/Đăng ký.

Khi đăng nhập, bổ sung khu vực tài khoản: Hồ sơ, Lịch sử đặt vé, Yêu cầu
đổi/hủy, Check-in, Điểm thưởng, Cài đặt.

### Nhân viên quầy

Bố cục dashboard với sidebar: Tổng quan, Bán vé, Tra cứu đặt chỗ,
Đổi/hủy vé, Thanh toán, Báo cáo nghiệp vụ (nếu được phân quyền).

### Quản trị viên

Bố cục dashboard với sidebar: Tổng quan, Sân bay, Tuyến bay, Máy bay, Sơ
đồ ghế, Chuyến bay, Bảng giá, Thuế & phí, Khuyến mãi, Báo cáo.

## 4. Danh sách trang UI Mockup

### 4.1. Trang chủ hành khách

**Mục tiêu:** giúp người dùng bắt đầu tìm chuyến bay nhanh chóng.

Thành phần: - Header và logo TEO Airlines. - Hero banner có hình ảnh
hàng không và thông điệp thương hiệu. - Khối tìm chuyến bay. - Chọn một
chiều/khứ hồi. - Điểm đi, điểm đến, ngày đi, ngày về. - Hạng ghế. - Số
lượng người lớn, trẻ em, em bé. - Nút "Tìm chuyến bay". - Các khối tiện
ích: hỗ trợ, đặt vé thuận tiện, ưu đãi và tích điểm. - Footer có thông
tin hỗ trợ và liên kết cần thiết.

### 4.2. Đăng nhập

Thành phần: - Email hoặc số điện thoại. - Mật khẩu và nút hiện/ẩn mật
khẩu. - Ghi nhớ đăng nhập (nếu phù hợp). - Quên mật khẩu. - Đăng nhập. -
Liên kết đăng ký. - Phân biệt vai trò khi đăng nhập nếu hệ thống yêu
cầu; không nên để người dùng tự chọn vai trò nếu quyền đã được xác định
từ tài khoản.

### 4.3. Đăng ký

Thành phần: - Họ và tên. - Email. - Số điện thoại. - Mật khẩu. - Xác
nhận mật khẩu. - Đồng ý điều khoản và chính sách riêng tư. - Nút tạo tài
khoản. - Liên kết đăng nhập.

### 4.4. Đổi/đặt lại mật khẩu

Thành phần: - Email hoặc số điện thoại. - Gửi mã xác thực hoặc liên kết
đặt lại. - Trường nhập mã xác thực khi cần. - Mật khẩu mới và xác nhận
mật khẩu mới. - Thông báo thành công/lỗi rõ ràng.

### 4.5. Kết quả tìm chuyến bay

Thành phần: - Tóm tắt hành trình, ngày bay, hạng ghế và số hành khách. -
Bộ lọc theo hãng, giờ bay, giá, thời lượng và điểm dừng (nếu dữ liệu hỗ
trợ). - Danh sách chuyến bay: mã chuyến, giờ đi/đến, thời lượng, hạng
ghế, giá và tình trạng chỗ. - Giá hiển thị cần làm rõ đã bao gồm
thuế/phí hay chưa. - Nút chọn chuyến. - Với khứ hồi, có bước chọn chuyến
đi và chuyến về rõ ràng.

### 4.6. Thông tin hành khách

Thành phần: - Thông tin liên hệ người đặt vé. - Họ tên và thông tin cần
thiết của từng hành khách. - Phân loại người lớn, trẻ em, em bé. - Lưu ý
em bé không chiếm ghế. - Kiểm tra trường bắt buộc và định dạng dữ
liệu. - Tóm tắt hành trình và chi phí tạm tính.

### 4.7. Chọn ghế

Thành phần: - Sơ đồ ghế theo cấu hình máy bay. - Chú giải ghế trống, đã
chọn, đã đặt, không sử dụng và ghế có phụ phí. - Hiển thị giá phụ thu
của ghế nếu có. - Gán ghế phù hợp với hành khách có ghế; em bé không
chiếm ghế. - Tổng hợp ghế đã chọn và chi phí bổ sung.

### 4.8. Xác nhận và thanh toán

Thành phần: - Tóm tắt hành trình, hành khách, ghế và giá. - Chi tiết giá
vé, thuế, phí, phụ phí và giảm giá. - Nhập mã khuyến mãi. - Chọn phương
thức thanh toán được hệ thống hỗ trợ. - Hiển thị rõ thời gian giữ chỗ
còn lại. - Trạng thái đang chờ thanh toán, thành công, thất bại hoặc hết
hạn. - Không hiển thị "đã thanh toán" cho đến khi hệ thống xác nhận.

Quy tắc nghiệp vụ cần thể hiện: - Giữ chỗ 10 phút với MoMo. - Giữ chỗ 1
giờ với thanh toán tiền mặt hoặc chuyển khoản tại quầy. - Hết thời hạn
mà chưa thanh toán thì đơn đặt chỗ tự hủy.

### 4.9. Đặt vé thành công

Thành phần: - Biểu tượng và thông báo xác nhận. - Mã đặt chỗ. - Tóm tắt
chuyến bay và hành khách. - Trạng thái thanh toán. - Thông báo thông tin
được gửi qua email/SMS. - Nút xem chi tiết đặt chỗ và quay về trang chủ.

### 4.10. Tra cứu đặt chỗ cho khách chưa đăng nhập

Đề xuất hỗ trợ tra cứu bằng mã đặt chỗ cùng email hoặc số điện thoại đã
dùng khi đặt vé. - Hiển thị hành trình, hành khách, trạng thái thanh
toán và trạng thái vé. - Chỉ hiển thị dữ liệu sau khi xác minh thông tin
phù hợp. - Hướng dẫn liên hệ nhân viên nếu yêu cầu đổi/hủy phải được xử
lý tại quầy.

### 4.11. Khu vực tài khoản hành khách

Các trang hoặc tab: - Hồ sơ cá nhân. - Lịch sử đặt vé. - Chi tiết đặt
vé. - Yêu cầu đổi vé. - Yêu cầu hủy vé. - Check-in trực tuyến. - Điểm
thưởng và lịch sử điểm. - Đổi điểm lấy mã giảm giá. - Cài đặt tài khoản
và đổi mật khẩu.

Bảng lịch sử nên có mã đặt chỗ, tuyến bay, ngày bay, trạng thái và nút
xem chi tiết.

### 4.12. Check-in trực tuyến

Thành phần đề xuất: - Tra cứu và xác minh đặt chỗ. - Danh sách hành
khách đủ điều kiện check-in. - Xác nhận thông tin. - Chọn ghế nếu chưa
chọn và nếu quy định cho phép. - Màn hình hoàn tất check-in và thẻ lên
máy bay minh họa.

Điều kiện check-in thực tế cần được xác định theo quy định vận hành của
hệ thống.

### 4.13. Giao diện nhân viên quầy

Bố cục sidebar + vùng nội dung chính.

Giao diện hiện tại có các mục **Đặt vé**, **Đổi / hủy vé**, **Tài khoản khách** và **Hồ sơ nhân viên**. Nhân viên không tự đăng ký; hồ sơ và đổi mật khẩu áp dụng cho nhân viên đang đăng nhập, còn mục tài khoản khách dùng để tra cứu và khóa tài khoản hành khách. Bản HTML hiện tại là giao diện minh họa, chưa kết nối xác thực, đặt chỗ, thanh toán hoặc dữ liệu khách hàng.

Chi tiết đổi/hủy cần thể hiện trạng thái yêu cầu, điều kiện áp dụng, phụ phí đổi, tiền hoàn dự kiến và lịch sử nhân viên xử lý, thời gian, lý do. Mọi khoản ước tính trên giao diện minh họa phải được hệ thống tính và xác nhận lại trước khi thực hiện.

Màn hình bán vé: - Tìm chuyến theo điểm đi, điểm đến, ngày bay và hạng
ghế. - Chọn chuyến và ghế. - Nhập thông tin hành khách. - Tính giá và áp
dụng khuyến mãi nếu được phép. - Ghi nhận tiền mặt hoặc xác nhận chuyển
khoản. - Phát hành vé sau khi hoàn tất các điều kiện cần thiết.

Màn hình tra cứu đặt chỗ: - Tìm bằng mã đặt chỗ, tên hoặc thông tin liên
hệ theo quyền được cấp. - Xem hành trình, thanh toán, hành khách và lịch
sử xử lý.

Màn hình xử lý đổi/hủy: - Danh sách yêu cầu mới, đang xử lý, đã duyệt,
từ chối hoặc hoàn tất. - Chi tiết yêu cầu và điều kiện áp dụng. - Phụ
phí đổi vé, số tiền hoàn dự kiến và kết quả xử lý. - Ghi nhận người xử
lý, thời gian và lý do.

Quy tắc nghiệp vụ cần thể hiện: - Đổi vé trước giờ khởi hành ít nhất 5
ngày và chịu phụ phí. - Hủy vé trước giờ khởi hành ít nhất 12 tiếng. -
Hủy trước hơn 3 ngày: hoàn toàn bộ tiền theo mô tả bài toán. - Các
trường hợp còn lại: hệ thống tính khoản hoàn một phần. - Nhân viên quầy
xử lý yêu cầu đổi/hủy cho cả khách có tài khoản và khách đặt vé không
tài khoản.

### 4.14. Giao diện quản trị viên

Bố cục dashboard với sidebar bên trái và vùng nội dung chính. Prototype
`admin.html` có bảy màn hình:

- **Tổng quan:** doanh thu, số vé đặt, tỉ lệ lấp đầy, vé bán/hủy/đổi, lượt truy
  cập website, xu hướng theo ngày/tuần/tháng/quý/năm/toàn bộ lịch sử và mã
  khuyến mãi nổi bật.
  Có thể xuất báo cáo ở định dạng CSV UTF-8 tương thích Excel.
- **Sân bay, tuyến bay và chuyến bay:** danh sách có tìm kiếm/lọc, phân trang
  minh họa, thêm/sửa và xác nhận trước thao tác xóa.
- **Máy bay và sơ đồ ghế:** xem đội bay và bản đồ ghế theo hạng; không có thao
  tác sửa/xóa máy bay trong prototype.
- **Bảng giá:** xem giá cơ bản, thuế và phí theo chuyến, hạng ghế, loại khách;
  hỗ trợ thêm và sửa.
- **Khuyến mãi:** xem mã, thời hạn, điều kiện và lượt dùng; chỉ hỗ trợ thêm/sửa,
  không có thao tác xóa; có xuất CSV UTF-8 tương thích Excel.

Các form, bộ lọc và thông báo là tương tác minh họa phía trình duyệt; dữ liệu
không được lưu vào cơ sở dữ liệu. Số liệu dashboard, bảng và sơ đồ ghế đều là
dữ liệu giả, cần thay bằng dữ liệu và quy tắc nghiệp vụ đã xác nhận trước khi
kết nối hệ thống thật. Việc xóa tuyến/sân bay/chuyến bay có thể bị khóa khi
có bản ghi liên quan; không xóa lịch sử đặt vé.

## 5. Nguyên tắc UX và trạng thái giao diện

Cần thiết kế các trạng thái: - Mặc định và đang tải. - Không có kết
quả. - Lỗi nhập liệu. - Lỗi hệ thống hoặc thanh toán thất bại. - Thành
công. - Chờ xác nhận. - Hết thời gian giữ chỗ. - Hết ghế hoặc ghế vừa
được người khác chọn. - Mã khuyến mãi không hợp lệ/hết hạn. - Yêu cầu
đổi/hủy không đủ điều kiện.

Nguyên tắc: - Dùng nhãn trường rõ ràng, không chỉ dựa vào placeholder. -
Báo lỗi gần trường nhập liệu gây lỗi. - Có màn hình xác nhận trước khi
đổi/hủy hoặc thực hiện thao tác quan trọng. - Hiển thị giá cuối cùng và
các thành phần giá trước khi thanh toán. - Tối ưu thao tác nhanh cho
nhân viên quầy. - Responsive cho desktop, tablet và mobile; dashboard ưu
tiên desktop nhưng vẫn cần dùng được trên màn hình nhỏ.

## 6. Định hướng triển khai ASP.NET MVC + SQL Server

Đây là định hướng cấu trúc giao diện, không phải thiết kế cơ sở dữ liệu
hoàn chỉnh.

### Layout dùng chung

-   `Views/Shared/_Layout.cshtml`: layout khu vực hành khách.
-   `Views/Shared/_AuthLayout.cshtml`: layout đăng nhập/đăng ký/đặt lại
    mật khẩu.
-   `Views/Shared/_StaffLayout.cshtml`: layout nhân viên quầy.
-   `Views/Shared/_AdminLayout.cshtml`: layout quản trị viên.

### Nhóm View gợi ý

-   `Views/Home/`: trang chủ và tìm kiếm.
-   `Views/Flights/`: kết quả chuyến bay và chi tiết chuyến.
-   `Views/Booking/`: hành khách, chọn ghế, thanh toán, xác nhận.
-   `Views/Account/`: đăng nhập, đăng ký, hồ sơ, đổi mật khẩu.
-   `Views/MyBookings/`: lịch sử, chi tiết, yêu cầu đổi/hủy, check-in.
-   `Views/Staff/`: bán vé và xử lý nghiệp vụ tại quầy.
-   `Views/Admin/`: quản lý danh mục, chuyến bay, giá và báo cáo.

Tên thư mục chỉ là gợi ý; có thể thay đổi theo cấu trúc dự án thực tế.

### Nguyên tắc kỹ thuật giao diện

-   Dùng ViewModel cho dữ liệu hiển thị và dữ liệu form; tránh truyền
    trực tiếp entity cơ sở dữ liệu vào mọi View.
-   Kiểm tra dữ liệu ở phía máy chủ, kể cả khi đã có kiểm tra JavaScript
    phía trình duyệt.
-   Phân quyền ở phía máy chủ; ẩn nút trên giao diện không thay thế được
    kiểm tra quyền.
-   Không đặt thông tin bí mật hoặc chuỗi kết nối trong mã JavaScript
    hay giao diện.
-   Dùng Anti-forgery token cho form thay đổi dữ liệu.
-   Quy trình giữ chỗ, thanh toán, tính giá và đổi/hủy phải được kiểm
    soát ở phía máy chủ.
-   UI mockup không đồng nghĩa với các chức năng đã hoạt động hoặc đã
    tích hợp thanh toán/email/SMS.

## 7. Các quyết định cần xác nhận trước khi hoàn thiện mockup chi tiết

Các nội dung dưới đây là lựa chọn đề xuất, chưa được xác nhận: 1.
**Phong cách:** hiện đại tối giản, cao cấp hay trẻ trung. 2. **Logo:**
giữ ý tưởng máy bay cách điệu hiện tại hay tạo biểu tượng từ chữ TEO. 3.
**Ngôn ngữ:** chỉ tiếng Việt hay có thể chuyển sang tiếng Anh. 4.
**Thiết bị:** desktop ưu tiên hay responsive đầy đủ. 5. **Quy trình đặt
vé:** theo từng bước hay một trang dài. 6. **Thanh toán:** ngoài MoMo,
tiền mặt và chuyển khoản, có cần mô phỏng thẻ/cổng thanh toán trực tuyến
không. 7. **Chọn ghế:** sơ đồ chi tiết có giá từng ghế hay sơ đồ cơ bản.
8. **Khách không tài khoản:** có được tự tra cứu đặt chỗ hay cần liên hệ
nhân viên để xử lý. 9. **Check-in trực tuyến:** làm trong giai đoạn đầu
hay để sau. 10. **Nhân viên quầy:** giao diện theo quy trình hay
dashboard. 11. **Admin:** có cần quản lý tài khoản/phân quyền người dùng
trong phạm vi đề tài không. 12. **Độ chi tiết:** mockup tổng quan hay
đầy đủ trạng thái lỗi/thành công/hết hạn.

## 8. Thứ tự thiết kế đề xuất

1.  Chốt nhận diện TEO Airlines và component cơ bản.
2.  Thiết kế trang chủ hành khách.
3.  Thiết kế luồng tìm chuyến → nhập hành khách → chọn ghế → thanh toán
    → xác nhận.
4.  Thiết kế đăng nhập, đăng ký, đặt lại mật khẩu.
5.  Thiết kế lịch sử đặt vé, chi tiết vé, đổi/hủy và check-in.
6.  Thiết kế giao diện nhân viên quầy.
7.  Thiết kế dashboard và các màn hình quản lý của admin.
8.  Kiểm tra tính nhất quán, trạng thái lỗi và khả năng chuyển sang
    ASP.NET MVC.

------------------------------------------------------------------------

**Trạng thái tài liệu:** Bản định hướng ban đầu, đã cập nhật màu nhận diện
xanh teal theo giao diện website; các lựa chọn còn lại cần được xác nhận
trước khi hoàn thiện UI Mockup chi tiết.
