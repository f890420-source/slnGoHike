# 揪團功能後端API與後台網頁
在GoHikeFront中已經簡略說明後端API的部分，這裡除了針對未做完的後台網頁做說明，也會補充說明後端邏輯部分。

## 功能
已完成：
- 山岳管理
- 活動管理

未完成：
- 活動管理的翻頁與篩選
- 檢舉管理


![管理活動頁面展示](docs/groupjoin_img/projectImages_back1.jpg)

![山岳管理頁面展示](docs/groupjoin_img/projectImages_back2.jpg)

![檢舉管理頁面展示](docs/groupjoin_img/projectImages_back3.jpg)



## 使用技術
- ASP.NET Core MVC、Razor、Entity Framework Core、SQL Server
- ViewModel、Wrapper
- Ajax（回傳 JSON，局部更新畫面）、htmlhelper

## 後台網頁
我認為管理者也屬於使用者的一種，所以後台網頁使用體驗上使用ajax技術使部分畫面更新，使用者不需要經歷重新整理畫面。

### 後端邏輯
- 前端送出帳密由後端驗證，通過後回傳Token，之後每當點擊功能，請求內就會帶Token，後端取得Token，所以前端就不用再傳UserId。
- 時間邏輯的部分 活動的開始時間必須大於現在時間，結束時間必須大於開始時間，否則會回傳錯誤訊息。
- 接收與回傳的 DTO 分開，避免多餘欄位與 Over-posting
- 圖片上傳驗證副檔名與大小，用 Guid 重新命名
- 資料異動時透過 SignalR 通知前端
- Serializable防止同時報名的狀況
- 退出活動後有冷卻時間，防止一個使用者短時間多次報名

## 若要查看專案請搭配前端專案一起開啟